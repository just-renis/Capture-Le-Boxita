using System;
using Sandbox.Enums;
namespace Sandbox.Managers;

public sealed class CollectibleCubeManager(Scene Scene) : GameObjectSystem(Scene)
{
	private float CubeSpawnTimer;
	private const float CubeSpawnTime = 0.1f;
	private float AlienShipSpawnTimer;
	private const float AlienShipSpawnTime = 2f;
	private PoolingManager PoolingManager;
	
	public void Init()
	{
		PoolingManager = PoolingManager.Instance;
		EventManager.OnResetWorld += ResetWorld;
	}
	public void OnDestroy()
	{
		EventManager.OnResetWorld -= ResetWorld;
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
		PoolingManager.SpawnObjectFromPool(EPoolObject.CollectibleCube, GetValidSpawnLocationForCube());
	}
	private Vector3 GetValidSpawnLocationForCube()
	{
		Vector3 Location;
		bool IsInvalid;
		do
		{
			Location = new Vector3(Random.Shared.Float(-7000f, 7000f), Random.Shared.Float(-7000f, 7000f), 1200f);
			IsInvalid = Location.x > 5200f && Location.y > 5200f;
		} while (IsInvalid);
		return Location;
	}
	private void HandleAlienShipSpawning()
	{
		if (!PoolingManager.CanSpawnObject(EPoolObject.AlienShip)) return;
		AlienShipSpawnTimer += Time.Delta;
		if (AlienShipSpawnTimer < AlienShipSpawnTime) return;
		AlienShipSpawnTimer = 0f;
		float RandomX = Random.Shared.Int(0, 1) == 0 ? Random.Shared.Float(-4200f, 200f) : Random.Shared.Float(800f, 5200f);
		PoolingManager.SpawnObjectFromPool(EPoolObject.AlienShip, new Vector3(RandomX, Random.Shared.Float(-5500f, 4500f), 8000f));
	}
	private void ResetWorld()
	{
		if (!Networking.IsHost) return;
		CubeSpawnTimer = 0f;
		AlienShipSpawnTimer = 0f;
		PoolingManager.ResetPoolType(EPoolObject.CollectibleCube);
		PoolingManager.ResetPoolType(EPoolObject.AlienShip);
	}
}
