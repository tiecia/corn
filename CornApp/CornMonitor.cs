using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace CornApp {
    public class CornMonitor {
        private static CornMonitor _singleton;
        public static CornMonitor Singleton {
            get => _singleton;
            set {
                _singleton = value;
            }
        }

        public static event EventHandler CornMonitorInitialized;

        private const string API_HOST = "cornbotdev.azurewebsites.net";
        private const string API_URI = $"https://{API_HOST}";

        private string _user = "";
        public string User { 
            get => _user;
            set {
                _user = value;
                if (userFile != null && userFile.CanWrite) {
                    using var writer = new StreamWriter(userFile);
                    writer.WriteLine(value);
                    writer.Close();
                }
            }
        }

        private string userFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "user.txt");
        private FileStream userFile;

        private HttpClient httpClient;

        public CornMonitor() {
            Singleton = this;

            userFile = File.Open(userFilePath, FileMode.OpenOrCreate, FileAccess.Read);

            using var reader = new StreamReader(userFile);
            User = reader.ReadLine();
            reader.Close();
            userFile.Close();

            userFile = File.Open(userFilePath, FileMode.OpenOrCreate, FileAccess.Write);

            httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(10);

            CornMonitorInitialized?.Invoke(null, null);
        }

        public async Task<ShuckerInfo> GetShuckerInfoAsync() {
            return await GetShuckerInfoAsync(-1);
        }

        public async Task<ShuckerInfo> GetShuckerInfoAsync(int guildId)
        {
            if(User == "" || User == null) {
                return new ShuckerInfo(ShuckerInfo.RequestStatus.UserError);
            }

            httpClient.CancelPendingRequests();
            HttpResponseMessage response;
            try {
                if(guildId == -1)
                {
                    response = await httpClient.GetAsync($"{API_URI}/shuckerinfo?user={User}");
                }
                else
                {
                    response = await httpClient.GetAsync($"{API_URI}/shuckerinfo?user={User}&guild={guildId}");
                }
            } catch (Exception e) {
                Console.WriteLine("Request failed: " + e.Message);
                return new ShuckerInfo(ShuckerInfo.RequestStatus.NetworkError);
            }

            if (response.IsSuccessStatusCode)
            {
                var info = await response.Content.ReadFromJsonAsync<ShuckerInfo>();
                info.Status = ShuckerInfo.RequestStatus.Success;
                return info;
            }
            else
            {
                return new ShuckerInfo(ShuckerInfo.RequestStatus.ServerError);
            }
        }
    }
}
