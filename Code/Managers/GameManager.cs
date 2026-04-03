namespace Sandbox.Managers;

public sealed class GameManager : Component
{
	[Property] 
	GameObject CollectibleCubePrefab { get; set; }
	
	private TimerManager TimerManager;
	private CollectibleCubeManager CollectibleCubeManager;
	protected override void OnStart()
	{
		base.OnStart();
		TimerManager = new TimerManager(Scene);
		CollectibleCubeManager = new CollectibleCubeManager(Scene, CollectibleCubePrefab);
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
