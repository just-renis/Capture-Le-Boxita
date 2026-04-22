using System;
using Sandbox.Components;
using Sandbox.Interfaces;
using Sandbox.Managers;
namespace Sandbox.AI;

public sealed class AlienShip : Component, IPoolObject
{
	private int MoveHandle;
	private int CheckIsInAirObjectsHandle;
	private const float MoveSpeed = 50f;
	private float ChangeDirectionTimer;
	private Vector2 Direction;
	private BoxCollider Collider;
	private CapsuleCollider BeamCollider;
	private readonly HashSet<PullableComponent> InsideBeamObjects = [];

	public void Init() { }

	protected override void OnStart()
	{
		base.OnStart();
		if (!Networking.IsHost) return;
		SetupBoxCollider();
		ChangeDirection();
		SetupBeamCollider();
		MoveHandle = TimerManager.AddTimer(MoveHandle, 0.05f, true, Move);
		CheckIsInAirObjectsHandle = TimerManager.AddTimer(CheckIsInAirObjectsHandle, 0.1f, true, CheckIsInAirObjects);
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
		List<PullableComponent> ToRemove = InsideBeamObjects.Where(Pullable => Pullable.IsInAir()).ToList();
		foreach (PullableComponent Pullable in ToRemove)
		{
			Pullable.SetIsPulled(true);
			InsideBeamObjects.Remove(Pullable);
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
		Direction *= -1;
		ChangeDirectionTimer = Random.Shared.Float(5f, 10f);
	}
	private void BeamExit(Collider InCollider)
	{
		PullableComponent Pullable = InCollider.GameObject.Root.GetComponent<PullableComponent>();
		InsideBeamObjects.Remove(Pullable);
		SetPullOnObject(Pullable, false);
	}
	private void BeamEntered(Collider InCollider)
	{
		PullableComponent Pullable = InCollider.GameObject.Root.GetComponent<PullableComponent>();
		if (Pullable == null) return;
		if (!Pullable.IsInAir()) InsideBeamObjects.Add(Pullable);
		else SetPullOnObject(Pullable, true);
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
	
	[Rpc.Broadcast]
	private void SetPullOnObject(PullableComponent InPullable, bool InState)
	{
		InPullable?.SetIsPulled(InState);
	}
}
