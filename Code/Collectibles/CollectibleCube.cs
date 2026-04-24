using Sandbox.Components;
using Sandbox.Enums;
using Sandbox.Interfaces;
using Sandbox.Managers;
namespace Sandbox.Collectibles;

public sealed class CollectibleCube : Component, IPoolObject
{
	private int OutOfBoundsHandle;
	private PullableComponent PullComp;
	private Rigidbody Rigidbody;
	
	public void Init()
	{
		OutOfBoundsHandle = TimerManager.AddTimer(OutOfBoundsHandle, 0.2f, true, CheckOutOfBounds);
	}
	
	protected override void OnStart()
	{
		base.OnStart();
		PullComp = GetComponent<PullableComponent>();
		Rigidbody = GetComponent<Rigidbody>();
	}

	private void CheckOutOfBounds()
	{
		if (GameObject.WorldPosition.z < 500f || !PullComp.GetIsPulled()) return;
	//	Log.Info("RETURN");
		TimerManager.ClearTimer(OutOfBoundsHandle);
		RequestReturnToPoolObject(EPoolObject.CollectibleCube, GameObject);
	}
	[Rpc.Broadcast]
	private void RequestReturnToPoolObject(EPoolObject InPoolObjectName, GameObject InPoolObject)
	{
		if (!Networking.IsHost) return;
		InPoolObject.Network.TakeOwnership();
		PullComp.SetIsPulled(false);
		EventManager.TriggerOnKill(PullComp.GetOwnerId(), PullComp.GetMoneyAmount());
		PullComp.ClearOwnerId();
		Rigidbody.Velocity = 0f;
		Scene.GetSystem<PoolingManager>().ReturnObjectToPool(InPoolObjectName, InPoolObject);
	}
}
