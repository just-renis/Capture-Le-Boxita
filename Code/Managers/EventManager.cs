using System;
using Sandbox.Enums;
namespace Sandbox.Managers;

public sealed class EventManager(Scene Scene) : GameObjectSystem(Scene)
{
	public event Action<float> OnPlayerClickStateChange;
	public event Action<Vector3> OnPlayerLocationSend;
	public event Action<EUi, bool> OnUiStateChange;
	
	public void TriggerPlayerClickState(float MouseClickState)
	{
		OnPlayerClickStateChange?.Invoke(MouseClickState);
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
