# Evidence `XAML-MapInsert-Apply_Click`

- **file:** `src/UI/MapInsert/MapInsertWindow.xaml.cs`
- **lines:** 378-440
- **git HEAD:** `3a16cb3959dc4e65c08f77b6de26669b309739cd`
- **redacted:** NO
- **note:** Validate tỉnh/meridian/zone → tạo MapInsertSettings → DialogResult=true (caller MapModule ApplyBasemap)

```csharp
{

        if (ProvinceCombo.SelectedItem is not ProvinceCrsGroup group)

        {

            System.Windows.MessageBox.Show(this, "Vui lòng chọn tỉnh / thành phố.", "iSurvey",

                MessageBoxButton.OK, MessageBoxImage.Warning);

            return;

        }



        if (!TryParseMeridian(out var meridian))

            return;

        var zone = Zone6Radio.IsChecked == true ? 6 : 3;
        if (zone == 6)
            meridian = CoordinateService.SnapToTm6CentralMeridian(meridian);

        LegacyAreaEntry? area = group.LegacyAreas.Count > 1

            ? AreaCombo.SelectedItem as LegacyAreaEntry

            : group.LegacyAreas.FirstOrDefault();



        Settings = new MapInsertSettings

        {

            ProvinceName = group.ProvinceName,

            AreaLabel = area?.Label ?? group.ProvinceName,

            CentralMeridian = meridian,

            ZoneWidthDegrees = zone,

            BasemapId = BasemapCombo.SelectedValue as string ?? TileCacheService.DefaultSourceId,

            AutoRefresh = true,

            UseBoundaryClip = BoundaryRadio.IsChecked == true,

            Action = MapInsertAction.Insert

        };



        DialogResult = true;

        Close();

    }
```
