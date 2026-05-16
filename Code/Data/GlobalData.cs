using System;
using Sandbox.Enums;
namespace Sandbox.Data;

public sealed class GlobalData : Component
{
	public struct GlobalUpgradeData
	{
		public int MaxAmount;
		public float Value;
	}
	
	public static GlobalData Instance { get; private set; }
	[Property] private Dictionary<EUpgradeName, GlobalUpgradeData> Upgrades { get; set; } = new();
	[Property] private Dictionary<EDefaultAttribute, float> DefaultAttributes { get; set; } = new();

	public float GetProcessedUpgradeValue(int InUpgradeAmount, EUpgradeName InUpgradeName)
	{
		return Math.Clamp(InUpgradeAmount, 0, GetUpgradeMaxAmount(InUpgradeName)) * GetUpgradeValue(InUpgradeName);
	}
	public float GetUpgradeValue(EUpgradeName InUpgradeName) { return Upgrades.TryGetValue(InUpgradeName, out GlobalUpgradeData UpgradeData) ? UpgradeData.Value : 0f; }
	public float GetDefaultAttributeValue(EDefaultAttribute InDefaultAttribute) { return DefaultAttributes.GetValueOrDefault(InDefaultAttribute, 0f); }
	
	protected override void OnAwake() { Instance = this; }
	protected override void OnDestroy() { if (Instance == this) Instance = null; }

	private int GetUpgradeMaxAmount(EUpgradeName InUpgradeName) { return Upgrades.TryGetValue(InUpgradeName, out GlobalUpgradeData UpgradeData) ? UpgradeData.MaxAmount : 0; }
}
