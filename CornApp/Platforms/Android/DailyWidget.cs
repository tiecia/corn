using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Views;
using Android.Widget;
using Intent = Android.Content.Intent;

namespace CornApp.Platforms.Android {
    // Add Exported = true for your app to run as expected in Android 12 and above.
    [BroadcastReceiver(Label = "Shuck Status", Exported = true)]
    [IntentFilter(new string[] { "android.appwidget.action.APPWIDGET_UPDATE" })]
    [MetaData("android.appwidget.provider", Resource = "@xml/appwidgetprovider")]
    [Service(Exported = true)]
    public class DailyWidget : AppWidgetProvider {
        public override void OnUpdate(Context context, AppWidgetManager appWidgetManager, int[] appWidgetIds) {
            UpdateWidgetAsync(context);
        }

        private static void SetOnClickAction(Context context) {
            var appWidgetManager = AppWidgetManager.GetInstance(context);
            int[] widgetIds = appWidgetManager.GetAppWidgetIds(new ComponentName(context, Java.Lang.Class.FromType(typeof(DailyWidget)).Name));

            for(int i = 0; i<widgetIds.Length; i++) {
                Intent intent = new Intent(context, typeof(MainActivity));
                PendingIntent pendingIntent = PendingIntent.GetActivity(context, 0, intent, PendingIntentFlags.Immutable);
                var remoteViews = new RemoteViews(context.PackageName, Resource.Layout.DailyWidget);
                remoteViews.SetOnClickPendingIntent(Resource.Id.cornView, pendingIntent);
                remoteViews.SetOnClickPendingIntent(Resource.Id.textView, pendingIntent);
                appWidgetManager.UpdateAppWidget(widgetIds[i], remoteViews);
            }
        }

        protected PendingIntent GetPendingSelfIntent(Context context, String action) {
            Intent intent = new Intent(context, this.Class);
            intent.SetAction(action);
            return PendingIntent.GetBroadcast(context, 0, intent, 0);
        }

        public static async void UpdateWidgetAsync(Context context) {
            SetOnClickAction(context);

            var appWidgetManager = AppWidgetManager.GetInstance(context);
            int[] widgetIds = appWidgetManager.GetAppWidgetIds(new ComponentName(context, Java.Lang.Class.FromType(typeof(DailyWidget)).Name));

            foreach (int id in widgetIds) {
                RemoteViews remoteViews = new RemoteViews(context.PackageName, Resource.Layout.DailyWidget);

                if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet) {
                    SetNoConnection(remoteViews);
                } else {
                    var info = await CornMonitor.Singleton.GetShuckerInfoAsync();
                    switch(info.Status) {
                        case ShuckerInfo.RequestStatus.NetworkError:
                            SetNoServer(remoteViews);
                            break;
                        case ShuckerInfo.RequestStatus.ServerError:
                            SetNoServer(remoteViews);
                            break;
                        case ShuckerInfo.RequestStatus.UserError:
                            SetNoUser(remoteViews);
                            break;
                        case ShuckerInfo.RequestStatus.Success:
                            bool shuckStatus = info.ShuckStatus;
                            if (shuckStatus) {
                                SetYellowCorn(remoteViews);
                            } else {
                                SetRedCorn(remoteViews);
                            }
                            break;
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
            remoteViews.SetTextViewText(Resource.Id.textView, "No Network");
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
            remoteViews.SetTextViewText(Resource.Id.textView, "No Server");
            remoteViews.SetImageViewResource(Resource.Id.cornView, Resource.Drawable.cloudoff);
        }
    }
}
