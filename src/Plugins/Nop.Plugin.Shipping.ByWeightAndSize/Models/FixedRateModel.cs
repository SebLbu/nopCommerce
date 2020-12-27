using Nop.Web.Framework.Mvc.ModelBinding;
using Nop.Web.Framework.Models;
using System.ComponentModel.DataAnnotations;

namespace Nop.Plugin.Shipping.ByWeightAndSize.Models
{
    public class FixedRateModel : BaseNopModel
    {
        public int ShippingMethodId { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.ByWeightAndSize.Fields.ShippingMethod")]
        public string ShippingMethodName { get; set; }

        [NopResourceDisplayName("Plugins.Shipping.ByWeightAndSize.Fields.Rate")]
        public decimal Rate { get; set; }

        [UIHint("Int32Nullable")]
        [NopResourceDisplayName("Plugins.Shipping.ByWeightAndSize.Fields.TransitDays")]
        public int? TransitDays { get; set; }
    }
}