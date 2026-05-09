using System;
using Sandbox.Collectibles;
using Sandbox.Components;
using Sandbox.Enums;
using Sandbox.Managers;
namespace Sandbox.Player;

public sealed class Sword : Component
{
	[Property] private Int64 PushForce { get; set; }
	[Property] private float BoxPushMultiplier { get; set; } = 7f;
	private bool IsInAttackMode;
	private int ProcessAttackHandle;
	private int ProcessAttackTimes;
	private const int SwordMaxProcessTimes = 9;
	private const float SwordAttackRotationSpeed = 10f;
	private readonly HashSet<GameObject> HitObjects = [];
	
	private void ProcessAttackForward()
	{
		LocalRotation *= Rotation.From(new Angles(SwordAttackRotationSpeed, 0f, 0f));
		CheckForHit(Network.OwnerId);
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

	private void CheckForHit(Guid InPusherId)
	{
		Vector3 TraceSize = new (20f, 20f, 150f);
		SceneTraceResult TraceResults = Scene.Trace.Box(TraceSize, WorldPosition, WorldPosition).WithAnyTags("player", "collectible").IgnoreGameObjectHierarchy(GameObject.Root).Rotated(WorldRotation).Run();
		//DebugOverlay.Box(Vector3.Zero, TraceSize, Color.Red, 2f, WorldTransform);
		if (!TraceResults.Hit) return;
		GameObject Target = TraceResults.GameObject.Root;
		if (!HitObjects.Add(Target)) return;
		SendToHostAboutTheHit(Target, InPusherId, EventManager.TriggerGetPlayerData().GetUpgradeAmount(EUpgradeName.SwordPower1));
	}
	private void SendToHostAboutTheHit(GameObject Target, Guid InPusherId, int InPushForceUpgradeAmount)
	{
		if (!Network.IsOwner || !IsInAttackMode || Target.Tags.Has("safe")) return;
		PullableComponent TargetPullableComponent = Target.GetComponent<PullableComponent>();
		if (TargetPullableComponent == null) return;
		float MassForceMultiplier = TargetPullableComponent.Tags.Has("collectible") ? BoxPushMultiplier : 1f;
		Int64 Force = (long)(PushForce + PushForce * (float)InPushForceUpgradeAmount * 0.1f);
		TargetPullableComponent.ReceivePush(InPusherId, GameObject.Parent.WorldRotation.Forward * Force * MassForceMultiplier);
		TargetPullableComponent.SetOwnerId(Network.OwnerId, false);
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
