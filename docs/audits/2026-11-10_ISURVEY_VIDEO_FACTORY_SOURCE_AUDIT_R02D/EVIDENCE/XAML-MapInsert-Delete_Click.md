# Evidence `XAML-MapInsert-Delete_Click`

- **file:** `src/UI/MapInsert/MapInsertWindow.xaml.cs`
- **lines:** 366-374
- **git HEAD:** `3a16cb3959dc4e65c08f77b6de26669b309739cd`
- **redacted:** NO
- **note:** Đóng dialog (DialogResult=false) rồi GeDeleteWorkflow.Run(doc) — xóa ảnh GE theo workflow DELETE_GE

```csharp
{
        DialogResult = false;
        Close();

        var doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
        if (doc is not null)
            GeDeleteWorkflow.Run(doc);
    }
```
