# E-NUMBER-TXT — NUMBER_VERTICES txt

- **file:** `src/Modules/Tuyen/VertexNumberingService.cs`
- **lines:** 225-237
- **git HEAD:** `3a16cb3959dc4e65c08f77b6de26669b309739cd`
- **redacted:** NO
- **note:** AngleTableExport file

```csharp
        tr.Commit();
        ed.WriteMessage($"\n[iSurvey] Đã đánh số thành công {vertices.Count} góc tuyến (layer {LayerName}).");
        if (pushed > 0)
            ed.WriteMessage($"\n[iSurvey] Đã đẩy {pushed} chữ ra xa để không đè tim tuyến.");

        // Mô tả tuyến .txt UTF-8: từ Góc → Góc+1; MOTATUYEN placeholder điền tay.
        var txtPath = AngleTableExport.PromptSaveWriteAndOpen(ed, vertices, prefix, startIndex);
        if (!string.IsNullOrEmpty(txtPath))
            ed.WriteMessage($"\n[iSurvey] Đã lưu mô tả tuyến: {txtPath}");

        ActivityLogger.Result("ISURVEY_NUMBER_VERTICES",
            $"{vertices.Count} góc, tiền tố {prefix}, bắt đầu {startIndex}, tỷ lệ 1/{mapScale}, đẩy né tim {pushed}" +
            (string.IsNullOrEmpty(txtPath) ? "" : $", file {ActivityLogger.FileName(txtPath)}"));
```
