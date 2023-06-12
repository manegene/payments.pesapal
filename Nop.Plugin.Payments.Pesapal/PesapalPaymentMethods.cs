using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Azure;
using DocumentFormat.OpenXml.EMMA;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Primitives;
using Nop.Core;
using Nop.Core.Domain.Cms;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Plugin.Payments.Pesapal.Models.PaymenRecords;
using Nop.Plugin.Payments.Pesapal.Models.PaymentOrder;
using Nop.Plugin.Payments.Pesapal.Services;
using Nop.Services.Cms;
using Nop.Services.Configuration;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Payments;
using Nop.Services.Plugins;
using Nop.Services.Stores;
using Nop.Web.Framework.Infrastructure;

namespace Nop.Plugin.Payments.Pesapal
{
    public class PesapalPaymentMethods : BasePlugin,IPaymentMethod
    {
        #region utilities
        private  readonly ILocalizationService _localizationService;
        private readonly ICustomerService _customerService;
        private  readonly IServiceProvider _serviceProvider;
        private  readonly ISettingService _settingService;
        private  readonly PaymentSettings _paymentSettings;
        private  readonly WidgetSettings _widgetSettings;
        private  readonly IStoreService _storeService;
        private readonly IUrlHelperFactory _urlHelperFactory;
        private readonly IActionContextAccessor _actionContextAccessor;
        private readonly IWebHelper _webHelper;
        private readonly IStoreContext _storeContext;
        private readonly ICurrencyService _currencyService;
        private readonly PesapalManagementService _pesapalManagementService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly PesapalPaymentViewService _pesapalPaymentViewService;
        
        #endregion
        #region ctor
        public PesapalPaymentMethods(
            ILocalizationService localizationService,
            IServiceProvider serviceProvider,
            ISettingService settingService,
            PaymentSettings paymentSettings,
            WidgetSettings widgetSettings,
            IStoreService storeService,
            IUrlHelperFactory urlHelperFactory,
            IActionContextAccessor actionContextAccessor,
            IWebHelper webHelper,
            IStoreContext storeContext,
            ICurrencyService currencyService,
            ICustomerService customerService,
            PesapalManagementService pesapalManagementService,
            IHttpContextAccessor httpContextAccessor,
            PesapalPaymentViewService pesapalPaymentViewService
            
            )
        {
            _localizationService=localizationService;
            _serviceProvider=serviceProvider;
            _settingService=settingService;
            _paymentSettings=paymentSettings;
            _widgetSettings=widgetSettings;
            _storeService=storeService;
            _urlHelperFactory=urlHelperFactory;
            _actionContextAccessor=actionContextAccessor;
            _webHelper=webHelper;
            _storeContext=storeContext;
            _currencyService=currencyService;
            _customerService=customerService;
            _pesapalManagementService=pesapalManagementService;
            _httpContextAccessor=httpContextAccessor;
            _pesapalPaymentViewService =pesapalPaymentViewService;
        }
        #endregion
        #region Properies

        /// <summary>
        /// Gets a value indicating whether capture is supported
        /// </summary>
        public bool SupportCapture => true;

        /// <summary>
        /// Gets a value indicating whether void is supported
        /// </summary>
        public bool SupportVoid => true;

        /// <summary>
        /// Gets a value indicating whether refund is supported
        /// </summary>
        public bool SupportRefund => false;

        /// <summary>
        /// Gets a value indicating whether partial refund is supported
        /// </summary>
        public bool SupportPartiallyRefund => false;

        /// <summary>
        /// Gets a recurring payment type of payment method
        /// </summary>
        public RecurringPaymentType RecurringPaymentType => RecurringPaymentType.NotSupported;

        /// <summary>
        /// Gets a payment method type
        /// </summary>
        public PaymentMethodType PaymentMethodType => PaymentMethodType.Redirection;

        /// <summary>
        /// Gets a value indicating whether we should display a payment information page for this plugin
        /// </summary>
        public bool SkipPaymentInfo => false;

        /// <summary>
        /// Gets a value indicating whether to hide this plugin on the widget list page in the admin area
        /// </summary>
        public bool HideInWidgetList => true;

        #endregion

        #region Methods

