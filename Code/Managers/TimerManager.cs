using System;
namespace Sandbox.Managers;

public sealed class TimerManager(Scene Scene) : GameObjectSystem(Scene)
{
	private class Timer
	{
		private readonly bool IsLooping;
		private readonly int Id;
		private float CurrentTime;
		private readonly float Interval;
		private readonly Action TimerCallback;
		
		public Timer(int InId, float InInterval, bool InIsLooping, Action InTimerCallback)
		{
			Id = InId;
			Interval = InInterval;
			IsLooping = InIsLooping;
			TimerCallback = InTimerCallback;
		}
		public void IncrementTimer(float InDeltaTime)
		{
			CurrentTime += InDeltaTime;
			if (CurrentTime < Interval) return;
			TimerCallback.Invoke();
			CurrentTime = 0f;
			if (!IsLooping) ClearTimer(Id);
		}
	}

	private static readonly Dictionary<int, Timer> TIMERS = new();
	private static int NEXT_ID;
	
	public int AddTimer(float InInterval, bool InIsLooping, Action InTimerCallback)
	{
		TIMERS.Add(++NEXT_ID, new Timer(NEXT_ID, InInterval, InIsLooping, InTimerCallback));
		return NEXT_ID;
	}
	public static void ClearTimer(int InId) { TIMERS.Remove(InId); }
	public void OnUpdate()
	{
		foreach (KeyValuePair<int, Timer> TimerObject in TIMERS) TimerObject.Value.IncrementTimer(Time.Delta);
	}
	public void OnDestroy()
	{
		NEXT_ID = 0;
		TIMERS.Clear();
	}
}
