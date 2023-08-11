using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.OS;
using Android.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CornApp.Platforms.Android {
    [Service]
    public class WidgetService : Service {
        public IBinder Binder { get; private set; }

        public CornMonitor CornMonitor { get; set; }

        private Timer UpdateTimer;

        public const int SERVICE_RUNNING_NOTIFICATION_ID = 10000;
        public override IBinder OnBind(Intent intent) {
            Binder = new WidgetServiceBinder(this);
            Console.WriteLine("Service bound");
            return this.Binder;
        }

        [return: GeneratedEnum]
        public override StartCommandResult OnStartCommand(Intent intent, [GeneratedEnum] StartCommandFlags flags, int startId) {
            Console.WriteLine("Service started");
            CreateNotificationChannel();

            //TODO: Customize notification
            Notification notification = new Notification.Builder(this, "0")
                .SetContentTitle("ServiceNotification")
                .SetContentText("Service Number " + 1)
                .SetChannelId("1000")
                .Build();

            StartForeground((SERVICE_RUNNING_NOTIFICATION_ID + System.DateTime.Now.Second % 10000), notification);
            base.OnStartCommand(intent, flags, startId);

            if(CornMonitor == null) {
                CornMonitor = new CornMonitor();
            }

            if (UpdateTimer == null) {
                UpdateTimer = new Timer((object state) => {
                    DailyWidget.UpdateWidgetAsync(ApplicationContext);
                }, null, 0, 5000);
            }

            return StartCommandResult.NotSticky;
        }

        private void CreateNotificationChannel() {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O) {
                NotificationChannel channel = new NotificationChannel("1000", "Service Notification", NotificationImportance.Low);

                NotificationManager notificationManager = (NotificationManager)GetSystemService(NotificationService);
                notificationManager.CreateNotificationChannel(channel);
            }
        }
    }
}
