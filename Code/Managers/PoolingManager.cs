using Sandbox.Enums;
namespace Sandbox.Managers;

public sealed class PoolingManager(Scene Scene) : GameObjectSystem(Scene)
{
	private class ObjectPoolData( GameObject InPrefab )
	{
		public readonly Stack<GameObject> Objects = new();
		public readonly GameObject Prefab = InPrefab;
	}
	
	private Dictionary<EPoolObject, ObjectPoolData> Pool;

	public void Init( Dictionary<EPoolObject, GameObject> InPrefabs )
	{
		Pool = new Dictionary<EPoolObject, ObjectPoolData>();
		foreach (KeyValuePair<EPoolObject, GameObject> Data in InPrefabs) Pool.Add(Data.Key, new ObjectPoolData(Data.Value));
	}
	public GameObject SpawnObjectFromPool( EPoolObject InObjectName, Vector3 InLocation, Rotation InRotation = default )
	{
		GameObject PoolObject;
		if (!Pool[InObjectName].Objects.Any())
		{
			AllocatePoolObject(InObjectName);
			PoolObject = Pool[InObjectName].Objects.Pop();
		}
		else PoolObject = Pool[InObjectName].Objects.Pop();
		PoolObject.LocalPosition = InLocation;
		PoolObject.LocalRotation = InRotation;
		PoolObject.Enabled = true;
		return PoolObject;
	}
	public void ReturnObjectToPool(EPoolObject InObjectName, GameObject InGameObject)
	{
		InGameObject.Enabled = false;
		Pool[InObjectName].Objects.Push(InGameObject);
	}
	private void AllocatePoolObject(EPoolObject InObjectName) { Pool[InObjectName].Objects.Push(Pool[InObjectName].Prefab.Clone()); }
}
