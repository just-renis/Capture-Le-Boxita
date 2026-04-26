using Sandbox.Components;
using Sandbox.Enums;
using Sandbox.Interfaces;
using Sandbox.Managers;
namespace Sandbox.Collectibles;

public sealed class CollectibleCube : Component, IPoolObject
{
	private PullableComponent PullComp;
	private Rigidbody Rigidbody;
	
	public void Init() { }
	
	protected override void OnStart()
	{
		base.OnStart();
		PullComp = GetComponent<PullableComponent>();
		Rigidbody = GetComponent<Rigidbody>();
	}
	
	public void RequestReturnToPoolObject()
	{
		if (!Networking.IsHost) return;
		GameObject.Network.TakeOwnership();
		PullComp.SetIsPulled(false);
		EventManager.TriggerOnKill(PullComp.GetOwnerId(), false, PullComp.GetMoneyAmount());
		PullComp.ClearOwnerId();
		Rigidbody.Velocity = 0f;
		Scene.GetSystem<PoolingManager>().ReturnObjectToPool(EPoolObject.CollectibleCube, GameObject);
	}
}
