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
	protected override void OnTriggerEnter(GameObject InGameObject)
	{
		Log.Info("ADDED SAFE TAG ON THE " + InGameObject.Name);
		InGameObject.Tags.Add("safe");
	}
	protected override void OnTriggerExit(GameObject InGameObject)
	{
		Log.Info("REMOVED SAFE TAG ON THE " + InGameObject.Name);
		InGameObject.Tags.Remove("safe");
	}

	private Vector3 GetLocation() { return WorldPosition; }
}
