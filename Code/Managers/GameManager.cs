using Sandbox.Data;
using Sandbox.Enums;
namespace Sandbox.Managers;

public sealed class GameManager : Component
{
	[Property] NetDictionary<EPoolObject, ObjectPoolData> Prefabs { get; set; }
	
	private CollectibleCubeManager CollectibleCubeManager;
	private UiManager UiManager;
	private PoolingManager PoolingManager;
	protected override void OnStart()
	{
		base.OnStart();
		UiManager = Scene.GetSystem<UiManager>();
		UiManager.OnStart();
		CollectibleCubeManager = Scene.GetSystem<CollectibleCubeManager>();
		CollectibleCubeManager.Init();
		PoolingManager = Scene.GetSystem<PoolingManager>();
		if (!Networking.IsHost) return;
		PoolingManager.Init(Prefabs);
	}
	
	protected override void OnUpdate()
	{
		TimerManager.OnUpdate();
		if (Networking.IsHost) CollectibleCubeManager.OnUpdate();
	}
	protected override void OnDestroy()
	{
		TimerManager.OnDestroy();
		base.OnDestroy();
	}
}
