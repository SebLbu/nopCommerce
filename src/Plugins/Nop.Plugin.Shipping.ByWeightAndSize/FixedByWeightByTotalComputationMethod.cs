using System;
using System.Collections.Generic;
using System.Linq;
using Nop.Core;
using Nop.Core.Domain.Shipping;
using Nop.Plugin.Shipping.ByWeightAndSize.Domain;
using Nop.Plugin.Shipping.ByWeightAndSize.Services;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Orders;
using Nop.Services.Plugins;
using Nop.Services.Shipping;
using Nop.Services.Shipping.Tracking;

namespace Nop.Plugin.Shipping.ByWeightAndSize
{
    /// <summary>
    /// Fixed rate or by weight shipping computation method 
    /// </summary>
    public class ByWeightAndSizeComputationMethod : BasePlugin, IShippingRateComputationMethod
    {
        #region Fields

        private readonly ByWeightAndSizeSettings _byWeightAndSizeSettings;
        private readonly ILocalizationService _localizationService;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly ISettingService _settingService;
        private readonly IShippingByWeightAndSizeService _shippingByWeightAndSizeAndSizeService;
        private readonly IShippingService _shippingService;
        private readonly IStoreContext _storeContext;
        private readonly IWebHelper _webHelper;

        #endregion

        #region Ctor

