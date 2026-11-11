# E-XAML-STN-ADD — StationList Add_Click

- **file:** `src/UI/Tuyen/StationListWindow.xaml.cs`
- **lines:** 96-100
- **git HEAD:** `3a16cb3959dc4e65c08f77b6de26669b309739cd`
- **redacted:** NO
- **note:** AddRequested

```csharp
    private void Add_Click(object sender, RoutedEventArgs e)
    {
        Result = new StationListResult { AddRequested = true };
        Close();
    }
```
