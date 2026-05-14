using System;
namespace Sandbox.Components;

public enum TriggerShape
{
	Box,
	Capsule
}

public class CustomTrigger : Component
{
	[Property] public TriggerShape Shape { get; set; } = TriggerShape.Box;
	[Property, Group("IsSetByTags")] public bool TriggerByTags { get; set; }
	[Property, Group("IsSetByTags")] 
	[ShowIf(nameof(TriggerByTags), true)]
	public string[] TriggerTags { get; set; }
	[Property, ShowIf( nameof( Shape ), TriggerShape.Box )] 
	public Vector3 Size { get; set; } = new( 100 );
	[Property, ShowIf( nameof( Shape ), TriggerShape.Capsule )] 
	public float CapsuleRadius { get; set; } = 50f;
	[Property, ShowIf( nameof( Shape ), TriggerShape.Capsule )] 
	public float CapsuleHeight { get; set; } = 100f;
	public float DefaultCapsuleHeight;
	public float DefaultCapsuleRadius;
	public Action<GameObject> OnEnter;
	public Action<GameObject> OnExit;
	
	private readonly HashSet<GameObject> Occupants = [];
	private readonly HashSet<GameObject> CurrentFrameOccupants = [];

	protected override void OnFixedUpdate()
	{
		if (!Networking.IsHost) return;
		// ----------------------------------------------------DEBUG-----------------------------------------------------
		/*
		Vector3 HalfHeight = WorldRotation.Up * (CapsuleHeight * 0.5f);
		Vector3 Top = WorldPosition + HalfHeight;
		Vector3 Bottom = WorldPosition - HalfHeight;

		if (Shape == TriggerShape.Box) 
		{
			DebugOverlay.Box( BBox.FromPositionAndSize( WorldPosition, Size ), Color.Cyan, Time.Delta );
		}
		else
		{
			Capsule Capsule = new (Bottom, Top, CapsuleRadius);
			DebugOverlay.Capsule(Capsule, color: Color.Cyan, duration: Time.Delta );
		}*/
		SceneTrace Trace = BuildTrace();
		IEnumerable<SceneTraceResult> Hits = Trace.RunAll();
		CurrentFrameOccupants.Clear();
		foreach (SceneTraceResult Hit in Hits)
		{
			GameObject Root = Hit.GameObject.Root;
			if (!Root.IsValid()) continue;
			if (TriggerByTags)
			{
				bool HasValidTag = false;
				foreach (string Tag in TriggerTags)
				{
					if (!Root.Tags.Has(Tag)) continue;
					HasValidTag = true;
					break;
				}
				if (!HasValidTag) continue;
			}

			CurrentFrameOccupants.Add(Root);
		}
		foreach (GameObject Object in CurrentFrameOccupants)
		{
			if (!Occupants.Add(Object)) continue;
			OnTriggerEnter(Object);
			OnEnter?.Invoke(Object);
		}
		Occupants.RemoveWhere(Object => 
		{
			if (!Object.IsValid() || !CurrentFrameOccupants.Contains(Object))
			{
				OnTriggerExit(Object);
				OnExit?.Invoke(Object);
				return true;
			}
			return false;
		});
	}

	private SceneTrace BuildTrace()
	{
		if (Shape == TriggerShape.Box) return Scene.Trace.Ray(WorldPosition, WorldPosition).Size(Size);
		Vector3 HalfHeight = WorldRotation.Up * (CapsuleHeight * 0.5f);
		Vector3 Start = WorldPosition + HalfHeight;
		Vector3 End = WorldPosition - HalfHeight;
		return Scene.Trace.Ray(Start, End).Radius(CapsuleRadius);
		/*if (!Networking.IsHost) return;
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
		});*/
	}
	protected virtual void OnTriggerEnter(GameObject InGameObject) { }
	protected virtual void OnTriggerExit(GameObject InGameObject) { }
}
