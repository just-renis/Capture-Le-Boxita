using System;
using Sandbox.Enums;
using Sandbox.Managers;
namespace Sandbox.AI;

public sealed class AlienShip : Component
{
	private int MoveHandle;
	private int BeamUpdateHandle;
	private const float MoveSpeed = 50f;
	private float ChangeDirectionTimer;
	private Vector2 Direction;
	private BoxCollider Collider;
	private CapsuleCollider BeamCollider;
	private HashSet<Rigidbody> InsideBeamPhysicalObjects = [];
	
	protected override void OnStart()
	{
		base.OnStart();
		ChangeDirection();
		Collider = GetComponent<BoxCollider>();
		BeamCollider = GetComponentInChildren<CapsuleCollider>();
		BeamCollider.OnTriggerEnter += BeamEntered;
		BeamCollider.OnTriggerExit += BeamExited;
		Collider.OnTriggerEnter += ChangeToOppositeDirection;
		MoveHandle = TimerManager.AddTimer(0.05f, true, Move);
		BeamUpdateHandle = TimerManager.AddTimer(0.05f, true, BeamUpdate);
	}
	protected override void OnDestroy()
	{
		Collider?.OnTriggerEnter -= ChangeToOppositeDirection;
		if (BeamCollider.IsValid())
		{
			BeamCollider.OnTriggerEnter -= BeamEntered;
			BeamCollider.OnTriggerExit -= BeamExited;
		}
		TimerManager.ClearTimer(MoveHandle);
		TimerManager.ClearTimer(BeamUpdateHandle);
		base.OnDestroy();
	}

	private void Move()
	{
		ChangeDirectionTimer -= 0.05f;
		if (ChangeDirectionTimer <= 0f) ChangeDirection();
		WorldPosition += new Vector3(Direction * MoveSpeed * 0.05f);
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
		if (InCollider.Tags.Contains("beamaffected"))
		{
			if (InCollider.Tags.Contains("collectible")) RequestReturnToPoolObject(EPoolObject.CollectibleCube, InCollider.GameObject.Root);
			else Log.Info("LMAO");
		}
		else
		{
			Direction *= -1;
			ChangeDirectionTimer = Random.Shared.Float(5f, 10f);
		}
	}
	private void BeamEntered(Collider InCollider)
	{
		if (!InCollider.Tags.Contains("beamaffected") || !InsideBeamPhysicalObjects.Add(InCollider.GameObject.Root.GetComponent<Rigidbody>())) return;
		//Log.Info("ENTERED: " + InCollider.GameObject.Root);
	}
	private void BeamExited(Collider InCollider)
	{
		if (!InCollider.Tags.Contains("beamaffected") || !InsideBeamPhysicalObjects.Contains(InCollider.GameObject.Root.GetComponent<Rigidbody>())) return;
		//Log.Info("EXITED: " + InCollider.GameObject.Root);
		InsideBeamPhysicalObjects.Remove(InCollider.GameObject.Root.GetComponent<Rigidbody>());
	}
	private void BeamUpdate()
	{
		foreach (Rigidbody Body in InsideBeamPhysicalObjects.Where(Body => Body.IsValid()))
		{
			Body.Velocity = Vector3.Up * 500f;
		}
	}
	[Rpc.Broadcast]
	private void RequestReturnToPoolObject(EPoolObject InPoolObjectName, GameObject InPoolObject)
	{
		if (!Networking.IsHost) return;
		InPoolObject.Network.TakeOwnership();
		Rigidbody Body = InPoolObject.Root.GetComponent<Rigidbody>();
		Body.Velocity = 0f;
		InsideBeamPhysicalObjects.Remove(Body);
		Scene.GetSystem<PoolingManager>().ReturnObjectToPool(InPoolObjectName, InPoolObject);
	}
}
