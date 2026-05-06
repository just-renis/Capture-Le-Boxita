using System;
using Sandbox.Enums;
namespace Sandbox.Managers;

public sealed class CollectibleCubeManager(Scene Scene) : GameObjectSystem(Scene)
{
	private float CubeSpawnTimer;
	private const float CubeSpawnTime = 2f;
	private float AlienShipSpawnTimer;
	private const float AlienShipSpawnTime = 2f;
	private PoolingManager PoolingManager;
	
	public void Init()
	{
		PoolingManager = Scene.GetSystem<PoolingManager>();
	}
	public void OnUpdate()
	{
		HandleCubeSpawning();
		HandleAlienShipSpawning();
	}

	private void HandleCubeSpawning()
	{
		if (!PoolingManager.CanSpawnObject(EPoolObject.CollectibleCube)) return;
		CubeSpawnTimer += Time.Delta;
		if (CubeSpawnTimer < CubeSpawnTime) return;
		CubeSpawnTimer = 0f;
		PoolingManager.SpawnObjectFromPool(EPoolObject.CollectibleCube, new Vector3(Random.Shared.Float(-5000f, -4500f), Random.Shared.Float(5000f, 6000f), 200f));
	}
	private void HandleAlienShipSpawning()
	{
		if (!PoolingManager.CanSpawnObject(EPoolObject.AlienShip)) return;
		AlienShipSpawnTimer += Time.Delta;
		if (AlienShipSpawnTimer < AlienShipSpawnTime) return;
		AlienShipSpawnTimer = 0f;
		PoolingManager.SpawnObjectFromPool(EPoolObject.AlienShip, new Vector3(Random.Shared.Float(-5100f, -5200f), Random.Shared.Float(5000f, 5100f), Random.Shared.Float(500f, 1100f)));
	}
}
