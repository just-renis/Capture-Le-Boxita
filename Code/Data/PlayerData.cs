using System.Security.Cryptography;
using Sandbox.Managers;
namespace Sandbox.Data;
using System;
using System.Text;

public sealed class PlayerData
{
	public float Money { get; set; }
	public string Checksum { get; set; }
	public Dictionary<int, int> OwnedUpgrades { get; set; }

    private static readonly string SECRET_KEY = "CRAZY_SECRET_KEY69";
    
    public float GetMoney() { return Money; }
    public Dictionary<int, int> GetOwnedUpgrades() { return OwnedUpgrades; }

    public void AddMoney(float InAmount)
    {
	    Money += InAmount;
	    Save(this);
	    EventManager.TriggerMoneyChange(Money);
    }
    public void SubtractMoney(float InAmount)
    {
		Money -= InAmount;
		Save(this);
		EventManager.TriggerMoneyChange(Money);
    }
    public void BuyUpgrade(int InUpgradeId)
    {
	    if (!OwnedUpgrades.TryAdd(InUpgradeId, 1))
	    {
		    OwnedUpgrades[InUpgradeId]++;
	    }
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
        if (!FileSystem.Data.FileExists("save.dat")) return new PlayerData { OwnedUpgrades = new Dictionary<int, int>() };
        try
        {
	        string Obfuscated = FileSystem.Data.ReadAllText("save.dat");
	        string Base64 = XorInput(Obfuscated, SECRET_KEY);
	        string Decrypted = Encoding.UTF8.GetString(Convert.FromBase64String(Base64));

            if (!int.TryParse(Decrypted[..2], out int NoiseLength)) throw new Exception("Header Corrupt");

            string JsonData = Decrypted[(2 + NoiseLength)..];
            PlayerData Data = Json.Deserialize<PlayerData>(JsonData);
            string ClientChecksum = Data.Checksum;

            Data.Checksum = "";
            string VerifyHash = CreateMd5(Json.Serialize(Data) + SECRET_KEY);

            if (ClientChecksum == VerifyHash) return Data;
            
            Log.Error("CRITICAL: Save file tampered with! Deleting progress.");
            FileSystem.Data.DeleteFile( "save.dat" );
            return new PlayerData { OwnedUpgrades = new Dictionary<int, int>() };

        }
        catch (Exception Exception)
        {
            Log.Warning($"Save Corrupted: {Exception.Message}");
            return new PlayerData { OwnedUpgrades = new Dictionary<int, int>() };
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
}
