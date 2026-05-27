using Sandbox.Enums;
namespace Sandbox.Managers;

public sealed class UiManager(Scene Scene) : GameObjectSystem(Scene)
{
	private static readonly Dictionary<EUi, bool> UI_STATES = new();

	public static bool IsAnyUiOpen() { return UI_STATES.Values.Any(State => State); }
	
	public static void ChangeUiState(EUi InUi)
	{
		bool NewRenderState = !UI_STATES[InUi];
		UI_STATES[InUi] = NewRenderState;
		EventManager.TriggerUiStateChange(InUi, NewRenderState);
		SoundManager.Instance?.Play2D(ESound.OpenCloseUi);
	}
	public static void ChangeUiState(EUi InUi, bool RenderState)
	{
		UI_STATES[InUi] = RenderState;
		EventManager.TriggerUiStateChange(InUi, RenderState);
		SoundManager.Instance?.Play2D(ESound.OpenCloseUi);
	}
	public static void OnStart()
	{
		UI_STATES.TryAdd(EUi.UpgradesMenu, false);
		UI_STATES.TryAdd(EUi.SettingsMenu, false);
		UI_STATES.TryAdd(EUi.QuestMenu, false);
	}
	public static void OnDestroy()
	{
		UI_STATES.Clear();
	}
}
