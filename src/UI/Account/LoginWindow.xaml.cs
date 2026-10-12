using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using iSurvey.Auth;
using WpfKeyEventArgs = System.Windows.Input.KeyEventArgs;
using WpfTextBox = System.Windows.Controls.TextBox;

namespace iSurvey.UI.Account;

/// <summary>
/// Cửa sổ đăng nhập Gmail + OTP 6 số (WPF dark theme). Mở bằng Application.ShowModalWindow().
/// Không có nút xác nhận: đủ 6 số là tự xác thực.
/// </summary>
public partial class LoginWindow : Window
{
    private const string HintText = "Nhập đủ 6 số để tự động xác thực.";

    private static readonly SolidColorBrush Red = Frozen(0xFF, 0x6B, 0x6B);
    private static readonly SolidColorBrush Green = Frozen(0x4A, 0xDE, 0x80);
    private static readonly SolidColorBrush Muted = Frozen(0x8C, 0x96, 0xA8);

    private readonly WpfTextBox[] _boxes;
    private readonly DispatcherTimer _cooldownTimer = new() { Interval = TimeSpan.FromSeconds(1) };
    private readonly DispatcherTimer _successTimer = new() { Interval = TimeSpan.FromMilliseconds(700) };
    private int _cooldownLeft;
    private bool _sending;

    /// <summary>Đang xác thực / hiệu ứng lỗi / đã thành công — bỏ qua mọi nhập liệu vào 6 ô.</summary>
    private bool _inputLocked;

    /// <summary>Đã đủ 6 số nhưng Gmail chưa hợp lệ — tự xác thực ngay khi Gmail được sửa đúng.</summary>
    private bool _awaitingEmail;

    private bool _closed;

    public LoginWindow()
    {
        InitializeComponent();

        _boxes = OtpPanel.Children.OfType<WpfTextBox>().ToArray();
        OtpPanel.RenderTransform = new TranslateTransform();

        foreach (var box in _boxes)
        {
            box.TextChanged += (_, _) => OnOtpTextChanged();
            box.PreviewTextInput += Box_PreviewTextInput;
            box.PreviewKeyDown += Box_PreviewKeyDown;
            box.PreviewMouseLeftButtonDown += Box_PreviewMouseLeftButtonDown;
            box.GotKeyboardFocus += (s, _) => ((WpfTextBox)s).SelectAll();
            System.Windows.DataObject.AddPastingHandler(box, Box_Pasting);
        }

        EmailBox.TextChanged += (_, _) => OnEmailTextChanged();

        FooterText.Text = $"Mã thiết bị (HWID): {HardwareId.Current} • Phiên bản: {AuthManager.AppVersion}";
        ShowHint();

        _cooldownTimer.Tick += CooldownTick;
        _successTimer.Tick += (_, _) =>
        {
            _successTimer.Stop();
            Close();
        };
        PreviewKeyDown += Window_PreviewKeyDown;
        Loaded += (_, _) => EmailBox.Focus();
        Closed += (_, _) =>
        {
            _closed = true;
            _cooldownTimer.Stop();
            _successTimer.Stop();
        };
    }

