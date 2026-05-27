using System.Security.Cryptography;
using Sandbox.Enums;
using Sandbox.Managers;
using Sandbox.Services;
namespace Sandbox.Data;
using System;
using System.Text;

public sealed class PlayerData
{
	public sealed class QuestState
	{
		public bool Completed { get; set; }
		public bool Claimed { get; set; }
		public int Progress { get; set; }
	}
	
	public bool ShowNameTag { get; set; }
	public int CurrentVersion { get; set; }
	public double Money { get; set; }
	public double TotalMoneyEarned { get; set; }
	public string Name { get; set; }
	public string Checksum { get; set; }
	public Dictionary<EUpgradeName, int> OwnedUpgrades { get; set; }
	public Dictionary<EQuest, QuestState> QuestsData { get; set; }

    private static readonly string SECRET_KEY = "CRAZY_SECRET_KEY69";
    private const int LiveVersion = 3;
    
    public PlayerData()
    {
	    Name = "I Dont Know My Name";
	    ShowNameTag = false;
	    CurrentVersion = LiveVersion;
	    OwnedUpgrades = new Dictionary<EUpgradeName, int>();
	    QuestsData = new Dictionary<EQuest, QuestState>();
	    foreach (EQuest QuestName in Enum.GetValues<EQuest>()) QuestsData.Add(QuestName, new QuestState());
    }

    public bool GetShowNameTag() { return ShowNameTag; }
    public double GetMoney() { return Money; }
    public double GetTotalMoneyEarned() { return TotalMoneyEarned; }
    public string GetName() { return Name; }
    public int GetUpgradeAmount(EUpgradeName InUpgradeName) { return OwnedUpgrades?.GetValueOrDefault(InUpgradeName, 0) ?? 0; }
    public Dictionary<EUpgradeName, int> GetOwnedUpgrades() { return OwnedUpgrades; }
    
    public bool IsQuestCompleted(EQuest InQuestName)
    {
	    return QuestsData.TryGetValue(InQuestName, out QuestState QuestState) && QuestState.Completed;
    }
    public bool IsQuestClaimed(EQuest InQuestName)
    {
	    return QuestsData.TryGetValue(InQuestName, out QuestState QuestState) && QuestState.Claimed;
    }
    public int GetQuestProgress(EQuest InQuestName)
    {
	    return QuestsData.TryGetValue(InQuestName, out QuestState QuestState) ? QuestState.Progress : 0;
    }

    public void AdvanceQuest(EQuest InQuestName, int InAmount)
    {
	    Stats.Increment(GetAchievementName(InQuestName), InAmount);
	    if (IsQuestCompleted(InQuestName)) return;
	    int RequiredProgress = GlobalData.Instance.GetQuestRequirement(InQuestName);
	    int CurrentProgress = GetQuestProgress(InQuestName) + InAmount;
	    QuestsData[InQuestName].Progress = CurrentProgress;
	    if (CurrentProgress >= RequiredProgress)
	    {
		    QuestsData[InQuestName].Progress = RequiredProgress;
		    QuestsData[InQuestName].Completed = true;
	    }
	    EventManager.TriggerQuestProgressed();
	    Save(this);
    }
    public void ClaimQuest(EQuest InQuestName, float InQuestReward)
    {
	    if (IsQuestClaimed(InQuestName)) return;
	    QuestsData[InQuestName].Claimed = true;
		AddMoney(InQuestReward);
    }
    public void AddMoney(double InAmount)
    {
	    Money += InAmount;
	    TotalMoneyEarned += InAmount;
	    Save(this);
	    EventManager.TriggerMoneyChange(Money);
    }
    public void SubtractMoney(double InAmount)
    {
		Money -= InAmount;
		Save(this);
		EventManager.TriggerMoneyChange(Money);
    }
    public void BuyUpgrade(EUpgradeName InUpgradeName)
    {
	    if (!OwnedUpgrades.TryAdd(InUpgradeName, 1)) OwnedUpgrades[InUpgradeName]++;
	    Save(this);
	    EventManager.TriggerUpgradePurchased(InUpgradeName);
    }
    public void ChangeName(string InName)
    {
	    Name = InName;
	    Save(this);
	    EventManager.TriggerPlayerNameChange(Name);
    }
    public void SetShowNameTag(bool InState)
    {
	    ShowNameTag = InState;
	    Save(this);
	    EventManager.TriggerPlayerNameShowTagChange(ShowNameTag);
    }
    
