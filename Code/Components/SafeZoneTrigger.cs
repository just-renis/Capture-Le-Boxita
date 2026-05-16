using Sandbox.Managers;
namespace Sandbox.Components;

public sealed class SafeZoneTrigger : CustomTrigger
{
	protected override void OnStart()
	{
		base.OnStart();
		EventManager.OnTeleportToSafeZoneLocation += GetLocation;
	}
	protected override void OnDestroy()
	{
		EventManager.OnTeleportToSafeZoneLocation -= GetLocation;
	}

	private Vector3 GetLocation() { return WorldPosition; }
	
	[Rpc.Broadcast]
	protected override void OnTriggerEnter(GameObject InGameObject)
	{
		InGameObject.Tags.Add("safe");
	}
	[Rpc.Broadcast]
	protected override void OnTriggerExit(GameObject InGameObject)
	{
		InGameObject.Tags.Remove("safe");
	}
}
