using System;
using Sandbox.Data;
using Sandbox.Enums;
using Sandbox.Interfaces;
namespace Sandbox.Managers;

public sealed class PoolingManager : Component
{
	public static PoolingManager Instance { get; private set; }
	[Sync, Property] private NetDictionary<EPoolObject, ObjectPoolData> Pool { get; set; } = new();

	public bool CanSpawnObject(EPoolObject InObjectName)
	{
		ObjectPoolData PoolData = Pool[InObjectName];
		return PoolData.ActiveCount < PoolData.MaxAmountActive;
	}
	public GameObject SpawnObjectFromPool(EPoolObject InObjectName, Vector3 InLocation, Rotation InRotation = default)
	{
		ObjectPoolData PoolData = Pool[InObjectName];
		if (PoolData.Objects.Count == 0) AllocatePoolObject(InObjectName);
		int LastIndex = PoolData.Objects.Count - 1;
		Guid ObjectGuid = PoolData.Objects[LastIndex];
		PoolData.Objects.RemoveAt(LastIndex);
		GameObject PoolObject = Scene.Directory.FindByGuid(ObjectGuid);
		PoolObject.WorldPosition = InLocation;
		PoolObject.WorldRotation = InRotation;
		PoolObject.Enabled = true;
		PoolObject.GetComponent<IPoolObject>().Init();
		PoolData.ActiveCount++;
		return PoolObject;
	}
	
	public void ReturnObjectToPool(EPoolObject InObjectName, GameObject InGameObject)
	{
		ObjectPoolData PoolData = Pool[InObjectName];
		InGameObject.Enabled = false;
		PoolData.Objects.Add(InGameObject.Id);
		PoolData.ActiveCount--;
	}
	
	protected override void OnAwake()
	{
		if (Instance != null && Instance != this)
		{
			GameObject.Destroy();
			return;
		}
		Instance = this;
	}
	protected override void OnDestroy()
	{
		if (Instance == this) Instance = null;
	}

	private void AllocatePoolObject(EPoolObject InObjectName)
	{
		GameObject PooledObject = Pool[InObjectName].Prefab.Clone();
		Pool[InObjectName].Objects.Add(PooledObject.Id);
		PooledObject.NetworkSpawn();
	}
}
