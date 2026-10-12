using System.ComponentModel;
using System.Runtime.InteropServices;

namespace iSurvey.Auth;

/// <summary>
/// DPAPI (CurrentUser) qua P/Invoke crypt32 — không cần thêm gói NuGet/DLL vào bundle.
/// Entropy bổ sung giúp dữ liệu gắn với MachineGuid.
/// </summary>
internal static class Dpapi
{
    private const int CryptProtectUiForbidden = 0x1;

    [StructLayout(LayoutKind.Sequential)]
    private struct DataBlob
    {
        public int cbData;
        public IntPtr pbData;
    }

    [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CryptProtectData(ref DataBlob pDataIn, string? szDataDescr, ref DataBlob pOptionalEntropy,
        IntPtr pvReserved, IntPtr pPromptStruct, int dwFlags, out DataBlob pDataOut);

    [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CryptUnprotectData(ref DataBlob pDataIn, IntPtr ppszDataDescr, ref DataBlob pOptionalEntropy,
        IntPtr pvReserved, IntPtr pPromptStruct, int dwFlags, out DataBlob pDataOut);

    [DllImport("kernel32.dll")]
    private static extern IntPtr LocalFree(IntPtr hMem);

    public static byte[] Protect(byte[] data, byte[] entropy) =>
        Run(data, entropy, protect: true);

    /// <summary>Giải mã; ném CryptographicException (Win32Exception) nếu sai user/entropy/dữ liệu hỏng.</summary>
    public static byte[] Unprotect(byte[] data, byte[] entropy) =>
        Run(data, entropy, protect: false);

    private static byte[] Run(byte[] data, byte[] entropy, bool protect)
    {
        var input = ToBlob(data);
        var ent = ToBlob(entropy);
        DataBlob output = default;
        try
        {
            var ok = protect
                ? CryptProtectData(ref input, "iSurvey license", ref ent, IntPtr.Zero, IntPtr.Zero, CryptProtectUiForbidden, out output)
                : CryptUnprotectData(ref input, IntPtr.Zero, ref ent, IntPtr.Zero, IntPtr.Zero, CryptProtectUiForbidden, out output);

            if (!ok)
                throw new System.Security.Cryptography.CryptographicException(
                    new Win32Exception(Marshal.GetLastWin32Error()).Message);

            var result = new byte[output.cbData];
            Marshal.Copy(output.pbData, result, 0, output.cbData);
            return result;
        }
        finally
        {
            Free(input);
            Free(ent);
            if (output.pbData != IntPtr.Zero)
                LocalFree(output.pbData);
        }
    }

    private static DataBlob ToBlob(byte[] bytes)
    {
        var blob = new DataBlob { cbData = bytes.Length, pbData = Marshal.AllocHGlobal(Math.Max(1, bytes.Length)) };
        Marshal.Copy(bytes, 0, blob.pbData, bytes.Length);
        return blob;
    }

    private static void Free(DataBlob blob)
    {
        if (blob.pbData != IntPtr.Zero)
            Marshal.FreeHGlobal(blob.pbData);
    }
}
