using Sandbox.Components;
using Sandbox.Managers;
using Sandbox.UI;

public sealed class WorldButton : Component
{
	private bool InInteractionRange;
	[Property] private CustomTrigger Trigger { get; set; }
	[Property] private WorldButtonText UiText { get; set; }

	protected override void OnStart()
	{
		EventManager.OnPlayerInteractClicked += OnClicked;
		if (Trigger == null) return;
		Trigger.OnEnter += TriggerEntered;
		Trigger.OnExit += TriggerExited;
	}
	protected override void OnDestroy()
	{
		EventManager.OnPlayerInteractClicked -= OnClicked;
		if (Trigger == null) return;
		Trigger.OnEnter -= TriggerEntered;
		Trigger.OnExit -= TriggerExited;
	}
	
	private void OnClicked()
	{
		if (InInteractionRange) EventManager.TriggerVotingStart();
	}
	private void TriggerEntered(GameObject InGameObject)
	{
		if (InGameObject.IsProxy) return;
		UiText?.SetIsVisible(true);
		InInteractionRange = true;
	}
	private void TriggerExited(GameObject InGameObject)
	{
		if (InGameObject.IsProxy) return;
		UiText?.SetIsVisible(false);
		InInteractionRange = false;
	}
}
