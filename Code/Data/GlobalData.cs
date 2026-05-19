using System;
using Sandbox.Enums;
namespace Sandbox.Data;

public sealed class GlobalData : Component
{
	public static GlobalData Instance { get; private set; }
	[Property] private Dictionary<EUpgradeName, float> UpgradesValues { get; set; }
	[Property] private Dictionary<EUpgradeName, int> UpgradesMaxAmounts { get; set; }
	[Property] private Dictionary<EDefaultAttribute, float> DefaultAttributes { get; set; }

	public float GetProcessedUpgradeValue(int InUpgradeAmount, EUpgradeName InUpgradeName) { return Math.Clamp(InUpgradeAmount, 0, GetUpgradeMaxAmount(InUpgradeName)) * GetUpgradeValue(InUpgradeName); }
	public float GetUpgradeValue(EUpgradeName InUpgradeName) { return UpgradesValues.GetValueOrDefault(InUpgradeName, 0f); }
	public float GetDefaultAttributeValue(EDefaultAttribute InDefaultAttribute) { return DefaultAttributes.GetValueOrDefault(InDefaultAttribute, 0f); }
	
	protected override void OnAwake() { Instance = this; }
	protected override void OnDestroy() { if (Instance == this) Instance = null; }

	private int GetUpgradeMaxAmount(EUpgradeName InUpgradeName) { return UpgradesMaxAmounts.GetValueOrDefault(InUpgradeName, 0); }
}
