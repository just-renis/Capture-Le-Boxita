using Sandbox.Managers;
using Sandbox.UI;
namespace Sandbox.Collectibles;

public sealed class CollectibleCube : Component
{
	[Property] private float MaxHp = 100f;
	[Property, Sync] private float CurrentHp { get; set; } = 100f;
	private int InitUiTimerHandle;
	private readonly float DistanceCull = 80000f;
	private readonly float HpBarOffset = 50f;
	private EventManager EventManager;
	private WorldPanel HpBarUi;
	private CubeHpBar HpBar;
	
	protected override void OnStart()
	{
		base.OnStart();
		EventManager = Scene.GetSystem<EventManager>();
		EventManager.OnPlayerLocationSend += CheckIfNeedToChangeRenderState;
		InitUiTimerHandle = TimerManager.AddTimer(0.1f, true, TryInitUi);
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();
		HpBarUi?.WorldPosition = GameObject.WorldPosition + Vector3.Up * HpBarOffset;
	}

	protected override void OnDestroy()
	{
		EventManager.OnPlayerLocationSend -= CheckIfNeedToChangeRenderState;
		base.OnDestroy();
	}

	private void CheckIfNeedToChangeRenderState(Vector3 InPlayerPosition)
	{
		HpBarUi?.Enabled = InPlayerPosition.DistanceSquared(GameObject.WorldPosition) < DistanceCull;
	}

	private void TryInitUi()
	{
		HpBarUi ??= GameObject.GetComponentInChildren<WorldPanel>(true);
		if (HpBarUi != null && HpBar == null) HpBar = HpBarUi.GetComponentInChildren<CubeHpBar>(true);
		if (HpBar == null) return;
		HpBar.SetMaxHp(MaxHp);
		HpBar.SetCurrentHp(CurrentHp);
		TimerManager.ClearTimer(InitUiTimerHandle);
	}
	[Rpc.Broadcast]
	private void ReceiveDamage(float InDamage)
	{
		CurrentHp -= InDamage;
		HpBar?.SetCurrentHp(CurrentHp);
	}
}
