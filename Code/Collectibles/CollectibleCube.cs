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
	private int SetMaterialHandle;
	private int CheckIfFallingOutOfWorldHandle;
	private ECubeMaterial CubeMaterial = ECubeMaterial.Cardboard;
	private PullableComponent PullComp;
	private Rigidbody Rigidbody;
	private PlayerData Data;
	// --------------------------------------------------------------PUBLIC METHODS--------------------------------------------------------------
	public float GetAlienScaleMultiplier()
	{
		if (Data == null) return 0f;
		return Data.GetUpgradeAmount(EUpgradeName.BiggerAlienMoreMoneyMultiplier1) * 0.05f;
	}
	
	public void RequestReturnToPoolObject(float InAlienScale)
	{
		if (!Networking.IsHost) return;
		TimerManager.ClearTimer(CheckIfFallingOutOfWorldHandle);
		GameObject.Network.TakeOwnership();
		PullComp.SetIsPulled(false);
		EventManager.TriggerOnKill(PullComp.GetOwnerId(), false, PullComp.GetMoneyMultiplier(InAlienScale));
		PullComp.ClearOwnerId();
		Rigidbody.Velocity = 0f;
		Scene.GetSystem<PoolingManager>().ReturnObjectToPool(EPoolObject.CollectibleCube, GameObject);
	}
	// --------------------------------------------------------------PROTECTED METHODS--------------------------------------------------------------
	protected override void OnStart()
	{
		SetMaterialHandle = TimerManager.AddTimer(SetMaterialHandle, 0.1f, true, SetMaterial);
		PullComp = GetComponent<PullableComponent>();
		Rigidbody = GetComponent<Rigidbody>();
	}
	protected override void OnDestroy()
	{
		TimerManager.ClearTimer(CheckIfFallingOutOfWorldHandle);
	}
	// --------------------------------------------------------------PRIVATE METHODS--------------------------------------------------------------
	public float GetMoneyMultiplier() { return MathF.Pow(2, (int)CubeMaterial); }
	
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
		int ObsidianChance = Math.Min(RemainingPercentage, Data.GetUpgradeAmount(EUpgradeName.ChanceToSpawnObsidian1));
		if (Roll <= ObsidianChance) { ChangeMaterial(ECubeMaterial.Obsidian); return; }
		RemainingPercentage -= ObsidianChance;
		Roll -= ObsidianChance;

		// --- AMETHYST ---
		int AmethystChance = Math.Min(RemainingPercentage, Data.GetUpgradeAmount(EUpgradeName.ChanceToSpawnAmethyst1) * 2);
		if (Roll <= AmethystChance) { ChangeMaterial(ECubeMaterial.Amethyst); return; }
		RemainingPercentage -= AmethystChance;
		Roll -= AmethystChance;
		
		// --- EMERALD ---
		int EmeraldChance = Math.Min(RemainingPercentage, Data.GetUpgradeAmount(EUpgradeName.ChanceToSpawnEmerald1) * 2);
		if (Roll <= EmeraldChance) { ChangeMaterial(ECubeMaterial.Emerald); return; }
		RemainingPercentage -= EmeraldChance;
		Roll -= EmeraldChance;
		
		// --- RUBY ---
		int RubyChance = Math.Min(RemainingPercentage, Data.GetUpgradeAmount(EUpgradeName.ChanceToSpawnRuby1) * 3);
		if (Roll <= RubyChance) { ChangeMaterial(ECubeMaterial.Ruby); return; }
		RemainingPercentage -= RubyChance;
		Roll -= RubyChance;
		
		// --- DIAMOND ---
		int DiamondChance = Math.Min(RemainingPercentage, Data.GetUpgradeAmount(EUpgradeName.ChanceToSpawnDiamond1) * 3);
		if (Roll <= DiamondChance) { ChangeMaterial(ECubeMaterial.Diamond); return; }
		RemainingPercentage -= DiamondChance;
		Roll -= DiamondChance;
		
		// --- GOLD ---
		int GoldChance = Math.Min(RemainingPercentage, Data.GetUpgradeAmount(EUpgradeName.ChanceToSpawnGold1) * 4);
		if (Roll <= GoldChance) { ChangeMaterial(ECubeMaterial.Gold); return; }
		RemainingPercentage -= GoldChance;
		Roll -= GoldChance;
		
		// --- METAL ---
		int MetalChance = Math.Min(RemainingPercentage, Data.GetUpgradeAmount(EUpgradeName.ChanceToSpawnMetal1) * 4);
		if (Roll <= MetalChance) { ChangeMaterial(ECubeMaterial.Metal); return; }
		RemainingPercentage -= MetalChance;
		Roll -= MetalChance;
		
		// --- ROCK ---
		int RockChance = Math.Min(RemainingPercentage, Data.GetUpgradeAmount(EUpgradeName.ChanceToSpawnRock1) * 5);
		if (Roll <= RockChance) { ChangeMaterial(ECubeMaterial.Rock); return; }
		RemainingPercentage -= RockChance;
		Roll -= RockChance;
		
		// --- WOOD ---
		int WoodChance = Math.Min(RemainingPercentage, Data.GetUpgradeAmount(EUpgradeName.ChanceToSpawnWood1) * 5);
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
		if (Data == null) return;
		ChangeMaterialByChances();
	}
}
