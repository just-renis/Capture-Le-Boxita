using System;
using Sandbox.Data;
using Sandbox.Enums;
using Sandbox.Managers;
namespace Sandbox.Components;

public class PullableComponent : Component, Component.ICollisionListener
{
	[Property] private bool IsInAirCheckRequired { get; set; }
	[Sync] private bool IsOwnerHolding { get; set; }
	[Sync] private float LastOwnerTime { get; set; }
	[Sync] private Guid LastOwnedBy { get; set; }
	private bool IsPulled;
	private int ClearOwnerIdHandle;
	private int CheckOutOfBoundsHandle;
	private const float BeamPullSpeed = 600.0f; 
	private const float ClearTime = 8f;
	private const Int64 PushForceMultiplier = 1250000;
	private Rigidbody Rigidbody;
	private GameObject AlienShipRef;
	
	public bool GetIsPulled() { return IsPulled; }
	public bool IsInAir() { return !IsInAirCheckRequired || MathF.Abs(Rigidbody.Velocity.z) > 0.1f; }
	public Guid GetOwnerId() { return LastOwnedBy; }
	// --------------------------------------------------------------PUBLIC METHODS--------------------------------------------------------------
	public void SetOwnerId(Guid InId, bool IsGrabbedByPlayer)
	{
		LastOwnerTime = ClearTime;
		LastOwnedBy = InId;
		SetIsOwnerHoldingState(true);
		if (!IsGrabbedByPlayer) SetClearOwnerIdTimerInternally();
		else TimerManager.ClearTimer(ClearOwnerIdHandle);
	}
	public void SetClearOwnerIdTimer()
	{
		SetIsOwnerHoldingState(false);
		ClearOwnerIdHandle = TimerManager.AddTimer(ClearOwnerIdHandle, ClearTime, false, ClearOwnerId);
	}
	public void ClearOwnerId() { LastOwnedBy = Guid.Empty; }
	public void OnCollisionStart(Collision InCollision)
	{
		if (GameObject.Tags.Has("player") || LastOwnedBy == Guid.Empty) return;
		GameObject CollisionObject = InCollision.Other.GameObject;
		if (!CollisionObject.Tags.Has("player") && !CollisionObject.Tags.Has("collectible")) return;
		PullableComponent OtherPullableComponent = CollisionObject.GetComponent<PullableComponent>();
		if (OtherPullableComponent == null) return;
		if (OtherPullableComponent.Tags.Has("player") && OtherPullableComponent.Network.OwnerId != LastOwnedBy)
		{
			Vector3 PushDir = (OtherPullableComponent.WorldPosition - WorldPosition).Normal;
			PushDir.z = 0;
			OtherPullableComponent.GetComponent<Player.Player>()?.ReceivePush(PushDir * PushForceMultiplier);
		}
		if (OtherPullableComponent.GetLastOwnerTime() > LastOwnerTime) return;
		if (OtherPullableComponent.GetLastOwnerTime().AlmostEqual(LastOwnerTime) && OtherPullableComponent.IsOwnerHolding) return;
		OtherPullableComponent.SetOwnerId(LastOwnedBy, false);
	}
	public void SetIsPulled(bool State)
	{
		IsPulled = State;
		if (IsPulled) Rigidbody?.Gravity = false;
		else
		{
			AlienShipRef = null;
			if (Rigidbody == null) return;
			Rigidbody.Gravity = true;
			Rigidbody.Velocity = 0f;
		}
	}
	// --------------------------------------------------------------PROTECTED METHODS--------------------------------------------------------------
	protected override void OnStart()
	{
		Rigidbody = GetComponent<Rigidbody>();
		CheckOutOfBoundsHandle = TimerManager.AddTimer(CheckOutOfBoundsHandle, 5f, true, CheckOutOfBounds);
	}
	protected override void OnUpdate()
	{
		if (!IsOwnerHolding) LastOwnerTime -= Time.Delta;
	}
	protected override void OnDestroy()
	{
		TimerManager.ClearTimer(CheckOutOfBoundsHandle);
	}
	protected override void OnFixedUpdate()
	{
		if (IsProxy || !IsPulled || AlienShipRef == null) return;
		Rigidbody.AngularVelocity = Vector3.Zero;
		Vector3 PullVelocity = (AlienShipRef.WorldPosition - WorldPosition) * 10.0f;
		Rigidbody.Velocity = PullVelocity.WithZ(BeamPullSpeed);
	}
	// --------------------------------------------------------------PRIVATE METHODS--------------------------------------------------------------
	private float GetLastOwnerTime() { return LastOwnerTime; }
	
	private void SetIsOwnerHoldingState(bool InState) { IsOwnerHolding = InState; }
	private void SetClearOwnerIdTimerInternally()
	{
		SetIsOwnerHoldingState(false);
		ClearOwnerIdHandle = TimerManager.AddTimer(ClearOwnerIdHandle, ClearTime, false, ClearOwnerId);
	}
	private void CheckOutOfBounds()
	{
		if (WorldPosition.z < 5000f) return;
		if (GetIsPulled()) SetIsPulled(false);
	}
	// --------------------------------------------------------------NETWORKING METHODS--------------------------------------------------------------
	[Rpc.Owner]
	public void SetAlienShipRef(Guid InAlienShipWhoPulls) { AlienShipRef = Scene.Directory.FindByGuid(InAlienShipWhoPulls); }
	
	[Rpc.Broadcast]
	public void ReceivePush(Guid InPusherId, Vector3 InPushForce)
	{
		if (IsProxy) return;
		SetOwnerId(InPusherId, false);
		Rigidbody?.ApplyImpulse(InPushForce);
	}
}
