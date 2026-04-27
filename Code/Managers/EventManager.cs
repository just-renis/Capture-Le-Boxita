using System;
using Sandbox.Data;
using Sandbox.Enums;
namespace Sandbox.Managers;

public sealed class EventManager(Scene Scene) : GameObjectSystem(Scene)
{
	public static event Action<float> OnMoneyChanged;
	public static event Action<EUi, bool> OnUiStateChanged;
	public static event Action<LeaderboardEntry, bool> OnPlayerConnectionChanged;
	public static event Action<Guid, bool, float> OnKill;
	public static event Func<Vector3> OnTeleportToSpawnLocation;
	public static event Func<PlayerData> OnPlayerDataRequired;
	
	public static void TriggerMoneyChange(float InMoney)
	{
		OnMoneyChanged?.Invoke(InMoney);
	}
	public static void TriggerUiStateChange(EUi InUi, bool RenderState)
	{
		OnUiStateChanged?.Invoke(InUi, RenderState);
	}
	public static void TriggerPlayerConnectionChange(LeaderboardEntry InLeaderboardEntry, bool InConnected)
	{
		OnPlayerConnectionChanged?.Invoke(InLeaderboardEntry, InConnected);
	}
	public static void TriggerOnKill(Guid InKillerPlayerId, bool InIsPlayerKill, float InMoneyAmount)
	{
		OnKill?.Invoke(InKillerPlayerId, InIsPlayerKill, InMoneyAmount);
	}
	public static Vector3 TriggerGetSpawnLocation()
	{
		return OnTeleportToSpawnLocation?.Invoke() ?? Vector3.Zero;
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
