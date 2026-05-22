using System;
using Sandbox.Components;
using Sandbox.Data;
using Sandbox.Enums;
using Sandbox.Managers;
namespace Sandbox.Player;

public sealed class Sword : Component
{
	[Property] private Int64 PushForce { get; set; }
	[Property] private float BoxPushMultiplier { get; set; } = 7f;
	private bool IsInAttackMode;
	private int ProcessForwardAttackHandle;
	private int ProcessBackwardAttackHandle;
	private float CurrentAttackAngle;
	private const float TargetMaxAngle = 90f;
	private const float SwordAttackRotationSpeed = 10f;
	private readonly HashSet<GameObject> HitObjects = [];

	public bool GetIsInAttackMode() { return IsInAttackMode; }

	protected override void OnDestroy()
	{
		TimerManager.ClearTimer(ProcessForwardAttackHandle);
		TimerManager.ClearTimer(ProcessBackwardAttackHandle);
	}

	private void ProcessAttackForward()
	{
		CurrentAttackAngle += SwordAttackRotationSpeed;
		LocalRotation *= Rotation.From(new Angles(SwordAttackRotationSpeed, 0f, 0f));
		CheckForHit(Network.OwnerId);
		if (CurrentAttackAngle < TargetMaxAngle) return;
		TimerManager.ClearTimer(ProcessForwardAttackHandle);
		ProcessBackwardAttackHandle = TimerManager.AddTimer(ProcessBackwardAttackHandle, 0.025f, true, ProcessAttackBackward);
	}
	private void ProcessAttackBackward()
	{
		CurrentAttackAngle -= SwordAttackRotationSpeed;
		LocalRotation *= Rotation.From(new Angles(-SwordAttackRotationSpeed, 0f, 0f));
		if (CurrentAttackAngle > 0f) return;
		TimerManager.ClearTimer(ProcessBackwardAttackHandle);
		IsInAttackMode = false;
	}
	private void CheckForHit(Guid InPusherId)
	{
		Vector3 TraceSize = new (20f, 20f, 80f);
		Vector3 TraceCenter = WorldPosition + WorldRotation.Up * 35f;
		SceneTraceResult TraceResults = Scene.Trace.Box(TraceSize, TraceCenter, TraceCenter).WithAnyTags("player", "collectible").IgnoreGameObjectHierarchy(GameObject.Root).Rotated(WorldRotation).Run();
		//DebugOverlay.Box(Vector3.Zero, TraceSize, Color.Red, 5f, WorldTransform.WithPosition(TraceCenter));
		if (!TraceResults.Hit) return;
		GameObject Target = TraceResults.GameObject.Root;
		if (!HitObjects.Add(Target)) return;
		GlobalData GlobalInstance = GlobalData.Instance;
		float SwordPower1 = GlobalInstance.GetProcessedUpgradeValue(EventManager.TriggerGetPlayerData().GetUpgradeAmount(EUpgradeName.SwordPower1), EUpgradeName.SwordPower1);
		float SwordPower2 = GlobalInstance.GetProcessedUpgradeValue(EventManager.TriggerGetPlayerData().GetUpgradeAmount(EUpgradeName.SwordPower2), EUpgradeName.SwordPower2);
		SendToHostAboutTheHit(Target, InPusherId, SwordPower1 + SwordPower2);
	}
	private void SendToHostAboutTheHit(GameObject Target, Guid InPusherId, float InPushForceUpgradeAmount)
	{
		if (!Network.IsOwner || !IsInAttackMode || Target.Tags.Has("safe")) return;
		PullableComponent TargetPullableComponent = Target.GetComponent<PullableComponent>();
		if (TargetPullableComponent == null) return;
		float MassForceMultiplier = TargetPullableComponent.Tags.Has("collectible") ? BoxPushMultiplier : 1f;
		Int64 Force = (long)(PushForce + PushForce * InPushForceUpgradeAmount * 0.01f);
		SoundManager.Instance?.PlayAtPosition(ESound.SwordHit, GameObject.WorldPosition);
		TargetPullableComponent.ReceivePush(InPusherId, GameObject.Parent.WorldRotation.Forward * Force * MassForceMultiplier);
		TargetPullableComponent.SetOwnerId(Network.OwnerId, false);
	}
	
	[Rpc.Broadcast]
	public void Attack()
	{
		if (IsInAttackMode) return;
		HitObjects.Clear();
		IsInAttackMode = true;
		CurrentAttackAngle = 0f;
		ProcessForwardAttackHandle = TimerManager.AddTimer(ProcessForwardAttackHandle, 0.025f, true, ProcessAttackForward);
	}
}
