using System;
using Sandbox.Components;
using Sandbox.Data;
using Sandbox.Enums;
using Sandbox.Managers;
namespace Sandbox.Player;

public sealed class Player : Component, Component.INetworkListener
{
	[Property] private float Speed { get; set; } = 600f;
	[Property] private float Sensitivity { get; set; } = 0.2f;
	[Property] private float CameraDistance { get; set; } = 200f;
	[Property] private float CameraHeight { get; set; } = 50f;
	[Property] private Sword Sword { get; set; }

	private int GrabTimerHandle;
	private Guid PreviousHostId;
	private readonly float SpringStrength = 150f; 
	private readonly float DampingStrength = 15f;
	private readonly float GrabDistance = 800f;
	private readonly float ThrowForce = 2000f;
	private readonly float MovementFriction = 5f;
	private float MoneyMultiplier = 1f;
	private Angles CurrentCameraRotation;
	private FHeldObjectData HeldObjectData;
	private UiManager UiManager;
	private CameraComponent Camera;
	private CharacterController Controller;
	private PullableComponent PullComp;
	private PlayerData PlayerData;
	private Rigidbody PlayerRigidbody;
	
	// --------------------------------------------------------------STRUCTS--------------------------------------------------------------
	private readonly struct FHeldObjectData
	{
		private readonly GameObject Object;
		private readonly Rigidbody RigidBody;
		private readonly PullableComponent PullableComp;
		private readonly float HoldDistance;

		public FHeldObjectData(GameObject InObject, Guid InOwnerId, float InHoldDistance)
		{
			Object = InObject;
			HoldDistance = InHoldDistance;
			PullableComp = InObject.GetComponent<PullableComponent>();
			PullableComp.SetOwnerId(InOwnerId, true);
			if (Object.Components.TryGet(out Rigidbody OutRigidBody)) { RigidBody = OutRigidBody; }
		}
		public bool GetIsObjectEnabled() { return Object.Enabled; }
		public float GetHoldDistance() { return HoldDistance; }
		public GameObject GetObject() { return Object; }
		public Rigidbody GetRigidBody() { return RigidBody; }

		public void SetupClearOwnerIdTimer() { PullableComp?.SetClearOwnerIdTimer(); }
	}
	// --------------------------------------------------------------PUBLIC METHODS--------------------------------------------------------------
	public void OnConnected(Connection InChannel)
	{
		//EventManager.TriggerPlayerConnectionChange(new LeaderboardEntry(InChannel.Id, InChannel.Name, 0), true);
		Log.Info("CONNECTED: " + InChannel.Id);
	}
	public void OnDisconnected(Connection InChannel)
	{
		if (Networking.IsHost) PreviousHostId = InChannel.Id;
		//	EventManager.TriggerPlayerConnectionChange(new LeaderboardEntry(channel.Id, channel.Name, 0), false);
		Log.Info("DISCONNECTED: " + InChannel.Id);
	}
	// --------------------------------------------------------------PROTECTED METHODS--------------------------------------------------------------
	protected override void OnStart()
	{
		base.OnStart();
		EventManager.OnKill += OnKillEventListener;
		PullComp = GetComponent<PullableComponent>();
		if (IsProxy) return;
		EventManager.OnPlayerDataRequired += GetPlayerData;
		PlayerRigidbody = GetComponent<Rigidbody>();
		MoneyMultiplier = Random.Shared.Float(2f, 60f);
		LoadSaveData();
		Camera = Scene.GetComponentInChildren<CameraComponent>();
		SetupManagers();
		Controller = GetComponent<CharacterController>();
		Controller.BoundingBox.AddBBox(new BBox(new Vector3(-5f, -5f, -50f / 2f), new Vector3(5f, 5f, 50f / 2f)));
	}
	protected override void OnUpdate()
	{
		if (IsProxy || Camera == null) return;
		ProcessInput();
	}
	protected override void OnDestroy()
	{
		EventManager.OnKill -= OnKillEventListener;
		if (!IsProxy)
		{
			ClearHeldObjectData();
			EventManager.OnPlayerDataRequired -= GetPlayerData;
		}
		base.OnDestroy();
	}
	// --------------------------------------------------------------PRIVATE METHODS--------------------------------------------------------------
	private PlayerData GetPlayerData() { return PlayerData; }
	
