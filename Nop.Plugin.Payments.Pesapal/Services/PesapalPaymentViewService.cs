using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nop.Core.Domain.Orders;
using Nop.Plugin.Payments.Pesapal.Models.PaymentOrder;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Services.Payments;
using Nop.Core.Http.Extensions;
using Nop.Web.Framework.Components;
using System.Net;

namespace Nop.Plugin.Payments.Pesapal.Services
{
    public class PesapalPaymentViewService: NopViewComponent
    {
        #region fields
        private readonly IPaymentService _paymentService;
        private readonly ILocalizationService _localizationService;
        private readonly OrderSettings _orderSettings;
        private readonly INotificationService _notificationService;
        private readonly PesapalManagementService _pesapalManagementService;
        private readonly PesapalPaymentSettings _settings;
        #endregion
        #region ctor
        public PesapalPaymentViewService
            (
            IPaymentService paymentService,
            ILocalizationService localizationService,
            OrderSettings orderSettings,
            INotificationService notificationService,
            PesapalManagementService pesapalManagementService,
            PesapalPaymentSettings settings
            )
        {
            _paymentService = paymentService;
            _localizationService = localizationService;
            _orderSettings = orderSettings;
            _notificationService = notificationService;
            _pesapalManagementService = pesapalManagementService;
            _settings = settings;
        }
        #endregion
        #region Methods

        /// <summary>
        /// Invoke view component
        /// </summary>
        /// <param name="widgetZone">Widget zone name</param>
        /// <param name="additionalData">Additional data</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the view component result
        /// </returns>
        public IViewComponentResult Invoke()
        {



            //prepare order GUID
            // var paymentRequest = new PostProcessPaymentRequest();
            //await new Task(() => { HttpContext.Session.Set(Pesapalpaymentdefaults.SessionName, paymentRequest); }).ConfigureAwait(true);
            //await HttpContext.Session.SetAsync(Pesapalpaymentdefaults.SessionName, paymentRequest);

            return View("~/Plugins/Payments.Pesapal/Views/PaymentInfo.cshtml");
        }

        #endregion
    }
}
