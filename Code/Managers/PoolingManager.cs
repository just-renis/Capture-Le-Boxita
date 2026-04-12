using System;
using Sandbox.Data;
using Sandbox.Enums;
namespace Sandbox.Managers;

public sealed class PoolingManager(Scene Scene) : GameObjectSystem(Scene)
{
	[Sync] private NetDictionary<EPoolObject, ObjectPoolData> Pool { get; set; } = new();

	public bool CanSpawnObject(EPoolObject InObjectName)
	{
		ObjectPoolData PoolData = Pool[InObjectName];
		return PoolData.ActiveCount < PoolData.MaxAmountActive;
	}
	public GameObject SpawnObjectFromPool(EPoolObject InObjectName, Vector3 InLocation, Rotation InRotation = default)
	{
		ObjectPoolData PoolData = Pool[InObjectName];
		if (!PoolData.Objects.Any()) AllocatePoolObject(InObjectName);
		Guid ObjectId = PoolData.Objects.Pop();
		GameObject PoolObject = Scene.Directory.FindByGuid(ObjectId);
		PoolObject.LocalPosition = InLocation;
		PoolObject.LocalRotation = InRotation;
		PoolObject.Enabled = true;
		PoolData.ActiveCount++;
		return PoolObject;
	}

	public void Init(NetDictionary<EPoolObject, ObjectPoolData> InPrefabs) { Pool = InPrefabs; }
	public void ReturnObjectToPool(EPoolObject InObjectName, GameObject InGameObject)
	{
		ObjectPoolData PoolData = Pool[InObjectName];
		InGameObject.Enabled = false;
		PoolData.Objects.Push(InGameObject.Id);
		PoolData.ActiveCount--;
	}

	private void AllocatePoolObject(EPoolObject InObjectName)
	{
		GameObject PooledObject = Pool[InObjectName].Prefab.Clone();
		Pool[InObjectName].Objects.Push(PooledObject.Id);
		PooledObject.Network.SetOwnerTransfer( OwnerTransfer.Takeover );
		PooledObject.NetworkSpawn();
		PooledObject.Network.DropOwnership();
	}
}
