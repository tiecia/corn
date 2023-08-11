using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Widget;
using CornApp.Platforms.Android;

namespace CornApp;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    private WidgetServiceConnection WidgetConnection = new WidgetServiceConnection();
    private Intent ServiceIntent;

    protected override void OnCreate(Bundle savedInstanceState) {
        base.OnCreate(savedInstanceState);

        ServiceIntent = new Intent(this, typeof(WidgetService));
        StartForegroundService(ServiceIntent);
    }

    private void BindService() {
        if(WidgetConnection == null) {
            WidgetConnection = new WidgetServiceConnection();
        }
        ApplicationContext.BindService(ServiceIntent, WidgetConnection, Bind.AutoCreate);
    }

    private void UnbindService() {
        ApplicationContext.UnbindService(WidgetConnection);
    }

    protected override void OnPause() {
        base.OnPause();
        UnbindService();
    }

    protected override void OnResume() {
        base.OnResume();
        BindService();
    }
}
