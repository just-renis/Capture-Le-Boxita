using System;
using Sandbox.Collectibles;
using Sandbox.Managers;
namespace Sandbox.Components;

public class PullableComponent : Component, Component.ICollisionListener
{
	[Property] private bool IsInAirCheckRequired { get; set; }
	[Sync] private bool IsOwnerHolding { get; set; }
	[Sync] private float LastOwnerTime { get; set; }
	[Sync] private float MoneyMultiplier { get; set; }
	[Sync] private float AlienScaleMultiplier { get; set; }
	[Sync] private float BoxSlownessPercentage { get; set; }
	[Sync] private Guid LastOwnedBy { get; set; }
	private bool IsPulled;
	private bool IsPlayer;
	private int ClearOwnerIdHandle;
	private int CheckOutOfBoundsHandle;
	private const float BeamPullSpeed = 600.0f; 
	private const float ClearTime = 8f;
	private const Int64 PushForceMultiplier = 1250000;
	private Rigidbody Rigidbody;
	private GameObject AlienShipRef;
	// --------------------------------------------------------------PUBLIC METHODS--------------------------------------------------------------
	public bool GetIsPulled() { return IsPulled; }
	public bool GetIsPlayer() { return IsPlayer; }
	public bool IsInAir()
	{
		if (Rigidbody == null) return false;
		return !IsInAirCheckRequired || LastOwnedBy != Guid.Empty || MathF.Abs(Rigidbody.Velocity.z) > 0.1f;
	}
	public float GetMoneyMultiplier(float InAlienScale)
	{
		float Stacks = (float)Math.Floor(InAlienScale / 0.5f);
		return MoneyMultiplier + MoneyMultiplier * Stacks * AlienScaleMultiplier;
	}
	public Guid GetOwnerId() { return LastOwnedBy; }
	
	public void SetOwnerId(Guid InId, bool IsGrabbedByPlayer)
	{
		LastOwnerTime = ClearTime;
		LastOwnedBy = InId;
		SetIsOwnerHoldingState(true);
		CollectibleCube Cube = GetComponent<CollectibleCube>();
		if (Cube != null)
		{
			MoneyMultiplier = Cube.GetMoneyMultiplier();
			AlienScaleMultiplier = Cube.GetAlienScaleMultiplier();
			BoxSlownessPercentage = Cube.GetBoxSlownessPercentage();
		}
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
		if (GetIsPlayer()) return; // Player objects dont use OnCollisionStart.
		GameObject CollisionObject = InCollision.Other.GameObject;
		if (!CollisionObject.Tags.HasAny("player", "collectible")) return; // If collided object is not player or collectible return.
		PullableComponent OtherPullableComponent = CollisionObject.GetComponent<PullableComponent>();
		if (OtherPullableComponent == null) return;
		bool IsNotHittingOwner = OtherPullableComponent.Network.OwnerId != LastOwnedBy;
		if (OtherPullableComponent.GetIsPlayer() && IsNotHittingOwner)
		{
			if (OtherPullableComponent.IsOwnerHolding)
			{
				Vector3 PushDir = (OtherPullableComponent.WorldPosition - WorldPosition).Normal;
				PushDir.z = 0;
				OtherPullableComponent.GetComponent<Player.Player>()?.ReceivePush(PushDir * PushForceMultiplier);
			}
			else if (Rigidbody.Velocity.Length > 200f) OtherPullableComponent.GetComponent<Player.Player>()?.ApplySlowDown(BoxSlownessPercentage);
		}
		if (OtherPullableComponent.GetLastOwnerTime() > LastOwnerTime) return; // If other object owning time is more recent than this object return.
		if (OtherPullableComponent.GetLastOwnerTime().AlmostEqual(LastOwnerTime) && OtherPullableComponent.IsOwnerHolding) return; // If both owning times are equal and cube is held by owner return.
		OtherPullableComponent.SetOwnerId(LastOwnedBy, false);
	}
	public void SetIsPulled(bool State)
	{
		IsPulled = State;
		if (IsPulled) return;
		AlienShipRef = null;
		Rigidbody?.Velocity = 0f;
	}
	public void SetRigidbody(Rigidbody InRigidbody) { Rigidbody = InRigidbody; }
	// --------------------------------------------------------------PROTECTED METHODS--------------------------------------------------------------
	protected override void OnStart()
	{
		CheckOutOfBoundsHandle = TimerManager.AddTimer(CheckOutOfBoundsHandle, 5f, true, CheckOutOfBounds);
		IsPlayer = Tags.Has("player");
	}
	protected override void OnUpdate()
	{
		if (!IsOwnerHolding) LastOwnerTime -= Time.Delta;
	}
	protected override void OnDestroy()
	{
		TimerManager.ClearTimer(ClearOwnerIdHandle);
		TimerManager.ClearTimer(CheckOutOfBoundsHandle);
	}
	protected override void OnFixedUpdate()
	{
		if (IsProxy || !IsPulled || AlienShipRef == null || Rigidbody == null) return;
		Rigidbody.AngularVelocity = Vector3.Zero;
		Vector3 PullVelocity = (AlienShipRef.WorldPosition - WorldPosition) * 10f;
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
		if (WorldPosition.z < 6000f) return;
		if (GetIsPulled()) SetIsPulled(false);
	}
	// --------------------------------------------------------------NETWORKING METHODS--------------------------------------------------------------
	[Rpc.Broadcast]
	public void SetAlienShipRef(Guid InAlienShipWhoPulls) { AlienShipRef = Scene.Directory.FindByGuid(InAlienShipWhoPulls); }
	[Rpc.Broadcast]
	public void ReceivePush(Guid InPusherId, Vector3 InPushForce)
	{
		if (IsProxy) return;
		SetOwnerId(InPusherId, false);
		Rigidbody?.ApplyImpulse(InPushForce);
	}
}
