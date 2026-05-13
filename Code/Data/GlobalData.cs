using Sandbox.Enums;
using Sandbox.Managers;
namespace Sandbox.Data;

public sealed class GlobalData : Component
{
	public static GlobalData Instance { get; private set; }
	[Property] private Dictionary<EUpgradeName, float> Upgrades { get; set; } = new();
	[Property] private Dictionary<EDefaultAttribute, float> DefaultAttributes { get; set; } = new();

	public float GetUpgradeValue(EUpgradeName InUpgradeName) { return Upgrades.GetValueOrDefault(InUpgradeName, 0f); }
	public float GetDefaultAttributeValue(EDefaultAttribute InDefaultAttribute) { return DefaultAttributes.GetValueOrDefault(InDefaultAttribute, 0f); }
	
	protected override void OnAwake() { Instance = this; }
	protected override void OnDestroy() { if (Instance == this) Instance = null; }
}
