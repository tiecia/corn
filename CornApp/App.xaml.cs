namespace CornApp
{
    public partial class App : Application
    {
        public static event EventHandler AppActivated;
        public App()
        {
            InitializeComponent();

            MainPage = new AppShell();
        }

        protected override Window CreateWindow(IActivationState activationState) {
            Window window = base.CreateWindow(activationState);

            window.Activated += (sender, args) => {
                AppActivated?.Invoke(null, null);
            };

            return window;
        }
    }
}