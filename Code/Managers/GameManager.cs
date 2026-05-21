using System;
namespace Sandbox.Managers;

public sealed class GameManager : Component
{
	private bool IsHostLogicInitiated;
	private const float VotingTimeOutTime = 300f;
	private int VotingTimerUpdateHandle;
	[Property] private List<GameObject> PlayerSpawners { get; set; }
	[Sync] private float VotingTimeOut { get; set; } = 300f;

	private CollectibleCubeManager CollectibleCubeManager;
	
	protected override void OnStart()
	{
		CollectibleCubeManager = Scene.GetSystem<CollectibleCubeManager>();
		CollectibleCubeManager.Init();
		EventManager.OnTeleportToSpawnLocation += GetSpawnerLocation;
		EventManager.OnRequestIsVotingAvailable += GetIsVotingAvailable;
		VotingTimerUpdateHandle = TimerManager.AddTimer(VotingTimerUpdateHandle, 0.5f, true, UpdateVotingTimer);
		UiManager.OnStart();
	}
	protected override void OnUpdate()
	{
		TimerManager.OnUpdate();
		if (!Networking.IsHost) return;
		VotingTimeOut += Time.Delta;
		CollectibleCubeManager.OnUpdate();
		if (!IsHostLogicInitiated) SetupHostLogic();
	}
	protected override void OnDestroy()
	{
		EventManager.OnTeleportToSpawnLocation -= GetSpawnerLocation;
		EventManager.OnRequestIsVotingAvailable -= GetIsVotingAvailable;
		CollectibleCubeManager?.OnDestroy();
		TimerManager.ClearTimer(VotingTimerUpdateHandle);
		if (IsHostLogicInitiated) EventManager.OnVotingFinished -= VotingFinished;
		TimerManager.OnDestroy();
		UiManager.OnDestroy();
	}

	private bool GetIsVotingAvailable() { return VotingTimeOut > VotingTimeOutTime; }
	private Vector3 GetSpawnerLocation()
	{
		return PlayerSpawners[Random.Shared.Int(0, PlayerSpawners.Count - 1)].WorldPosition;
	}

	private void UpdateVotingTimer()
	{
		EventManager.TriggerVotingCooldownUpdate(VotingTimeOutTime - VotingTimeOut);
	}
	private void SetupHostLogic()
	{
		IsHostLogicInitiated = true;
		EventManager.OnVotingFinished += VotingFinished;
	}
	private void VotingFinished() { VotingTimeOut = 0f; }
}
