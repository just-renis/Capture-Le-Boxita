using Sandbox.Managers;
namespace Sandbox.Components;

public sealed class SafeZoneTrigger : CustomTrigger
{
	protected override void OnStart()
	{
		EventManager.OnTeleportToSafeZoneLocation += GetLocation;
	}
	protected override void OnDestroy()
	{
		EventManager.OnTeleportToSafeZoneLocation -= GetLocation;
	}
	protected override void OnTriggerEnter(GameObject InGameObject) { InGameObject.Tags.Add("safe"); }
	protected override void OnTriggerExit(GameObject InGameObject) { InGameObject.Tags.Remove("safe"); }

	private Vector3 GetLocation() { return WorldPosition; }
}
