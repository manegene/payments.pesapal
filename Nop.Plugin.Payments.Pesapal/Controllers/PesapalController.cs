using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Web.Framework.Mvc.Filters;
using Nop.Web.Framework;
using Nop.Services.Plugins;
using Nop.Web.Framework.Controllers;
using Nop.Services.Configuration;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Core;
using Nop.Services.Security;
using Nop.Services.Orders;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Services.Common;
using Nop.Plugin.Payments.Pesapal.Models.Settings;
using Nop.Plugin.Payments.Pesapal.Services;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Diagnostics;

namespace Nop.Plugin.Payments.Pesapal.Controllers
{
    [AutoValidateAntiforgeryToken]
    [AuthorizeAdmin] //confirms access to the admin panel
    [Area(AreaNames.Admin)] //specifies the area containing a controller or action
    public class PesapalController:BasePluginController
    {
        #region fields
        protected readonly ISettingService _settingService;
        protected readonly IPermissionService _permissionService;
        protected readonly IStoreContext _storeContext;
        protected readonly ShoppingCartSettings _shoppingCartSettings;
        protected readonly ILocalizationService _localizationService;
        protected readonly INotificationService _notificationService;
        protected readonly IWorkContext _workContext;
        protected readonly IGenericAttributeService _genericAttributeService;
        private readonly PesapalManagementService _pesapalManagementService;
        //protected readonly servicema
        #endregion fields
        #region ctor
        public PesapalController(
            ISettingService settingService, 
            IPermissionService permissionService, 
            IStoreContext storeContext,
            ShoppingCartSettings shoppingCartSettings,
            ILocalizationService localizationService,
            INotificationService notificationService,
            IWorkContext workContext,
            IGenericAttributeService genericAttributeService,
            PesapalManagementService pesapalManagementService)
        {
            _settingService = settingService;
            _permissionService = permissionService;
            _storeContext = storeContext;
            _shoppingCartSettings = shoppingCartSettings;
            _localizationService = localizationService;
            _notificationService = notificationService;
            _workContext = workContext;
            _genericAttributeService = genericAttributeService;
            _pesapalManagementService = pesapalManagementService;

        }
        #endregion ctor

