using System;
using Sandbox.Collectibles;
using Sandbox.Components;
using Sandbox.Enums;
using Sandbox.Interfaces;
using Sandbox.Managers;
namespace Sandbox.AI;

public sealed class AlienShip : Component, IPoolObject
{
	[Sync] private bool IsFlyUpProcessInProgress { get; set; }
	[Sync] private bool IsFlyDownProcessInProgress { get; set; }
	[Sync] private int TimesToFlyUpwards { get; set; }
	[Sync] private float RandomDownwardsStopZ { get; set; }
	[Property] private GameObject BeamVfx { get; set; }
	private bool IsLogicInitialized;
	public static int LiveCount { get; private set; }
	private int MoveHandle;
	private int CheckIsInAirObjectsHandle;
	private int FlyUpwardsHandle;
	private int FlyDownwardsHandle;
	private int SmallFlyUpwardsHandle;
	private const float MoveSpeed = 50f;
	private const float MoveUpwardsSpeed = 2000f;
	private const float SmallMoveUpwardsSpeed = 300f;
	private const float FpsTime = 1f / 60f;
	private const float ScaleIncrease = 0.1f;
	private const float MaxScale = 4f;
	private float ChangeDirectionTimer;
	private Vector2 Direction;
	private BoxCollider Collider;
	private CustomTrigger BeamCollider;
	private readonly List<PullableComponent> ImmovableBeamObjects = [];
	private readonly HashSet<PullableComponent> InsideBeamObjects = [];
	private readonly Dictionary<Guid, float> RecentlyTeleported = [];
	// --------------------------------------------------------------PUBLIC METHODS--------------------------------------------------------------
	public void Init()
	{
		Collider?.Enabled = true;
		BeamCollider?.SetTraceState(true);
		RandomDownwardsStopZ = Random.Shared.Float(1500f, 2100f);
		SetupBeamVfx();
		WorldScale = new Vector3(1f);
		IsFlyDownProcessInProgress = true;
		FlyDownwardsHandle = TimerManager.AddTimer(FlyDownwardsHandle, FpsTime, true, FlyDownProcess);
	}
	// --------------------------------------------------------------PROTECTED METHODS--------------------------------------------------------------
	protected override void OnUpdate()
	{
		if (Networking.IsHost && !IsLogicInitialized) SetupHostLogic();
	}
	protected override void OnDestroy()
	{
		Collider?.OnTriggerEnter -= ChangeToOppositeDirection;
		BeamCollider?.OnEnter -= BeamEntered;
		BeamCollider?.OnExit -= BeamExit;
		TimerManager.ClearTimer(MoveHandle);
		TimerManager.ClearTimer(CheckIsInAirObjectsHandle);
		TimerManager.ClearTimer(SmallFlyUpwardsHandle);
		TimerManager.ClearTimer(FlyUpwardsHandle);
		TimerManager.ClearTimer(FlyDownwardsHandle);
	}
	protected override void OnEnabled() { LiveCount++; }
	protected override void OnDisabled() { LiveCount--; }
	// --------------------------------------------------------------PRIVATE METHODS--------------------------------------------------------------
	private void SetupHostLogic()
	{
		IsLogicInitialized = true;
		SetupBoxCollider();
		ChangeDirection();
		SetupBeamCollider();
		MoveHandle = TimerManager.AddTimer(MoveHandle, 0.05f, true, Move);
		CheckIsInAirObjectsHandle = TimerManager.AddTimer(CheckIsInAirObjectsHandle, 0.1f, true, CheckIsInAirObjects);
		SmallFlyUpwardsHandle = TimerManager.AddTimer(SmallFlyUpwardsHandle, 0.25f, true, SmallFlyUpProcess);
		if (IsFlyUpProcessInProgress) FlyUpwardsHandle = TimerManager.AddTimer(FlyUpwardsHandle, FpsTime, true, FlyUpProcess);
		if (IsFlyDownProcessInProgress) FlyDownwardsHandle = TimerManager.AddTimer(FlyDownwardsHandle, FpsTime, true, FlyDownProcess);
	}
	private void Move()
	{
		ChangeDirectionTimer -= 0.05f;
		if (ChangeDirectionTimer <= 0f) ChangeDirection();
		WorldPosition += new Vector3(Direction * MoveSpeed * 0.05f);
	}
	private void CheckIsInAirObjects()
	{
		for (int i = ImmovableBeamObjects.Count - 1; i >= 0; i--)
		{
			PullableComponent Pullable = ImmovableBeamObjects[i];
			if (!Pullable.IsInAir()) continue;
			SetPullOnObject(Pullable, true);
			ImmovableBeamObjects.RemoveAt(i);
		}
	}
	private void ChangeDirection()
	{
		float X = Random.Shared.Float(0.5f, 1f) * (Random.Shared.Next(2) * 2 - 1);
		float Y = Random.Shared.Float(0.5f, 1f) * (Random.Shared.Next(2) * 2 - 1);
		Direction = new Vector2(X, Y);
		ChangeDirectionTimer = Random.Shared.Float(5f, 10f);
	}
	private void ChangeToOppositeDirection(Collider InCollider)
	{
		if (InCollider.Tags.Has("collectible"))
		{
			InCollider.GameObject.Root.GetComponent<CollectibleCube>()?.RequestReturnToPoolObject(WorldScale.x);
			AddScale();
		}
		else if (InCollider.Tags.Has("player"))
		{
			GameObject Root = InCollider.GameObject.Root;
			Root.GetComponent<Player.Player>()?.ReturnToSpawnLocation();
			if (RecentlyTeleported.TryGetValue(Root.Id, out float LastTime))
			{
				if (Time.Now - LastTime < 3f) return;
			}
			RecentlyTeleported[Root.Id] = Time.Now;
			AddScale();
		}
		else
		{
			Direction *= -1;
			WorldPosition += new Vector3(Direction * 10f);
			ChangeDirectionTimer = Random.Shared.Float(5f, 10f);
		}
	}
	private void AddScale()
	{
		GameObject.WorldScale += new Vector3(ScaleIncrease);
		TimesToFlyUpwards += 5;
		if (BeamCollider != null)
		{
			BeamCollider.CapsuleRadius = BeamCollider.DefaultCapsuleRadius * GameObject.WorldScale.x;
			BeamCollider.CapsuleHeight = BeamCollider.DefaultCapsuleHeight * GameObject.WorldScale.x;
		}
		if (GameObject.WorldScale.x >= MaxScale) FlyAway();
	}
	private void FlyAway()
	{
		foreach (PullableComponent Pullable in InsideBeamObjects.ToList().Where(Pullable => Pullable.IsValid()))
		{
			SetPullOnObject(Pullable, false);
			ImmovableBeamObjects.Remove(Pullable);
			InsideBeamObjects.Remove(Pullable);
		}
		Collider?.Enabled = false;
		BeamCollider?.SetTraceState(false);
		ChangeBeamVfxState(false);
		IsFlyUpProcessInProgress = true;
		FlyUpwardsHandle = TimerManager.AddTimer(FlyUpwardsHandle, FpsTime, true, FlyUpProcess);
	}
	private void SmallFlyUpProcess()
	{
		if (TimesToFlyUpwards < 1) return;
		TimesToFlyUpwards--;
		WorldPosition += new Vector3(Vector3.Up * SmallMoveUpwardsSpeed * FpsTime);
	}
	private void FlyUpProcess()
	{
		WorldPosition += new Vector3(Vector3.Up * MoveUpwardsSpeed * FpsTime);
		if (WorldPosition.z < 8000f) return;
		IsFlyUpProcessInProgress = false;
		ReturnToPool();
	}
	private void FlyDownProcess()
	{
		WorldPosition -= new Vector3(Vector3.Up * MoveUpwardsSpeed * FpsTime);
		if (WorldPosition.z > RandomDownwardsStopZ) return;
		IsFlyDownProcessInProgress = false;
		TimerManager.ClearTimer(FlyDownwardsHandle);
	}
	private void ReturnToPool()
	{
		if (!Networking.IsHost) return;
		if (BeamCollider != null)
		{
			BeamCollider.CapsuleRadius = BeamCollider.DefaultCapsuleRadius;
			BeamCollider.CapsuleHeight = BeamCollider.DefaultCapsuleHeight;
		}
		RecentlyTeleported.Clear();
		TimerManager.ClearTimer(FlyUpwardsHandle);
		GameObject.Network.TakeOwnership();
		PoolingManager.Instance.ReturnObjectToPool(EPoolObject.AlienShip, GameObject);
	}
	private void BeamExit(GameObject InGameObject)
	{
		if (InGameObject.Tags.Has("safe")) return;
		PullableComponent Pullable = InGameObject.Root.GetComponent<PullableComponent>(true);
		if (Pullable == null) return;
		if (Pullable.GetIsPulled()) SetPullOnObject(Pullable, false);
		ImmovableBeamObjects.Remove(Pullable);
		InsideBeamObjects.Remove(Pullable);
	}
	private void BeamEntered(GameObject InGameObject)
	{
		if (InGameObject.Tags.Has("safe")) return;
		PullableComponent Pullable = InGameObject.Root.GetComponent<PullableComponent>();
		if (Pullable == null) return;
		if (!Pullable.IsInAir()) ImmovableBeamObjects.Add(Pullable);
		else SetPullOnObject(Pullable, true);
		InsideBeamObjects.Add(Pullable);
	}
	private void SetupBoxCollider()
	{
		Collider = GameObject.GetComponent<BoxCollider>() ?? GameObject.AddComponent<BoxCollider>();
		Collider.Scale = new Vector3(400f, 400f, 100f);
		Collider.Center = new Vector3(0f, 0f, 20f);
		Collider.IsTrigger = true;
		Collider.ColliderFlags |= ColliderFlags.IgnoreTraces;
		Collider.ColliderFlags |= ColliderFlags.IgnoreMass;
		Collider.OnTriggerEnter += ChangeToOppositeDirection;
	}
	private void SetupBeamCollider()
	{
		if (BeamCollider == null)
		{
			BeamCollider = GameObject.GetComponentInChildren<CustomTrigger>(true);
			if (BeamCollider == null)
			{
				GameObject BeamObject = new() { Parent = GameObject, Name = "BeamTrigger" };
				BeamCollider = BeamObject.AddComponent<CustomTrigger>();
			}
			BeamCollider.OnEnter -= BeamEntered;
			BeamCollider.OnExit -= BeamExit;
			BeamCollider.OnEnter += BeamEntered;
			BeamCollider.OnExit += BeamExit;
		}
		BeamCollider.Shape = TriggerShape.Capsule;
		BeamCollider.TriggerByTags = true;
		BeamCollider.DefaultCapsuleHeight = 2000f;
		BeamCollider.DefaultCapsuleRadius = 100f;
		BeamCollider.TriggerTags = ["player", "collectible"];
		BeamCollider.LocalPosition = new Vector3(0, 0, -1050f);
		BeamCollider.CapsuleRadius = BeamCollider.DefaultCapsuleRadius * WorldScale.x;
		BeamCollider.CapsuleHeight = BeamCollider.DefaultCapsuleHeight * WorldScale.x;
	}
	private void SetupBeamVfx()
	{
		if (BeamVfx == null) return;
		SetupBeamVfxPositionAndScale(RandomDownwardsStopZ);
		ChangeBeamVfxState(true);
	}
	[Rpc.Broadcast]
	private void SetupBeamVfxPositionAndScale(float InRandomDownwardsStopZ)
	{
		BeamVfx.LocalPosition = new Vector3(0f, 0f, InRandomDownwardsStopZ * -0.5f);
		BeamVfx.LocalScale = new Vector3(1f, 1f, InRandomDownwardsStopZ * 0.005f);
	}
	// --------------------------------------------------------------NETWORKING METHODS--------------------------------------------------------------
	[Rpc.Broadcast]
	private void ChangeBeamVfxState(bool InState) { BeamVfx?.Enabled = InState; }
	[Rpc.Broadcast]
	private void SetPullOnObject(PullableComponent InPullable, bool InState)
	{
		if (InPullable == null) return;
		InPullable.SetIsPulled(InState);
		if (InState) InPullable.SetAlienShipRef(GameObject.Id);
	}
}
