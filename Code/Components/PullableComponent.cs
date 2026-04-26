using System;
using Sandbox.Managers;
namespace Sandbox.Components;

public class PullableComponent : Component, Component.ICollisionListener
{
	[Property] private bool IsInAirCheckRequired { get; set; }
	[Sync] private bool IsOwnerHolding { get; set; }
	[Sync] private float LastOwnerTime { get; set; }
	[Sync] private Guid LastOwnedBy { get; set; }
	private bool IsPulled { get; set; }
	private int PoolingHandle;
	private int ClearOwnerIdHandle;
	private float MoneyAmount = 1f;
	private const float ClearTime = 8f;
	private Rigidbody Rigidbody;
	
	public bool GetIsPulled() { return IsPulled; }
	public bool IsInAir() { return !IsInAirCheckRequired || MathF.Abs(Rigidbody.Velocity.z) > 0.1f; }
	public float GetMoneyAmount() { return MoneyAmount; }
	public float GetLastOwnerTime() { return LastOwnerTime; }
	public Guid GetOwnerId() { return LastOwnedBy; }
	// --------------------------------------------------------------PUBLIC METHODS--------------------------------------------------------------
	[Rpc.Broadcast]
	public void SetIsPulled(bool State)
	{
		IsPulled = State;
		if (IsPulled)
		{
			Rigidbody?.Gravity = false;
			PoolingHandle = TimerManager.AddTimer(PoolingHandle, 1f / 60f, true, PullingLogic);
		}
		else
		{
			if (Rigidbody != null)
			{
				Rigidbody.Gravity = true;
				Rigidbody.Velocity = 0f;
			}
			TimerManager.ClearTimer(PoolingHandle);
		}
	}
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
		if (OtherPullableComponent == null || OtherPullableComponent.GetLastOwnerTime() > LastOwnerTime) return;
		if (OtherPullableComponent.GetLastOwnerTime().AlmostEqual(LastOwnerTime) && OtherPullableComponent.IsOwnerHolding) return;
		OtherPullableComponent.SetOwnerId(LastOwnedBy, false);
	}
	// --------------------------------------------------------------PROTECTED METHODS--------------------------------------------------------------
	protected override void OnStart() { Rigidbody = GetComponent<Rigidbody>(); }
	protected override void OnUpdate()
	{
		base.OnUpdate();
		if (!IsOwnerHolding) LastOwnerTime -= Time.Delta;
	}
	// --------------------------------------------------------------PRIVATE METHODS--------------------------------------------------------------
	private void SetIsOwnerHoldingState(bool InState) { IsOwnerHolding = InState; }
	private void PullingLogic()
	{
		Rigidbody?.Velocity = Vector3.Up * 500f;
	}
	private void SetClearOwnerIdTimerInternally()
	{
		SetIsOwnerHoldingState(false);
		ClearOwnerIdHandle = TimerManager.AddTimer(ClearOwnerIdHandle, ClearTime, false, ClearOwnerId);
	}
}
