using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentMigrator.Builders.Create.Table;
using Nop.Data.Mapping.Builders;
using Nop.Plugin.Payments.Pesapal.Models.PaymenRecords;

namespace Nop.Plugin.Payments.Pesapal.Services.Data
{
    public class PesapalPaymentBuilderService : NopEntityBuilder<PesapalPayRecords>
    {
        public override void MapEntity(CreateTableExpressionBuilder table)
        {
            table
                .WithColumn(nameof(PesapalPayRecords.StoreId)).AsInt32()
                .WithColumn(nameof(PesapalPayRecords.OrderGuid)).AsGuid()
                .WithColumn(nameof(PesapalPayRecords.OrderTrackingId)).AsString()
                .WithColumn(nameof(PesapalPayRecords.OrderAmount)).AsDecimal()
                .WithColumn(nameof(PesapalPayRecords.OrderCurrency)).AsString()
                .WithColumn(nameof(PesapalPayRecords.PaidAmount)).AsDecimal().Nullable()
                .WithColumn(nameof(PesapalPayRecords.PaidCurrency)).AsString().Nullable()
                .WithColumn(nameof(PesapalPayRecords.StatusCode)).AsInt32().Nullable()
                .WithColumn(nameof(PesapalPayRecords.StatusMessage)).AsString().Nullable()
                .WithColumn(nameof(PesapalPayRecords.PaidDate)).AsString().Nullable();



        }
    }
}
