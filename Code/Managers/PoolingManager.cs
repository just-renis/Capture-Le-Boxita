using Sandbox.Data;
using Sandbox.Enums;
namespace Sandbox.Managers;

public sealed class PoolingManager(Scene Scene) : GameObjectSystem(Scene)
{
	[Sync]
	private NetDictionary<EPoolObject, ObjectPoolData> Pool { get; set; } = new();

	public bool CanSpawnObject( EPoolObject InObjectName )
	{
		ObjectPoolData PoolData = Pool[InObjectName];
		return PoolData.ActiveCount < PoolData.MaxAmountActive;
	}
	public GameObject SpawnObjectFromPool( EPoolObject InObjectName, Vector3 InLocation, Rotation InRotation = default )
	{
		GameObject PoolObject;
		ObjectPoolData PoolData = Pool[InObjectName];
		if (!PoolData.Objects.Any())
		{
			AllocatePoolObject(InObjectName);
			PoolObject = PoolData.Objects.Pop();
		}
		else PoolObject = PoolData.Objects.Pop();
		PoolObject.LocalPosition = InLocation;
		PoolObject.LocalRotation = InRotation;
		PoolObject.Enabled = true;
		PoolData.ActiveCount++;
		return PoolObject;
	}

	public void Init( NetDictionary<EPoolObject, ObjectPoolData> InPrefabs ) { Pool = InPrefabs; }
	public void ReturnObjectToPool(EPoolObject InObjectName, GameObject InGameObject)
	{
		ObjectPoolData PoolData = Pool[InObjectName];
		InGameObject.Enabled = false;
		PoolData.Objects.Push(InGameObject);
		PoolData.ActiveCount--;
	}
	private void AllocatePoolObject(EPoolObject InObjectName) { Pool[InObjectName].Objects.Push(Pool[InObjectName].Prefab.Clone()); }
}
