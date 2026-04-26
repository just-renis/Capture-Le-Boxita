using System;
using Sandbox.Components;
using Sandbox.Managers;
namespace Sandbox.Player;

public sealed class Sword : Component
{
	[Property] private float PushForce { get; set; }
	private bool IsInAttackMode;
	private int ProcessAttackHandle;
	private int ProcessAttackTimes;
	private const int SwordMaxProcessTimes = 9;
	private const float SwordAttackRotationSpeed = 10f;
	private BoxCollider Collider;
	private readonly HashSet<GameObject> HitObjects = [];
	
	protected override void OnStart()
	{
		base.OnStart();
		Collider = GetComponentInChildren<BoxCollider>();
	}
	
	private void ProcessAttackForward()
	{
		LocalRotation *= Rotation.From(new Angles(SwordAttackRotationSpeed, 0f, 0f));
		ApplyPushForce(Network.OwnerId);
		if (++ProcessAttackTimes <= SwordMaxProcessTimes) return;
		ProcessAttackTimes = 0;
		TimerManager.ClearTimer(ProcessAttackHandle);
		ProcessAttackHandle = TimerManager.AddTimer(ProcessAttackHandle, 0.025f, true, ProcessAttackBackward);
	}
	private void ProcessAttackBackward()
	{
		LocalRotation *= Rotation.From(new Angles(-SwordAttackRotationSpeed, 0f, 0f));
		if (++ProcessAttackTimes <= SwordMaxProcessTimes) return;
		ProcessAttackTimes = 0;
		TimerManager.ClearTimer(ProcessAttackHandle);
		IsInAttackMode = false;
	}

	[Rpc.Host]
	private void ApplyPushForce(Guid InPusherId)
	{
		foreach (Collider InTouchCollider in Collider.Touching)
		{
			GameObject Target = InTouchCollider.GameObject.Root;
			if (!InTouchCollider.GameObject.Tags.Has("player") || !HitObjects.Add(Target)) continue;
			Target.GetComponent<Player>()?.ReceivePush(InPusherId, GameObject.Parent.WorldRotation.Forward * PushForce);
			Target.GetComponent<PullableComponent>()?.SetOwnerId(Network.OwnerId, false);
		}
	}
	[Rpc.Broadcast]
	public void Attack()
	{
		if (IsInAttackMode) return;
		HitObjects.Clear();
		IsInAttackMode = true;
		ProcessAttackHandle = TimerManager.AddTimer(ProcessAttackHandle, 0.025f, true, ProcessAttackForward);
	}
}
