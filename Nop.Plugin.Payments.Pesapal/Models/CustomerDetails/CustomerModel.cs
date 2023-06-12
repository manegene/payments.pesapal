using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Plugin.Payments.Pesapal.Models.CustomerDetails
{
    public class CustomerModel
    {
        public string Phone_number { get;set; }
        public string Email_Address { get;set; }
        public string Country_code { get;set;}
        public string First_name { get;set; }
        public string Middle_name { get;set;}
        public string Last_name { get; set; }
        public string Line_1 { get;set; }
        public string Line_2 { get;set; }
        public string City { get;set; }
        public string State { get;set; }
        public int Postal_code { get; set; }
        public int Zip_code { get; set; }
    }
    
}