    private static SolidColorBrush Frozen(byte r, byte g, byte b)
    {
        var brush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(r, g, b));
        brush.Freeze();
        return brush;
    }

    // ---------- Cửa sổ ----------
    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
            DragMove();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void Window_PreviewKeyDown(object sender, WpfKeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            e.Handled = true;
            Close();
        }
        else if (e.Key == Key.Enter && EmailBox.IsKeyboardFocused)
        {
            e.Handled = true;
            Send_Click(sender, e);
        }
    }

    // ---------- Gửi mã ----------
    private async void Send_Click(object sender, RoutedEventArgs e)
    {
        if (_sending || !SendButton.IsEnabled)
            return;

        _sending = true;
        SendButton.IsEnabled = false;
        SetSendStatus("Đang gửi mã...", Muted);
        ShowHint();

        SendResult result;
        try
        {
            result = await AuthManager.Otp.SendAsync(EmailBox.Text);
        }
        catch (System.Exception ex)
        {
            _sending = false;
            SendButton.IsEnabled = true;
            SetSendStatus($"Lỗi gửi mã: {ex.Message}", Red);
            return;
        }

        _sending = false;

        switch (result.Outcome)
        {
            case SendOutcome.InvalidEmail:
                SendButton.IsEnabled = true;
                SetSendStatus("Chỉ chấp nhận địa chỉ Gmail hợp lệ (…@gmail.com).", Red);
                EmailBox.Focus();
                return;

            case SendOutcome.Cooldown:
                StartCooldown(result.CooldownSeconds);
                SetSendStatus($"Vui lòng chờ {result.CooldownSeconds} giây trước khi gửi lại.", Muted);
                return;

            case SendOutcome.Delivered:
                StartCooldown((int)OtpAuthService.ResendCooldown.TotalSeconds);
                SetSendStatus($"Đã gửi mã tới {EmailBox.Text.Trim()}. Mã có hiệu lực trong vài phút.", Green);
                FocusBox(0);
                return;

            case SendOutcome.RateLimited:
                StartCooldown((int)OtpAuthService.ResendCooldown.TotalSeconds);
                SetSendStatus("Máy chủ đang giới hạn số lần gửi mã. Hãy chờ ít phút rồi thử lại.", Red);
                PrintDetail(result.Detail);
                return;

            default:
                StartCooldown((int)OtpAuthService.ResendCooldown.TotalSeconds);
                var reason = result.Outcome == SendOutcome.NotConfigured
                    ? "Máy này chưa cấu hình gửi mail."
                    : "Không gửi được mail (lỗi mạng hoặc máy chủ).";
                PrintDetail(result.Detail);
                var text = $"{reason} Hãy kiểm tra mạng hoặc liên hệ Admin để nhận mã kích hoạt trong ngày.";
                if (result.TestHint is not null)
                {
                    text += $"\n{result.TestHint}";
                    AuthManager.Print($"\n[iSurvey] {result.TestHint}");
                }

                SetSendStatus(text, Red);
                FocusBox(0);
                return;
        }
    }

    private void StartCooldown(int seconds)
    {
        _cooldownLeft = Math.Max(1, seconds);
        SendButton.IsEnabled = false;
        SendButton.Content = $"Gửi lại ({_cooldownLeft}s)";
        _cooldownTimer.Start();
    }

    private void CooldownTick(object? sender, EventArgs e)
    {
        _cooldownLeft--;
        if (_cooldownLeft <= 0)
        {
            _cooldownTimer.Stop();
            SendButton.Content = "Gửi mã";
            SendButton.IsEnabled = true;
        }
        else
        {
            SendButton.Content = $"Gửi lại ({_cooldownLeft}s)";
        }
    }

    private void SetSendStatus(string text, System.Windows.Media.Brush brush)
    {
        SendStatus.Text = text;
        SendStatus.Foreground = brush;
    }

    // ---------- Tự xác thực khi đủ 6 số ----------
    private void SetVerifyStatus(string text, System.Windows.Media.Brush brush)
    {
        VerifyStatus.Text = text;
        VerifyStatus.Foreground = brush;
    }

    private void ShowHint() => SetVerifyStatus(HintText, Muted);

    private bool AllBoxesFilled() => _boxes.All(b => b.Text.Length == 1);

    private void OnOtpTextChanged()
    {
        if (_inputLocked || _closed || !AllBoxesFilled())
            return;

        // Tách khỏi handler nhập liệu đang chạy (gõ/dán) rồi mới xác thực.
        Dispatcher.BeginInvoke(new Action(() => _ = VerifyAsync()), DispatcherPriority.Input);
    }

    private void OnEmailTextChanged()
    {
        if (_awaitingEmail && !_inputLocked && AllBoxesFilled()
            && OtpAuthService.TryNormalizeGmail(EmailBox.Text, out _))
        {
            _awaitingEmail = false;
            ShowHint();
            Dispatcher.BeginInvoke(new Action(() => _ = VerifyAsync()), DispatcherPriority.Input);
        }
    }

    private async Task VerifyAsync()
    {
        if (_inputLocked || _closed || !AllBoxesFilled())
            return;

        if (!OtpAuthService.TryNormalizeGmail(EmailBox.Text, out var email))
        {
            // Giữ nguyên 6 số, không tính lượt thử; sửa Gmail đúng là tự xác thực tiếp.
            _awaitingEmail = true;
            SetVerifyStatus("Hãy nhập Gmail hợp lệ ở ô phía trên.", Red);
            EmailBox.Focus();
            return;
        }

        _inputLocked = true;
        SetVerifyStatus("Đang xác thực...", Muted);

        var code = string.Concat(_boxes.Select(b => b.Text));
        var result = await AuthManager.Otp.VerifyAsync(email, code);

        if (_closed)
        {
            // Người dùng đóng cửa sổ trong lúc đang xác thực: vẫn phải lưu giấy phép nếu đã thành công.
            if (result.Ok)
            {
                try
                {
                    AuthManager.Activate(email, result);
                }
                catch
                {
                    // bỏ qua
                }
            }

            return;
        }

        if (result.Ok)
        {
            try
            {
                AuthManager.Activate(email, result);
            }
            catch (System.Exception ex)
            {
                SetVerifyStatus($"Không lưu được giấy phép: {ex.Message}", Red);
                ClearBoxes();
                _inputLocked = false;
                return;
            }

            Keyboard.ClearFocus();   // để cả 6 ô cùng viền xanh lá
            SetBoxBorders(Green);
            SetVerifyStatus("✓ Kích hoạt thành công", Green);
            AuthManager.Print($"\n[iSurvey] Kich hoat thanh cong ({email}).");
            _successTimer.Start();   // giữ khóa nhập; đóng cửa sổ sau ~0,7 giây
            return;
        }

        switch (result.Outcome)
        {
            case VerifyOutcome.WrongCode:
                SetVerifyStatus($"Mã không đúng hoặc đã hết hạn. Còn {result.AttemptsLeft} lần thử.", Red);
                PrintDetail(result.Detail);
                break;

            case VerifyOutcome.LockedOut when result.JustLocked:
                SetVerifyStatus(
                    $"Mã không đúng. Bạn đã nhập sai {OtpAuthService.MaxAttempts} lần, thử lại sau {Math.Max(1, result.LockedMinutes)} phút.",
                    Red);
                break;

            case VerifyOutcome.LockedOut:
                SetVerifyStatus($"Đã nhập sai quá nhiều lần. Thử lại sau {Math.Max(1, result.LockedMinutes)} phút.", Red);
                break;

            case VerifyOutcome.Expired:
                SetVerifyStatus("Mã đã hết hạn. Hãy bấm Gửi mã để nhận mã mới.", Red);
                break;

            case VerifyOutcome.Blocked:
                SetVerifyStatus("Tài khoản hoặc thiết bị này đã bị khóa bản quyền. Vui lòng liên hệ Admin!", Red);
                break;

            case VerifyOutcome.DeviceLimit:
                SetVerifyStatus(
                    $"Gmail này đã dùng đủ {(result.MaxDevices > 0 ? result.MaxDevices : 2)} thiết bị. " +
                    "Hãy bấm \"Đăng Xuất / Đổi Máy\" trên máy cũ hoặc liên hệ Admin.",
                    Red);
                break;

            case VerifyOutcome.RateLimited:
                SetVerifyStatus("Máy chủ đang giới hạn tần suất xác thực. Hãy chờ ít phút rồi nhập lại mã.", Red);
                PrintDetail(result.Detail);
                break;

            case VerifyOutcome.NetworkError:
                SetVerifyStatus("Không kết nối được máy chủ. Kiểm tra mạng rồi nhập lại mã (không bị trừ lượt thử).", Red);
                PrintDetail(result.Detail);
                break;

            case VerifyOutcome.ServerRejected:
                SetVerifyStatus("Máy chủ từ chối kích hoạt thiết bị này. Vui lòng liên hệ Admin.", Red);
                PrintDetail(result.Detail);
                break;

            case VerifyOutcome.NoPending:
                SetVerifyStatus("Chưa có mã. Hãy bấm Gửi mã để nhận mã trước.", Red);
                _inputLocked = false;
                EmailBox.Focus();
                return;

            default:
                SetVerifyStatus("Mã phải gồm đúng 6 chữ số.", Red);
                break;
        }

        // Sai: viền đỏ + rung, xóa sạch 6 ô; sau hiệu ứng ngắn mở lại ô đầu để nhập mã mới.
        Keyboard.ClearFocus();   // ô đang focus sẽ đè viền đỏ bằng viền xanh của trạng thái focus
        SetBoxBorders(Red);
        Shake();
        ClearBoxes(focus: false);

        await Task.Delay(450);
        if (_closed)
            return;

        SetBoxBorders(null);
        _inputLocked = false;
        FocusBox(0);
    }

    /// <summary>Chi tiết kỹ thuật (ASCII) lên Command Line để Admin chẩn đoán (không hiện cho người dùng trong cửa sổ).</summary>
    private static void PrintDetail(string? detail)
    {
        if (!string.IsNullOrEmpty(detail))
            AuthManager.Print($"\n[iSurvey] Chi tiet: {detail}");
    }

    private void Shake()
    {
        var anim = new DoubleAnimationUsingKeyFrames { Duration = TimeSpan.FromMilliseconds(360) };
        double[] offsets = [0, -8, 8, -6, 6, -3, 0];
        for (var i = 0; i < offsets.Length; i++)
            anim.KeyFrames.Add(new LinearDoubleKeyFrame(offsets[i], KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(i * 60))));

        ((TranslateTransform)OtpPanel.RenderTransform).BeginAnimation(TranslateTransform.XProperty, anim);
    }

    /// <summary>null = trả về viền mặc định (xanh dương) của style.</summary>
    private void SetBoxBorders(System.Windows.Media.Brush? brush)
    {
        foreach (var b in _boxes)
        {
            if (brush is null)
                b.ClearValue(WpfTextBox.BorderBrushProperty);
            else
                b.BorderBrush = brush;
        }
    }

    // ---------- 6 ô OTP ----------
    private void ClearBoxes(bool focus = true)
    {
        foreach (var b in _boxes)
            b.Text = "";
        if (focus)
            FocusBox(0);
    }

    private void FocusBox(int index)
    {
        if (index >= _boxes.Length)
            return;

        _boxes[Math.Max(0, index)].Focus();
    }

    /// <summary>Bắt đầu nhập mã mới: bỏ thông báo lỗi cũ và viền đỏ.</summary>
    private void BeginNewInput()
    {
        _awaitingEmail = false;
        if (!ReferenceEquals(VerifyStatus.Foreground, Muted))
            ShowHint();
        SetBoxBorders(null);
    }

    private void Box_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = true;
        if (_inputLocked || e.Text.Length != 1 || !Ascii.IsDigit(e.Text[0]))
            return;

        BeginNewInput();
        var box = (WpfTextBox)sender;
        box.Text = e.Text;
        FocusBox(Array.IndexOf(_boxes, box) + 1);
    }

    private void Box_PreviewKeyDown(object sender, WpfKeyEventArgs e)
    {
        var box = (WpfTextBox)sender;
        var i = Array.IndexOf(_boxes, box);

        if (_inputLocked)
        {
            e.Handled = true;
            return;
        }

        switch (e.Key)
        {
            case Key.Space:
                e.Handled = true;
                break;

            case Key.Back when box.Text.Length == 0 && i > 0:
                _boxes[i - 1].Text = "";
                _boxes[i - 1].Focus();
                e.Handled = true;
                break;

            case Key.Left when i > 0:
                _boxes[i - 1].Focus();
                e.Handled = true;
                break;

            case Key.Right when i < _boxes.Length - 1:
                _boxes[i + 1].Focus();
                e.Handled = true;
                break;
        }
    }

    /// <summary>Click chuột vào ô: focus + chọn hết để gõ số mới thay thế ngay.</summary>
    private void Box_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var box = (WpfTextBox)sender;
        if (!box.IsKeyboardFocusWithin)
        {
            box.Focus();
            e.Handled = true;
        }
    }

    /// <summary>Ctrl+V: chỉ lấy chữ số; đủ 6 số thì điền từ ô đầu (rồi tự xác thực), ít hơn thì điền từ ô hiện tại.</summary>
    private void Box_Pasting(object sender, System.Windows.DataObjectPastingEventArgs e)
    {
        e.CancelCommand();
        if (_inputLocked)
            return;

        var raw = e.DataObject.GetData(System.Windows.DataFormats.UnicodeText) as string;
        if (string.IsNullOrEmpty(raw))
            return;

        var digits = new string(raw.Where(Ascii.IsDigit).ToArray());
        if (digits.Length == 0)
            return;

        BeginNewInput();
        var start = digits.Length >= _boxes.Length ? 0 : Array.IndexOf(_boxes, (WpfTextBox)sender);
        var k = 0;
        for (; k < digits.Length && start + k < _boxes.Length; k++)
            _boxes[start + k].Text = digits[k].ToString();

        FocusBox(start + k);
    }
}
