using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Text.Style;
using Android.Views;
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

                if(CornMonitor.Singleton.User == "")
                {
                    remoteViews.SetTextViewText(Resource.Id.textView, "No user");
                    remoteViews.SetViewVisibility(Resource.Id.textView, ViewStates.Visible);
                    remoteViews.SetViewVisibility(Resource.Id.cornView, ViewStates.Invisible);
                } else {
                    remoteViews.SetViewVisibility(Resource.Id.textView, ViewStates.Invisible);
                    remoteViews.SetViewVisibility(Resource.Id.cornView, ViewStates.Visible);
                    try
                    {
                        bool shuckStatus = (await CornMonitor.Singleton.GetShuckerInfoAsync()).ShuckStatus;
                        if(shuckStatus)
                        {
                            remoteViews.SetImageViewResource(Resource.Id.cornView, Resource.Drawable.corn);
                        } else
                        {
                            remoteViews.SetImageViewResource(Resource.Id.cornView, Resource.Drawable.redcorn);
                        }
                    } catch (TaskCanceledException ex)
                    {
                        Console.WriteLine("GetShuckerInfoAsync() Timeout");
                    }
                    appWidgetManager.UpdateAppWidget(id, remoteViews);
                }
            }
        }

        public override void OnReceive(Context context, Intent intent) {
            base.OnReceive(context, intent);
            Debug.WriteLine("OnReceive " + intent.Action);
        }

    }
}
