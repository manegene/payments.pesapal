using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Nop.Web.Framework.Mvc.Routing;

namespace Nop.Plugin.Payments.Pesapal.Infra
{
    public class Routes:IRouteProvider
    {
        /// <summary>
        /// Register routes
        /// </summary>
        /// <param name="endpointRouteBuilder">Route builder</param>
        public void RegisterRoutes(IEndpointRouteBuilder endpointRouteBuilder)
        {
           
            endpointRouteBuilder.MapControllerRoute(Pesapalpaymentdefaults.RedirectUrl,
                "Pesapal/verify/Paymentstatus",
                new { controller = "PesapalPaymentStatus", action = "CheckPayStatus" });

            endpointRouteBuilder.MapControllerRoute(Pesapalpaymentdefaults.RedirectUrl,
               "Pesapal/verify/Ipnstatus",
               new { controller = "PesapalPaymentStatus", action = "IpnStatus" });
        }

        /// <summary>
        /// Gets a priority of route provider
        /// </summary>
        public int Priority => 0;
    }
}
