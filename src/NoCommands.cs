namespace iSurvey;

/// <summary>
/// Marker cho DevReload: ngăn AutoCAD tự đăng ký [CommandMethod] khi nạp DLL.
/// DevReload đăng ký lệnh thủ công và có thể gỡ trước khi reload.
/// Chỉ dùng khi build Debug (xem README).
/// </summary>
public sealed class NoCommands;
