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
        }

        public static async void UpdateWidgetAsync(Context context) {
            Debug.WriteLine("UpdateWidget");
            var appWidgetManager = AppWidgetManager.GetInstance(context);
            int[] widgetIds = appWidgetManager.GetAppWidgetIds(new ComponentName(context, Java.Lang.Class.FromType(typeof(DailyWidget)).Name));

            foreach (int id in widgetIds) {
                RemoteViews remoteViews = new RemoteViews(context.PackageName, Resource.Layout.DailyWidget);

                if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet) {
                    SetNoConnection(remoteViews);
                } else if (CornMonitor.Singleton.User == "") {
                    SetNoUser(remoteViews);
                } else {
                    var info = await CornMonitor.Singleton.GetShuckerInfoAsync();
                    if (info != null) {
                        bool shuckStatus = info.ShuckStatus;
                        if (shuckStatus) {
                            SetYellowCorn(remoteViews);
                        } else {
                            SetRedCorn(remoteViews);
                        }
                    } else {
                        SetNoServer(remoteViews);
                    }
                }
                appWidgetManager.UpdateAppWidget(id, remoteViews);
            }
        }

        private static void SetRedCorn(RemoteViews remoteViews) {
            remoteViews.SetViewVisibility(Resource.Id.textView, ViewStates.Invisible);
            remoteViews.SetViewVisibility(Resource.Id.cornView, ViewStates.Visible);
            remoteViews.SetImageViewResource(Resource.Id.cornView, Resource.Drawable.redcorn);
        }

        private static void SetYellowCorn(RemoteViews remoteViews) {
            remoteViews.SetViewVisibility(Resource.Id.textView, ViewStates.Invisible);
            remoteViews.SetViewVisibility(Resource.Id.cornView, ViewStates.Visible);
            remoteViews.SetImageViewResource(Resource.Id.cornView, Resource.Drawable.corn);
        }

        private static void SetNoConnection(RemoteViews remoteViews) {
            remoteViews.SetViewVisibility(Resource.Id.textView, ViewStates.Visible);
            remoteViews.SetViewVisibility(Resource.Id.cornView, ViewStates.Visible);
            remoteViews.SetTextViewText(Resource.Id.textView, "No internet");
            remoteViews.SetImageViewResource(Resource.Id.cornView, Resource.Drawable.cloudoff);
        }

        private static void SetNoUser(RemoteViews remoteViews) {
            remoteViews.SetViewVisibility(Resource.Id.textView, ViewStates.Invisible);
            remoteViews.SetViewVisibility(Resource.Id.cornView, ViewStates.Visible);
            remoteViews.SetImageViewResource(Resource.Id.cornView, Resource.Drawable.nouser);
        }   

        private static void SetNoServer(RemoteViews remoteViews) {
            remoteViews.SetViewVisibility(Resource.Id.textView, ViewStates.Visible);
            remoteViews.SetViewVisibility(Resource.Id.cornView, ViewStates.Visible);
            remoteViews.SetTextViewText(Resource.Id.textView, "No server");
            remoteViews.SetImageViewResource(Resource.Id.cornView, Resource.Drawable.cloudoff);
        }
    }
}