        public ByWeightAndSizeComputationMethod(ByWeightAndSizeSettings byWeightAndSizeSettings,
            ILocalizationService localizationService,
            IShoppingCartService shoppingCartService,
            ISettingService settingService,
            IShippingByWeightAndSizeService shippingByWeightAndSizeAndSizeService,
            IShippingService shippingService,
            IStoreContext storeContext,
            IWebHelper webHelper)
        {
            _byWeightAndSizeSettings = byWeightAndSizeSettings;
            _localizationService = localizationService;
            _shoppingCartService = shoppingCartService;
            _settingService = settingService;
            _shippingByWeightAndSizeAndSizeService = shippingByWeightAndSizeAndSizeService;
            _shippingService = shippingService;
            _storeContext = storeContext;
            _webHelper = webHelper;
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Get fixed rate
        /// </summary>
        /// <param name="shippingMethodId">Shipping method ID</param>
        /// <returns>Rate</returns>
        private decimal GetRate(int shippingMethodId)
        {
            return _settingService.GetSettingByKey<decimal>(string.Format(ByWeightAndSizeDefaults.FixedRateSettingsKey, shippingMethodId));
        }

        /// <summary>
        /// Gets the transit days
        /// </summary>
        /// <param name="shippingMethodId">Shipping method ID</param>
        /// <returns>Transit days</returns>
        private int? GetTransitDays(int shippingMethodId)
        {
            return _settingService.GetSettingByKey<int?>(string.Format(ByWeightAndSizeDefaults.TransitDaysSettingsKey, shippingMethodId));
        }

        /// <summary>
        /// Get rate by weight and by total
        /// </summary>
        /// <param name="shippingByWeightAndSizeAndSizeRecord">Shipping by weight/by total record</param>
        /// <param name="subTotal">Subtotal</param>
        /// <param name="weight">Weight</param>
        /// <returns>Rate</returns>
        private decimal GetRate(ShippingByWeightAndSizeRecord shippingByWeightAndSizeAndSizeRecord, decimal subTotal, decimal weight)
        {
            //additional fixed cost
            var shippingTotal = shippingByWeightAndSizeAndSizeRecord.AdditionalFixedCost;

            //charge amount per weight unit
            if (shippingByWeightAndSizeAndSizeRecord.RatePerWeightUnit > decimal.Zero)
            {
                var weightRate = Math.Max(weight - shippingByWeightAndSizeAndSizeRecord.LowerWeightLimit, decimal.Zero);
                shippingTotal += shippingByWeightAndSizeAndSizeRecord.RatePerWeightUnit * weightRate;
            }

            //percentage rate of subtotal
            if (shippingByWeightAndSizeAndSizeRecord.PercentageRateOfSubtotal > decimal.Zero)
            {
                shippingTotal += Math.Round((decimal)((((float)subTotal) * ((float)shippingByWeightAndSizeAndSizeRecord.PercentageRateOfSubtotal)) / 100f), 2);
            }

            return Math.Max(shippingTotal, decimal.Zero);
        }

        #endregion

        #region Methods

        /// <summary>
        ///  Gets available shipping options
        /// </summary>
        /// <param name="getShippingOptionRequest">A request for getting shipping options</param>
        /// <returns>Represents a response of getting shipping rate options</returns>
        public GetShippingOptionResponse GetShippingOptions(GetShippingOptionRequest getShippingOptionRequest)
        {
            if (getShippingOptionRequest == null)
                throw new ArgumentNullException(nameof(getShippingOptionRequest));

            var response = new GetShippingOptionResponse();

            if (getShippingOptionRequest.Items == null || !getShippingOptionRequest.Items.Any())
            {
                response.AddError("No shipment items");
                return response;
            }

            //choose the shipping rate calculation method
            if (_byWeightAndSizeSettings.ShippingByWeightAndSizeEnabled)
            {
                //shipping rate calculation by products weight

                if (getShippingOptionRequest.ShippingAddress == null)
                {
                    response.AddError("Shipping address is not set");
                    return response;
                }

                var storeId = getShippingOptionRequest.StoreId != 0 ? getShippingOptionRequest.StoreId : _storeContext.CurrentStore.Id;
                var countryId = getShippingOptionRequest.ShippingAddress.CountryId ?? 0;
                var stateProvinceId = getShippingOptionRequest.ShippingAddress.StateProvinceId ?? 0;
                var warehouseId = getShippingOptionRequest.WarehouseFrom?.Id ?? 0;
                var zip = getShippingOptionRequest.ShippingAddress.ZipPostalCode;

                //get subtotal of shipped items
                var subTotal = decimal.Zero;
                foreach (var packageItem in getShippingOptionRequest.Items)
                {
                    if (_shippingService.IsFreeShipping(packageItem.ShoppingCartItem))
                        continue;

                    subTotal += _shoppingCartService.GetSubTotal(packageItem.ShoppingCartItem);
                }

                //get weight of shipped items (excluding items with free shipping)
                var weight = _shippingService.GetTotalWeight(getShippingOptionRequest, ignoreFreeShippedItems: true);

                foreach (var shippingMethod in _shippingService.GetAllShippingMethods(countryId))
                {
                    int? transitDays = null;
                    var rate = decimal.Zero;

                    var shippingByWeightAndSizeAndSizeRecord = _shippingByWeightAndSizeAndSizeService.FindRecords(
                            shippingMethod.Id, storeId, warehouseId, countryId, stateProvinceId, zip, weight, subTotal);
                    if (shippingByWeightAndSizeAndSizeRecord == null)
                    {
                        if (_byWeightAndSizeSettings.LimitMethodsToCreated)
                            continue;
                    }
                    else
                    {
                        rate = GetRate(shippingByWeightAndSizeAndSizeRecord, subTotal, weight);
                        transitDays = shippingByWeightAndSizeAndSizeRecord.TransitDays;
                    }

                    response.ShippingOptions.Add(new ShippingOption
                    {
                        Name = _localizationService.GetLocalized(shippingMethod, x => x.Name),
                        Description = _localizationService.GetLocalized(shippingMethod, x => x.Description),
                        Rate = rate,
                        TransitDays = transitDays
                    });
                }
            }
            else
            {
                //shipping rate calculation by fixed rate
                var restrictByCountryId = getShippingOptionRequest.ShippingAddress?.CountryId;
                response.ShippingOptions = _shippingService.GetAllShippingMethods(restrictByCountryId).Select(shippingMethod => new ShippingOption
                {
                    Name = _localizationService.GetLocalized(shippingMethod, x => x.Name),
                    Description = _localizationService.GetLocalized(shippingMethod, x => x.Description),
                    Rate = GetRate(shippingMethod.Id),
                    TransitDays = GetTransitDays(shippingMethod.Id)
                }).ToList();
            }

            return response;
        }

        /// <summary>
        /// Gets fixed shipping rate (if shipping rate computation method allows it and the rate can be calculated before checkout).
        /// </summary>
        /// <param name="getShippingOptionRequest">A request for getting shipping options</param>
        /// <returns>Fixed shipping rate; or null in case there's no fixed shipping rate</returns>
        public decimal? GetFixedRate(GetShippingOptionRequest getShippingOptionRequest)
        {
            if (getShippingOptionRequest == null)
                throw new ArgumentNullException(nameof(getShippingOptionRequest));

            //if the "shipping calculation by weight" method is selected, the fixed rate isn't calculated
            if (_byWeightAndSizeSettings.ShippingByWeightAndSizeEnabled)
                return null;

            var restrictByCountryId = getShippingOptionRequest.ShippingAddress?.CountryId;
            var rates = _shippingService.GetAllShippingMethods(restrictByCountryId)
                .Select(shippingMethod => GetRate(shippingMethod.Id)).Distinct().ToList();

            //return default rate if all of them equal
            if (rates.Count == 1)
                return rates.FirstOrDefault();

            return null;
        }

        /// <summary>
        /// Gets a configuration page URL
        /// </summary>
        public override string GetConfigurationPageUrl()
        {
            return $"{_webHelper.GetStoreLocation()}Admin/ByWeightAndSize/Configure";
        }

        /// <summary>
        /// Install plugin
        /// </summary>
        public override void Install()
        {
            //settings
            _settingService.SaveSetting(new ByWeightAndSizeSettings());

            //locales
            _localizationService.AddPluginLocaleResource(new Dictionary<string, string>
            {
                ["Plugins.Shipping.ByWeightAndSize.AddRecord"] = "Add record",
                ["Plugins.Shipping.ByWeightAndSize.Fields.AdditionalFixedCost"] = "Additional fixed cost",
                ["Plugins.Shipping.ByWeightAndSize.Fields.AdditionalFixedCost.Hint"] = "Specify an additional fixed cost per shopping cart for this option. Set to 0 if you don't want an additional fixed cost to be applied.",
                ["Plugins.Shipping.ByWeightAndSize.Fields.Country"] = "Country",
                ["Plugins.Shipping.ByWeightAndSize.Fields.Country.Hint"] = "If an asterisk is selected, then this shipping rate will apply to all customers, regardless of the country.",
                ["Plugins.Shipping.ByWeightAndSize.Fields.DataHtml"] = "Data",
                ["Plugins.Shipping.ByWeightAndSize.Fields.LimitMethodsToCreated"] = "Limit shipping methods to configured ones",
                ["Plugins.Shipping.ByWeightAndSize.Fields.LimitMethodsToCreated.Hint"] = "If you check this option, then your customers will be limited to shipping options configured here. Otherwise, they'll be able to choose any existing shipping options even they are not configured here (zero shipping fee in this case).",
                ["Plugins.Shipping.ByWeightAndSize.Fields.LowerWeightLimit"] = "Lower weight limit",
                ["Plugins.Shipping.ByWeightAndSize.Fields.LowerWeightLimit.Hint"] = "Lower weight limit. This field can be used for \"per extra weight unit\" scenarios.",
                ["Plugins.Shipping.ByWeightAndSize.Fields.OrderSubtotalFrom"] = "Order subtotal from",
                ["Plugins.Shipping.ByWeightAndSize.Fields.OrderSubtotalFrom.Hint"] = "Order subtotal from.",
                ["Plugins.Shipping.ByWeightAndSize.Fields.OrderSubtotalTo"] = "Order subtotal to",
                ["Plugins.Shipping.ByWeightAndSize.Fields.OrderSubtotalTo.Hint"] = "Order subtotal to.",
                ["Plugins.Shipping.ByWeightAndSize.Fields.PercentageRateOfSubtotal"] = "Charge percentage (of subtotal)",
                ["Plugins.Shipping.ByWeightAndSize.Fields.PercentageRateOfSubtotal.Hint"] = "Charge percentage (of subtotal).",
                ["Plugins.Shipping.ByWeightAndSize.Fields.Rate"] = "Rate",
                ["Plugins.Shipping.ByWeightAndSize.Fields.RatePerWeightUnit"] = "Rate per weight unit",
                ["Plugins.Shipping.ByWeightAndSize.Fields.RatePerWeightUnit.Hint"] = "Rate per weight unit.",
                ["Plugins.Shipping.ByWeightAndSize.Fields.ShippingMethod"] = "Shipping method",
                ["Plugins.Shipping.ByWeightAndSize.Fields.ShippingMethod.Hint"] = "Choose shipping method.",
                ["Plugins.Shipping.ByWeightAndSize.Fields.StateProvince"] = "State / province",
                ["Plugins.Shipping.ByWeightAndSize.Fields.StateProvince.Hint"] = "If an asterisk is selected, then this shipping rate will apply to all customers from the given country, regardless of the state.",
                ["Plugins.Shipping.ByWeightAndSize.Fields.Store"] = "Store",
                ["Plugins.Shipping.ByWeightAndSize.Fields.Store.Hint"] = "If an asterisk is selected, then this shipping rate will apply to all stores.",
                ["Plugins.Shipping.ByWeightAndSize.Fields.TransitDays"] = "Transit days",
                ["Plugins.Shipping.ByWeightAndSize.Fields.TransitDays.Hint"] = "The number of days of delivery of the goods.",
                ["Plugins.Shipping.ByWeightAndSize.Fields.Warehouse"] = "Warehouse",
                ["Plugins.Shipping.ByWeightAndSize.Fields.Warehouse.Hint"] = "If an asterisk is selected, then this shipping rate will apply to all warehouses.",
                ["Plugins.Shipping.ByWeightAndSize.Fields.WeightFrom"] = "Order weight from",
                ["Plugins.Shipping.ByWeightAndSize.Fields.WeightFrom.Hint"] = "Order weight from.",
                ["Plugins.Shipping.ByWeightAndSize.Fields.WeightTo"] = "Order weight to",
                ["Plugins.Shipping.ByWeightAndSize.Fields.WeightTo.Hint"] = "Order weight to.",
                ["Plugins.Shipping.ByWeightAndSize.Fields.Zip"] = "Zip",
                ["Plugins.Shipping.ByWeightAndSize.Fields.Zip.Hint"] = "Zip / postal code. If zip is empty, then this shipping rate will apply to all customers from the given country or state, regardless of the zip code.",
                ["Plugins.Shipping.ByWeightAndSize.Fixed"] = "Fixed Rate",
                ["Plugins.Shipping.ByWeightAndSize.Formula"] = "Formula to calculate rates",
                ["Plugins.Shipping.ByWeightAndSize.Formula.Value"] = "[additional fixed cost] + ([order total weight] - [lower weight limit]) * [rate per weight unit] + [order subtotal] * [charge percentage]",
                ["Plugins.Shipping.ByWeightAndSize.ShippingByWeight"] = "By Weight"
            });

            base.Install();
        }

        /// <summary>
        /// Uninstall plugin
        /// </summary>
        public override void Uninstall()
        {
            //settings
            _settingService.DeleteSetting<ByWeightAndSizeSettings>();

            //fixed rates
            var fixedRates = _shippingService.GetAllShippingMethods()
                .Select(shippingMethod => _settingService.GetSetting(
                    string.Format(ByWeightAndSizeDefaults.FixedRateSettingsKey, shippingMethod.Id)))
                .Where(setting => setting != null).ToList();
            _settingService.DeleteSettings(fixedRates);

            //locales
            _localizationService.DeletePluginLocaleResources("Plugins.Shipping.ByWeightAndSize");

            base.Uninstall();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets a shipping rate computation method type
        /// </summary>
        public ShippingRateComputationMethodType ShippingRateComputationMethodType => ShippingRateComputationMethodType.Offline;

        /// <summary>
        /// Gets a shipment tracker
        /// </summary>
        /// <remarks>
        /// uncomment a line below to return a general shipment tracker (finds an appropriate tracker by tracking number)
        /// return new GeneralShipmentTracker(EngineContext.Current.Resolve<ITypeFinder>());
        /// </remarks>
        public IShipmentTracker ShipmentTracker => null;

        #endregion
    }
}