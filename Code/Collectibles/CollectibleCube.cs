using Sandbox.Components;
using Sandbox.Data;
using Sandbox.Enums;
using Sandbox.Interfaces;
using Sandbox.Managers;
namespace Sandbox.Collectibles;

public sealed class CollectibleCube : Component, IPoolObject
{
	[Property] private Dictionary<ECubeMaterial, Material> Materials = [];
	[Property] private ModelRenderer CubeModelRenderer { get; set; }
	private int SetMaterialHandle;
	private PullableComponent PullComp;
	private Rigidbody Rigidbody;

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

	protected override void OnStart()
	{
		base.OnStart();
		SetMaterialHandle = TimerManager.AddTimer(SetMaterialHandle, 0.1f, true, SetMaterial);
		PullComp = GetComponent<PullableComponent>();
		Rigidbody = GetComponent<Rigidbody>();
	}
	
	private void SetMaterial()
	{
		PlayerData Data = EventManager.TriggerGetPlayerData();
		if (Data == null) return;
		if (Data.Money >= 500f)
		{
			CubeModelRenderer?.SetMaterial(Materials[ECubeMaterial.Metal]);
		}
		else CubeModelRenderer?.SetMaterial(Materials[ECubeMaterial.Cardboard]);
		TimerManager.ClearTimer(SetMaterialHandle);
	}
	
	[Rpc.Broadcast]
	public void Init()
	{
		PlayerData Data = EventManager.TriggerGetPlayerData();
		if (Data == null) return;
		if (Data.Money >= 500f)
		{
			CubeModelRenderer?.SetMaterial(Materials[ECubeMaterial.Metal]);
		}
		else CubeModelRenderer?.SetMaterial(Materials[ECubeMaterial.Cardboard]);
	}
}
