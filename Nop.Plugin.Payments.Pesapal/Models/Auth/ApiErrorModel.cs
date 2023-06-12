using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Plugin.Payments.Pesapal.Models.Auth
{
    public class ApiErrorModel
    {
        public string Error_type { get; set; }
        public string Code { get; set; }
        public string Message { get; set;}
    }
}