        /// <summary>
        /// Process a payment
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the process payment result
        /// </returns>
        public  Task<ProcessPaymentResult> ProcessPaymentAsync(ProcessPaymentRequest processPaymentRequest)
        {
            return Task.FromResult(new ProcessPaymentResult());
        }
        /// <summary>
        /// Post process payment (used by payment gateways that require redirecting to a third-party URL)
        /// </summary>
        /// <param name="postProcessPaymentRequest">Payment info required for an order processing</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        public async Task PostProcessPaymentAsync(PostProcessPaymentRequest postProcessPaymentRequest)
        {            
            
            var storeId = await _storeContext.GetActiveStoreScopeConfigurationAsync();
            var pesapalsetting = await _settingService.LoadSettingAsync<PesapalPaymentSettings>(storeId);
            var cursettings = await _settingService.GetSettingAsync("currencysettings.primarystorecurrencyid", storeId);
            var storecur = await _currencyService.GetCurrencyByIdAsync(Convert.ToInt32(cursettings.Value));
            var cust = await _customerService.GetCustomerByIdAsync(postProcessPaymentRequest.Order.CustomerId);
            var isSandbox = pesapalsetting.UseSandbox;

            //compose pesapal order body load
            var orderAmount = Math.Round(postProcessPaymentRequest.Order.OrderTotal, 2);
            var orderload = new PaymentRequestModel
            {
                Id = postProcessPaymentRequest.Order.OrderGuid.ToString(),
                Currency = storecur.CurrencyCode,
                Amount = (float)orderAmount,
                Description = DateTime.UtcNow.ToString(),
                Callback_url = pesapalsetting.Callback_url,
                Notification_id = pesapalsetting.Ipn_id,
                Billing_address = new()
                {
                    Phone_number = cust.Phone,
                    Email_Address = cust.Email,
                    First_name = cust.FirstName,
                    Last_name = cust.LastName
                }

            };
            //pesapal auth credentials
            var creds = new
            {
                consumer_key = pesapalsetting.Consumer_key,
                consumer_secret = pesapalsetting.Consumer_secret
            };
            
            var getToken = await _pesapalManagementService.GetTokenAsync(creds, isSandbox);
            var postorder = await _pesapalManagementService.SubmitOrderAsync(getToken.Token, orderload, isSandbox);


            if (string.IsNullOrEmpty(postorder.Redirect_url))
                throw new Exception("payment error. please contact store owner for payment details");

            //checck if order is already existing. In retry payment or user reloads payment method
            var exist = await _pesapalManagementService.OrderExist(postorder.Order_tracking_id);

            //only add new order record if the order does not exist
            if (exist == false)
            {


                //add order tracking, amount, order guid and id to pesapal payment record table
                var trackOrder = new PesapalPayRecords
                {
                    StoreId = storeId,
                    OrderGuid = postProcessPaymentRequest.Order.OrderGuid,
                    OrderTrackingId = postorder.Order_tracking_id,
                    OrderAmount = orderAmount,
                    OrderCurrency = storecur.CurrencyCode

                };
                await _pesapalManagementService.CreateOrderTrackRecord(trackOrder);
            }

            //finally redirect user to redirect url for payment processing
            
            await Task.Run(() => _httpContextAccessor.HttpContext.Response.Redirect(postorder.Redirect_url));
            
        }

        /// <summary>
        /// Returns a value indicating whether payment method should be hidden during checkout
        /// </summary>
        /// <param name="cart">Shopping cart</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the rue - hide; false - display.
        /// </returns>
        public Task<bool> HidePaymentMethodAsync(IList<ShoppingCartItem> cart)
        {
            return Task.FromResult(false);
        }

        /// <summary>
        /// Gets additional handling fee
        /// </summary>
        /// <param name="cart">Shopping cart</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the additional handling fee
        /// </returns>
        public Task<decimal> GetAdditionalHandlingFeeAsync(IList<ShoppingCartItem> cart)
        {
            return Task.FromResult(decimal.Zero);
        }

        /// <summary>
        /// Captures payment
        /// </summary>
        /// <param name="capturePaymentRequest">Capture payment request</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the capture payment result
        /// </returns>
        public Task<CapturePaymentResult> CaptureAsync(CapturePaymentRequest capturePaymentRequest)
        {
            return Task.FromResult(new CapturePaymentResult { Errors = new List<string>() { "Capture not supported" } });
        }

        /// <summary>
        /// Refunds a payment
        /// </summary>
        /// <param name="refundPaymentRequest">Request</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the result
        /// </returns>
        public Task<RefundPaymentResult> RefundAsync(RefundPaymentRequest refundPaymentRequest)
        {
            return Task.FromResult(new RefundPaymentResult { Errors = new List<string>() { "Return not supported" } });

        }

        /// <summary>
        /// Voids a payment
        /// </summary>
        /// <param name="voidPaymentRequest">Request</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the result
        /// </returns>
        public Task<VoidPaymentResult> VoidAsync(VoidPaymentRequest voidPaymentRequest)
        {
            return Task.FromResult(new VoidPaymentResult { Errors = new List<string>() { "Void method not supported" } });

        }

