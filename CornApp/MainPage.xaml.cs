namespace CornApp;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;

public partial class MainPage : ContentPage
{
    public string User { get; set; } = "";

    private bool _shuckStatus = false;
    public bool ShuckStatus {
        get => _shuckStatus;
        set {
            _shuckStatus = value;
            StatusText = $"Shuck status: {value}";
            OnPropertyChanged(nameof(StatusText));
        }
    }

    public string StatusText { get; set; } = "Shuck status: ";

	public MainPage()
	{
        BindingContext = this;
		InitializeComponent();

        CornMonitor.CornMonitorInitialized += (object sender, EventArgs e) => {
            User = CornMonitor.Singleton.User;
            OnPropertyChanged(nameof(User));
            UpdateShuckStatusAsync();
        };

    }

	private void OnConfirmClicked(object sender, EventArgs e)
	{
        if(User == "") {
            DisplayAlert("Error", "Please enter a valid username", "OK");
        } else {
            if(CornMonitor.Singleton != null) {
                CornMonitor.Singleton.User = User;
                UpdateShuckStatusAsync();
            }
        }
	}

    private async void UpdateShuckStatusAsync() {
        ShuckStatus = (await CornMonitor.Singleton.GetShuckerInfoAsync()).ShuckStatus;
    }

    //public async void InitShuckSocketAsync() {
    //    var socket = new ClientWebSocket();
    //    await socket.ConnectAsync(new Uri($"{WS_URI}/shucksocket"), CancellationToken.None);
    //    var buffer = new byte[1024];
    //    var segment = new ArraySegment<byte>(buffer);

    //    while(socket.State == WebSocketState.Open) {
    //        var result = await socket.ReceiveAsync(segment, CancellationToken.None);
    //        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
    //        Debug.WriteLine($"Message: {message}");
    //        if(message == "reset") {

    //        } else if(message.StartsWith("shuck:")){
    //            var user = message.Split(":")[1];
    //            if(user == User) {
    //                ShuckStatus = true;
    //            }
    //        }
    //    }
    //}

    //public async void InitShuckStatusAsync(string user) {

    //    OnPropertyChanged(nameof(StatusText));
    //}
}

