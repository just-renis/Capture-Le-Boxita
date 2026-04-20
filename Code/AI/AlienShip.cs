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
		ChangeDirection();
		Collider = GetComponent<BoxCollider>();
		BeamCollider = GetComponentInChildren<CapsuleCollider>();
		BeamCollider?.OnTriggerEnter += BeamEntered;
		BeamCollider?.OnTriggerExit += BeamExit;
		Collider.OnTriggerEnter += ChangeToOppositeDirection;
		MoveHandle = TimerManager.AddTimer(MoveHandle, 0.05f, true, Move);
		CheckIsInAirObjectsHandle = TimerManager.AddTimer(CheckIsInAirObjectsHandle, 0.1f, true, CheckIsInAirObjects);
	}
	protected override void OnDestroy()
	{
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
	private void BeamEntered(Collider InCollider)
	{
		PullableComponent Pullable = InCollider.GameObject.Root.GetComponent<PullableComponent>();
		if (Pullable == null) return;
		if (!Pullable.IsInAir())
		{
			InsideBeamObjects.Add(Pullable);
			return;
		}
		Pullable.SetIsPulled(true);
	}
	private void BeamExit(Collider InCollider)
	{
		PullableComponent Pullable = InCollider.GameObject.Root.GetComponent<PullableComponent>();
		InsideBeamObjects.Remove(Pullable);
		Pullable?.SetIsPulled(false);
	}
}
