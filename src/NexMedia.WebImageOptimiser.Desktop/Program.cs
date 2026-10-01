using Velopack;

namespace NexMedia.WebImageOptimiser.Desktop;

internal static class Program
{
    [STAThread]
    public static void Main()
    {
        VelopackApp.Build().SetAutoApplyOnStartup(false).Run();
        Configuration.AppUpdateService.ApplyStablePendingUpdateOnStartup();
        var app = new App();
        app.InitializeComponent();
        app.Run();
    }
}
