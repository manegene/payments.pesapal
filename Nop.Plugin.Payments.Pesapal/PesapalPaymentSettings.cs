using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nop.Core.Configuration;

namespace Nop.Plugin.Payments.Pesapal
{
    public class PesapalPaymentSettings : ISettings
    {
        public string Consumer_key { get; set; }
        public string Consumer_secret { get; set; }
        public string Callback_url { get; set; }
        public string Url { get; set; }
        public string Ipn_notification_type { get; set; }
        public string Ipn_id { get; set; }
        public bool UseSandbox { get; set; }

    }
}
