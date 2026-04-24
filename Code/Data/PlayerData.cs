using System.Security.Cryptography;
namespace Sandbox.Data;
using System;
using System.Text;

public sealed class PlayerData
{
	public float Money { get; set; }
	public string Checksum { get; set; }

    private static readonly string SECRET_KEY = "CRAZY_SECRET_KEY69"; 

   public static void Save(PlayerData InData)
    {
        InData.Checksum = "";
        string RawJson = Json.Serialize(InData);
        string Hash = CreateMd5(RawJson + SECRET_KEY);
        InData.Checksum = Hash;

        string FinalJson = Json.Serialize(InData);
        string Noise = Guid.NewGuid().ToString()[..Random.Shared.Int( 1, 15 )];
        
        string Header = Noise.Length.ToString("D2");
        string ContentToHide = Header + Noise + FinalJson;
        
        string Obfuscated = XorInput( ContentToHide, SECRET_KEY );
        string Base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(Obfuscated));
        FileSystem.Data.WriteAllText("save.dat", Base64);
    }

    public static PlayerData Load()
    {
        if (!FileSystem.Data.FileExists("save.dat")) return new PlayerData();
        try
        {
            string Base64 = FileSystem.Data.ReadAllText("save.dat");
            string Obfuscated = Encoding.UTF8.GetString(Convert.FromBase64String(Base64));
            string Decrypted = XorInput( Obfuscated, SECRET_KEY );

            if (!int.TryParse(Decrypted[..2], out int NoiseLength)) throw new Exception("Header Corrupt");

            string JsonData = Decrypted[(2 + NoiseLength)..];
            PlayerData Data = Json.Deserialize<PlayerData>(JsonData);
            string ClientChecksum = Data.Checksum;

            Data.Checksum = "";
            string VerifyHash = CreateMd5(Json.Serialize(Data) + SECRET_KEY);

            if (ClientChecksum == VerifyHash) return Data;
            
            Log.Error( "CRITICAL: Save file tampered with! Deleting progress." );
            FileSystem.Data.DeleteFile( "save.dat" );
            return new PlayerData();

        }
        catch (Exception Exception)
        {
            Log.Warning($"Save Corrupted: {Exception.Message}");
            return new PlayerData();
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
