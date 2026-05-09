namespace Sandbox.Components;

public sealed class SafeZoneTrigger : CustomTrigger
{
	protected override void OnTriggerEnter(GameObject InGameObject) { InGameObject.Tags.Add("safe"); }
	protected override void OnTriggerExit(GameObject InGameObject) { InGameObject.Tags.Remove("safe"); }
}
