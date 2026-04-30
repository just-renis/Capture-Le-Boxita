using System;
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
	private float MoneyAmount = 1f;
	private const float BeamPullSpeed = 600.0f; 
	private const float ClearTime = 8f;
	private const float PushForceMultiplier = 200000f;
	private Rigidbody Rigidbody;
	private GameObject AlienShipRef;
	private Player.Player OwnerPlayer;
	
	public bool GetIsPulled() { return IsPulled; }
	public bool IsInAir() { return !IsInAirCheckRequired || MathF.Abs(Rigidbody.Velocity.z) > 0.1f; }
	public float GetMoneyAmount() { return MoneyAmount; }
	public float GetLastOwnerTime() { return LastOwnerTime; }
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
	public void SetOwnerPlayer(Player.Player InOwnerPlayer) { OwnerPlayer = InOwnerPlayer; }
	// --------------------------------------------------------------PROTECTED METHODS--------------------------------------------------------------
	protected override void OnStart() { Rigidbody = GetComponent<Rigidbody>(); }
	protected override void OnUpdate()
	{
		if (!IsOwnerHolding) LastOwnerTime -= Time.Delta;
	}
	protected override void OnFixedUpdate()
	{
		if (IsProxy) return;
		if (!IsPulled && Tags.Has("player"))
		{
		//	Log.Info("ROTATE " + Id);
			WorldRotation = Rotation.FromYaw(OwnerPlayer.GetCurrentCameraRotationYaw());
			return;
		}
		if (AlienShipRef == null) return;
		//Log.Info("STOP ROTATE " + Id);
		Vector3 PullVelocity = (AlienShipRef.WorldPosition - WorldPosition) * 10.0f;
		Rigidbody.Velocity = PullVelocity.WithZ(BeamPullSpeed);
	}
	// --------------------------------------------------------------PRIVATE METHODS--------------------------------------------------------------
	private void SetIsOwnerHoldingState(bool InState) { IsOwnerHolding = InState; }
	private void SetClearOwnerIdTimerInternally()
	{
		SetIsOwnerHoldingState(false);
		ClearOwnerIdHandle = TimerManager.AddTimer(ClearOwnerIdHandle, ClearTime, false, ClearOwnerId);
	}
	// --------------------------------------------------------------NETWORKING METHODS--------------------------------------------------------------
	[Rpc.Owner]
	public void SetAlienShipRef(Guid InAlienShipWhoPulls) { AlienShipRef = Scene.Directory.FindByGuid(InAlienShipWhoPulls); }
	[Rpc.Broadcast]
	public void SetIsPulled(bool State)
	{
		Log.Info("IS PULLED STATE: " + State);
		IsPulled = State;
		if (IsPulled) Rigidbody?.Gravity = false;
		else
		{
			if (Rigidbody == null) return;
			Rigidbody.Gravity = true;
			Rigidbody.Velocity = 0f;
		}
	}
}
