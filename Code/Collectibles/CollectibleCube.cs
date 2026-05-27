using System;
using Sandbox.Components;
using Sandbox.Data;
using Sandbox.Enums;
using Sandbox.Interfaces;
using Sandbox.Managers;
namespace Sandbox.Collectibles;

public sealed class CollectibleCube : Component, IPoolObject
{
	[Property] private Dictionary<ECubeMaterial, Material> Materials { get; set; }
	[Property] private ModelRenderer CubeModelRenderer { get; set; }
	public static int LiveCount { get; private set; }
	private int SetMaterialHandle;
	private int CheckIfFallingOutOfWorldHandle;
	private ECubeMaterial CubeMaterial = ECubeMaterial.Cardboard;
	private PullableComponent PullComp;
	private Rigidbody Rigidbody;
	private PlayerData Data;
	// --------------------------------------------------------------PUBLIC METHODS--------------------------------------------------------------
	public float GetMoneyMultiplier() { return MathF.Pow(2, (int)CubeMaterial); }
	public float GetAlienScaleMultiplier()
	{
		if (Data == null) return 0f;
		return GlobalData.Instance.GetProcessedUpgradeValue(Data.GetUpgradeAmount(EUpgradeName.BiggerAlienMoreMoneyMultiplier1), EUpgradeName.BiggerAlienMoreMoneyMultiplier1) * 0.01f;
	}
	public float GetBoxSlownessPercentage()
	{
		if (Data == null) return 0f;
		GlobalData GlobalInstance = GlobalData.Instance;
		float BoxThrowSlowDownPlayer1 = GlobalInstance.GetProcessedUpgradeValue(Data.GetUpgradeAmount(EUpgradeName.BoxThrowSlowDownPlayer1), EUpgradeName.BoxThrowSlowDownPlayer1);
		float BoxThrowSlowDownPlayer2 = GlobalInstance.GetProcessedUpgradeValue(Data.GetUpgradeAmount(EUpgradeName.BoxThrowSlowDownPlayer2), EUpgradeName.BoxThrowSlowDownPlayer2);
		return (BoxThrowSlowDownPlayer1 + BoxThrowSlowDownPlayer2) * 0.01f;
	}
	
	public void RequestReturnToPoolObject(float InAlienScale)
	{
		if (!Networking.IsHost) return;
		TimerManager.ClearTimer(CheckIfFallingOutOfWorldHandle);
		GameObject.Network.TakeOwnership();
		PullComp.SetIsPulled(false);
		if (PullComp.GetWasThrown())
		{
			float ThrowDistanceUnits = Vector3.DistanceBetween(PullComp.GetLastOwnerThrownPosition().WithZ(0), WorldPosition.WithZ(0));
			float ThrowDistanceMeters = ThrowDistanceUnits * 0.0254f;
			if (ThrowDistanceMeters >= 30f) EventManager.TriggerBoxDistanceThrownQuestProgressed(PullComp.GetOwnerId());
			PullComp.SetWasThrown(false);
		}
		EventManager.TriggerOnKill(PullComp.GetOwnerId(), false, PullComp.GetMoneyMultiplier(InAlienScale));
		PullComp.ClearOwnerId();
		Rigidbody.Velocity = 0f;
		PoolingManager.Instance.ReturnObjectToPool(EPoolObject.CollectibleCube, GameObject);
	}
	// --------------------------------------------------------------PROTECTED METHODS--------------------------------------------------------------
	protected override void OnEnabled() { LiveCount++; }
	protected override void OnDisabled() { LiveCount--; }
	protected override void OnAwake()
	{
		PullComp = GetComponent<PullableComponent>();
	}
	protected override void OnStart()
	{
		SetMaterialHandle = TimerManager.AddTimer(SetMaterialHandle, 0.1f, true, SetMaterial);
		Rigidbody = GetComponent<Rigidbody>();
		PullComp?.SetRigidbody(Rigidbody);
	}
	protected override void OnDestroy()
	{
		TimerManager.ClearTimer(SetMaterialHandle);
		TimerManager.ClearTimer(CheckIfFallingOutOfWorldHandle);
	}
	// --------------------------------------------------------------PRIVATE METHODS--------------------------------------------------------------
	