        /// <summary>
        /// Process recurring payment
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the process payment result
        /// </returns>
        public Task<ProcessPaymentResult> ProcessRecurringPaymentAsync(ProcessPaymentRequest processPaymentRequest)
        {
            return Task.FromResult(new ProcessPaymentResult { Errors = new List<string>() { "Recurring payment method not supported" } });

        }

        /// <summary>
        /// Cancels a recurring payment
        /// </summary>
        /// <param name="cancelPaymentRequest">Request</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the result
        /// </returns>
        public Task<CancelRecurringPaymentResult> CancelRecurringPaymentAsync(CancelRecurringPaymentRequest cancelPaymentRequest)
        {
            return Task.FromResult(new CancelRecurringPaymentResult { Errors = new List<string>() { "Cancel recurring payment not supported" } });

        }

        /// <summary>
        /// Gets a value indicating whether customers can complete a payment after order is placed but not completed (for redirection payment methods)
        /// </summary>
        /// <param name="order">Order</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the result
        /// </returns>
        public Task<bool> CanRePostProcessPaymentAsync(Order order)
        {
            if(order == null)
            {
                throw new ArgumentNullException(nameof(order));
            }
            return Task.FromResult(true);
        }

        /// <summary>
        /// Validate payment form
        /// </summary>
        /// <param name="form">The parsed form values</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the list of validating errors
        /// </returns>
        public Task<IList<string>> ValidatePaymentFormAsync(IFormCollection form)
        {
            var errors = new List<string>();
           
            return Task.FromResult<IList<string>>(errors);
        }

        /// <summary>
        /// Get payment information
        /// </summary>
        /// <param name="form">The parsed form values</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the payment info holder
        /// </returns>
       public async Task<ProcessPaymentRequest> GetPaymentInfoAsync(IFormCollection form)
        {


            return await Task.FromResult(new ProcessPaymentRequest());
           
        }

        /// <summary>
        /// Gets a type of a view component for displaying plugin in public store ("payment info" checkout step)
        /// </summary>
        /// <returns>View component type</returns>
        /// 
        /// <summary>
        /// Gets a configuration page URL
        /// </summary>
        public override string GetConfigurationPageUrl()
        {
            return $"{_webHelper.GetStoreLocation()}Admin/Pesapal/Configure";

        }
        public Type GetPublicViewComponent()
        {
            return typeof(PesapalPaymentViewService);
        }

        /// <summary>
        /// Gets a payment method description that will be displayed on checkout pages in the public store
        /// </summary>
        /// <returns>A task that represents the asynchronous operation</returns>
        public async Task<string> GetPaymentMethodDescriptionAsync()
        {
            return await _localizationService.GetResourceAsync("Plugins.Payments.Pesapal.PaymentMethodDescription");

        }


