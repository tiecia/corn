using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Text.Style;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Timer = System.Threading.Timer;

namespace CornApp.Platforms.Android {
    // Add Exported = true for your app to run as expected in Android 12 and above.
    [BroadcastReceiver(Label = "Corn Status", Exported = true)]
    [IntentFilter(new string[] { "android.appwidget.action.APPWIDGET_UPDATE" })]
    [MetaData("android.appwidget.provider", Resource = "@xml/appwidgetprovider")]
    [Service(Exported = true)]
    public class DailyWidget : AppWidgetProvider {
        public override void OnUpdate(Context context, AppWidgetManager appWidgetManager, int[] appWidgetIds) {
            UpdateWidgetAsync(context);
            Debug.WriteLine("OnUpdate");
        }

        public static async void UpdateWidgetAsync(Context context) {
            Debug.WriteLine("UpdateWidget");
            var appWidgetManager = AppWidgetManager.GetInstance(context);
            int[] widgetIds = appWidgetManager.GetAppWidgetIds(new ComponentName(context, Java.Lang.Class.FromType(typeof(DailyWidget)).Name));

            foreach (int id in widgetIds) {
                RemoteViews remoteViews = new RemoteViews(context.PackageName, Resource.Layout.DailyWidget);
                //remoteViews.SetTextViewText(Resource.Id.statusText, "Status: " + (await CornMonitor.Singleton.GetShuckerInfoAsync()).ShuckStatus);
                //remoteViews.SetImageViewResource();
                appWidgetManager.UpdateAppWidget(id, remoteViews);
            }
        }

        public override void OnReceive(Context context, Intent intent) {
            base.OnReceive(context, intent);
            Debug.WriteLine("OnReceive " + intent.Action);
        }

    }
}
