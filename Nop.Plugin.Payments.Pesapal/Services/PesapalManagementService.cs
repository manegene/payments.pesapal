using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using LinqToDB;
using MaxMind.GeoIP2.Exceptions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Data;
using Nop.Plugin.Payments.Pesapal.Models.Auth;
using Nop.Plugin.Payments.Pesapal.Models.PaymenRecords;
using Nop.Plugin.Payments.Pesapal.Models.PaymentOrder;
using Nop.Plugin.Payments.Pesapal.Models.Settings;
using Nop.Services.Configuration;

namespace Nop.Plugin.Payments.Pesapal.Services
{
    public class PesapalManagementService
    {
        #region fields
        private readonly IStoreContext _storeContext;
        private readonly ISettingService _settingService;
        private readonly HttpClient _httpClient;
        private readonly IRepository<PesapalPayRecords> _pesapalDb;
        #endregion

        #region ctor
        public PesapalManagementService
            (
            IStoreContext storeContext,
            ISettingService settingService,
            HttpClient httpClient,
            IRepository<PesapalPayRecords> pesapalDb
            )
        {
            _storeContext = storeContext;
            _settingService = settingService;
            _httpClient = httpClient;
            _pesapalDb= pesapalDb;
        }
        #endregion


        #region pesapal services
        public async Task<IpnResponseModel> GetIpnIdAsync( string token,object credentials,bool isSandbox)
        {

            var ipn = new IpnResponseModel();

            var storeId = await _storeContext.GetActiveStoreScopeConfigurationAsync();
            var storesetting = await _settingService.LoadSettingAsync<PesapalPaymentSettings>(storeId);

            var urlTail = "api/URLSetup/RegisterIPN";

            try
            {
                //no valid store
                if (storeId < 0)
                {
                    throw new ArgumentException("no valid store could be found");
                }

                //get the store saved user key and user secret
                var notifyUrl = await _settingService
                    .GetSettingByKeyAsync<string>($"{nameof(PesapalPaymentSettings)}.{nameof(PesapalPaymentSettings.Url)}", storeId: storeId);
                var notifyType = await _settingService
                    .GetSettingByKeyAsync<string>($"{nameof(PesapalPaymentSettings)}.{nameof(PesapalPaymentSettings.Ipn_notification_type)}", storeId: storeId);

                //format the body params
                
                var bodyParms = JsonConvert.SerializeObject(credentials);
                var httpContent = new StringContent(bodyParms, Encoding.UTF8, "application/json");

                //construct URL based on if its sandbox or production instance
                var url = new UriBuilder(isSandbox ?
                    Pesapalpaymentdefaults.SandBoxUrl + urlTail :
                    Pesapalpaymentdefaults.ProdUrl + urlTail);

                //format the http content
                _httpClient.DefaultRequestHeaders
                    .Accept
                    .Add(new MediaTypeWithQualityHeaderValue("application/json"));
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                //Debug.WriteLine(url.ToString());
                //make the post command
                var response = await _httpClient.PostAsync(url.ToString(), httpContent);

                //check the http response status and content
                //First check that the response has succeded
                if (response != null && response.IsSuccessStatusCode)
                {
                    var result = response.Content.ReadAsStringAsync().Result;
                    if (result != null)
                        //Debug.WriteLine(result);

                    ipn = JsonConvert.DeserializeObject<IpnResponseModel>(result);

                }
                //the response did not succeed or the response is null
                else
                {
                    var result = response.Content.ReadAsStringAsync().Result;
                    if (result != null)
                       // Debug.WriteLine(result);


                    ipn = JsonConvert.DeserializeObject<IpnResponseModel>(result);

                   // Debug.WriteLine("ipnsfailed"+ipn.Error.Message);


                }
            }
            catch (Exception e)
            {
                throw new ArgumentException("Error occured with ipn service" +e.Message);
            }
            return ipn;

        }
        public async Task<TokenModel> GetTokenAsync(object credentials,bool isSandbox)
        {

            var token = new TokenModel();

            var storeId = await _storeContext.GetActiveStoreScopeConfigurationAsync();
            var storesetting = await _settingService.LoadSettingAsync<PesapalPaymentSettings>(storeId);

            var urlTail = "api/Auth/RequestToken";

            try
            {
                //no valid store
                if (storeId < 0)
                {
                    throw new ArgumentException("no valid store could be found");
                }

              
                //jsonify the body params
               
                var bodyParms = JsonConvert.SerializeObject(credentials);

                var httpContent = new StringContent(bodyParms, Encoding.UTF8, "application/json");

                //construct URL based on if its sandbox or production instance
                var url = new UriBuilder(isSandbox ?
                    Pesapalpaymentdefaults.SandBoxUrl + urlTail :
                    Pesapalpaymentdefaults.ProdUrl + urlTail);

                //format the http content
                _httpClient.DefaultRequestHeaders
                    .Accept
                    .Add(new MediaTypeWithQualityHeaderValue("application/json"));
                Debug.WriteLine(url.ToString());
                //make the post command
                var response = await _httpClient.PostAsync(url.ToString(), httpContent);

                //check the http response status and content
                //First check that the response has succeded
                if (response != null && response.IsSuccessStatusCode)
                {
                    var result = response.Content.ReadAsStringAsync().Result;
                    if (result != null)

                        token = JsonConvert.DeserializeObject<TokenModel>(result);
                }
                //the response did not succeed or the response is null
                else
                {
                    var result = response.Content.ReadAsStringAsync().Result;

                    if (result != null)

                        token = JsonConvert.DeserializeObject<TokenModel>(result);

                }
            }
            catch (Exception e)
            {
                throw new ArgumentException("Error occured getting token. Please try again" + e.Message);
            }
            return token;

        }
        public async Task<PaymentResponse> SubmitOrderAsync(string accesstoken,PaymentRequestModel order, bool isSandbox)
        {

            var orderResponse = new PaymentResponse();

            var storeId = await _storeContext.GetActiveStoreScopeConfigurationAsync();
            var storesetting = await _settingService.LoadSettingAsync<PesapalPaymentSettings>(storeId);

            var urlTail = "api/Transactions/SubmitOrderRequest";

            try
            {
                //no valid store
                if (storeId < 0)
                {
                    throw new ArgumentException("no valid store could be found");
                }


                //jsonify the body params

                var bodyParms = JsonConvert.SerializeObject(order);

                var httpContent = new StringContent(bodyParms, Encoding.UTF8, "application/json");

                //construct URL based on if its sandbox or production instance
                var url = new UriBuilder(isSandbox ?
                    Pesapalpaymentdefaults.SandBoxUrl + urlTail :
                    Pesapalpaymentdefaults.ProdUrl + urlTail);

                //format the http content
                _httpClient.DefaultRequestHeaders
                    .Accept
                    .Add(new MediaTypeWithQualityHeaderValue("application/json"));
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accesstoken);

                //Debug.WriteLine(url.ToString());
                //make the post command
                var response = await _httpClient.PostAsync(url.ToString(), httpContent);

                //check the http response status and content
                //First check that the response has succeded
                if (response != null && response.IsSuccessStatusCode)
                {
                    var result = response.Content.ReadAsStringAsync().Result;
                    if (result != null)

                        orderResponse = JsonConvert.DeserializeObject<PaymentResponse>(result);
                    //Debug.WriteLine(orderResponse);
                }
                //the response did not succeed or the response is null
                else
                {
                    var result = response.Content.ReadAsStringAsync().Result;
                    Debug.WriteLine($"{result}");
                     if (result != null)

                        orderResponse = JsonConvert.DeserializeObject<PaymentResponse>(result);

                }
            }
            catch (Exception e)
            {
                throw new ArgumentException("unknown error occured during payment. Please try again" + e.Message);
            }
            //Debug.WriteLine(orderResponse);

