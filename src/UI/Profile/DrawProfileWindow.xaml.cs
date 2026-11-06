using System.Windows;
using System.Windows.Input;
using iSurvey.Modules.Profile;
using iSurvey.Modules.Tuyen;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using Win32OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using Win32SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace iSurvey.UI.Profile;

public enum ProfileDialogAction
{
    Cancel,
    Draw,
    UpdateFromProfile,
    UpdateProperties,
    UpdateFromTin,
    SaveSettingsOnly
}

public sealed class DrawProfileDialogResult
{
    public ProfileDialogAction Action { get; init; }

    public ProfileDrawOptions? Options { get; init; }
}

public partial class DrawProfileWindow : Window
{
    private readonly DrawProfileViewModel _vm;

    public DrawProfileDialogResult? Result { get; private set; }

    public DrawProfileWindow(IReadOnlyList<RouteRecord> routes, string? preferredRouteId)
    {
        InitializeComponent();
        _vm = new DrawProfileViewModel(routes, preferredRouteId, ProfileSettingsStore.Load());
        DataContext = _vm;
    }

    public static DrawProfileDialogResult? Prompt(IReadOnlyList<RouteRecord> routes, string? preferredRouteId)
    {
        var window = new DrawProfileWindow(routes, preferredRouteId);
        AcadApp.ShowModalWindow(window);
        return window.Result;
    }

    private void Draw_Click(object sender, RoutedEventArgs e) => Finish(ProfileDialogAction.Draw);

    private void UpdateFromProfile_Click(object sender, RoutedEventArgs e) =>
        Finish(ProfileDialogAction.UpdateFromProfile);

    private void UpdateProps_Click(object sender, RoutedEventArgs e) =>
        Finish(ProfileDialogAction.UpdateProperties);

    private void UpdateFromTin_Click(object sender, RoutedEventArgs e) =>
        Finish(ProfileDialogAction.UpdateFromTin);

    private void SaveSettings_Click(object sender, RoutedEventArgs e)
    {
        if (!_vm.TryBuildOptions(out var opts, out var error))
        {
            System.Windows.MessageBox.Show(this, error, "iSurvey");
            return;
        }

        ProfileSettingsStore.Save(opts);
        System.Windows.MessageBox.Show(this, "Đã lưu thiết lập VEMCD.", "iSurvey");
    }

    private void Finish(ProfileDialogAction action)
    {
        if (!_vm.TryBuildOptions(out var opts, out var error))
        {
            System.Windows.MessageBox.Show(this, error, "iSurvey");
            return;
        }

        Result = new DrawProfileDialogResult { Action = action, Options = opts };
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Result = new DrawProfileDialogResult { Action = ProfileDialogAction.Cancel };
        Close();
    }

    private void AddRow_Click(object sender, RoutedEventArgs e) => _vm.AddRow();

    private void RemoveRow_Click(object sender, RoutedEventArgs e) => _vm.RemoveSelectedRow();

    private void Import_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new Win32OpenFileDialog
        {
            Title = "Nhập mẫu bảng trắc dọc",
            Filter = "JSON (*.json)|*.json|CSV (*.csv)|*.csv|Tất cả|*.*"
        };
        if (dlg.ShowDialog(this) != true)
            return;
        try
        {
            var path = dlg.FileName;
            if (path.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                var rows = ProfileTemplateIO.ImportCsv(path);
                _vm.LoadTemplate(new ProfileTemplateIO.TemplateFile
                {
                    Name = System.IO.Path.GetFileNameWithoutExtension(path),
                    Rows = rows
                });
            }
            else
            {
                _vm.LoadTemplate(ProfileTemplateIO.ImportJson(path));
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(this, "Không nhập được: " + ex.Message, "iSurvey");
        }
    }

    private void Export_Click(object sender, RoutedEventArgs e)
    {
        if (!_vm.TryBuildOptions(out var opts, out var error))
        {
            System.Windows.MessageBox.Show(this, error, "iSurvey");
            return;
        }

        var dlg = new Win32SaveFileDialog
        {
            Title = "Xuất mẫu bảng trắc dọc",
            Filter = "JSON (*.json)|*.json|CSV (*.csv)|*.csv",
            FileName = opts.TemplateName
        };
        if (dlg.ShowDialog(this) != true)
            return;
        try
        {
            if (dlg.FilterIndex == 2 || dlg.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                ProfileTemplateIO.ExportCsv(dlg.FileName, opts.Rows);
            else
                ProfileTemplateIO.ExportJson(dlg.FileName, opts.TemplateName, opts.LabelWidth, opts.Rows);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(this, "Không xuất được: " + ex.Message, "iSurvey");
        }
    }

    private void Header_Drag(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
            DragMove();
    }

    private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key != Key.Escape)
            return;
        Result = new DrawProfileDialogResult { Action = ProfileDialogAction.Cancel };
        Close();
    }
}
