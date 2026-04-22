using System;
using Sandbox.Data;
using Sandbox.Enums;
namespace Sandbox.Managers;

public sealed class EventManager(Scene Scene) : GameObjectSystem(Scene)
{
	public static event Action<int> OnMoneyChanged;
	public static event Action<Guid, int> OnKill;
	public static event Func<Vector3> OnTeleportToSpawnLocation;
	public static event Action<EUi, bool> OnUiStateChanged;
	public static event Action<LeaderboardEntry, bool> OnPlayerConnectionChanged;
	
	public static Vector3 TriggerGetSpawnLocation()
	{
		return OnTeleportToSpawnLocation?.Invoke() ?? Vector3.Zero;
	}
	
	public static void TriggerOnKill(Guid InKillerPlayerId, int InMoneyAmount)
	{
		OnKill?.Invoke(InKillerPlayerId, InMoneyAmount);
	}
	public static void TriggerMoneyChange(int InMoney)
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
}