            return orderResponse;

        }
        public async Task<PesapalPayStatusResponseModel> PaymentStatusAsync(string ordertrackid)
        {
            
            var paymentResponse = new PesapalPayStatusResponseModel();

            var storeId = await _storeContext.GetActiveStoreScopeConfigurationAsync();
            var storesetting = await _settingService.LoadSettingAsync<PesapalPaymentSettings>(storeId);

            var urlTail = "api/Transactions/GetTransactionStatus?orderTrackingId="+ordertrackid;
            
            try
            {
                //no valid store
                if (storeId < 0)
                {
                    throw new ArgumentException("no valid store could be found");
                }
                var creds = new
                {
                    consumer_key = storesetting.Consumer_key,
                    consumer_secret = storesetting.Consumer_secret
                };

                //first get the api access token
                var token = await GetTokenAsync(creds, storesetting.UseSandbox);

                var isSandbox = storesetting.UseSandbox;

                //construct URL based on if its sandbox or production instance
                var url = new UriBuilder(isSandbox ?
                    Pesapalpaymentdefaults.SandBoxUrl + urlTail :
                    Pesapalpaymentdefaults.ProdUrl + urlTail);

                //format the http content
                _httpClient.DefaultRequestHeaders
                    .Accept
                    .Add(new MediaTypeWithQualityHeaderValue("application/json"));
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);

                //make the get request
                var response = await _httpClient.GetAsync(url.ToString());

                //check the http response status and content
                //First check that the response has succeded
                if (response != null && response.IsSuccessStatusCode)
                {
                    var result = response.Content.ReadAsStringAsync().Result;
                    if (result != null)

                        paymentResponse = JsonConvert.DeserializeObject<PesapalPayStatusResponseModel>(result);
                    //Debug.WriteLine(orderResponse);
                }
                //the response did not succeed or the response is null
                else
                {
                    var result = response.Content.ReadAsStringAsync().Result;
                    Debug.WriteLine($"{result}");
                    if (result != null)

                        paymentResponse = JsonConvert.DeserializeObject<PesapalPayStatusResponseModel>(result);

                }
            }
            catch (Exception e)
            {
                throw new ArgumentException("unknown error occured during payment. Please try again" + e.Message);
            }

