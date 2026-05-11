using System;
using Sandbox.Components;
using Sandbox.Data;
using Sandbox.Enums;
using Sandbox.Managers;
namespace Sandbox.Player;

public sealed class Player : Component, Component.INetworkListener
{
	[Sync, Change(nameof(OnCurrentMaterialChanged))] private ECubeMaterial CurrentMaterial { get; set; } = ECubeMaterial.None;
	[Property] private float DefaultMovementSpeed { get; set; } = 2500f;
	[Property] private float DefaultGrabDistance { get; set; } = 600f;
	[Property] private float DefaultMoneyLossToAliensPercentage { get; set; } = 0.25f;
	[Property] private float DefaultBoxThrowImpulseStrength { get; set; } = 2000f;
	[Property] private float Sensitivity { get; set; } = 0.2f;
	[Property] private float CameraDistance { get; set; } = 200f;
	[Property] private float CameraHeight { get; set; } = 50f;
	[Property] private Sword Sword { get; set; }
	[Property] private BoxCollider PlayerCollider { get; set; }
	[Property] private ModelRenderer Mesh { get; set; }
	[Property] private Dictionary<ECubeMaterial, Material> Materials { get; set; }
	[Property, Group("Dash")] private float DefaultDashImpulseStrength { get; set; } = 2000f;
	[Property, Group("Dash")] private float DefaultDashCooldown { get; set; } = 8f;

	private bool IsStuck
	{
		get;
		set
		{
			if (field == value) return;
			field = value;
			EventManager.TriggerStuckPopupStateChange(field);
		}
	}
	private int GrabTimerHandle;
	private int HostClientCollisionUpdateHandle;
	private int PhysicsFrameWaitTimerHandle;
	private int CheckIfFallingOutOfWorldHandle;
	private int AdditionalMoneyAmount = 1;
	private readonly float SpringStrength = 150f; 
	private readonly float DampingStrength = 15f;
	private float StuckTimer;
	private float GrabDistance;
	private float DashImpulseStrength;
	private float DashCooldown;
	private float MoneyLossToAliensPercentage;
	private float MovementSpeed;
	private float BoxThrowImpulseStrength;
	private TimeSince TimeSinceLastDash;
	private Vector3 LastFramePosition;
	private Angles CurrentCameraRotation;
	private FHeldObjectData HeldObjectData;
	private CameraComponent Camera;
	private PullableComponent PullComp;
	private PlayerData Data;
	private Rigidbody PlayerRigidbody;
	private Rotation LastRotation;
	
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
	public float GetCurrentCameraRotationYaw() { return CurrentCameraRotation.yaw; }
	
