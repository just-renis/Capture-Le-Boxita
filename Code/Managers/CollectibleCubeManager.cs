using System;

namespace Sandbox.Managers;

public sealed class CollectibleCubeManager(Scene Scene) : GameObjectSystem(Scene)
{
	private float SpawnTimer;
	private readonly float SpawnTime = 2f;
	private GameObject CollectibleCubePrefab;

	public void Init(GameObject InCollectibleCubePrefab)
	{
		CollectibleCubePrefab = InCollectibleCubePrefab;
	}
	public void OnUpdate()
	{
		SpawnTimer += Time.Delta;
		if (SpawnTimer >= SpawnTime)
		{
			SpawnTimer = 0f;
			CollectibleCubePrefab.Clone(new Vector3(Random.Shared.Float(100f, 500f), Random.Shared.Float(100f, 500f), Random.Shared.Float(0f, 300f)));
		}
	}
	public void OnDestroy()
	{
		
	}
}
