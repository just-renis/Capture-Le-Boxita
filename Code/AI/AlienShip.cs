using System;
using Sandbox.Collectibles;
using Sandbox.Components;
using Sandbox.Enums;
using Sandbox.Interfaces;
using Sandbox.Managers;
namespace Sandbox.AI;

public sealed class AlienShip : Component, IPoolObject
{
	[Sync] private int TimesToFlyUpwards { get; set; }
	[Property] private GameObject BeamVfx { get; set; }
	private bool IsLogicInitialized;
	private int MoveHandle;
	private int CheckIsInAirObjectsHandle;
	private int FlyUpwardsHandle;
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
	private CapsuleCollider BeamCollider;
	private readonly HashSet<PullableComponent> ImmovableBeamObjects = [];
	private readonly HashSet<PullableComponent> InsideBeamObjects = [];

	public void Init()
	{
		Collider?.Enabled = true;
		BeamCollider?.Enabled = true;
		SetupBeamVfx();
		WorldScale = new Vector3(1f);
	}
	
	protected override void OnUpdate()
	{
		if (Networking.IsHost && !IsLogicInitialized) SetupHostLogic();
	}
	private void SetupHostLogic()
	{
		IsLogicInitialized = true;
		SetupBoxCollider();
		ChangeDirection();
		SetupBeamCollider();
		MoveHandle = TimerManager.AddTimer(MoveHandle, 0.05f, true, Move);
		CheckIsInAirObjectsHandle = TimerManager.AddTimer(CheckIsInAirObjectsHandle, 0.1f, true, CheckIsInAirObjects);
		SmallFlyUpwardsHandle = TimerManager.AddTimer(SmallFlyUpwardsHandle, 0.25f, true, SmallFlyUpProcess);
	}
	protected override void OnDestroy()
	{
		if (!Networking.IsHost) return;
		Collider?.OnTriggerEnter -= ChangeToOppositeDirection;
		BeamCollider?.OnTriggerEnter -= BeamEntered;
		BeamCollider?.OnTriggerExit -= BeamExit;
		TimerManager.ClearTimer(MoveHandle);
		TimerManager.ClearTimer(CheckIsInAirObjectsHandle);
		TimerManager.ClearTimer(SmallFlyUpwardsHandle);
		base.OnDestroy();
	}

	private void Move()
	{
		ChangeDirectionTimer -= 0.05f;
		if (ChangeDirectionTimer <= 0f) ChangeDirection();
		WorldPosition += new Vector3(Direction * MoveSpeed * 0.05f);
	}
	private void CheckIsInAirObjects()
	{
		List<PullableComponent> ToRemove = ImmovableBeamObjects.Where(Pullable => Pullable.IsInAir()).ToList();
		foreach (PullableComponent Pullable in ToRemove)
		{
			SetPullOnObject(Pullable, true);
			ImmovableBeamObjects.Remove(Pullable);
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
			InCollider.GameObject.Root.GetComponent<CollectibleCube>()?.RequestReturnToPoolObject();
			AddScale();
		}
		else if (InCollider.Tags.Has("player"))
		{
			InCollider.GameObject.Root.GetComponent<Player.Player>()?.ReturnToSpawnLocation();
			AddScale();
		}
		else
		{
			Direction *= -1;
			ChangeDirectionTimer = Random.Shared.Float(5f, 10f);
		}
	}
	private void AddScale()
	{
		GameObject.WorldScale += new Vector3(ScaleIncrease);
		TimesToFlyUpwards += 5;
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
		BeamCollider?.Enabled = false;
		ChangeBeamVfxState(false);
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
		if (WorldPosition.z >= 7000f) ReturnToPool();
	}
	private void ReturnToPool()
	{
		if (!Networking.IsHost) return;
		TimerManager.ClearTimer(FlyUpwardsHandle);
		GameObject.Network.TakeOwnership();
		Scene.GetSystem<PoolingManager>().ReturnObjectToPool(EPoolObject.AlienShip, GameObject);
	}
	private void BeamExit(Collider InCollider)
	{
		PullableComponent Pullable = InCollider.GameObject.Root.GetComponent<PullableComponent>(true);
		if (Pullable == null) return;
		ImmovableBeamObjects.Remove(Pullable);
		InsideBeamObjects.Remove(Pullable);
		SetPullOnObject(Pullable, false);
	}
	private void BeamEntered(Collider InCollider)
	{
		PullableComponent Pullable = InCollider.GameObject.Root.GetComponent<PullableComponent>();
		if (Pullable == null) return;
		if (!Pullable.IsInAir()) ImmovableBeamObjects.Add(Pullable);
		else SetPullOnObject(Pullable, true);
		InsideBeamObjects.Add(Pullable);
		Pullable.SetAlienShipRef(GameObject.Id);
	}
	private void SetupBoxCollider()
	{
		Collider = GameObject.AddComponent<BoxCollider>();
		Collider.Scale = new Vector3(400f, 400f, 50f);
		Collider.IsTrigger = true;
		Collider.ColliderFlags |= ColliderFlags.IgnoreTraces;
		Collider.ColliderFlags |= ColliderFlags.IgnoreMass;
		Collider.OnTriggerEnter += ChangeToOppositeDirection;
	}
	private void SetupBeamCollider()
	{
		GameObject BeamObject = new() { Parent = GameObject };
		BeamCollider = BeamObject.AddComponent<CapsuleCollider>();
		BeamCollider.End = new Vector3(0f, 0f, -2000f);
		BeamCollider.Radius = 100f;
		BeamCollider.IsTrigger = true;
		BeamCollider.ColliderFlags |= ColliderFlags.IgnoreTraces;
		BeamCollider.ColliderFlags |= ColliderFlags.IgnoreMass;
		BeamCollider.OnTriggerEnter += BeamEntered;
		BeamCollider.OnTriggerExit += BeamExit;
	}
	private void SetPullOnObject(PullableComponent InPullable, bool InState)
	{
		InPullable?.SetIsPulled(InState);
	}

	private void SetupBeamVfx()
	{
		if (BeamVfx == null) return;
		BeamVfx.LocalPosition = new Vector3(0f, 0f, GameObject.WorldPosition.z * -0.5f);
		BeamVfx.LocalScale = new Vector3(1f, 1f, GameObject.WorldPosition.z * 0.005f);
		ChangeBeamVfxState(true);
	}
	// --------------------------------------------------------------NETWORKING METHODS--------------------------------------------------------------
	[Rpc.Broadcast]
	private void ChangeBeamVfxState(bool InState) { BeamVfx?.Enabled = InState; }
}
