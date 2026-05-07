using Sandbox.Components;
using Sandbox.Data;
using Sandbox.Enums;
using Sandbox.Interfaces;
using Sandbox.Managers;
namespace Sandbox.Collectibles;

public sealed class CollectibleCube : Component, IPoolObject
{
	[Property] private Dictionary<ECubeMaterial, Material> Materials { get; set; }
	[Property] private ModelRenderer CubeModelRenderer { get; set; }
	private int SetMaterialHandle;
	private PullableComponent PullComp;
	private Rigidbody Rigidbody;
	// --------------------------------------------------------------PUBLIC METHODS--------------------------------------------------------------
	public void RequestReturnToPoolObject()
	{
		if (!Networking.IsHost) return;
		GameObject.Network.TakeOwnership();
		PullComp.SetIsPulled(false);
		EventManager.TriggerOnKill(PullComp.GetOwnerId(), false, 1f);
		PullComp.ClearOwnerId();
		Rigidbody.Velocity = 0f;
		Scene.GetSystem<PoolingManager>().ReturnObjectToPool(EPoolObject.CollectibleCube, GameObject);
	}
	// --------------------------------------------------------------PROTECTED METHODS--------------------------------------------------------------
	protected override void OnStart()
	{
		SetMaterialHandle = TimerManager.AddTimer(SetMaterialHandle, 0.1f, true, SetMaterial);
		PullComp = GetComponent<PullableComponent>();
		Rigidbody = GetComponent<Rigidbody>();
	}
	// --------------------------------------------------------------PRIVATE METHODS--------------------------------------------------------------
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
	// --------------------------------------------------------------NETWORKING METHODS--------------------------------------------------------------
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
