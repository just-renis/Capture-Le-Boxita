using System;
using Sandbox.Data;
using Sandbox.Enums;
namespace Sandbox.Managers;

public sealed class EventManager(Scene Scene) : GameObjectSystem(Scene)
{
	public static event Action<bool> OnStuckPopupStateChanged;
	public static event Action<float> OnAlienBoxComboChanged;
	public static event Action<double> OnMoneyChanged;
	public static event Action<EUpgradeName> OnUpgradePurchased;
	public static event Action<EUi, bool> OnUiStateChanged;
	public static event Action<LeaderboardEntry, bool> OnPlayerConnectionChanged;
	public static event Action<Guid, bool, double> OnKill;
	public static event Func<Vector3> OnTeleportToSpawnLocation;
	public static event Func<Vector3> OnTeleportToSafeZoneLocation;
	public static event Func<PlayerData> OnPlayerDataRequired;
	
	public static void TriggerStuckPopupStateChange(bool InState)
	{
		OnStuckPopupStateChanged?.Invoke(InState);
	}
	public static void TriggerAlienBoxComboChange(float InCombo)
	{
		OnAlienBoxComboChanged?.Invoke(InCombo);
	}
	public static void TriggerMoneyChange(double InMoney)
	{
		OnMoneyChanged?.Invoke(InMoney);
	}
	public static void TriggerUpgradePurchased(EUpgradeName InUpgradeName)
	{
		OnUpgradePurchased?.Invoke(InUpgradeName);
	}
	public static void TriggerUiStateChange(EUi InUi, bool RenderState)
	{
		OnUiStateChanged?.Invoke(InUi, RenderState);
	}
	public static void TriggerPlayerConnectionChange(LeaderboardEntry InLeaderboardEntry, bool InConnected)
	{
		OnPlayerConnectionChanged?.Invoke(InLeaderboardEntry, InConnected);
	}
	public static void TriggerOnKill(Guid InKillerPlayerId, bool InIsPlayerKill, double InMoneyAmount)
	{
		OnKill?.Invoke(InKillerPlayerId, InIsPlayerKill, InMoneyAmount);
	}
	public static Vector3 TriggerGetSpawnLocation()
	{
		return OnTeleportToSpawnLocation?.Invoke() ?? Vector3.Zero;
	}
	public static Vector3 TriggerGetSafeZoneLocation()
	{
		return OnTeleportToSafeZoneLocation?.Invoke() ?? Vector3.Zero;
	}
	public static PlayerData TriggerGetPlayerData()
	{
		return OnPlayerDataRequired?.Invoke();
	}
	/*public override void Dispose()
	{
		OnMoneyChanged = null;
		OnSendAlienShipCenterLocation = null;
		OnKill = null;
		OnUiStateChanged = null;
		OnPlayerConnectionChanged = null;
		OnTeleportToSpawnLocation = null;
		OnPlayerDataRequired = null;
		base.Dispose();
	}*/
}
