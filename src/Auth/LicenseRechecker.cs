namespace iSurvey.Auth;

public enum RecheckAction
{
    /// <summary>Chưa tới hạn 24 giờ / giấy phép không kiểm tra lại được (offline) / không có refresh token.</summary>
    Skip,
    /// <summary>Không xác minh được (mạng lỗi, timeout, refresh bị từ chối...) — giữ nguyên kích hoạt.</summary>
    NoChange,
    /// <summary>Server xác nhận thiết bị còn hiệu lực: cập nhật LastVerifiedAt và refresh token mới.</summary>
    Verified,
    /// <summary>Kết quả không rõ ràng nhưng refresh token đã bị đổi: chỉ lưu token mới, KHÔNG gia hạn.</summary>
    RotateOnly,
    /// <summary>Server trả rõ ràng thiết bị bị khóa (is_active=false) hoặc không còn đăng ký → khóa lại.</summary>
    Revoke
}

public sealed record RecheckOutcome(RecheckAction Action, string? NewRefreshToken = null);

/// <summary>
/// Logic kiểm tra lại giấy phép qua mạng — không phụ thuộc AutoCAD, không ném exception.
/// Nguyên tắc: chỉ <see cref="RecheckAction.Revoke"/> khi server nói RÕ RÀNG (blocked / not_registered).
/// Mọi lỗi khác (mạng, timeout, 429, 5xx, refresh token hỏng) → giữ nguyên; giấy phép tự hết hạn sau 7 ngày không xác minh.
/// </summary>
public static class LicenseRechecker
{
    public static async Task<RecheckOutcome> RunAsync(LicenseInfo lic, ISupabaseAuth client, DateTimeOffset now,
        TimeSpan timeout, CancellationToken ct = default)
    {
        if (lic.Mode != LicenseMode.Online || string.IsNullOrEmpty(lic.RefreshToken) || !lic.NeedsRecheck(now))
            return new RecheckOutcome(RecheckAction.Skip);

        string? rotated = null;
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(timeout);

            var refreshed = await client.RefreshAsync(lic.RefreshToken!, cts.Token).ConfigureAwait(false);
            if (refreshed.Session is null)
                return new RecheckOutcome(RecheckAction.NoChange);

            // Supabase đổi refresh token mỗi lần dùng: phải giữ token mới dù bước sau có lỗi.
            rotated = refreshed.Session.RefreshToken;

            var check = await client.CheckAsync(refreshed.Session.AccessToken, lic.HardwareId, cts.Token)
                .ConfigureAwait(false);

            if (!check.Result.Ok)
                return new RecheckOutcome(RecheckAction.RotateOnly, rotated);

            return check.Status switch
            {
                DeviceStatus.Ok => new RecheckOutcome(RecheckAction.Verified, rotated),
                DeviceStatus.Blocked or DeviceStatus.NotRegistered => new RecheckOutcome(RecheckAction.Revoke),
                _ => new RecheckOutcome(RecheckAction.RotateOnly, rotated)
            };
        }
        catch
        {
            return rotated is null
                ? new RecheckOutcome(RecheckAction.NoChange)
                : new RecheckOutcome(RecheckAction.RotateOnly, rotated);
        }
    }

    /// <summary>Giấy phép mới sau kiểm tra; null nghĩa là phải thu hồi.</summary>
    public static LicenseInfo? Apply(LicenseInfo current, RecheckOutcome outcome, DateTimeOffset now) =>
        outcome.Action switch
        {
            RecheckAction.Verified => current with { LastVerifiedAt = now, RefreshToken = outcome.NewRefreshToken },
            RecheckAction.RotateOnly => current with { RefreshToken = outcome.NewRefreshToken },
            RecheckAction.Revoke => null,
            _ => current
        };
}
