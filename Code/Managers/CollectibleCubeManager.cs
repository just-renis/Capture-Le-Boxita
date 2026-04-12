using System;
using Sandbox.Enums;
namespace Sandbox.Managers;

public sealed class CollectibleCubeManager(Scene Scene) : GameObjectSystem(Scene)
{
	private float SpawnTimer;
	private readonly float SpawnTime = 2f;
	private PoolingManager PoolingManager;
	
	public void Init()
	{
		PoolingManager = Scene.GetSystem<PoolingManager>();
	}
	public void OnUpdate()
	{
		if (!PoolingManager.CanSpawnObject(EPoolObject.CollectibleCube)) return;
		SpawnTimer += Time.Delta;
		if (SpawnTimer >= SpawnTime)
		{
			SpawnTimer = 0f;
			PoolingManager.SpawnObjectFromPool(EPoolObject.CollectibleCube, new Vector3(Random.Shared.Float(100f, 500f), Random.Shared.Float(100f, 500f), Random.Shared.Float(0f, 300f)));
		}
	}
}
