using System;
using Sandbox.Enums;
namespace Sandbox.Managers;

public sealed class UiManager(Scene Scene) : GameObjectSystem(Scene)
{
	private Dictionary<EUi, bool> UiStates = new()
	{
		{ EUi.UpgradesMenu, false }
	};
	private EventManager EventManager;

	public void OnStart()
	{
		EventManager = Scene.GetSystem<EventManager>();
	}
	public void TriggerUiStateChange(EUi InUi)
	{
		bool NewRenderState = !UiStates[InUi];
		UiStates[InUi] = NewRenderState;
		EventManager.TriggerUiStateChange( InUi, NewRenderState );
	}
	public void TriggerUiStateChange(EUi InUi, bool RenderState)
	{
		UiStates[InUi] = RenderState;
		EventManager.TriggerUiStateChange( InUi, RenderState );
	}
}
