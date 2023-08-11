using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CornApp
{
    public class ShuckerInfo
    {
        public ShuckerInfo() { }
        public ShuckerInfo(RequestStatus status) {
            Status = status;
        }
        public string Username { get; set; }
        public bool ShuckStatus { get; set; }
        public long CornCount { get; set; }
        public RequestStatus Status { get; set; } = RequestStatus.Success;
        public enum RequestStatus {
            NetworkError,
            ServerError,
            UserError,
            Success
        }
    }
}
