using Sandbox.Enums;
using Sandbox.Managers;
namespace Sandbox.Player;

public sealed class Player : Component, Component.INetworkListener
{
	[Property] private float Speed { get; set; } = 600f;
	[Property] private float Sensitivity { get; set; } = 0.2f;
	[Property] private float CameraDistance { get; set; } = 200f;
	[Property] private float CameraHeight { get; set; } = 50f;
	[RequireComponent] private CharacterController Controller { get; set; }

	private int GrabTimerHandle;
	private int LocationSenderTimerHandle;
	private readonly float SpringStrength = 150f; 
	private readonly float DampingStrength = 15f;
	private readonly float GrabDistance = 800f;
	private readonly float ThrowForce = 2000f;
	private readonly float MovementFriction = 5f;
	private readonly float SendPlayerLocationInterval = 0.5f;
	private Angles CurrentCameraRotation;
	private FHeldObjectData HeldObjectData;
	private EventManager EventManager;
	private UiManager UiManager;
	private CameraComponent Camera;
	
	private readonly struct FHeldObjectData
	{
		private readonly GameObject Object;
		private readonly Rigidbody RigidBody;
		private readonly float HoldDistance;

		public FHeldObjectData(GameObject InObject, float InHoldDistance)
		{
			Object = InObject;
			HoldDistance = InHoldDistance;
			if (Object.Components.TryGet(out Rigidbody OutRigidBody)) { RigidBody = OutRigidBody; }
		}
		public float GetHoldDistance() { return HoldDistance; }
		public GameObject GetObject() { return Object; }
		public Rigidbody GetRigidBody() { return RigidBody; }
	}

	protected override void OnStart()
	{
		base.OnStart();
		if (IsProxy) return;
		Camera = Scene.GetComponentInChildren<CameraComponent>();
		SetupManagers();
		LocationSenderTimerHandle = Scene.GetSystem<TimerManager>().AddTimer(SendPlayerLocationInterval, true, SendPlayerLocation);
		if (Components.TryGet(out PlayerController OutPlayerController)) OutPlayerController.UseCameraControls = false;
	}

	protected override void OnUpdate()
	{
		if (IsProxy || Camera == null) return;
		ProcessInput();
	}

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
		GameObject HeldObject = HeldObjectData.GetObject();
		if (HeldObject == null || !Input.Pressed("Attack2")) return;
		Rigidbody Rigidbody = HeldObjectData.GetRigidBody();
		if (Rigidbody == null) return;
		Vector3 ThrowDir = Camera.WorldRotation.Forward;
		Rigidbody.Velocity = ThrowDir * ThrowForce;
		ClearHeldObjectData();
	}
	private void ProcessMouseLeftClickInput()
	{
		if (Input.Pressed("Attack1"))
		{
			EventManager.TriggerPlayerClickState(1f);
			Ray Ray = Camera.ScreenPixelToRay(new Vector2(Screen.Width / 2, Screen.Height / 2));
			Vector3 Start = Ray.Position;
			Vector3 End = Start + Ray.Forward * GrabDistance;
			SceneTraceResult Trace = Scene.Trace.WithTag("Collectible").Ray(Start, End).Run();
			DebugOverlay.Line(Start, End, Color.Red, 2f);
			if (!Trace.Hit) return;
			Trace.GameObject.Network.TakeOwnership();
			HeldObjectData = new FHeldObjectData(Trace.GameObject, Trace.Distance);
			SetupGrabbedObjectTimer();
		}
		else if (Input.Released("Attack1"))
		{
			EventManager.TriggerPlayerClickState(5f);
			ClearHeldObjectData();
		}
	}
	private void SetupGrabbedObjectTimer()
	{
		GrabTimerHandle = Scene.GetSystem<TimerManager>().AddTimer(1 / 60f, true, ProcessGrabbedObjectLogic);
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
		if (!HeldObjectData.GetRigidBody().IsValid()) return;
		Vector3 TargetPosition = Camera.WorldPosition + Camera.WorldRotation.Forward * HeldObjectData.GetHoldDistance();
		Vector3 Displacement = TargetPosition - HeldObjectData.GetObject().WorldPosition;
		Vector3 Force = Displacement * SpringStrength - HeldObjectData.GetRigidBody().Velocity * DampingStrength;
		HeldObjectData.GetRigidBody().ApplyForce(Force * HeldObjectData.GetRigidBody().PhysicsBody.Mass);
		HeldObjectData.GetRigidBody().AngularVelocity = Vector3.Zero;
	}

	private void SetupManagers()
	{
		EventManager = Scene.GetSystem<EventManager>();
		UiManager = Scene.GetSystem<UiManager>();
	}

	private void ClearHeldObjectData()
	{
		if (HeldObjectData.GetObject() != null) HeldObjectData.GetObject().Network.DropOwnership();
		HeldObjectData = default;
		TimerManager.ClearTimer(GrabTimerHandle);
	}

	private void SendPlayerLocation() { EventManager.TriggerPlayerLocationSend(GameObject.WorldPosition); }

	public void OnConnected( Connection channel )
	{
		Log.Info(channel.Id);
	}

	public void OnDisconnected( Connection channel )
	{
		Log.Info(channel.Id);
	}

	[Rpc.Broadcast]
	private void RequestReturnToPoolObject(EPoolObject InPoolObjectName, GameObject InPoolObject)
	{
		if (!Networking.IsHost) return;
		InPoolObject.Network.TakeOwnership();
		Scene.GetSystem<PoolingManager>().ReturnObjectToPool(InPoolObjectName, InPoolObject);
	}
}
