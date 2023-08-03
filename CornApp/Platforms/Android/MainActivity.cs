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
    protected override void OnCreate(Bundle savedInstanceState) {
        base.OnCreate(savedInstanceState);

        Intent serviceIntent = new Intent(this, typeof(WidgetService));
        StartForegroundService(serviceIntent);
        ApplicationContext.BindService(serviceIntent, new WidgetServiceConnection(), Bind.AutoCreate);


        //AppWidgetManager appWidgetManager = AppWidgetManager.GetInstance(this);
        //RemoteViews remoteViews = new RemoteViews(PackageName, Resource.Layout.Widget);
        //remoteViews.SetTextViewText(Resource.Id.statusText, "Updated text1");
        //var me = new ComponentName(this, Java.Lang.Class.FromType(typeof(AppWidget)).Name);
        //appWidgetManager.UpdateAppWidget(me, remoteViews);
    }
}
