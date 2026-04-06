using Sandbox.Enums;
using Sandbox.Managers;
namespace Sandbox.Player;

public sealed class Player : Component
{
	[Property] private float Speed { get; set; } = 100f;
	[Property] private float Sensitivity { get; set; } = 0.2f;
	[Property] private CameraComponent Camera { get; set; }
	[RequireComponent] private CharacterController Controller { get; set; }

	private int GrabTimerHandle;
	private readonly float SpringStrength = 150f; 
	private readonly float DampingStrength = 15f;
	private Angles CameraRotation;
	private FHeldObjectData HeldObjectData;
	private EventManager EventManager;
	private UiManager UiManager;
	
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
		SetupManagers();
		if (Components.TryGet(out PlayerController OutPlayerController)) OutPlayerController.UseCameraControls = false;
	}

	protected override void OnUpdate()
	{
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
		CameraRotation.yaw -= MouseInput.x * Sensitivity;
		CameraRotation.pitch += MouseInput.y * Sensitivity;
		CameraRotation.pitch = CameraRotation.pitch.Clamp( -89f, 89f );
		Camera.LocalRotation = Rotation.From(CameraRotation);
	}
	
	private void ProcessMovementInput()
	{
		Vector3 CameraForward = Camera.WorldRotation.Forward;
		Vector3 CameraRight = Camera.WorldRotation.Right;

		CameraForward = new Vector3(CameraForward.x, CameraForward.y, 0).Normal;
		CameraRight = new Vector3(CameraRight.x, CameraRight.y, 0).Normal;
		
		Vector3 MovementVelocity = Vector3.Zero;
		if (Input.Down("Forward")) MovementVelocity += CameraForward * Speed * Time.Delta;
		if (Input.Down("Backward")) MovementVelocity -= CameraForward * Speed * Time.Delta;
		if (Input.Down("Left")) MovementVelocity -= CameraRight * Speed * Time.Delta;
		if (Input.Down("Right")) MovementVelocity += CameraRight * Speed * Time.Delta;
		MovementVelocity = MovementVelocity.Normal * Speed;
		Controller.Accelerate(MovementVelocity);
		Controller.ApplyFriction( 5.0f );
		Controller.Move();
	}
	
	private void ProcessKeyboardInput()
	{
		ProcessMovementInput();
		if (Input.Pressed("OpenUpgrades"))
		{
			UiManager.TriggerUiStateChange(EUi.UpgradesMenu);
		}
	}
	
	private void ProcessMouseInput()
	{
		ProcessCameraLook();
		if (Input.Pressed("Attack1"))
		{
			EventManager.TriggerPlayerClickState(true);
			Vector3 Start = Camera.WorldPosition;
			Vector3 End = Start + Camera.WorldRotation.Forward * 1000f;
			SceneTraceResult Trace = Scene.Trace.WithTag("Collectible").Ray(Start, End).Run();
			DebugOverlay.Line(Start, End, Color.Red, 2.0f);
			if (!Trace.Hit) return;
			HeldObjectData = new FHeldObjectData(Trace.GameObject, Trace.Distance);
			SetupGrabbedObjectTimer();
		}
		else if (Input.Released("Attack1"))
		{
			EventManager.TriggerPlayerClickState(false);
			TimerManager.ClearTimer(GrabTimerHandle);
		}
	}
	
	private void SetupGrabbedObjectTimer()
	{
		GrabTimerHandle = Scene.GetSystem<TimerManager>().AddTimer(1 / 60f, true, ProcessGrabbedObjectLogic);
	}
	
	private void ProcessGrabbedObjectLogic()
	{
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
}
