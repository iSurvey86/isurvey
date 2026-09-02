namespace iSurvey.Core;

/// <summary>
/// Giao diện chung cho các module mở rộng của iSurvey.
/// </summary>
public interface ISurveyModule
{
    /// <summary>Tên hiển thị của module trên ribbon hoặc menu.</summary>
    string DisplayName { get; }

    /// <summary>Khởi tạo module khi add-in được nạp.</summary>
    void Initialize();

    /// <summary>Giải phóng tài nguyên khi add-in thoát.</summary>
    void Terminate();
}
