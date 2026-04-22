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

	private int OutOfBoundsHandle;
	private int GrabTimerHandle;
	private int Money;
	private readonly float SpringStrength = 150f; 
	private readonly float DampingStrength = 15f;
	private readonly float GrabDistance = 800f;
	private readonly float ThrowForce = 2000f;
	private readonly float MovementFriction = 5f;
	private Angles CurrentCameraRotation;
	private FHeldObjectData HeldObjectData;
	private UiManager UiManager;
	private CameraComponent Camera;
	private CharacterController Controller;
	private PullableComponent PullComp;
	
	// --------------------------------------------------------------STRUCTS--------------------------------------------------------------
	private readonly struct FHeldObjectData
	{
		private readonly GameObject Object;
		private readonly Rigidbody RigidBody;
		private readonly PullableComponent PullableComp;
		private readonly float HoldDistance;

		public FHeldObjectData(GameObject InObject, float InHoldDistance)
		{
			Object = InObject;
			HoldDistance = InHoldDistance;
			PullableComp = InObject.GetComponent<PullableComponent>();
			PullableComp.SetOwnerId(Connection.Local.Id);
			if (Object.Components.TryGet(out Rigidbody OutRigidBody)) { RigidBody = OutRigidBody; }
		}
		public float GetHoldDistance() { return HoldDistance; }
		public GameObject GetObject() { return Object; }
		public Rigidbody GetRigidBody() { return RigidBody; }

		public void SetupClearOwnerIdTimer() { PullableComp?.SetClearOwnerIdTimer(); }
	}
	// --------------------------------------------------------------PUBLIC METHODS--------------------------------------------------------------
	public void OnConnected(Connection InChannel)
	{
		EventManager.TriggerPlayerConnectionChange(new LeaderboardEntry(InChannel.Id, InChannel.Name, 0), true);
		Log.Info("CONNECTED: " + InChannel.Id);
	}
	public void OnDisconnected(Connection InChannel)
	{
		//	EventManager.TriggerPlayerConnectionChange(new LeaderboardEntry(channel.Id, channel.Name, 0), false);
		Log.Info("DISCONNECTED: " + InChannel.Id);
	}
	// --------------------------------------------------------------PROTECTED METHODS--------------------------------------------------------------
	protected override void OnStart()
	{
		base.OnStart();
		if (IsProxy) return;
		PullComp = GetComponent<PullableComponent>();
		OutOfBoundsHandle = TimerManager.AddTimer(OutOfBoundsHandle, 0.2f, true, CheckOutOfBounds);
		Camera = Scene.GetComponentInChildren<CameraComponent>();
		SetupManagers();
		Controller = GetComponent<CharacterController>();
		EventManager.OnKill += IncreaseMoney;
		Controller.BoundingBox.AddBBox(new BBox(new Vector3( -5f, -5f, -50f / 2f ), new Vector3( 5f, 5f, 50f / 2f )));
	}
	protected override void OnUpdate()
	{
		if (IsProxy || Camera == null) return;
		ProcessInput();
	}
	protected override void OnDestroy()
	{
		ClearHeldObjectData();
		EventManager.OnKill -= IncreaseMoney;
		base.OnDestroy();
	}
	// --------------------------------------------------------------PRIVATE METHODS--------------------------------------------------------------
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
		if (Input.Pressed("OpenUpgrades"))
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
			HeldObjectData = new FHeldObjectData(GrabbedObject, Trace.Distance);
			SetupGrabbedObjectTimer();
		}
		else if (Input.Released("Attack2")) ClearHeldObjectData();
	}
	private void ProcessMouseLeftClickInput()
	{
		if (!Input.Pressed("Attack1")) return;
		if (HeldObjectData.GetObject() == null)
		{
			Sword.Attack();
		}
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
		if (HeldObject == null) return;
		if (!HeldObject.Network.IsOwner)
		{
			ClearHeldObjectData();
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
	private void ClearHeldObjectData()
	{
		GameObject HeldObject = HeldObjectData.GetObject();
		HeldObject?.Network.DropOwnership();
		HeldObjectData.SetupClearOwnerIdTimer();
		HeldObjectData = default;
		TimerManager.ClearTimer(GrabTimerHandle);
	}
	private void CheckOutOfBounds()
	{
		if (GameObject.WorldPosition.z < 500f || !PullComp.GetIsPulled()) return;
		WorldPosition = EventManager.TriggerGetSpawnLocation();
	}
	[Rpc.Broadcast]
	private void IncreaseMoney(Guid InKillerOwnerId, int InMoneyAmount)
	{
		Log.Info("IncreaseMoney TEST");
		if (Connection.Local.Id != InKillerOwnerId) return;
		Log.Info("IncreaseMoney " + Connection.Local.Id + " " + InKillerOwnerId);
		Money += InMoneyAmount;
		EventManager.TriggerMoneyChange(Money);
	}
}