	private void SetMaterial()
	{
		Data = EventManager.TriggerGetPlayerData();
		if (Data == null) return;
		ChangeMaterialByChances();
		TimerManager.ClearTimer(SetMaterialHandle);
	}
	private void CheckIfFallingOutOfWorld() { if (WorldPosition.z <= -1000f) RequestReturnToPoolObject(0f); }
	private void ChangeMaterialByChances()
	{
		int RemainingPercentage = 100;
		int Roll = Random.Shared.Int(1, 100);

		// --- OBSIDIAN ---
		int ObsidianChanceFromUpgrades = (int)GlobalData.Instance.GetProcessedUpgradeValue(Data.GetUpgradeAmount(EUpgradeName.ChanceToSpawnObsidian1), EUpgradeName.ChanceToSpawnObsidian1);
		int ObsidianChance = Math.Min(RemainingPercentage, ObsidianChanceFromUpgrades);
		if (Roll <= ObsidianChance) { ChangeMaterial(ECubeMaterial.Obsidian); return; }
		RemainingPercentage -= ObsidianChance;
		Roll -= ObsidianChance;

		// --- AMETHYST ---
		int AmethystChanceFromUpgrades = (int)GlobalData.Instance.GetProcessedUpgradeValue(Data.GetUpgradeAmount(EUpgradeName.ChanceToSpawnAmethyst1), EUpgradeName.ChanceToSpawnAmethyst1);
		int AmethystChance = Math.Min(RemainingPercentage, ObsidianChanceFromUpgrades + AmethystChanceFromUpgrades);
		if (Roll <= AmethystChance) { ChangeMaterial(ECubeMaterial.Amethyst); return; }
		RemainingPercentage -= AmethystChance;
		Roll -= AmethystChance;
		
		// --- EMERALD ---
		int EmeraldChanceFromUpgrades = (int)GlobalData.Instance.GetProcessedUpgradeValue(Data.GetUpgradeAmount(EUpgradeName.ChanceToSpawnEmerald1), EUpgradeName.ChanceToSpawnEmerald1);
		int EmeraldChance = Math.Min(RemainingPercentage, AmethystChanceFromUpgrades + EmeraldChanceFromUpgrades);
		if (Roll <= EmeraldChance) { ChangeMaterial(ECubeMaterial.Emerald); return; }
		RemainingPercentage -= EmeraldChance;
		Roll -= EmeraldChance;
		
		// --- RUBY ---
		int RubyChanceFromUpgrades = (int)GlobalData.Instance.GetProcessedUpgradeValue(Data.GetUpgradeAmount(EUpgradeName.ChanceToSpawnRuby1), EUpgradeName.ChanceToSpawnRuby1);
		int RubyChance = Math.Min(RemainingPercentage, EmeraldChanceFromUpgrades + RubyChanceFromUpgrades);
		if (Roll <= RubyChance) { ChangeMaterial(ECubeMaterial.Ruby); return; }
		RemainingPercentage -= RubyChance;
		Roll -= RubyChance;
		
		// --- DIAMOND ---
		int DiamondChanceFromUpgrades = (int)GlobalData.Instance.GetProcessedUpgradeValue(Data.GetUpgradeAmount(EUpgradeName.ChanceToSpawnDiamond1), EUpgradeName.ChanceToSpawnDiamond1);
		int DiamondChance = Math.Min(RemainingPercentage, RubyChanceFromUpgrades + DiamondChanceFromUpgrades);
		if (Roll <= DiamondChance) { ChangeMaterial(ECubeMaterial.Diamond); return; }
		RemainingPercentage -= DiamondChance;
		Roll -= DiamondChance;
		
		// --- GOLD ---
		int GoldChanceFromUpgrades = (int)GlobalData.Instance.GetProcessedUpgradeValue(Data.GetUpgradeAmount(EUpgradeName.ChanceToSpawnGold1), EUpgradeName.ChanceToSpawnGold1);
		int GoldChance = Math.Min(RemainingPercentage, DiamondChanceFromUpgrades + GoldChanceFromUpgrades);
		if (Roll <= GoldChance) { ChangeMaterial(ECubeMaterial.Gold); return; }
		RemainingPercentage -= GoldChance;
		Roll -= GoldChance;
		
		// --- METAL ---
		int MetalChanceFromUpgrades = (int)GlobalData.Instance.GetProcessedUpgradeValue(Data.GetUpgradeAmount(EUpgradeName.ChanceToSpawnMetal1), EUpgradeName.ChanceToSpawnMetal1);
		int MetalChance = Math.Min(RemainingPercentage, GoldChanceFromUpgrades + MetalChanceFromUpgrades);
		if (Roll <= MetalChance) { ChangeMaterial(ECubeMaterial.Metal); return; }
		RemainingPercentage -= MetalChance;
		Roll -= MetalChance;
		
		// --- ROCK ---
		int RockChanceFromUpgrades = (int)GlobalData.Instance.GetProcessedUpgradeValue(Data.GetUpgradeAmount(EUpgradeName.ChanceToSpawnRock1), EUpgradeName.ChanceToSpawnRock1);
		int RockChance = Math.Min(RemainingPercentage, MetalChanceFromUpgrades + RockChanceFromUpgrades);
		if (Roll <= RockChance) { ChangeMaterial(ECubeMaterial.Rock); return; }
		RemainingPercentage -= RockChance;
		Roll -= RockChance;
		
		// --- WOOD ---
		int WoodChance = Math.Min(RemainingPercentage, RockChanceFromUpgrades + (int)GlobalData.Instance.GetProcessedUpgradeValue(Data.GetUpgradeAmount(EUpgradeName.ChanceToSpawnWood1), EUpgradeName.ChanceToSpawnWood1));
		if (Roll <= WoodChance) { ChangeMaterial(ECubeMaterial.Wood); return; }
		
		ChangeMaterial(ECubeMaterial.Cardboard);
	}
	private void ChangeMaterial(ECubeMaterial InCubeMaterial)
	{
		CubeMaterial = InCubeMaterial;
		CubeModelRenderer?.SetMaterial(Materials[InCubeMaterial]);
	}
	// --------------------------------------------------------------NETWORKING METHODS--------------------------------------------------------------
	[Rpc.Broadcast]
	public void Init()
	{
		if (Networking.IsHost) CheckIfFallingOutOfWorldHandle = TimerManager.AddTimer(CheckIfFallingOutOfWorldHandle, 10f, true, CheckIfFallingOutOfWorld);
		Transform.ClearInterpolation();
		Data = EventManager.TriggerGetPlayerData();
		if (Data == null) return;
		ChangeMaterialByChances();
	}
}
