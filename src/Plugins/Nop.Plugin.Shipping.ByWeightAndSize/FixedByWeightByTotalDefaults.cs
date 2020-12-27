
namespace Nop.Plugin.Shipping.ByWeightAndSize
{
    /// <summary>
    /// Represents constants of the "Fixed or by weight" shipping plugin
    /// </summary>
    public static class ByWeightAndSizeDefaults
    {
        /// <summary>
        /// The key of the settings to save fixed rate of the shipping method
        /// </summary>
        public const string FixedRateSettingsKey = "ShippingRateComputationMethod.ByWeightAndSize.Rate.ShippingMethodId{0}";

        /// <summary>
        /// The key of the settings to save transit days of the shipping method
        /// </summary>
        public const string TransitDaysSettingsKey = "ShippingRateComputationMethod.ByWeightAndSize.TransitDays.ShippingMethodId{0}";
    }
}
