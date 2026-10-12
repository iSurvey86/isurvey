using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;

namespace iSurvey.Auth;

/// <summary>Mã thiết bị: lấy MachineGuid (HKLM) → SHA-256 → "PC-SURVEY-XXXXXXXX" (8 hex).</summary>
public static class HardwareId
{
    private static string? _guid;

    /// <summary>MachineGuid thô (dùng làm entropy/khóa HMAC cục bộ, không hiển thị cho người dùng).</summary>
    public static string MachineGuid => _guid ??= ReadMachineGuid();

    public static string Current => FromMachineGuid(MachineGuid);

    public static string FromMachineGuid(string machineGuid)
    {
        var hash = Digest.Sha256(Encoding.UTF8.GetBytes("iSurvey|hwid|" + machineGuid));
        return "PC-SURVEY-" + Digest.Hex(hash, 0, 4);
    }

    private static string ReadMachineGuid()
    {
        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            using var key = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography");
            if (key?.GetValue("MachineGuid") is string g && !string.IsNullOrWhiteSpace(g))
                return g.Trim();
        }
        catch
        {
            // rơi xuống dự phòng
        }

        // Dự phòng hiếm gặp (registry bị chặn): tên máy — vẫn ổn định trên cùng một máy.
        return "FALLBACK-" + Environment.MachineName;
    }
}
