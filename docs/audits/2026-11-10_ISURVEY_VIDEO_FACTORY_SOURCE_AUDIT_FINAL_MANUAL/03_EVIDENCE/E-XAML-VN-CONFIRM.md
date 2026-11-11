# E-XAML-VN-CONFIRM — Vn2000 Confirm_Click

- **file:** `src/UI/Vn2000/Vn2000SetupWindow.xaml.cs`
- **lines:** 197-230
- **git HEAD:** `3a16cb3959dc4e65c08f77b6de26669b309739cd`
- **redacted:** NO
- **note:** CRS helper

```csharp
    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        if (ProvinceCombo.SelectedItem is not ProvinceCrsGroup group)
        {
            System.Windows.MessageBox.Show(this, "Hãy chọn tỉnh / thành phố.", "iSurvey");
            return;
        }

        if (!TryReadMeridian(out var entered, out var error))
        {
            System.Windows.MessageBox.Show(this, error, "iSurvey");
            return;
        }

        var zone = Zone6Radio.IsChecked == true ? 6 : 3;
        var used = zone == 6 ? CoordinateService.SnapToTm6CentralMeridian(entered) : entered;

        var textHeight = _saved.ImportTextHeight;
        var tolerance = _saved.ImportSimplifyTolerance > 0 ? _saved.ImportSimplifyTolerance : 0.2;
        if (_mode == Vn2000SetupMode.Import)
        {
            if (!double.TryParse(TextHeightBox.Text.Trim().Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out textHeight)
                || textHeight <= 0 || textHeight > 1000)
            {
                System.Windows.MessageBox.Show(this, "Cỡ chữ phải lớn hơn 0.", "iSurvey");
                return;
            }

            if (!double.TryParse(ToleranceBox.Text.Trim().Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out tolerance)
                || tolerance <= 0 || tolerance > 1000)
            {
                System.Windows.MessageBox.Show(this, "Sai số lọc đỉnh phải lớn hơn 0 mét.", "iSurvey");
                return;
            }
```
