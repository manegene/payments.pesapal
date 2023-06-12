using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentMigrator;
using Nop.Data.Extensions;
using Nop.Data.Migrations;
using Nop.Plugin.Payments.Pesapal.Models.PaymenRecords;

namespace Nop.Plugin.Payments.Pesapal.Services.Data
{
    [NopMigration("2023/02/03 09:30:17:6455422", "payments.Pesapal base pay record schema", MigrationProcessType.Installation)]
    public class SchemaMigration : AutoReversingMigration
    {
        public override void Up()
        {
            Create.TableFor<PesapalPayRecords>();
        }
    }
}
