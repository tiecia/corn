// SignalR Hub for CornAPI
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using System.Diagnostics;

namespace CornBot.API {
    public class CornHub : Hub {

        public async Task NotifyShuckerStatusChange(ShuckerStatus status) {
            await Clients.All.SendAsync("ShuckerStatusChange", JsonConvert.SerializeObject(status));
        }

        public override Task OnConnectedAsync() {
            Debug.WriteLine("Client connected");
            NotifyShuckerStatusChange(new ShuckerStatus() {
                Username = "tiec",
                ShuckStatus = false,
                CornCount = 15
            });
            return base.OnConnectedAsync();
        }
        public override Task OnDisconnectedAsync(Exception? exception) {
            Debug.WriteLine("Client disconnected");
            return base.OnDisconnectedAsync(exception);
        }
    }
}
