using System;
using Sandbox.Collectibles;
using Sandbox.Components;
using Sandbox.Enums;
using Sandbox.Interfaces;
using Sandbox.Managers;
namespace Sandbox.AI;

public sealed class AlienShip : Component, IPoolObject
{
	private int MoveHandle;
	private int CheckIsInAirObjectsHandle;
	private int PullObjectsTowardsCenterHandle;
	private int FlyUpwardsHandle;
	private const float MoveSpeed = 50f;
	private const float MoveUpwardsSpeed = 2000f;
	private const float FpsTime = 1f / 60f;
	private const float ScaleIncrease = 1f;
	private const float MaxScale = 4f;
	private float ChangeDirectionTimer;
	private Vector2 Direction;
	private BoxCollider Collider;
	private CapsuleCollider BeamCollider;
	private GameObject BeamVfx;
	private readonly HashSet<PullableComponent> ImmovableBeamObjects = [];
	private readonly HashSet<PullableComponent> InsideBeamObjects = [];

	public void Init()
	{
		Collider?.Enabled = true;
		BeamCollider?.Enabled = true;
		ChangeBeamVfxState(true);
		WorldScale = new Vector3(1f);
	}

	protected override void OnStart()
	{
		base.OnStart();
		BeamVfx = GameObject.Children.FirstOrDefault(OutGameObject => OutGameObject.Name == "BeamShader");
		ChangeBeamVfxState(true);
		if (!Networking.IsHost) return;
		SetupBoxCollider();
		ChangeDirection();
		SetupBeamCollider();
		MoveHandle = TimerManager.AddTimer(MoveHandle, 0.05f, true, Move);
		CheckIsInAirObjectsHandle = TimerManager.AddTimer(CheckIsInAirObjectsHandle, 0.1f, true, CheckIsInAirObjects);
		PullObjectsTowardsCenterHandle = TimerManager.AddTimer(PullObjectsTowardsCenterHandle, 0.025f, true, PullObjectsTowardsCenter);
	}
	private void PullObjectsTowardsCenter()
	{
		foreach (PullableComponent Pullable in InsideBeamObjects)
		{
			Vector3 TargetPos = new (WorldPosition.x, WorldPosition.y, Pullable.WorldPosition.z);
			Pullable.WorldPosition = Vector3.Lerp(Pullable.WorldPosition, TargetPos, 0.1f);
		}
	}
	protected override void OnDestroy()
	{
		if (!Networking.IsHost) return;
		Collider?.OnTriggerEnter -= ChangeToOppositeDirection;
		BeamCollider?.OnTriggerEnter -= BeamEntered;
		BeamCollider?.OnTriggerExit -= BeamExit;
		TimerManager.ClearTimer(MoveHandle);
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
			GameObject.WorldScale += new Vector3(ScaleIncrease);
			if (GameObject.WorldScale.x >= MaxScale) FlyAway();
		}
		else if (InCollider.Tags.Has("player"))
		{
			InCollider.GameObject.Root.GetComponent<Player.Player>()?.ReturnToSpawnLocation();
			GameObject.WorldScale += new Vector3(ScaleIncrease);
			if (GameObject.WorldScale.x >= MaxScale) FlyAway();
		}
		else
		{
			Direction *= -1;
			ChangeDirectionTimer = Random.Shared.Float(5f, 10f);
		}
	}
	private void FlyAway()
	{
		foreach (PullableComponent Pullable in InsideBeamObjects.ToList())
		{
			if (!Pullable.IsValid()) continue;
			SetPullOnObject(Pullable, false);
			ImmovableBeamObjects.Remove(Pullable);
			InsideBeamObjects.Remove(Pullable);
		}
		Collider?.Enabled = false;
		BeamCollider?.Enabled = false;
		ChangeBeamVfxState(false);
		FlyUpwardsHandle = TimerManager.AddTimer(FlyUpwardsHandle, FpsTime,  true, FlyUpProcess);
	}
	private void FlyUpProcess()
	{
		WorldPosition += new Vector3(Vector3.Up * MoveUpwardsSpeed * FpsTime);
		if (WorldPosition.z >= 5000f) ReturnToPool();
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
		InsideBeamObjects.Remove(Pullable);
		ImmovableBeamObjects.Remove(Pullable);
		SetPullOnObject(Pullable, false);
	}
	private void BeamEntered(Collider InCollider)
	{
		PullableComponent Pullable = InCollider.GameObject.Root.GetComponent<PullableComponent>();
		if (Pullable == null) return;
		if (!Pullable.IsInAir()) ImmovableBeamObjects.Add(Pullable);
		else SetPullOnObject(Pullable, true);
		InsideBeamObjects.Add(Pullable);
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
		BeamCollider.End = new Vector3(0f, 0f, -1000f);
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
	// --------------------------------------------------------------NETWORKING METHODS--------------------------------------------------------------
	[Rpc.Broadcast]
	private void ChangeBeamVfxState(bool InState) { BeamVfx?.Enabled = InState; }
}
