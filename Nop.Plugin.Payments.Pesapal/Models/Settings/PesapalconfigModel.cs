using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Payments.Pesapal.Models.Settings
{
    public record PesapalconfigModel : BaseNopModel
    {

        public bool IsConfigured { get; set; }
        public int ActiveStoreScopeConfiguration { get; set; }

        [NopResourceDisplayName("Plugins.Payments.Pesapal.Fields.Consumer_key"),Required]
        public string Consumer_key { get; set; }
        public bool Override_storekey { get; set; }

        [NopResourceDisplayName("Plugins.Payments.Pesapal.Fields.UseSandbox"),Required]
        public bool UseSandbox { get; set; }
        public bool Override_storesandbox { get; set; }

        [NopResourceDisplayName("Plugins.Payments.Pesapal.Fields.ConsumerSecret"),Required]
        public string Consumer_secret { get; set; }
        public bool Override_storesecret { get;set; }

        [NopResourceDisplayName("Plugins.Payments.Pesapal.Fields.RedirectUrl"), Required]
        public string Callback_Url { get; set; }
        public bool Override_storecallbackurl { get; set; }

        [NopResourceDisplayName("Plugins.Payments.Pesapal.Fields.IPNUrl"), Required]
        public string Url { get; set; }
        public bool Override_storeurl { get;set; }

    //These two fields will be explicitly set and not shown to users
        public string Ipn_notification_type { get; set; } 
        public bool Overrider_storeipntype { get; set; }

        public string Ipn_id { get; set; }
        public bool Override_storeipnid { get;set; }
    }
}
