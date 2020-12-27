using FluentMigrator.Builders.Create.Table;
using Nop.Data.Mapping.Builders;
using Nop.Plugin.Shipping.ByWeightAndSize.Domain;

namespace Nop.Plugin.Shipping.ByWeightAndSize.Data
{
    public class ShippingByWeightAndSizeRecordBuilder : NopEntityBuilder<ShippingByWeightAndSizeRecord>
    {
        public override void MapEntity(CreateTableExpressionBuilder table)
        {
            table
                .WithColumn(nameof(ShippingByWeightAndSizeRecord.WeightFrom))
                .AsDecimal(18, 2)
                .WithColumn(nameof(ShippingByWeightAndSizeRecord.WeightTo))
                .AsDecimal(18, 2)
                .WithColumn(nameof(ShippingByWeightAndSizeRecord.OrderSubtotalFrom))
                .AsDecimal(18, 2)
                .WithColumn(nameof(ShippingByWeightAndSizeRecord.OrderSubtotalTo))
                .AsDecimal(18, 2)
                .WithColumn(nameof(ShippingByWeightAndSizeRecord.AdditionalFixedCost))
                .AsDecimal(18, 2)
                .WithColumn(nameof(ShippingByWeightAndSizeRecord.PercentageRateOfSubtotal))
                .AsDecimal(18, 2)
                .WithColumn(nameof(ShippingByWeightAndSizeRecord.RatePerWeightUnit))
                .AsDecimal(18, 2)
                .WithColumn(nameof(ShippingByWeightAndSizeRecord.LowerWeightLimit))
                .AsDecimal(18, 2)
                .WithColumn(nameof(ShippingByWeightAndSizeRecord.Zip))
                .AsString(400)
                .Nullable();
        }
    }
}