	public void OnConnected(Connection InChannel)
	{
		//EventManager.TriggerPlayerConnectionChange(new LeaderboardEntry(InChannel.Id, InChannel.Name, 0), true);
		Log.Info("CONNECTED: " + InChannel.Id);
	}
	public void OnDisconnected(Connection InChannel)
	{
		//	EventManager.TriggerPlayerConnectionChange(new LeaderboardEntry(channel.Id, channel.Name, 0), false);
		Log.Info("DISCONNECTED: " + InChannel.Id);
	}
	public void GiveImpulseForce(Vector3 InImpulseForce)
	{
		InImpulseForce.z = 0f;
		PlayerRigidbody?.ApplyImpulse(InImpulseForce);
	}
	// --------------------------------------------------------------PROTECTED METHODS--------------------------------------------------------------
	protected override void OnStart()
	{
		EventManager.OnKill += OnKillEventListener;
		PullComp = GetComponent<PullableComponent>();
		HostClientCollisionUpdateHandle = TimerManager.AddTimer(HostClientCollisionUpdateHandle, 1f / 60f, true, UpdateClientsForHost);
		if (IsProxy) return;
		EventManager.OnPlayerDataRequired += GetPlayerData;
		EventManager.OnUpgradePurchased += UpgradePurchased;
		CheckIfFallingOutOfWorldHandle = TimerManager.AddTimer(CheckIfFallingOutOfWorldHandle, 10f, true, CheckIfFallingOutOfWorld);
		LoadSaveData();
		ApplyUpgradesOnJoin();
		Camera = Scene.GetComponentInChildren<CameraComponent>();
		PlayerRigidbody = GetComponent<Rigidbody>();
		SetMaterial();
	}
	protected override void OnUpdate()
	{
		if (IsProxy || Camera == null) return;
		ProcessInput();
	}
	protected override void OnDestroy()
	{
		EventManager.OnKill -= OnKillEventListener;
		TimerManager.ClearTimer(HostClientCollisionUpdateHandle);
		TimerManager.ClearTimer(CheckIfFallingOutOfWorldHandle);
		if (IsProxy) return;
		ClearHeldObjectData();
		EventManager.OnPlayerDataRequired -= GetPlayerData;
		EventManager.OnUpgradePurchased -= UpgradePurchased;
	}
	// --------------------------------------------------------------PRIVATE METHODS--------------------------------------------------------------
	private ECubeMaterial GetCubeMaterialByTotalMoney()
	{
		if (Data == null) return ECubeMaterial.Cardboard;
		double TotalMoneyEarned = Data.GetTotalMoneyEarned();
		switch (TotalMoneyEarned)
		{
			case < 200f: return ECubeMaterial.Cardboard;
			case < 800f: return ECubeMaterial.Wood;
			case < 1500f: return ECubeMaterial.Rock;
			case < 3000f: return ECubeMaterial.Metal;
			case < 6900f: return ECubeMaterial.Gold;
			case < 15000f: return ECubeMaterial.Diamond;
			case < 35000f: return ECubeMaterial.Ruby;
			case < 80000f: return ECubeMaterial.Emerald;
			default: return TotalMoneyEarned < 200000f ? ECubeMaterial.Amethyst : ECubeMaterial.Obsidian;
		}
	}
	private PlayerData GetPlayerData() { return Data; }
	
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
		CurrentCameraRotation.pitch = CurrentCameraRotation.pitch.Clamp(-20f, 60f);
		Rotation NextCameraRotation = Rotation.From(CurrentCameraRotation);
		Vector3 PlayerPosition = WorldPosition + Vector3.Up * CameraHeight;
		Vector3 CameraOffset = NextCameraRotation.Backward * CameraDistance;
		Camera.WorldPosition = PlayerPosition + CameraOffset;
		Camera.WorldRotation = NextCameraRotation;
		if (PullComp != null && !PullComp.GetIsPulled())
		{
			WorldRotation = Rotation.FromYaw(CurrentCameraRotation.yaw);
		}
	}
	private void ProcessMovementInput()
	{
		if (PullComp != null && PullComp.GetIsPulled()) return;
		Vector3 MovementInput = Input.AnalogMove; 
		Vector3 WishDir = (WorldRotation * MovementInput).WithZ(0).Normal;
		bool IsMovementHappening = MovementInput.LengthSquared > 0;
		float WishSpeed = IsMovementHappening ? MovementSpeed : 0f;
		PlayerRigidbody.Velocity += WishDir * WishSpeed * Time.Delta;
		CheckIsStuck(IsMovementHappening);
	}
	private void CheckIsStuck(bool InIsMovementHappening)
	{
		if (InIsMovementHappening)
		{
			float DistanceMoved = Vector3.DistanceBetweenSquared(WorldPosition, LastFramePosition);
			if (DistanceMoved < 1f)
			{
				StuckTimer += Time.Delta;
				if (StuckTimer > 10f && !IsStuck) IsStuck = true;
			}
			else
			{
				StuckTimer = 0f;
				IsStuck = false;
			}
		}
		LastFramePosition = WorldPosition;
	}
	private void ProcessKeyboardInput()
	{
		ProcessMovementInput();
		ProcessCameraLook();
		if (Input.Pressed("Score")) UiManager.TriggerUiStateChange(EUi.UpgradesMenu);
		if (Input.Pressed("Dash") && Data?.GetUpgradeAmount(EUpgradeName.UnlockDashAbility) == 1) Dash();
	}
	private void Dash()
	{
		if (TimeSinceLastDash < DashCooldown) return;
		Vector3 DashDir = (WorldRotation * Input.AnalogMove).WithZ(0).Normal;
		if (DashDir.LengthSquared < 0.1f) DashDir = WorldRotation.Forward.WithZ(0);
		PlayerRigidbody.ApplyImpulse(DashDir * DashImpulseStrength * PlayerRigidbody.PhysicsBody.Mass);
		TimeSinceLastDash = 0;
	}
	private void ProcessMouseInput()
	{
		ProcessMouseRightClickInput();
		ProcessMouseLeftClickInput();
		ProcessUnstuckInput();
	}
	private void ProcessUnstuckInput()
	{
		if (!IsStuck || !Input.Pressed("Unstuck")) return;
		StuckTimer = 0f;
		IsStuck = false;
		TeleportToSafeZone();
	}
	private void ProcessMouseRightClickInput()
	{
		if (Input.Pressed("Attack2"))
		{
			Ray Ray = Camera.ScreenPixelToRay(new Vector2(Screen.Width / 2, Screen.Height / 2));
			Vector3 Start = Ray.Position;
			Vector3 End = Start + Ray.Forward * GrabDistance;
			SceneTraceResult Trace = Scene.Trace.WithTag("Collectible").Ray(Start, End).Run();
		//	DebugOverlay.Line(Start, End, Color.Red, 2f);
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
			Vector3 ThrowVelocity = Camera.WorldRotation.Forward * BoxThrowImpulseStrength;
			ClearHeldObjectData(false, ThrowVelocity);
		}
	}
	private void UpdateClientsForHost()
	{
		if (!Networking.IsHost || !IsProxy || PlayerCollider == null) return;
		if (WorldRotation == LastRotation) return;
		PlayerCollider.Enabled = false;
		PlayerCollider.Enabled = true;
		LastRotation = WorldRotation;
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
	private void ClearHeldObjectData(bool InWasTakenFromOtherPlayer = false, Vector3 InThrownVelocity = default)
	{
		GameObject HeldObject = HeldObjectData.GetObject();
		ReturnAuthorityToHost(HeldObject, InThrownVelocity);
		if (!InWasTakenFromOtherPlayer) HeldObjectData.SetupClearOwnerIdTimer();
		HeldObjectData = default;
		TimerManager.ClearTimer(GrabTimerHandle);
	}
	private void AddMoney(double InMoneyAmount)
	{
		Data.AddMoney(InMoneyAmount);
		SetMaterial();
	}
	private void SubtractMoney(double InMoneyAmount) { Data.SubtractMoney(InMoneyAmount); }
	private void LoadSaveData()
	{
		Data = PlayerData.Load();
		EventManager.TriggerMoneyChange(Data.GetMoney());
	}
	private void OnKillEventListener(Guid InKillerId, bool InIsPlayerKill, double InMoneyAmount)
	{
		if (!Networking.IsHost) return;
		IncreaseMoney(InKillerId, InIsPlayerKill, InMoneyAmount);
	}
	private void TeleportPlayerToSpawnLocation(Vector3 InPosition)
	{
		PhysicsFrameWaitTimerHandle = TimerManager.AddTimer(PhysicsFrameWaitTimerHandle, 0.03f, false, () =>
		{
			WorldPosition = InPosition;
			PlayerRigidbody.PhysicsBody.Position = InPosition;
		});
	}
	private void SetMaterial()
	{
		ECubeMaterial CubeMaterialToApply = GetCubeMaterialByTotalMoney();
		if (CubeMaterialToApply == CurrentMaterial) return;
		CurrentMaterial = CubeMaterialToApply;
	}
	private void OnCurrentMaterialChanged(ECubeMaterial InOldMaterialValue, ECubeMaterial InNewMaterialValue)
	{
		Mesh?.SetMaterial(Materials[InNewMaterialValue]);
	}
	private void CheckIfFallingOutOfWorld()
	{
		if (WorldPosition.z <= -1000f) TeleportToSafeZone();
	}
	private void TeleportToSafeZone()
	{
		WorldPosition = EventManager.TriggerGetSafeZoneLocation();
	}
	private void UpgradePurchased(EUpgradeName InUpgradeName)
	{
		switch (InUpgradeName)
		{
			case EUpgradeName.LessMoneyLoss1: MoneyLossToAliensPercentage = DefaultMoneyLossToAliensPercentage - Data.GetUpgradeAmount(EUpgradeName.LessMoneyLoss1) * 0.01f; break;
			case EUpgradeName.MoreMoney1: AdditionalMoneyAmount = 1 + Data.GetUpgradeAmount(EUpgradeName.MoreMoney1); break;
			case EUpgradeName.BoxGrabRange1: GrabDistance = DefaultGrabDistance * (1f + Data.GetUpgradeAmount(EUpgradeName.BoxGrabRange1) * 0.15f); break;
			case EUpgradeName.MovementSpeed1: MovementSpeed = DefaultMovementSpeed * (1f + Data.GetUpgradeAmount(EUpgradeName.MovementSpeed1) * 0.05f); break;
			case EUpgradeName.DashCooldown1: DashCooldown = DefaultDashCooldown - Data.GetUpgradeAmount(EUpgradeName.DashCooldown1) * 0.5f; break;
			case EUpgradeName.DashImpulseStrength1: DashImpulseStrength = DefaultDashImpulseStrength * (1f + Data.GetUpgradeAmount(EUpgradeName.DashImpulseStrength1) * 0.05f); break;
			case EUpgradeName.BoxThrowImpulseStrength1: BoxThrowImpulseStrength = DefaultBoxThrowImpulseStrength * (1f + Data.GetUpgradeAmount(EUpgradeName.BoxThrowImpulseStrength1) * 0.08f); break;
		}
	}
	private void ApplyUpgradesOnJoin()
	{
		GrabDistance = DefaultGrabDistance;
		DashImpulseStrength = DefaultDashImpulseStrength;
		DashCooldown = DefaultDashCooldown;
		MoneyLossToAliensPercentage = DefaultMoneyLossToAliensPercentage;
		MovementSpeed = DefaultMovementSpeed;
		BoxThrowImpulseStrength = DefaultBoxThrowImpulseStrength;
		foreach (KeyValuePair<EUpgradeName, int> Upgrade in Data.GetOwnedUpgrades()) UpgradePurchased(Upgrade.Key);
	}
	// --------------------------------------------------------------NETWORKING METHODS--------------------------------------------------------------
	[Rpc.Broadcast]
	public void ReceivePush(Vector3 InPushForce)
	{
		if (IsProxy) return;
		PlayerRigidbody?.ApplyImpulse(InPushForce);
	}
	[Rpc.Broadcast]
	public void ReturnToSpawnLocation()
	{
		if (IsProxy) return;
		TeleportPlayerToSpawnLocation(EventManager.TriggerGetSpawnLocation());
		if (PullComp == null || !PullComp.GetIsPulled()) return;
		PullComp.SetIsPulled(false);
		Guid LastOwnedId = PullComp.GetOwnerId();
		if (LastOwnedId == Guid.Empty || Network.OwnerId == LastOwnedId)
		{
			double MoneyLoss = Data.GetMoney() * MoneyLossToAliensPercentage;
			SubtractMoney(MoneyLoss);
			return;
		}
		RequestMoneyTransfer(PullComp.Network.OwnerId);
	}
	
	[Rpc.Host]
	private void SendBackMoney(double InMoney)
	{
		EventManager.TriggerOnKill(PullComp.GetOwnerId(), true, InMoney);
	}
	[Rpc.Host]
	private void ReturnAuthorityToHost(GameObject InHeldObject, Vector3 InThrownVelocity)
	{
		if (InHeldObject == null) return;
		InHeldObject.Network.TakeOwnership();
		if (InThrownVelocity != Vector3.Zero && InHeldObject.Components.TryGet(out Rigidbody Rigidbody))
		{
			Rigidbody.Velocity = InThrownVelocity;
		}
	}
	
	[Rpc.Broadcast]
	private void IncreaseMoney(Guid InKillerOwnerId, bool InIsPlayerKill, double InMoneyAmount)
	{
		if (Network == null || IsProxy || Network.OwnerId != InKillerOwnerId) return;
		if (InIsPlayerKill) AddMoney(InMoneyAmount);
		else AddMoney(AdditionalMoneyAmount * InMoneyAmount);
	}
	[Rpc.Broadcast]
	private void RequestMoneyTransfer(Guid TargetId)
	{
		if (IsProxy || TargetId != Network.OwnerId) return;
		double MoneyLoss = Data.GetMoney() * 0.25f;
		SubtractMoney(MoneyLoss);
		SendBackMoney(MoneyLoss);
	}
}
