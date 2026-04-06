namespace Sandbox.Managers;

public sealed class GameManager : Component
{
	[Property] 
	GameObject CollectibleCubePrefab { get; set; }
	
	private TimerManager TimerManager;
	private CollectibleCubeManager CollectibleCubeManager;
	private UiManager UiManager;
	protected override void OnStart()
	{
		base.OnStart();
		TimerManager = Scene.GetSystem<TimerManager>();
		CollectibleCubeManager = Scene.GetSystem<CollectibleCubeManager>();
		CollectibleCubeManager.Init( CollectibleCubePrefab );
		UiManager = Scene.GetSystem<UiManager>();
		UiManager.OnStart();
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
