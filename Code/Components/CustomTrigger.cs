using System;
using Sandbox.Managers;
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
	private int RunTraceHandle;
	
	private readonly HashSet<GameObject> Occupants = [];
	private readonly HashSet<GameObject> CurrentFrameOccupants = [];
	private readonly List<GameObject> RemoveOccupants = [];

	protected override void OnStart()
	{
		RunTraceHandle = TimerManager.AddTimer(RunTraceHandle, 0.1f, true, RunTraceCheck);
	}
	protected override void OnDestroy()
	{
		TimerManager.ClearTimer(RunTraceHandle);
	}
	protected virtual void OnTriggerEnter(GameObject InGameObject) { }
	protected virtual void OnTriggerExit(GameObject InGameObject) { }
	
	private SceneTrace BuildTrace()
	{
		SceneTrace Trace;
		if (Shape == TriggerShape.Box) Trace = Scene.Trace.Ray(WorldPosition, WorldPosition).Size(Size);
		else
		{
			Vector3 HalfHeight = WorldRotation.Up * (CapsuleHeight * 0.5f);
			Vector3 Start = WorldPosition + HalfHeight;
			Vector3 End = WorldPosition - HalfHeight;
			Trace = Scene.Trace.Ray(Start, End).Radius(CapsuleRadius);
		}
		if (TriggerByTags && TriggerTags != null) Trace = Trace.WithAnyTags(TriggerTags);
		return Trace;
	}

	private void RunTraceCheck()
	{
		if (!Networking.IsHost) return;
		// ----------------------------------------------------DEBUG-----------------------------------------------------
		Vector3 HalfHeight = WorldRotation.Up * (CapsuleHeight * 0.5f);
		Vector3 Top = WorldPosition + HalfHeight;
		Vector3 Bottom = WorldPosition - HalfHeight;

		if (Shape == TriggerShape.Box)
		{
			DebugOverlay.Box(BBox.FromPositionAndSize(WorldPosition, Size), Color.Cyan, 0.1f);
		}
		else
		{
			Capsule Capsule = new (Bottom, Top, CapsuleRadius);
			DebugOverlay.Capsule(Capsule, Color.Cyan, 0.1f);
		}
		RemoveOccupants.Clear();
		CurrentFrameOccupants.Clear();
		SceneTrace Trace = BuildTrace();
		IEnumerable<SceneTraceResult> Hits = Trace.RunAll();
		foreach (SceneTraceResult Hit in Hits)
		{
			if (!Hit.Hit) continue;
			GameObject Root = Hit.GameObject.Root;
			if (!Root.IsValid()) continue;
			CurrentFrameOccupants.Add(Root);
		}
		foreach (GameObject Object in CurrentFrameOccupants)
		{
			if (!Occupants.Add(Object)) continue;
			OnTriggerEnter(Object);
			OnEnter?.Invoke(Object);
		}
		foreach (GameObject Occupant in Occupants)
		{
			if (!Occupant.IsValid() || !CurrentFrameOccupants.Contains(Occupant)) RemoveOccupants.Add(Occupant);
		}
		foreach (GameObject Occupant in RemoveOccupants)
		{
			OnTriggerExit(Occupant);
			OnExit?.Invoke(Occupant);
			Occupants.Remove(Occupant);
		}
	}
}
