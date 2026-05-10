using System.Security.Cryptography;
using Sandbox.Enums;
using Sandbox.Managers;
namespace Sandbox.Data;
using System;
using System.Text;

public sealed class PlayerData
{
	public int CurrentVersion { get; set; } = LiveVersion;
	public float Money { get; set; }
	public float TotalMoneyEarned { get; set; }
	public string Checksum { get; set; }
	public Dictionary<EUpgradeName, int> OwnedUpgrades { get; set; }

    private static readonly string SECRET_KEY = "CRAZY_SECRET_KEY69";
    private const int LiveVersion = 1;
    
    public float GetMoney() { return Money; }
    public float GetTotalMoneyEarned() { return TotalMoneyEarned; }
    public int GetUpgradeAmount(EUpgradeName InUpgradeName) { return OwnedUpgrades?.GetValueOrDefault(InUpgradeName, 0) ?? 0; }
    public Dictionary<EUpgradeName, int> GetOwnedUpgrades() { return OwnedUpgrades; }

    public void AddMoney(float InAmount)
    {
	    Money += InAmount;
	    TotalMoneyEarned += InAmount;
	    Save(this);
	    EventManager.TriggerMoneyChange(Money);
    }
    public void SubtractMoney(float InAmount)
    {
		Money -= InAmount;
		Save(this);
		EventManager.TriggerMoneyChange(Money);
    }
    public void BuyUpgrade(EUpgradeName InUpgradeName)
    {
	    if (!OwnedUpgrades.TryAdd(InUpgradeName, 1)) OwnedUpgrades[InUpgradeName]++;
	    Save(this);
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
        PlayerData DefaultData = new() { OwnedUpgrades = new Dictionary<EUpgradeName, int>(), CurrentVersion = LiveVersion };
        if (!FileSystem.Data.FileExists("save.dat")) return DefaultData;
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
                return DefaultData;
            }
            PlayerData ValidData = Json.Deserialize<PlayerData>(JsonData);
            Migrate(ValidData);
            return ValidData;
        }
        catch (Exception Exception)
        {
            Log.Warning($"Save Corrupted: {Exception.Message}");
            return DefaultData;
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
	    if (InData.CurrentVersion < LiveVersion)
	    {
		    InData.CurrentVersion = LiveVersion;
		    NeedsSave = true;
	    }
	    if (NeedsSave) Save(InData);
    }
}
