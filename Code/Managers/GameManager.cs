using System;
using Sandbox.Data;
using Sandbox.Enums;
namespace Sandbox.Managers;

public sealed class GameManager : Component
{
	[Property] private NetDictionary<EPoolObject, ObjectPoolData> Prefabs { get; set; }
	[Property] private List<GameObject> PlayerSpawners { get; set; }
	
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
		EventManager.OnTeleportToSpawnLocation += GetSpawnerLocation;
		PoolingManager.Init(Prefabs);
	}
	protected override void OnUpdate()
	{
		TimerManager.OnUpdate();
		if (Networking.IsHost) CollectibleCubeManager.OnUpdate();
	}
	protected override void OnDestroy()
	{
		EventManager.OnTeleportToSpawnLocation -= GetSpawnerLocation;
		TimerManager.OnDestroy();
		base.OnDestroy();
	}

	private Vector3 GetSpawnerLocation()
	{
		return PlayerSpawners[Random.Shared.Int(0, PlayerSpawners.Count - 1)].WorldPosition;
	}
}
