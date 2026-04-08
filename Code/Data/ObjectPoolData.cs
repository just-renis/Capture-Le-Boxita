namespace Sandbox.Data;

public sealed class ObjectPoolData
{
	public GameObject Prefab { get; set; }
	public int MaxAmountActive { get; set; }
	
	[Hide] public readonly Stack<GameObject> Objects = new();
	[Hide] public int ActiveCount;
}
