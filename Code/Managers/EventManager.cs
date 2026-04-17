using System;
using Sandbox.Data;
using Sandbox.Enums;
namespace Sandbox.Managers;

public sealed class EventManager(Scene Scene) : GameObjectSystem(Scene)
{
	public static event Action<LeaderboardEntry, bool> OnPlayerConnectionChanged;
	public static event Action<Vector3> OnPlayerLocationSend;
	public static event Action<EUi, bool> OnUiStateChange;
	
	public static void TriggerPlayerConnectionChange(LeaderboardEntry InLeaderboardEntry, bool InConnected)
	{
		OnPlayerConnectionChanged?.Invoke(InLeaderboardEntry, InConnected);
	}
	public static void TriggerUiStateChange(EUi InUi, bool RenderState)
	{
		OnUiStateChange?.Invoke(InUi, RenderState);
	}
	public static void TriggerPlayerLocationSend(Vector3 InPlayerLocation)
	{
		OnPlayerLocationSend?.Invoke(InPlayerLocation);
	}
}
