using Sandbox.Enums;
namespace Sandbox.Data;

public class Upgrade
{
	public EUpgradeName UpgradeName { get; set; }
	public EUpgradeValue UpgradeValue { get; set; }
	public int MaxAmount { get; set { field = value; AdjustCostsList(); } } = 1;
	public string Name { get; set; }
	public string Description { get; set; }
	public List<int> Costs { get; set; }
	public List<EUpgradeName> ParentsUpgradeNames { get; set; } = [];
	[ImageAssetPath]
	public string Icon { get; set; }
	public float X { get; set; }
	public float Y { get; set; }
	
	private void AdjustCostsList()
	{
		Costs ??= [];
		while (Costs.Count < MaxAmount) Costs.Add(0);
		while (Costs.Count > MaxAmount) Costs.RemoveAt(Costs.Count - 1);
	}
	public override string ToString()
	{
		return UpgradeName == EUpgradeName.None ? "New Upgrade" : UpgradeName.ToString();
	}
}
