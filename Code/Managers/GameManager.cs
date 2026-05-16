using System;
namespace Sandbox.Managers;

public sealed class GameManager : Component
{
	[Property] private List<GameObject> PlayerSpawners { get; set; }

	private CollectibleCubeManager CollectibleCubeManager;
	
	protected override void OnStart()
	{
		CollectibleCubeManager = Scene.GetSystem<CollectibleCubeManager>();
		CollectibleCubeManager.Init();
		EventManager.OnTeleportToSpawnLocation += GetSpawnerLocation;
		UiManager.OnStart();
	}
	protected override void OnUpdate()
	{
		TimerManager.OnUpdate();
		if (!Networking.IsHost) return;
		CollectibleCubeManager.OnUpdate();
	}
	protected override void OnDestroy()
	{
		EventManager.OnTeleportToSpawnLocation -= GetSpawnerLocation;
		TimerManager.OnDestroy();
		UiManager.OnDestroy();
	}
	private Vector3 GetSpawnerLocation()
	{
		return PlayerSpawners[Random.Shared.Int(0, PlayerSpawners.Count - 1)].WorldPosition;
	}
}