        #region utils
        protected async Task PrepareCredentialsAsync(PesapalconfigModel model, PesapalPaymentSettings settings, int storeId)
        {
           

            if (!string.IsNullOrEmpty(settings.Consumer_key) && !model.Consumer_key.Equals(settings.Consumer_key, StringComparison.InvariantCultureIgnoreCase))
            {
                settings.Consumer_key = model.Consumer_key;
                settings.Consumer_secret = model.Consumer_key;
                settings.Url= model.Url;
            }

            var overrideSettings = storeId > 0;
            await _settingService.SaveSettingOverridablePerStoreAsync(settings, setting => setting.Consumer_key, overrideSettings, storeId, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(settings, setting => settings.Consumer_secret, overrideSettings, storeId, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(settings, setting => settings.Url, overrideSettings, storeId, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(settings, setting => setting.Ipn_notification_type, overrideSettings, storeId, false);
            await _settingService.ClearCacheAsync();
        }
        #endregion


        #region methods
        public async Task<IActionResult> Configure()
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            var storeId = await _storeContext.GetActiveStoreScopeConfigurationAsync();
            var settings = await _settingService.LoadSettingAsync<PesapalPaymentSettings>(storeId);

            //we don't need some of the shared settings that loaded above, so load them separately for chosen store
            if (storeId > 0)
            {
                settings.Consumer_key = await _settingService
                    .GetSettingByKeyAsync<string>($"{nameof(PesapalPaymentSettings)}.{nameof(PesapalPaymentSettings.Consumer_key)}", storeId: storeId);
                settings.UseSandbox = await _settingService
                    .GetSettingByKeyAsync<bool>($"{nameof(PesapalPaymentSettings)}.{nameof(PesapalPaymentSettings.UseSandbox)}", storeId: storeId);
                settings.Callback_url = await _settingService
                   .GetSettingByKeyAsync<string>($"{nameof(PesapalPaymentSettings)}.{nameof(PesapalPaymentSettings.Callback_url)}", storeId: storeId);

                settings.Consumer_secret = await _settingService
                    .GetSettingByKeyAsync<string>($"{nameof(PesapalPaymentSettings)}.{nameof(PesapalPaymentSettings.Consumer_secret)}", storeId: storeId);
                settings.Url = await _settingService
                    .GetSettingByKeyAsync<string>($"{nameof(PesapalPaymentSettings)}.{nameof(PesapalPaymentSettings.Url)}", storeId: storeId);
                settings.Ipn_notification_type = await _settingService
                    .GetSettingByKeyAsync<string>($"{nameof(PesapalPaymentSettings)}.{nameof(PesapalPaymentSettings.Ipn_notification_type)}", storeId: storeId);
                settings.Ipn_id = await _settingService
                    .GetSettingByKeyAsync<string>($"{nameof(PesapalPaymentSettings)}.{nameof(PesapalPaymentSettings.Ipn_id)}", storeId: storeId);

            }

            var model = new PesapalconfigModel
            {
                Consumer_key = settings.Consumer_key,
                Consumer_secret = settings.Consumer_secret,
                UseSandbox = settings.UseSandbox,
                Callback_Url=settings.Callback_url,
                Url = settings.Url,
                Ipn_notification_type = settings.Ipn_notification_type,
                Ipn_id = settings.Ipn_id,
                ActiveStoreScopeConfiguration = storeId,
            };

            await PrepareCredentialsAsync(model, settings, storeId);

            if (storeId > 0)
            {
                model.Override_storekey = await _settingService.SettingExistsAsync(settings, setting => setting.Consumer_key, storeId);
                model.Override_storekey = await _settingService.SettingExistsAsync(settings, setting => setting.Consumer_secret, storeId);
                model.Override_storesandbox = await _settingService.SettingExistsAsync(settings, setting => setting.UseSandbox, storeId);
                model.Override_storecallbackurl = await _settingService.SettingExistsAsync(settings, setting => setting.Callback_url, storeId);
                model.Override_storeurl = await _settingService.SettingExistsAsync(settings, setting => setting.Url, storeId);
                model.Overrider_storeipntype = await _settingService.SettingExistsAsync(settings, setting => setting.Ipn_notification_type, storeId);
                model.Override_storeipnid = await _settingService.SettingExistsAsync(settings, setting => setting.Ipn_id, storeId);
            }

            

            //ensure credentials are valid
            if (!string.IsNullOrEmpty(settings.Consumer_key) && !string.IsNullOrEmpty(settings.Consumer_secret)&& !string.IsNullOrEmpty(settings.Url))
            {
               
                    _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Plugins.Payments.PayPalCommerce.Credentials.Valid"));
            }

          

            return View("~/Plugins/Payments.Pesapal/Views/Configure.cshtml", model);
        }

        [HttpPost, ActionName("Configure")]
        [FormValueRequired("save")]
        public async Task<IActionResult> Configure(PesapalconfigModel model)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            var storeId = await _storeContext.GetActiveStoreScopeConfigurationAsync();
            var settings = await _settingService.LoadSettingAsync<PesapalPaymentSettings>(storeId);

            if (string.IsNullOrEmpty(model.Consumer_key) &&
               string.IsNullOrEmpty(model.Consumer_secret) &&
               string.IsNullOrEmpty(model.Url) &&
               string.IsNullOrEmpty(model.Ipn_notification_type))
            {
                var locale = await _localizationService.GetResourceAsync("Plugins.Payments.Pesapal.Configuration.Fields.Error");
                var errorMessage = string.Format("validation error:\n {0}", locale);
                _notificationService.ErrorNotification(errorMessage, false);
                return await Configure();
            }
           /* if (string.IsNullOrEmpty(model.Consumer_key))
                throw new NopException("consumer is not set");*/

            //set new settings values
            settings.Consumer_key = model.Consumer_key;
            settings.Consumer_secret = model.Consumer_secret;
            settings.Callback_url = model.Callback_Url;
            settings.UseSandbox = model.UseSandbox;
            settings.Url = model.Url;

            //explicitly set the IPN call method. Only post is supported anyway
            settings.Ipn_notification_type = "GET";


            await _settingService.SaveSettingOverridablePerStoreAsync(settings, setting => setting.UseSandbox, model.Override_storesandbox, storeId, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(settings, setting => setting.Consumer_key, model.Override_storekey, storeId, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(settings, setting => setting.Consumer_secret, model.Override_storesecret, storeId, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(settings, setting => setting.Callback_url, model.Override_storecallbackurl, storeId, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(settings, setting => setting.Url, model.Override_storeurl, storeId, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(settings, setting => setting.Ipn_notification_type, model.Overrider_storeipntype, storeId, false);

            //register the IPN URL and type. get the IPN ID and save it in settings
            if (string.IsNullOrEmpty(settings.Ipn_id))
            {

                if (string.IsNullOrEmpty(model.Consumer_key) ||
                    string.IsNullOrEmpty(model.Consumer_secret) ||
                    string.IsNullOrEmpty(model.Url))
                {
                    _notificationService.ErrorNotification(await _localizationService.GetResourceAsync("Plugins.Payments.Pesapal.Configuration.IPN.Error"));
                    return await Configure();
                }
                else
                {
                    var isSandbox = settings.UseSandbox;
                    var cred = new
                    {
                        consumer_key = settings.Consumer_key,
                        consumer_secret = settings.Consumer_secret
                    };

                    var token = await _pesapalManagementService.GetTokenAsync(cred,isSandbox);// ?? throw new ArgumentException("error getting token");
                    
                    if (token.Error != null)
                    {
                       await _notificationService.ErrorNotificationAsync(new Exception(token.Error.Message));
                        return await Configure();
                    }
                    var ipnbody = new
                    {
                        Url = settings.Url,
                        Ipn_notification_type = settings.Ipn_notification_type
                    };
                    var ipnId = await _pesapalManagementService.GetIpnIdAsync(token.Token,ipnbody,isSandbox);
                    if (ipnId.Error !=null)
                    {
                        await _notificationService.ErrorNotificationAsync(new Exception(ipnId.Error.Message));
                        return await Configure();
                    }
                    settings.Ipn_id = ipnId.Ipn_id;

                    await _settingService.SaveSettingOverridablePerStoreAsync(settings, setting => setting.Ipn_id, model.Override_storeipnid, storeId, false);
                }
            }
            await _settingService.ClearCacheAsync();

            _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Plugins.Saved"));

            return await Configure();
        }

        [HttpPost, ActionName("Configure")]
        [FormValueRequired("revoke")]
        public async Task<IActionResult> RevokeAccess()
        {
            var storeId = await _storeContext.GetActiveStoreScopeConfigurationAsync();
            var settings = await _settingService.LoadSettingAsync<PesapalPaymentSettings>(storeId);

            settings.UseSandbox = false;
            settings.Consumer_secret = string.Empty;
            settings.Consumer_key = string.Empty;
            settings.Callback_url = string.Empty;
            settings.Url = string.Empty;
            settings.Ipn_notification_type = string.Empty;
            settings.Ipn_id = string.Empty;
            var overrideSettings = storeId > 0;
            await _settingService.SaveSettingOverridablePerStoreAsync(settings, setting => setting.UseSandbox, overrideSettings, storeId, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(settings, setting => setting.Consumer_secret, overrideSettings, storeId, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(settings, setting => setting.Consumer_key, overrideSettings, storeId, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(settings, setting => setting.Callback_url, overrideSettings, storeId, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(settings, setting => setting.Url, overrideSettings, storeId, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(settings, setting => setting.Ipn_notification_type, overrideSettings, storeId, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(settings, setting => setting.Ipn_id, overrideSettings, storeId, false);
            await _settingService.ClearCacheAsync();

            var accessRevoked = await _localizationService.GetResourceAsync("Plugins.Payments.Pesapal.Onboarding.AccessRevoked");
            _notificationService.SuccessNotification(accessRevoked);

            return await Configure();
        }
        

        #endregion methods
    }
}
