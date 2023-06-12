using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nop.Core;

namespace Nop.Plugin.Payments.Pesapal.Models.PaymenRecords
{
    public class PesapalPayRecords:BaseEntity
    {
        public int StoreId { get; set; }
        public Guid OrderGuid { get;set; }
        public string OrderTrackingId { get;set; }
        public decimal OrderAmount { get;set; }
        public decimal PaidAmount { get;set; }
        public string OrderCurrency { get;set; }
        public string PaidCurrency { get;set; }
        public int StatusCode { get;set; }
        public string StatusMessage { get;set; }
        public string PaidDate { get;set; }

    }
}
