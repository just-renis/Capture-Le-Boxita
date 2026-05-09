namespace Sandbox.Components;

public class CustomTrigger : Component
{
	[Property] protected Vector3 Size { get; set; } = new (100);
	[Property, Group("IsSetByTags")] protected bool TriggerByTags { get; set; }
	[Property, Group("IsSetByTags")] 
	[ShowIf(nameof(TriggerByTags), true)]
	protected string[] TriggerTags { get; set; }

	private BBox OverlapBounds;
	private readonly HashSet<GameObject> Occupants = [];

	protected override void OnStart()
	{
		OverlapBounds = BBox.FromPositionAndSize(WorldPosition, Size);
	}
	protected override void OnFixedUpdate()
	{
		if (!Networking.IsHost) return;
		IEnumerable<GameObject> CurrentlyTouching;
		if (TriggerByTags)
		{
			CurrentlyTouching = Scene.FindInPhysics(OverlapBounds)
				.Select(X => X.Root)
				.Where(X => TriggerTags.Any(Tag => X.Tags.Has(Tag)))
				.Distinct();
		}
		else
		{
			CurrentlyTouching = Scene.FindInPhysics(OverlapBounds)
				.Select(X => X.Root)
				.Distinct();
		}
		IEnumerable<GameObject> GameObjects = CurrentlyTouching.ToList();
		foreach (GameObject Object in GameObjects)
		{
			if (Occupants.Add(Object)) OnTriggerEnter(Object);
		}
		Occupants.RemoveWhere(Object => 
		{
			if (GameObjects.Contains(Object)) return false;
			OnTriggerExit(Object);
			return true;
		});
	}
	protected virtual void OnTriggerEnter(GameObject InGameObject) { }
	protected virtual void OnTriggerExit(GameObject InGameObject) { }
}
