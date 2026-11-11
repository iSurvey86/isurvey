# E-XAML-STN-DEL — StationList Delete_Click

- **file:** `src/UI/Tuyen/StationListWindow.xaml.cs`
- **lines:** 78-94
- **git HEAD:** `3a16cb3959dc4e65c08f77b6de26669b309739cd`
- **redacted:** NO
- **note:** RemoveStation

```csharp
    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (Grid.SelectedItem is not StationListRow row)
        {
            System.Windows.MessageBox.Show(this, "Hãy chọn một cọc trên bảng.", "iSurvey");
            return;
        }

        if (!RouteService.RemoveStation(_doc, _route, row.Station))
        {
            System.Windows.MessageBox.Show(this, "Không xóa được cọc đã chọn.", "iSurvey");
            return;
        }

        ReloadRows();
        TitleText.Text = "iSurvey — Cọc: " + _route.Name + " (" + _rows.Count + ")";
    }
```
