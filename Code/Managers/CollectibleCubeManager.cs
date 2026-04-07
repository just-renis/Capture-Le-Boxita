using System;
using Sandbox.Enums;
namespace Sandbox.Managers;

public sealed class CollectibleCubeManager(Scene Scene) : GameObjectSystem(Scene)
{
	private float SpawnTimer;
	private readonly float SpawnTime = 2f;
	public void OnUpdate()
	{
		SpawnTimer += Time.Delta;
		if (SpawnTimer >= SpawnTime)
		{
			SpawnTimer = 0f;
			Scene.GetSystem<PoolingManager>().SpawnObjectFromPool( EPoolObject.CollectibleCube, new Vector3(Random.Shared.Float(100f, 500f), Random.Shared.Float(100f, 500f), Random.Shared.Float(0f, 300f)) );
		}
	}
	public void OnDestroy()
	{
		
	}
}
