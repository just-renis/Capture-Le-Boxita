using System;
using Sandbox.Data;
using Sandbox.Enums;
namespace Sandbox.Managers;

public sealed class EventManager(Scene Scene) : GameObjectSystem(Scene)
{
	public event Action<LeaderboardEntry, bool> OnPlayerConnectionChanged;
	public event Action<Vector3> OnPlayerLocationSend;
	public event Action<EUi, bool> OnUiStateChange;
	
	public void TriggerPlayerConnectionChange(LeaderboardEntry InLeaderboardEntry, bool InConnected)
	{
		OnPlayerConnectionChanged?.Invoke(InLeaderboardEntry, InConnected);
	}
	public void TriggerUiStateChange(EUi InUi, bool RenderState)
	{
		OnUiStateChange?.Invoke(InUi, RenderState);
	}
	public void TriggerPlayerLocationSend(Vector3 InPlayerLocation)
	{
		OnPlayerLocationSend?.Invoke(InPlayerLocation);
	}
}