    public static void Save(PlayerData InData)
    {
        InData.Checksum = "";
        string RawJson = Json.Serialize(InData);
        string Hash = CreateMd5(RawJson + SECRET_KEY);
        InData.Checksum = Hash;

        string FinalJson = Json.Serialize(InData);
        string Noise = Guid.NewGuid().ToString()[..Random.Shared.Int(1, 15)];
        
        string Header = Noise.Length.ToString("D2");
        string ContentToHide = Header + Noise + FinalJson;
        
        string Base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(ContentToHide));
        string Obfuscated = XorInput(Base64, SECRET_KEY);
        FileSystem.Data.WriteAllText("save.dat", Obfuscated);
    }
    public static PlayerData Load()
    {
        if (!FileSystem.Data.FileExists("save.dat")) return new PlayerData();
        try
        {
            string Obfuscated = FileSystem.Data.ReadAllText("save.dat");
            string Base64 = XorInput(Obfuscated, SECRET_KEY);
            string Decrypted = Encoding.UTF8.GetString(Convert.FromBase64String(Base64));
            if (!int.TryParse(Decrypted[..2], out int NoiseLength)) throw new Exception("Header Corrupt");
            string JsonData = Decrypted[(2 + NoiseLength)..];
            Dictionary<string, object> RawDataMap = Json.Deserialize<Dictionary<string, object>>(JsonData);
            if (RawDataMap == null || !RawDataMap.TryGetValue("Checksum", out object OutValue)) throw new Exception("Invalid Save Structure");
            string StoredChecksum = OutValue?.ToString();
            RawDataMap["Checksum"] = "";
            string VerificationJson = Json.Serialize(RawDataMap);
            string CalculatedHash = CreateMd5(VerificationJson + SECRET_KEY);
            if (StoredChecksum != CalculatedHash)
            {
                Log.Error("CRITICAL: Save file tampered with! Hash mismatch.");
                FileSystem.Data.DeleteFile("save.dat");
                return new PlayerData();
            }
            PlayerData ValidData = Json.Deserialize<PlayerData>(JsonData);
            Migrate(ValidData);
            return ValidData;
        }
        catch (Exception Exception)
        {
            Log.Warning($"Save Corrupted: {Exception.Message}");
            return new PlayerData();
        }
    }

    private string GetAchievementName(EQuest InQuestName)
    {
	    switch (InQuestName)
	    {
		    case EQuest.Thrower: return "thrown_boxes_to_aliens";
		    case EQuest.Winner: return "kill_one_player";
		    case EQuest.Walker:
		    case EQuest.Swordsman:
		    case EQuest.EliteThrower:
		    case EQuest.Maxer:
		    default: return "";
	    }
    }
    private static string XorInput(string InInput, string InKey)
    {
        StringBuilder Output = new();
        for (int Index = 0; Index < InInput.Length; Index++) Output.Append((char)(InInput[Index] ^ InKey[Index % InKey.Length]));
        return Output.ToString();
    }
    private static string CreateMd5(string InInput)
    {
        using MD5 Md5 = MD5.Create();
        byte[] InputBytes = Encoding.ASCII.GetBytes(InInput);
        byte[] HashBytes = Md5.ComputeHash(InputBytes);
        return Convert.ToHexString(HashBytes);
    }
    private static void Migrate(PlayerData InData)
    {
	    bool NeedsSave = false;
	    if (InData.CurrentVersion < 2)
	    {
		    if (InData.OwnedUpgrades.TryGetValue(EUpgradeName.AlienComboMaxMultiplier2, out int CurrentLevel))
		    {
				if (CurrentLevel > 2) InData.OwnedUpgrades[EUpgradeName.AlienComboMaxMultiplier2] = CurrentLevel - 1;
		    }
		    InData.CurrentVersion = 2;
		    NeedsSave = true;
	    }
	    if (InData.CurrentVersion < 3)
	    {
		    Log.Info("ADDING QUESTS FOR OLD USERS");
		    InData.QuestsData = new Dictionary<EQuest, QuestState>
		    {
			    { EQuest.Thrower, new QuestState() },
			    { EQuest.Winner, new QuestState() },
			    { EQuest.Walker, new QuestState() },
			    { EQuest.Swordsman, new QuestState() },
			    { EQuest.EliteThrower, new QuestState() },
			    { EQuest.Maxer, new QuestState() }
		    };
		    InData.CurrentVersion = 3;
		    NeedsSave = true;
	    }
	    if (NeedsSave) Save(InData);
    }
}