	private void ProcessInput()
	{
		ProcessMouseInput();
		ProcessKeyboardInput();
	}
	private void ProcessCameraLook()
	{
		Vector2 MouseInput = Input.MouseDelta;
		CurrentCameraRotation.yaw -= MouseInput.x * Sensitivity;
		CurrentCameraRotation.pitch += MouseInput.y * Sensitivity;
		CurrentCameraRotation.pitch = CurrentCameraRotation.pitch.Clamp(-40f, 60f);
		Rotation NextCameraRotation = Rotation.From(CurrentCameraRotation);
		Vector3 PlayerPosition = WorldPosition + Vector3.Up * CameraHeight;
		Vector3 CameraOffset = NextCameraRotation.Backward * CameraDistance;
		Camera.WorldPosition = PlayerPosition + CameraOffset;
		Camera.WorldRotation = NextCameraRotation;
		WorldRotation = Rotation.FromYaw(CurrentCameraRotation.yaw);
	}
	private void ProcessMovementInput()
	{
		Vector3 Forward = WorldRotation.Forward;
		Vector3 Right = WorldRotation.Right;

		Forward = new Vector3(Forward.x, Forward.y, 0).Normal;
		Right = new Vector3(Right.x, Right.y, 0).Normal;

		Vector3 MovementVelocity = Vector3.Zero;

		if (Input.Down("Forward")) MovementVelocity += Forward;
		if (Input.Down("Backward")) MovementVelocity -= Forward;
		if (Input.Down("Left")) MovementVelocity -= Right;
		if (Input.Down("Right")) MovementVelocity += Right;

		Controller.ApplyFriction(MovementFriction);
		Controller.Accelerate(MovementVelocity.Normal * (MovementVelocity.Length > 0 ? Speed : 0));
		Controller.Move();
	}
	private void ProcessKeyboardInput()
	{
		ProcessMovementInput();
		ProcessCameraLook();
		if (Input.Pressed("Score"))
		{
			UiManager.TriggerUiStateChange(EUi.UpgradesMenu);
		}
	}
	private void ProcessMouseInput()
	{
		ProcessMouseRightClickInput();
		ProcessMouseLeftClickInput();
	}
	private void ProcessMouseRightClickInput()
	{
		if (Input.Pressed("Attack2"))
		{
			Ray Ray = Camera.ScreenPixelToRay(new Vector2(Screen.Width / 2, Screen.Height / 2));
			Vector3 Start = Ray.Position;
			Vector3 End = Start + Ray.Forward * GrabDistance;
			SceneTraceResult Trace = Scene.Trace.WithTag("Collectible").Ray(Start, End).Run();
			DebugOverlay.Line(Start, End, Color.Red, 2f);
			if (!Trace.Hit) return;
			GameObject GrabbedObject = Trace.GameObject;
			GrabbedObject.Network.TakeOwnership();
			HeldObjectData = new FHeldObjectData(GrabbedObject, Network.OwnerId, Trace.Distance);
			SetupGrabbedObjectTimer();
		}
		else if (Input.Released("Attack2")) ClearHeldObjectData();
	}
	private void ProcessMouseLeftClickInput()
	{
		if (!Input.Pressed("Attack1")) return;
		if (HeldObjectData.GetObject() == null) Sword.Attack();
		else
		{
			Rigidbody Rigidbody = HeldObjectData.GetRigidBody();
			if (Rigidbody == null) return;
			Vector3 ThrowDir = Camera.WorldRotation.Forward;
			Rigidbody.Velocity = ThrowDir * ThrowForce;
			ClearHeldObjectData();
		}
	}
	private void SetupGrabbedObjectTimer()
	{
		GrabTimerHandle = TimerManager.AddTimer(GrabTimerHandle, 1f / 60f, true, ProcessGrabbedObjectLogic);
	}
	private void ProcessGrabbedObjectLogic()
	{
		GameObject HeldObject = HeldObjectData.GetObject();
		if (HeldObject == null || !HeldObject.Network.IsOwner || !HeldObjectData.GetIsObjectEnabled())
		{
			ClearHeldObjectData(true);
			return;
		}
		Rigidbody HeldObjectRigidbody = HeldObjectData.GetRigidBody();
		if (!HeldObjectRigidbody.IsValid()) return;
		Vector3 TargetPosition = Camera.WorldPosition + Camera.WorldRotation.Forward * HeldObjectData.GetHoldDistance();
		Vector3 Displacement = TargetPosition - HeldObject.WorldPosition;
		Vector3 Force = Displacement * SpringStrength - HeldObjectRigidbody.Velocity * DampingStrength;
		HeldObjectRigidbody.ApplyForce(Force * HeldObjectRigidbody.PhysicsBody.Mass);
		HeldObjectRigidbody.AngularVelocity = Vector3.Zero;
	}
	private void SetupManagers()
	{
		UiManager = Scene.GetSystem<UiManager>();
	}
	private void ClearHeldObjectData(bool InWasTakenFromOtherPlayer = false)
	{
		GameObject HeldObject = HeldObjectData.GetObject();
		ReturnAuthorityToHost(HeldObject);
		if (!InWasTakenFromOtherPlayer) HeldObjectData.SetupClearOwnerIdTimer();
		HeldObjectData = default;
		TimerManager.ClearTimer(GrabTimerHandle);
	}
	[Rpc.Host]
	private void ReturnAuthorityToHost(GameObject InHeldObject) { InHeldObject?.Network.TakeOwnership(); }
	public void ReturnToSpawnLocation()
	{
		TeleportPlayerToSpawnLocation(EventManager.TriggerGetSpawnLocation());
		if (PullComp == null || !PullComp.GetIsPulled()) return;
		Guid LastOwnedId = PullComp.GetOwnerId();
		if (LastOwnedId == Guid.Empty || Network.OwnerId == LastOwnedId) return;
		RequestMoneyTransfer(PullComp.Network.OwnerId);
	}
	private void AddMoney(float InMoneyAmount)
	{
		PlayerData.Money += InMoneyAmount;
		PlayerData.Save(PlayerData);
		EventManager.TriggerMoneyChange(PlayerData.Money);
	}
	private void SubtractMoney(float InMoneyAmount)
	{
		PlayerData.Money -= InMoneyAmount;
		PlayerData.Save(PlayerData);
		EventManager.TriggerMoneyChange(PlayerData.Money);
	}
	private void LoadSaveData()
	{
		PlayerData = PlayerData.Load();
		EventManager.TriggerMoneyChange(PlayerData.Money);
	}
	private void OnKillEventListener(Guid InKillerId, bool InIsPlayerKill, float InMoneyAmount)
	{
		if (!Networking.IsHost) return;
		IncreaseMoney(InKillerId, InIsPlayerKill, InMoneyAmount);
	}
	// --------------------------------------------------------------NETWORKING METHODS--------------------------------------------------------------
	[Rpc.Broadcast]
	public void ReceivePush(Guid InPusherId, Vector3 InPushForce)
	{
		if (IsProxy) return;
		PullComp.SetOwnerId(InPusherId, false);
		PlayerRigidbody?.ApplyImpulse(InPushForce);
	}
	
	[Rpc.Broadcast]
	private void IncreaseMoney(Guid InKillerOwnerId, bool InIsPlayerKill, float InMoneyAmount)
	{
		if (Network == null || IsProxy || Network.OwnerId != InKillerOwnerId) return;
		AddMoney(InMoneyAmount * (InIsPlayerKill ? 1f : MoneyMultiplier));
	}
	[Rpc.Broadcast]
	private void RequestMoneyTransfer(Guid TargetId)
	{
		if (IsProxy || TargetId != Network.OwnerId) return;
		float MoneyLoss = PlayerData.Money * 0.25f;
		SubtractMoney(MoneyLoss);
		SendBackMoney(MoneyLoss);
	}
	[Rpc.Broadcast]
	private void TeleportPlayerToSpawnLocation(Vector3 InPosition)
	{
		if (IsProxy) return;
		WorldPosition = InPosition;
	}
	
	[Rpc.Host]
	private void SendBackMoney(float InMoney)
	{
		EventManager.TriggerOnKill(PullComp.GetOwnerId(), true, InMoney);
	}
}
