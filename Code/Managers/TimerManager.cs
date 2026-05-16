using System;
namespace Sandbox.Managers;

public sealed class TimerManager(Scene Scene) : GameObjectSystem(Scene)
{
	private class Timer
	{
		public bool IsLooping;
		public  int Id;
		public float CurrentTime;
		public float Interval;
		public Action TimerCallback;
		public bool PendingDelete;
		
		public Timer(int InId, float InInterval, bool InIsLooping, Action InTimerCallback)
		{
			Id = InId;
			Interval = InInterval;
			IsLooping = InIsLooping;
			TimerCallback = InTimerCallback;
			PendingDelete = false;
		}
	}

	private static readonly List<Timer> TIMERS = [];
	private static int NEXT_ID;
	
	public static int AddTimer(int InTimerHandle, float InInterval, bool InIsLooping, Action InTimerCallback)
	{
		for (int i = 0; i < TIMERS.Count; i++)
		{
			Timer Timer = TIMERS[i];
			if (Timer.Id != InTimerHandle || InTimerHandle == 0) continue;
			Timer.Interval = InInterval;
			Timer.IsLooping = InIsLooping;
			Timer.TimerCallback = InTimerCallback;
			Timer.CurrentTime = 0f;
			Timer.PendingDelete = false;
			return InTimerHandle;
		}
		TIMERS.Add(new Timer(++NEXT_ID, InInterval, InIsLooping, InTimerCallback));
		return NEXT_ID;
	}

	public static void ClearTimer(int InId)
	{
		for (int i = 0; i < TIMERS.Count; i++)
		{
			if (TIMERS[i].Id != InId) continue;
			TIMERS[i].PendingDelete = true;
			break;
		}
	}
	public static void OnUpdate()
	{
		for (int i = TIMERS.Count - 1; i >= 0; i--)
		{
			Timer Timer = TIMERS[i];
			if (Timer.PendingDelete)
			{
				TIMERS.RemoveAt(i);
				continue;
			}
			Timer.CurrentTime += Time.Delta;
			if (Timer.CurrentTime < Timer.Interval) continue;
			Timer.TimerCallback?.Invoke();
			if (Timer.IsLooping && !Timer.PendingDelete) Timer.CurrentTime = 0f;
			else TIMERS.RemoveAt(i);
		}
	}
	public static void OnDestroy()
	{
		NEXT_ID = 0;
		TIMERS.Clear();
	}
}
