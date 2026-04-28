using Sandbox.Enums;
namespace Sandbox.Managers;

public sealed class UiManager(Scene Scene) : GameObjectSystem(Scene)
{
	private static readonly Dictionary<EUi, bool> UI_STATES = new()
	{
		{ EUi.UpgradesMenu, false }
	};

	public static void TriggerUiStateChange(EUi InUi)
	{
		bool NewRenderState = !UI_STATES[InUi];
		UI_STATES[InUi] = NewRenderState;
		EventManager.TriggerUiStateChange(InUi, NewRenderState);
	}
	public static void TriggerUiStateChange(EUi InUi, bool RenderState)
	{
		UI_STATES[InUi] = RenderState;
		EventManager.TriggerUiStateChange(InUi, RenderState);
	}
}
