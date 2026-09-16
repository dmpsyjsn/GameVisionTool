using System.Diagnostics;

namespace GameVisionTool.UI.Maui.Views;

public partial class MainPage : ContentPage
{
    public MainPage()
    {

        InitializeComponent();

        BindingContext = this;
    }

    private void OnOpenLogsFolderClicked(object? sender, EventArgs e)
    {
        var logsFolder = Path.Combine(FileSystem.AppDataDirectory, "logs");

        Directory.CreateDirectory(logsFolder);

        Process.Start(new ProcessStartInfo(logsFolder) { UseShellExecute = true });
    }
}