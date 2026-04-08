using System;
using Sandbox.Enums;
using Sandbox.Managers;
namespace Sandbox.Player;

public sealed class Player : Component
{
	[Property] private float Speed { get; set; } = 100f;
	[Property] private float Sensitivity { get; set; } = 0.2f;
	[Property] private float CameraDistance { get; set; } = 200f;
	[Property] private float CameraHeight { get; set; } = 50f;
	[RequireComponent] private CharacterController Controller { get; set; }

	private int GrabTimerHandle;
	private readonly float SpringStrength = 150f; 
	private readonly float DampingStrength = 15f;
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
			Trace.GameObject.Network.TakeOwnership();
			HeldObjectData = new FHeldObjectData(Trace.GameObject, Trace.Distance);
			SetupGrabbedObjectTimer();
		}
		else if (Input.Released("Attack1"))
		{
			if ( HeldObjectData.GetObject() != null )
			{
				RequestReturnToPoolObject(EPoolObject.CollectibleCube, HeldObjectData.GetObject());
				HeldObjectData = default;
			}
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
	[Rpc.Broadcast]
	private void RequestReturnToPoolObject(EPoolObject InPoolObjectName, GameObject InPoolObject)
	{
		if ( !Networking.IsHost ) return;
		InPoolObject.Network.TakeOwnership();
		Scene.GetSystem<PoolingManager>().ReturnObjectToPool( InPoolObjectName, InPoolObject );
	}
}