            return paymentResponse;

        }

        public virtual async Task<bool> OrderExist(string ordertrackid)
        {
            var exist = await _pesapalDb.Table.AnyAsync(tid=>tid.OrderTrackingId == ordertrackid);
            return exist;
        }
        public virtual async Task CreateOrderTrackRecord(PesapalPayRecords pesapalPayRecords)
        {
            await _pesapalDb.InsertAsync(pesapalPayRecords);
        }
        public virtual async Task UpdateOrderPaymentAsync(PesapalPayRecords pesapalPayRecords)
        {
            var order=_pesapalDb.Table.FirstOrDefaultAsync(id=>id.OrderTrackingId==pesapalPayRecords.OrderTrackingId).Result;
            if (order == null)
                throw new ArgumentException("error getting order track id");

            order.PaidDate = pesapalPayRecords.PaidDate;
            order.StatusMessage = pesapalPayRecords.StatusMessage;
            order.StatusCode = pesapalPayRecords.StatusCode;
            order.PaidCurrency=pesapalPayRecords.PaidCurrency;
            order.PaidAmount=pesapalPayRecords.PaidAmount;

            await _pesapalDb.UpdateAsync(order);
        }
        #endregion
         
        #region utils
        /// <summary>
        /// Handle function and get result
        /// </summary>
        /// <typeparam name="TResult">Result type</typeparam>
        /// <param name="function">Function</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the result; error message if exists
        /// </returns>
        protected async Task<(TResult Result, string Error)> HandleFunctionAsync<TResult>(Func<Task<TResult>> function)
        {
            try
            {
                //invoke function
                return (await function(), default);
            }
            catch (Exception exception)
            {
                //get a short error message
                var message = exception.Message;
                if (exception is HttpException httpException)
                {
                    //get error details if exist
                    //var details = JsonConvert.DeserializeObject<ExceptionDetails>(httpException.Message);
                    //message = details.Message?.Trim('.') ?? details.Name ?? message;
                    //if (details?.Details?.Any() ?? false)
                    //{
                       // message += details.Details.Aggregate(":", (text, issue) => $"{text} " +
                         //   $"{(issue.Description ?? issue.Issue).Trim('.')}{(!string.IsNullOrEmpty(issue.Field) ? $"({issue.Field})" : null)},").Trim(',');
                    //}
                }

                //log errors
                //var logMessage = $"{PayPalCommerceDefaults.SystemName} error: {System.Environment.NewLine}{message}";
                //await _logger.ErrorAsync(logMessage, exception, await _workContext.GetCurrentCustomerAsync());

                return (default, message);
            }
        }

        #endregion
        #region payment services
        /// <summary>
        /// Create an order
        /// </summary>
        /// <param name="settings">Plugin settings</param>
        /// <param name="orderGuid">Order GUID</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the created order; error message if exists
        /// </returns>
        public async Task<(Order Order, string Error)> CreateOrderAsync(PesapalPaymentSettings settings, Guid orderGuid)
        {
            /* return await HandleFunctionAsync(async () =>
             {
                 //ensure that plugin is configured
                 if (!IsConfigured(settings))
                     throw new NopException("Plugin not configured");

                 var customer = await _workContext.GetCurrentCustomerAsync();
                 var store = await _storeContext.GetCurrentStoreAsync();

                 var currency = (await _currencyService.GetCurrencyByIdAsync(_currencySettings.PrimaryStoreCurrencyId))?.CurrencyCode;
                 if (string.IsNullOrEmpty(currency))
                     throw new NopException("Primary store currency not set");

                 var billingAddress = await _addresService.GetAddressByIdAsync(customer.BillingAddressId ?? 0)
                     ?? throw new NopException("Customer billing address not set");

                 var shoppingCart = (await _shoppingCartService
                     .GetShoppingCartAsync(customer, Core.Domain.Orders.ShoppingCartType.ShoppingCart, store.Id))
                     .ToList();

                 var shippingAddress = await _addresService.GetAddressByIdAsync(customer.ShippingAddressId ?? 0);
                 if (!await _shoppingCartService.ShoppingCartRequiresShippingAsync(shoppingCart))
                     shippingAddress = null;

                 var billStateProvince = await _stateProvinceService.GetStateProvinceByAddressAsync(billingAddress);
                 var shipStateProvince = await _stateProvinceService.GetStateProvinceByAddressAsync(shippingAddress);

                 //prepare order details
                 var orderDetails = new OrderRequest { CheckoutPaymentIntent = settings.PaymentType.ToString().ToUpperInvariant() };

                 //prepare some common properties
                 orderDetails.ApplicationContext = new ApplicationContext
                 {
                     BrandName = CommonHelper.EnsureMaximumLength(store.Name, 127),
                     LandingPage = LandingPageType.Billing.ToString().ToUpperInvariant(),
                     UserAction = settings.PaymentType == Domain.PaymentType.Authorize
                         ? UserActionType.Continue.ToString().ToUpperInvariant()
                         : UserActionType.Pay_now.ToString().ToUpperInvariant(),
                     ShippingPreference = (shippingAddress != null ? ShippingPreferenceType.Set_provided_address : ShippingPreferenceType.No_shipping)
                         .ToString().ToUpperInvariant()
                 };

                 //prepare customer billing details
                 orderDetails.Payer = new Payer
                 {
                     Name = new Name
                     {
                         GivenName = CommonHelper.EnsureMaximumLength(billingAddress.FirstName, 140),
                         Surname = CommonHelper.EnsureMaximumLength(billingAddress.LastName, 140)
                     },
                     Email = CommonHelper.EnsureMaximumLength(billingAddress.Email, 254),
                     AddressPortable = new AddressPortable
                     {
                         AddressLine1 = CommonHelper.EnsureMaximumLength(billingAddress.Address1, 300),
                         AddressLine2 = CommonHelper.EnsureMaximumLength(billingAddress.Address2, 300),
                         AdminArea2 = CommonHelper.EnsureMaximumLength(billingAddress.City, 120),
                         AdminArea1 = CommonHelper.EnsureMaximumLength(billStateProvince?.Abbreviation, 300),
                         CountryCode = (await _countryService.GetCountryByIdAsync(billingAddress.CountryId ?? 0))?.TwoLetterIsoCode,
                         PostalCode = CommonHelper.EnsureMaximumLength(billingAddress.ZipPostalCode, 60)
                     }
                 };
                 if (!string.IsNullOrEmpty(billingAddress.PhoneNumber))
                 {
                     var cleanPhone = CommonHelper.EnsureMaximumLength(CommonHelper.EnsureNumericOnly(billingAddress.PhoneNumber), 14);
                     orderDetails.Payer.PhoneWithType = new PhoneWithType { PhoneNumber = new Phone { NationalNumber = cleanPhone } };
                 }

                 //prepare purchase unit details
                 var taxTotal = Math.Round((await _orderTotalCalculationService.GetTaxTotalAsync(shoppingCart, false)).taxTotal, 2);
                 var (cartShippingTotal, _, _) = await _orderTotalCalculationService.GetShoppingCartShippingTotalAsync(shoppingCart, false);
                 var shippingTotal = Math.Round(cartShippingTotal ?? decimal.Zero, 2);
                 var (shoppingCartTotal, _, _, _, _, _) = await _orderTotalCalculationService
                     .GetShoppingCartTotalAsync(shoppingCart, usePaymentMethodAdditionalFee: false);
                 var orderTotal = Math.Round(shoppingCartTotal ?? decimal.Zero, 2);

                 var purchaseUnit = new PurchaseUnitRequest
                 {
                     ReferenceId = CommonHelper.EnsureMaximumLength(orderGuid.ToString(), 256),
                     CustomId = CommonHelper.EnsureMaximumLength(orderGuid.ToString(), 127),
                     Description = CommonHelper.EnsureMaximumLength($"Purchase at '{store.Name}'", 127),
                     SoftDescriptor = CommonHelper.EnsureMaximumLength(store.Name, 22)
                 };

                 //prepare shipping address details
                 if (shippingAddress != null)
                 {
                     purchaseUnit.ShippingDetail = new ShippingDetail
                     {
                         Name = new Name { FullName = CommonHelper.EnsureMaximumLength($"{shippingAddress.FirstName} {shippingAddress.LastName}", 300) },
                         AddressPortable = new AddressPortable
                         {
                             AddressLine1 = CommonHelper.EnsureMaximumLength(shippingAddress.Address1, 300),
                             AddressLine2 = CommonHelper.EnsureMaximumLength(shippingAddress.Address2, 300),
                             AdminArea2 = CommonHelper.EnsureMaximumLength(shippingAddress.City, 120),
                             AdminArea1 = CommonHelper.EnsureMaximumLength(shipStateProvince?.Abbreviation, 300),
                             CountryCode = (await _countryService.GetCountryByIdAsync(billingAddress.CountryId ?? 0))?.TwoLetterIsoCode,
                             PostalCode = CommonHelper.EnsureMaximumLength(shippingAddress.ZipPostalCode, 60)
                         }
                     };
                 }

                 PayPalCheckoutSdk.Orders.Money prepareMoney(decimal value) => new()
                 {
                     CurrencyCode = currency,
                     Value = value.ToString(PayPalCommerceDefaults.CurrenciesWithoutDecimals.Contains(currency.ToUpperInvariant()) ? "0" : "0.00", CultureInfo.InvariantCulture)
                 };

                 //set order items
                 purchaseUnit.Items = await shoppingCart.SelectAwait(async item =>
                 {
                     var product = await _productService.GetProductByIdAsync(item.ProductId);

                     var (unitPrice, _, _) = await _shoppingCartService.GetUnitPriceAsync(item, true);
                     var (itemPrice, _) = await _taxService.GetProductPriceAsync(product, unitPrice, false, customer);
                     return new Item
                     {
                         Name = CommonHelper.EnsureMaximumLength(product.Name, 127),
                         Description = CommonHelper.EnsureMaximumLength(product.ShortDescription, 127),
                         Sku = CommonHelper.EnsureMaximumLength(product.Sku, 127),
                         Quantity = item.Quantity.ToString(),
                         Category = (product.IsDownload ? ItemCategoryType.Digital_goods : ItemCategoryType.Physical_goods)
                             .ToString().ToUpperInvariant(),
                         UnitAmount = prepareMoney(itemPrice)
                     };
                 }).ToListAsync();

                 //add checkout attributes as order items
                 var checkoutAttributes = await _genericAttributeService
                     .GetAttributeAsync<string>(customer, NopCustomerDefaults.CheckoutAttributes, store.Id);
                 var checkoutAttributeValues = _checkoutAttributeParser.ParseAttributeValues(checkoutAttributes);
                 await foreach (var (attribute, values) in checkoutAttributeValues)
                 {
                     await foreach (var attributeValue in values)
                     {
                         var (attributePrice, _) = await _taxService.GetCheckoutAttributePriceAsync(attribute, attributeValue, false, customer);
                         purchaseUnit.Items.Add(new Item
                         {
                             Name = CommonHelper.EnsureMaximumLength(attribute.Name, 127),
                             Description = CommonHelper.EnsureMaximumLength($"{attribute.Name} - {attributeValue.Name}", 127),
                             Quantity = 1.ToString(),
                             UnitAmount = prepareMoney(attributePrice)
                         });
                     }
                 }

                 //set totals
                 //there may be a problem with a mismatch of amounts since ItemTotal should equal sum of (unit amount * quantity) across all items
                 //but PayPal forcibly rounds all amounts to two decimal, so the more items, the higher the chance of rounding errors
                 //we obviously cannot change the order total, so slightly adjust other totals to match all requirements
                 var itemTotal = Math.Round(purchaseUnit.Items.Sum(item =>
                     decimal.Parse(item.UnitAmount.Value, NumberStyles.Any, CultureInfo.InvariantCulture) * int.Parse(item.Quantity)), 2);
                 var discountTotal = Math.Round(itemTotal + taxTotal + shippingTotal - orderTotal, 2);
                 if (discountTotal < decimal.Zero || discountTotal < settings.MinDiscountAmount)
                 {
                     taxTotal -= discountTotal;
                     discountTotal = decimal.Zero;
                 }
                 purchaseUnit.AmountWithBreakdown = new AmountWithBreakdown
                 {
                     CurrencyCode = currency,
                     Value = prepareMoney(orderTotal).Value,
                     AmountBreakdown = new AmountBreakdown
                     {
                         ItemTotal = prepareMoney(itemTotal),
                         TaxTotal = prepareMoney(taxTotal),
                         Shipping = prepareMoney(shippingTotal),
                         Discount = prepareMoney(discountTotal)
                     }
                 };

                 orderDetails.PurchaseUnits = new List<PurchaseUnitRequest> { purchaseUnit };

                 var orderRequest = new OrdersCreateRequest().RequestBody(orderDetails);
                 return await HandleCheckoutRequestAsync<OrdersCreateRequest, Order>(settings, orderRequest);
             })*/
            return await CreateOrderAsync(settings, orderGuid);
        }

        #endregion
    }
}
