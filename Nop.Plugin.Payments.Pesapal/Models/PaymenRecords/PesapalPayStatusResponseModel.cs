using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nop.Plugin.Payments.Pesapal.Models.Auth;

namespace Nop.Plugin.Payments.Pesapal.Models.PaymenRecords
{
    public class PesapalPayStatusResponseModel
    {
        public string Payment_method { get; set; }
        public decimal Amount { get; set; }
        public string Created_date { get; set;}
        public string Confirmation_code { get; set; }
        public string Payment_status_description { get; set; }
        public string Description { get; set;}
        public string Message { get; set;}
        public string Payment_account { get;set; }
        public string Call_back_url { get; set; }
        public int Status_code { get;set; }
        public string Merchant_reference { get; set; }
        public string Payment_status_code { get; set; }
        public string Currency { get; set; }
        public ApiErrorModel Error { get; set; }
        public string Status { get; set; }
    }
}
