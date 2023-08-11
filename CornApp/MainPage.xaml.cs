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
            StatusText = value ? "You've finished shucking for today!" : "You still have shucking to do.";
            CornImage = value ? ImageSource.FromFile("corn.png") : ImageSource.FromFile("redcorn.png");
            OnPropertyChanged(nameof(StatusText));
            OnPropertyChanged(nameof(CornImage));
        }
    }

    public string StatusText { get; set; } = "Shuck status: ";

    public ImageSource CornImage { get; set; } = ImageSource.FromFile("redcorn.png");


    public MainPage()
	{
        BindingContext = this;
		InitializeComponent();

        CornMonitor.CornMonitorInitialized += (object sender, EventArgs e) => {
            User = CornMonitor.Singleton.User;
            OnPropertyChanged(nameof(User));
            UpdateShuckStatusAsync();



            App.AppActivated += (object sender, EventArgs e) => {
                UpdateShuckStatusAsync();
            };
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
        var info = await CornMonitor.Singleton.GetShuckerInfoAsync();
        if(info != null) {
            ShuckStatus = info.ShuckStatus;
        } else {
            ShuckStatus = false;
            StatusText = "Please enter a username";
            OnPropertyChanged(nameof(StatusText));
        }
    }
}

