# E-XAML-STN-ZOOM — StationList Zoom_Click

- **file:** `src/UI/Tuyen/StationListWindow.xaml.cs`
- **lines:** 67-76
- **git HEAD:** `3a16cb3959dc4e65c08f77b6de26669b309739cd`
- **redacted:** NO
- **note:** ZoomTo

```csharp
    private void Zoom_Click(object sender, RoutedEventArgs e)
    {
        if (Grid.SelectedItem is not StationListRow row)
        {
            System.Windows.MessageBox.Show(this, "Hãy chọn một cọc trên bảng.", "iSurvey");
            return;
        }

        RouteService.ZoomTo(_doc, row.Station.X, row.Station.Y);
    }
```
