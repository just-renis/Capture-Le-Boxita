using System;
namespace Sandbox.Data;

public sealed class ObjectPoolData
{
	public GameObject Prefab { get; set; }
	public int MaxAmountActive { get; set; }
	
	[Hide] public List<GameObject> InactiveObjects { get; set; } = [];
	[Hide] public List<GameObject> ActiveObjects { get; set; } = [];
	[Hide] public int ActiveCount { get; set; }
}
