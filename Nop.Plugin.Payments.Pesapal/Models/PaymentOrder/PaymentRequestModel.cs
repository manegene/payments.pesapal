using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nop.Plugin.Payments.Pesapal.Models.CustomerDetails;
using Nop.Web.Framework.Models;

namespace Nop.Plugin.Payments.Pesapal.Models.PaymentOrder
{
    public record PaymentRequestModel:BaseNopModel
    {
        public string Id { get; set; }
        public string Currency { get; set; }
        public float Amount { get; set; }
        public string Description { get; set; }
        public string Callback_url { get; set; }
        public string Notification_id { get; set; }
        public CustomerModel Billing_address { get; set; }

        public string Errors { get; set; }


    }
}
