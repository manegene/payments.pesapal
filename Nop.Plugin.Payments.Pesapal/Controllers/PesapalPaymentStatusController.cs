using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Data;
using Nop.Plugin.Payments.Pesapal.Models.PaymenRecords;
using Nop.Plugin.Payments.Pesapal.Services;
using Nop.Services.Orders;

namespace Nop.Plugin.Payments.Pesapal.Controllers
{
    public class PesapalPaymentStatusController:Controller
    {
        #region fields
        private readonly IRepository<PesapalPayRecords> _recordsRepository;
        private readonly PesapalManagementService _managementService;
        private readonly IOrderService _orderService;
        #endregion
        #region ctor
        public PesapalPaymentStatusController(IRepository<PesapalPayRecords> recordsRepository,
            PesapalManagementService managementService,
            IOrderService orderService)
        {
            _recordsRepository = recordsRepository;
            _managementService = managementService;
            _orderService = orderService;
        }
        #endregion

        //pesapal IPN endpoint
        [HttpGet]
        public async Task<IActionResult> IpnStatus(string orderTrackingId,string ordermerchantReference, string orderNotificationType)
        {
            if (string.IsNullOrWhiteSpace(orderTrackingId))
                throw new Exception("invalid order track");
            //get matching order by tracking id
            var saved = await _recordsRepository.Table.Where(pid => pid.OrderTrackingId == orderTrackingId).FirstOrDefaultAsync();

            if (string.IsNullOrEmpty(saved.OrderTrackingId))
            {
                
                //cant process a null order. return statu 500
                return new JsonResult(new
                {
                    orderNotificationType=orderNotificationType,
                    orderTrackingId=orderTrackingId,
                    orderMerchantReference=ordermerchantReference,
                    status=500
                });
            }

            //order succeded and we have the tracking id.
            return new JsonResult(new
            {
                orderNotificationType = orderNotificationType,
                orderTrackingId = orderTrackingId,
                orderMerchantReference = ordermerchantReference,
                status = 200
            });

        }
        //pesapal redirect url
        [HttpGet]
        public async Task<IActionResult> CheckPayStatus(string orderTrackingId, string ordermerchantReference, string orderNotificationType)
        {
            if (string.IsNullOrWhiteSpace(orderTrackingId))
                throw new Exception("invalid order track");
            //get matching order by tracking id
            var paidorder = await _recordsRepository.Table.Where(pid => pid.OrderTrackingId == orderTrackingId).FirstOrDefaultAsync();

            if (paidorder == null)
                throw new Exception("operation not supported for this request");
            //check the payment status of the order

            var status = await _managementService.PaymentStatusAsync(orderTrackingId);
            if (status == null)
                throw new Exception("error fething payment from Pesapal");

            //update payment record
            var payrecord = new PesapalPayRecords
            {
                OrderTrackingId= paidorder.OrderTrackingId,
                PaidDate = status.Created_date,
                StatusMessage = status.Description,
                StatusCode = status.Status_code,
                PaidCurrency = status.Currency,
                PaidAmount = status.Amount


            };
            var viewname = "";
            
            //failed transaction
            if (status.Status_code != 1)
            {
                viewname = "~/Plugins/Payments.Pesapal/Views/Failed.cshtml";
                

            }
            //payment succeded
            else
            {
                //first update pesapapal payrecord table
                await _managementService.UpdateOrderPaymentAsync(payrecord);
                
                //now update the site order payment status
                var order = await _orderService.GetOrderByGuidAsync(paidorder.OrderGuid);
                order.PaymentStatus = PaymentStatus.Paid;
                await _orderService.UpdateOrderAsync(order);
                
                viewname = "~/Plugins/Payments.Pesapal/Views/Success.cshtml";

            }




            return View(viewname);

        }

    }
}
