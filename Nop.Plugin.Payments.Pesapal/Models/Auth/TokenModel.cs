using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Plugin.Payments.Pesapal.Models.Auth
{
    public class TokenModel
    {
        public string Token { get; set; }
        public string ExpiryDate { get; set; }
        public ApiErrorModel Error { get; set;}
        public string Status { get; set; }
        public string Message { get; set; }
    }
}
