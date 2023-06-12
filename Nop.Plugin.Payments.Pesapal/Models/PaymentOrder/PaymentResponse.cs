using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nop.Plugin.Payments.Pesapal.Models.Auth;
using Nop.Web.Framework.Models;

namespace Nop.Plugin.Payments.Pesapal.Models.PaymentOrder
{
    public record PaymentResponse:BaseNopModel
    {
        public string Order_tracking_id { get;set; }
        public string Merchant_reference { get; set; }
        public string Redirect_url { get; set; }
        public ApiErrorModel Error { get; set; }
        public string Status { get; set; }


    }
}
