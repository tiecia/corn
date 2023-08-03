using Android.Content;
using Android.OS;
using Android.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CornApp.Platforms.Android {
    internal class WidgetServiceConnection : Java.Lang.Object, IServiceConnection {
        static readonly string TAG = typeof(WidgetServiceBinder).FullName;
        public bool IsConnected { get; private set; }
        public WidgetServiceBinder Binder { get; private set; }

        public WidgetServiceConnection() {
            IsConnected = false;
            Binder = null;
        }

        public void OnServiceConnected(ComponentName name, IBinder service) {
            Binder = service as WidgetServiceBinder;
            IsConnected = this.Binder != null;

            string message = "onServiceConnected - ";
            Log.Debug(TAG, $"OnServiceConnected {name.ClassName}");

            if (IsConnected) {
                message = message + " bound to service " + name.ClassName;
            } else {
                message = message + " not bound to service " + name.ClassName;
            }
        }

        public void OnServiceDisconnected(ComponentName name) {
            Log.Debug(TAG, $"OnServiceDisconnected {name.ClassName}");
            IsConnected = false;
            Binder = null;
        }
    }
}
