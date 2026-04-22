using Sandbox.Managers;
namespace Sandbox.Player;

public sealed class Sword : Component
{
	[Property] private float PushForce { get; set; }
	private bool IsInAttackMode;
	private int ProcessAttackHandle;
	private int ProcessAttackTimes;
	private BoxCollider Collider;
	private readonly HashSet<GameObject> HitObjects = [];
	
	protected override void OnStart()
	{
		base.OnStart();
		Collider = GetComponentInChildren<BoxCollider>();
	}

	[Rpc.Broadcast]
	public void Attack()
	{
		if (IsInAttackMode) return;
		HitObjects.Clear();
		IsInAttackMode = true;
		ProcessAttackHandle = TimerManager.AddTimer(ProcessAttackHandle, 0.025f, true, ProcessAttackForward);
	}
	private void ProcessAttackForward()
	{
		LocalRotation *= Rotation.From(new Angles(5f, 0f, 0f));
		foreach (Collider InTouchCollider in Collider.Touching)
		{
			if (!InTouchCollider.GameObject.Tags.Has("player") || !HitObjects.Add(InTouchCollider.GameObject.Root)) continue;
			InTouchCollider.GameObject.Root.GetComponent<Rigidbody>()?.ApplyImpulse(GameObject.Parent.WorldRotation.Forward * PushForce);
		}
		if (++ProcessAttackTimes <= 18) return;
		ProcessAttackTimes = 0;
		TimerManager.ClearTimer(ProcessAttackHandle);
		ProcessAttackHandle = TimerManager.AddTimer(ProcessAttackHandle, 0.025f, true, ProcessAttackBackward);
	}
	private void ProcessAttackBackward()
	{
		LocalRotation *= Rotation.From(new Angles(-5f, 0f, 0f));
		if (++ProcessAttackTimes <= 18) return;
		ProcessAttackTimes = 0;
		TimerManager.ClearTimer(ProcessAttackHandle);
		IsInAttackMode = false;
	}
}
