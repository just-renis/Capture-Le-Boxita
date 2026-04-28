using System;
using Sandbox.Data;
using Sandbox.Enums;
namespace Sandbox.Managers;

public sealed class GameManager : Component
{
	[Property] private NetDictionary<EPoolObject, ObjectPoolData> Prefabs { get; set; }
	[Property] private List<GameObject> PlayerSpawners { get; set; }

	private CollectibleCubeManager CollectibleCubeManager;
	private PoolingManager PoolingManager;
	
	protected override void OnStart()
	{
		base.OnStart();
		CollectibleCubeManager = Scene.GetSystem<CollectibleCubeManager>();
		CollectibleCubeManager.Init();
		PoolingManager = Scene.GetSystem<PoolingManager>();
		EventManager.OnTeleportToSpawnLocation += GetSpawnerLocation;
		if (!Networking.IsHost) return;
		PoolingManager.Init(Prefabs);
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
		base.OnDestroy();
	}
	private Vector3 GetSpawnerLocation()
	{
		return PlayerSpawners[Random.Shared.Int(0, PlayerSpawners.Count - 1)].WorldPosition;
	}
}
