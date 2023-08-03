using Android.Content;
using Android.OS;
using Android.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CornApp.Platforms.Android {
    internal class WidgetServiceBinder : Binder {
        public WidgetService Service { get; private set; }
        public WidgetServiceBinder(WidgetService service) {
            this.Service = service;
        }
    }
}
