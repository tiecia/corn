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

        private const string API_HOST = "10.0.2.2:5000";
        private const string API_URI = $"http://{API_HOST}";

        private string _user = "tiec";
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

            httpClient = new HttpClient(new SocketsHttpHandler()
            {
                ConnectTimeout = TimeSpan.FromSeconds(10),
            });

            CornMonitorInitialized?.Invoke(null, null);
        }

        public async Task<ShuckerInfo> GetShuckerInfoAsync() {
            return await GetShuckerInfoAsync(-1);
        }

        public async Task<ShuckerInfo> GetShuckerInfoAsync(int guildId)
        {
            httpClient.CancelPendingRequests();
            HttpResponseMessage response;
            if(guildId == -1)
            {
                response = await httpClient.GetAsync($"{API_URI}/shuckerinfo?user={User}");
            }
            else
            {
                response = await httpClient.GetAsync($"{API_URI}/shuckerinfo?user={User}&guild={guildId}");
            }

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ShuckerInfo>();
            }
            else
            {
                return null;
            }
        }
    }
}
