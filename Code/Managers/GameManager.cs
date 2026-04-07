using Sandbox.Enums;
namespace Sandbox.Managers;

public sealed class GameManager : Component
{
	[Property] 
	Dictionary<EPoolObject, GameObject> Prefabs { get; set; }
	
	private TimerManager TimerManager;
	private CollectibleCubeManager CollectibleCubeManager;
	private UiManager UiManager;
	private PoolingManager PoolingManager;
	protected override void OnStart()
	{
		base.OnStart();
		TimerManager = Scene.GetSystem<TimerManager>();
		CollectibleCubeManager = Scene.GetSystem<CollectibleCubeManager>();
		CollectibleCubeManager.Init();
		UiManager = Scene.GetSystem<UiManager>();
		UiManager.OnStart();
		PoolingManager = Scene.GetSystem<PoolingManager>();
		PoolingManager.Init( Prefabs );
	}
	protected override void OnUpdate()
	{
		TimerManager.OnUpdate();
		CollectibleCubeManager.OnUpdate();
	}
	protected override void OnDestroy()
	{
		TimerManager.OnDestroy();
		CollectibleCubeManager.OnDestroy();
		base.OnDestroy();
	}
}
