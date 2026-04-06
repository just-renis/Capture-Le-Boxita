using System;
using Sandbox.Enums;
namespace Sandbox.Managers;

public sealed class EventManager(Scene Scene) : GameObjectSystem(Scene)
{
	public event Action<bool> OnPlayerClickStateChange;
	public event Action<EUi, bool> OnUiStateChange;
	
	public void TriggerPlayerClickState(bool MouseClickState)
	{
		OnPlayerClickStateChange?.Invoke(MouseClickState);
	}
	public void TriggerUiStateChange(EUi InUi, bool RenderState)
	{
		OnUiStateChange?.Invoke(InUi, RenderState);
	}
}
