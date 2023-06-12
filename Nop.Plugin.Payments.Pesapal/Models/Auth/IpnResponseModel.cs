using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Plugin.Payments.Pesapal.Models.Auth
{
    public class IpnResponseModel
    {
        public string Url { get; set; }
        public string Created_date { get; set; }
        public string Ipn_id { get; set; }
        public ApiErrorModel Error { get; set; }
        public string Status { get; set; }

    }
}
