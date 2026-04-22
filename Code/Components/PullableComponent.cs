using System;
using Sandbox.Managers;
namespace Sandbox.Components;

public class PullableComponent : Component
{
	[Property] private bool IsInAirCheckRequired { get; set; }
	[Sync] private Guid LastOwnedBy {get; set;}
	private bool IsPulled;
	private int PoolingHandle;
	private int ClearOwnerIdHandle;
	private int MoneyAmount = 1;
	private const float ClearTime = 8f;
	private Rigidbody Rigidbody;
	
	public bool GetIsPulled() { return IsPulled; }
	public bool IsInAir() { return !IsInAirCheckRequired || MathF.Abs(Rigidbody.Velocity.z) > 0.1f; }
	public int GetMoneyAmount() { return MoneyAmount; }
	public Guid GetOwnerId() { return LastOwnedBy; }

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
			Rigidbody?.Gravity = true;
			Rigidbody?.Velocity = 0f;
			TimerManager.ClearTimer(PoolingHandle);
		}
	}
	public void SetOwnerId(Guid InId)
	{
		Log.Info("SetOwnerId " + InId);
		LastOwnedBy = InId;
		TimerManager.ClearTimer(ClearOwnerIdHandle);
	}
	public void SetClearOwnerIdTimer()
	{
		Log.Info("SetClearOwnerIdTimer");
		ClearOwnerIdHandle = TimerManager.AddTimer(ClearOwnerIdHandle, ClearTime, false, ClearOwnerId);
	}
	public void ClearOwnerId()
	{
		Log.Info("ClearOwnerId");
		LastOwnedBy = Guid.Empty;
	}

	protected override void OnStart()
	{
		Rigidbody = GetComponent<Rigidbody>();
	}

	private void PullingLogic()
	{
		Rigidbody?.Velocity = Vector3.Up * 500f;
	}
}
