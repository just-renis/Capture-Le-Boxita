using System;
using Sandbox.Data;
using Sandbox.Enums;
using Sandbox.Interfaces;
namespace Sandbox.Managers;

public sealed class PoolingManager : Component
{
	public static PoolingManager Instance { get; private set; }
	[Property] private Dictionary<EPoolObject, ObjectPoolData> Pool { get; set; } = new();

	public bool CanSpawnObject(EPoolObject InObjectName)
	{
		if (!Pool.TryGetValue(InObjectName, out ObjectPoolData PoolData)) return false;
		return PoolData.ActiveCount < PoolData.MaxAmountActive;
	}
	public GameObject SpawnObjectFromPool(EPoolObject InObjectName, Vector3 InLocation, Rotation InRotation = default)
	{
		ObjectPoolData PoolData = Pool[InObjectName];
		if (PoolData.InactiveObjects.Count == 0) AllocatePoolObject(InObjectName);
		int LastIndex = PoolData.InactiveObjects.Count - 1;
		GameObject PoolObject = PoolData.InactiveObjects[LastIndex];
		PoolData.InactiveObjects.RemoveAt(LastIndex);
		PoolData.ActiveObjects.Add(PoolObject);
		PoolObject.WorldPosition = InLocation;
		PoolObject.WorldRotation = InRotation;
		PoolObject.Enabled = true;
		PoolObject.GetComponent<IPoolObject>().Init();
		PoolData.ActiveCount++;
		return PoolObject;
	}
	
	public void ReturnObjectToPool(EPoolObject InObjectName, GameObject InGameObject)
	{
		if (InGameObject == null) return;
		ObjectPoolData PoolData = Pool[InObjectName];
		InGameObject.Enabled = false;
		PoolData.ActiveObjects.Remove(InGameObject);
		PoolData.InactiveObjects.Add(InGameObject);
		PoolData.ActiveCount--;
	}
	public void ResetPoolType(EPoolObject InObjectName)
	{
		if (!Pool.TryGetValue(InObjectName, out ObjectPoolData PoolData)) return;
		foreach (GameObject Obj in PoolData.ActiveObjects) Obj?.Destroy();
		PoolData.ActiveObjects.Clear();
		foreach (GameObject Obj in PoolData.InactiveObjects) Obj?.Destroy();
		PoolData.InactiveObjects.Clear();
		PoolData.ActiveCount = 0;
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
		Pool[InObjectName].InactiveObjects.Add(PooledObject);
		PooledObject.NetworkSpawn();
	}
}