        /// <summary>
        /// Install the plugin
        /// </summary>
        /// <returns>A task that represents the asynchronous operation</returns>
        public override async Task InstallAsync()
        {

            if (!_paymentSettings.ActivePaymentMethodSystemNames.Contains(Pesapalpaymentdefaults.SystemName))
            {
                _paymentSettings.ActivePaymentMethodSystemNames.Add(Pesapalpaymentdefaults.SystemName);
                await _settingService.SaveSettingAsync(_paymentSettings);
            }

            //locales
            await _localizationService.AddOrUpdateLocaleResourceAsync(new Dictionary<string, string>
            {
                ["Plugins.Payments.Pesapal.Configuration.Reset"] = "Reset Pesapal",

                ["Plugins.Payments.Pesapal.Configuration.Fields.Error"] = "Error: {0} </a>)",
                ["Plugins.Payments.Pesapal.Configuration.Error"] = "Error: {0} (see details in the <a href=\"{1}\" target=\"_blank\">log</a>)",
                ["Plugins.Payments.Pesapal.Credentials.Valid"] = "The specified credentials are valid",
                ["Plugins.Payments.Pesapal.Credentials.Invalid"] = "The specified credentials are invalid",

                ["Plugins.Payments.Pesapal.Fields.Consumer_key"] = "Consumer key",
                ["Plugins.Payments.Pesapal.Fields.Consumer_key.Hint"] = "Enter your Pesapal consumer key.",
                ["Plugins.Payments.Pesapal.Fields.Consumer_key.Required"] = "Client ID is required",

                ["Plugins.Payments.Pesapal.Fields.RedirectUrl"] = "Redirect url",
                ["Plugins.Payments.Pesapal.Fields.RedirectUrl.Hint"] = "Address customer is redirected to after payment processing.\n enter: https://Your_store_address/Pesapal/verify/paymentstatus",
                ["Plugins.Payments.Pesapal.Fields.RedirectUrl.required"] = "redirect address is required",

                ["Plugins.Payments.Pesapal.Fields.SecretKey"] = "Secret key",
                ["Plugins.Payments.Pesapal.Fields.SecretKey.Hint"] = "Enter your pesapal API secret.",
                ["Plugins.Payments.Pesapal.Fields.SecretKey.Required"] = "Secret is required",

                ["Plugins.Payments.Pesapal.Fields.ConsumerSecret"] = "Consumer secret.",
                ["Plugins.Payments.Pesapal.Fields.ConsumerSecret.Hint"] = "Enter your Pesapal consumer secret.",
                ["Plugins.Payments.Pesapal.Fields.ConsumerSecret.Required"] = "Consumer secret is required",

                ["Plugins.Payments.Pesapal.Fields.UseSandbox"] = "Use sandbox",
                ["Plugins.Payments.Pesapal.Fields.UseSandbox.Hint"] = "Determine whether to use the sandbox environment for testing purposes.",
               
                ["Plugins.Payments.Pesapal.Fields.IPNUrl"] = "Ipn address",
                ["Plugins.Payments.Pesapal.Fields.IPNUrl.Hint"] = "Address pesapal will use to make a backend payment status validation.\n enter: https://Your_store_address/Pesapal/verify/ipnstatus",
               
                ["Plugins.Payments.Pesapal.Configuration.IPN.Error"] = "IPN Id not issued. Check credentials and try again",
                ["Plugins.Payments.Pesapal.Configuraton.Reset"] = "Reset",
                ["Plugins.Payments.Pesapal.Onboarding.AccessRevoked"] = "Plugin business credentials successfully reset",

                ["Plugins.Payments.Pesapal.PaymentMethodDescription"] = "Pesapal is your solution to accept mobile and card payments",
            });

            await base.InstallAsync();
        }

        /// <summary>
        /// Uninstall the plugin
        /// </summary>
        /// <returns>A task that represents the asynchronous operation</returns>
        public override async Task UninstallAsync()
        {
            //webhooks
            var stores = await _storeService.GetAllStoresAsync();
            var storeIds = new List<int> { 0 }.Union(stores.Select(store => store.Id));
            foreach (var storeId in storeIds)
            {
                var settings = await _settingService.LoadSettingAsync<PesapalPaymentSettings>(storeId);
                if (!string.IsNullOrEmpty(settings.Consumer_key))
                {
                    settings.Consumer_key = string.Empty;

                    await _settingService.SaveSettingOverridablePerStoreAsync(settings, setting => setting.Consumer_key, true, storeId, false);

                    // await _settingService.DeleteSetting(settings.Consumer_key);
                }
            }

            //settings
            if (_paymentSettings.ActivePaymentMethodSystemNames.Contains(Pesapalpaymentdefaults.SystemName))
            {
                _paymentSettings.ActivePaymentMethodSystemNames.Remove(Pesapalpaymentdefaults.SystemName);
                await _settingService.SaveSettingAsync(_paymentSettings);
            }

            if (_widgetSettings.ActiveWidgetSystemNames.Contains(Pesapalpaymentdefaults.SystemName))
            {
                _widgetSettings.ActiveWidgetSystemNames.Remove(Pesapalpaymentdefaults.SystemName);
                await _settingService.SaveSettingAsync(_widgetSettings);
            }
           
            await _settingService.DeleteSettingAsync<PesapalPaymentSettings>();

            //locales
            await _localizationService.DeleteLocaleResourcesAsync("Enums.Nop.Plugin.Payments.Pesapal");
            await _localizationService.DeleteLocaleResourcesAsync("Plugins.Payments.Pesapal");

            await base.UninstallAsync();
        }

        /// <summary>
        /// Update plugin
        /// </summary>
        /// <param name="currentVersion">Current version of plugin</param>
        /// <param name="targetVersion">New version of plugin</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        public override async Task UpdateAsync(string currentVersion, string targetVersion)
        {
            var current = decimal.TryParse(currentVersion, NumberStyles.Any, CultureInfo.InvariantCulture, out var value) ? value : 1.00M;

            //new setting added in 1.09
            if (current < 1.09M)
            {
                 await _settingService.LoadSettingAsync<PesapalPaymentSettings>();
                
            }
        }

        /// <summary>
        /// Gets a payment method description that will be displayed on checkout pages in the public store
        /// </summary>
        /// <returns>A task that represents the asynchronous operation</returns>

        #endregion
    }
}