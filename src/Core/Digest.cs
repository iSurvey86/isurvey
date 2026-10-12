using System.Security.Cryptography;

namespace iSurvey;

/// <summary>
/// SHA-256 / HMAC / hex dùng API có trên cả .NET Framework 4.8 (AutoCAD 2023) và .NET 8/10 (AutoCAD 2025+).
/// Kết quả trùng SHA256.HashData / Convert.ToHexString (chữ hoa).
/// </summary>
internal static class Digest
{
    public static byte[] Sha256(byte[] data)
    {
        using var sha = SHA256.Create();
        return sha.ComputeHash(data);
    }

    public static byte[] HmacSha256(byte[] key, byte[] data)
    {
        using var hmac = new HMACSHA256(key);
        return hmac.ComputeHash(data);
    }

    public static bool FixedEquals(byte[] a, byte[] b)
    {
        if (a.Length != b.Length)
            return false;
        var diff = 0;
        for (var i = 0; i < a.Length; i++)
            diff |= a[i] ^ b[i];
        return diff == 0;
    }

    public static string Hex(byte[] data) => Hex(data, 0, data.Length);

    public static string Hex(byte[] data, int offset, int count)
    {
        var chars = new char[count * 2];
        for (var i = 0; i < count; i++)
        {
            var b = data[offset + i];
            chars[i * 2] = ToHex(b >> 4);
            chars[i * 2 + 1] = ToHex(b & 0xF);
        }

        return new string(chars);
    }

    private static char ToHex(int value) => (char)(value < 10 ? '0' + value : 'A' + (value - 10));
}
