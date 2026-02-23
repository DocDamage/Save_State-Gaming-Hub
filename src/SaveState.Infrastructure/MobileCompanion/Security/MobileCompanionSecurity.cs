using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;

namespace SaveState.Infrastructure.MobileCompanion.Security;

/// <summary>
/// Security service for mobile companion pairing and encryption.
/// </summary>
public interface IMobileCompanionSecurity
{
    string GeneratePairingCode();
    string GenerateToken();
    bool ValidatePairingCode(string code);
    bool ValidateToken(string token);
    byte[] EncryptData(byte[] data, byte[] key);
    byte[] DecryptData(byte[] data, byte[] key);
    (byte[] publicKey, byte[] privateKey) GenerateKeyPair();
    byte[] DeriveSharedSecret(byte[] privateKey, byte[] otherPublicKey);
    byte[] GenerateRandomBytes(int length);
    byte[] ComputeHash(string input);
}

public class MobileCompanionSecurity : IMobileCompanionSecurity
{
    private readonly ILogger<MobileCompanionSecurity> _logger;
    private const int PairingCodeLength = 6;
    private const int TokenLength = 32;

    public MobileCompanionSecurity(ILogger<MobileCompanionSecurity> logger)
    {
        _logger = logger;
    }

    public string GeneratePairingCode()
    {
        var random = RandomNumberGenerator.GetInt32(0, 1_000_000);
        var code = random.ToString($"D{PairingCodeLength}");
        _logger.LogDebug("Generated pairing code");
        return code;
    }

    public string GenerateToken()
    {
        var bytes = GenerateRandomBytes(TokenLength);
        return Convert.ToBase64String(bytes);
    }

    public bool ValidatePairingCode(string code)
    {
        if (string.IsNullOrEmpty(code) || code.Length != PairingCodeLength)
            return false;

        foreach (var c in code)
        {
            if (!char.IsDigit(c))
                return false;
        }

        return true;
    }

    public bool ValidateToken(string token)
    {
        if (string.IsNullOrEmpty(token))
            return false;

        try
        {
            var bytes = Convert.FromBase64String(token);
            return bytes.Length >= 16;
        }
        catch
        {
            return false;
        }
    }

    public byte[] EncryptData(byte[] data, byte[] key)
    {
        using var aes = Aes.Create();
        aes.Key = key;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        using var ms = new System.IO.MemoryStream();
        
        ms.Write(aes.IV, 0, aes.IV.Length);
        
        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        {
            cs.Write(data, 0, data.Length);
        }
        
        return ms.ToArray();
    }

    public byte[] DecryptData(byte[] data, byte[] key)
    {
        using var aes = Aes.Create();
        aes.Key = key;

        var iv = new byte[16];
        Array.Copy(data, 0, iv, 0, 16);
        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor();
        using var ms = new System.IO.MemoryStream();
        
        using (var cs = new CryptoStream(
            new System.IO.MemoryStream(data, 16, data.Length - 16), 
            decryptor, 
            CryptoStreamMode.Read))
        {
            cs.CopyTo(ms);
        }
        
        return ms.ToArray();
    }

    public (byte[] publicKey, byte[] privateKey) GenerateKeyPair()
    {
        using var ecdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        
        var publicKey = ecdh.PublicKey.ExportSubjectPublicKeyInfo();
        var privateKey = ecdh.ExportECPrivateKey();
        
        _logger.LogDebug("Generated ECDH key pair");
        return (publicKey, privateKey);
    }

    public byte[] DeriveSharedSecret(byte[] privateKey, byte[] otherPublicKey)
    {
        using var ecdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        ecdh.ImportECPrivateKey(privateKey, out _);
        
        using var otherKey = ECDiffieHellman.Create();
        otherKey.ImportSubjectPublicKeyInfo(otherPublicKey, out _);
        
        return ecdh.DeriveKeyMaterial(otherKey.PublicKey);
    }

    public byte[] GenerateRandomBytes(int length)
    {
        var bytes = new byte[length];
        RandomNumberGenerator.Fill(bytes);
        return bytes;
    }

    public byte[] ComputeHash(string input)
    {
        return SHA256.HashData(Encoding.UTF8.GetBytes(input));
    }
}
