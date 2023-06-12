using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Plugin.Payments.Pesapal
{
    public class Pesapalpaymentdefaults
    {
        public static string SystemName => "Payments.Pesapal";
        public static string RedirectUrl => "Plugin.Payments.Pesapal.PaymentStatus";
        public static string IpntUrl => "Plugin.Payments.Pesapal.IpnStatus";
        public static string SessionName => "Payment-"+DateTime.UtcNow.ToString();
        public static string ProdUrl => "https://pay.pesapal.com/v3/";
        public static string SandBoxUrl => "https://cybqa.pesapal.com/pesapalv3/";

    }
}
