using System;
using Sandbox.Managers;
namespace Sandbox.Components;

public class PullableComponent : Component
{
	[Property] private bool IsInAirCheckRequired { get; set; }
	private bool IsPulled;
	private int PoolingHandle;
	private Rigidbody Rigidbody;
	
	public bool GetIsPulled() { return IsPulled; }
	public bool IsInAir() { return !IsInAirCheckRequired || MathF.Abs(Rigidbody.Velocity.z) > 0.1f; }

	public void SetIsPulled(bool State)
	{
		IsPulled = State;
		if (IsPulled)
		{
			Rigidbody.Gravity = false;
			PoolingHandle = TimerManager.AddTimer(PoolingHandle, 1f / 60f, true, PullingLogic);
		}
		else
		{
			Rigidbody.Gravity = true;
			Rigidbody.Velocity = 0f;
			TimerManager.ClearTimer(PoolingHandle);
		}
	}

	protected override void OnStart()
	{
		Rigidbody = GetComponent<Rigidbody>();
	}

	private void PullingLogic()
	{
		Rigidbody.Velocity = Vector3.Up * 500f;
	}
}
