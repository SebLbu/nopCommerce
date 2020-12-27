using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core.Domain.Directory;
using Nop.Plugin.Shipping.ByWeightAndSize.Domain;
using Nop.Plugin.Shipping.ByWeightAndSize.Models;
using Nop.Plugin.Shipping.ByWeightAndSize.Services;
using Nop.Services.Configuration;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Security;
using Nop.Services.Shipping;
using Nop.Services.Stores;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Models.Extensions;
using Nop.Web.Framework.Mvc;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Shipping.ByWeightAndSize.Controllers
{
    [AuthorizeAdmin]
    [Area(AreaNames.Admin)]
    [AutoValidateAntiforgeryToken]
    public class ByWeightAndSizeController : BasePluginController
    {
        #region Fields

        private readonly CurrencySettings _currencySettings;
        private readonly ByWeightAndSizeSettings _byWeightAndSizeSettings;
        private readonly ICountryService _countryService;
        private readonly ICurrencyService _currencyService;
        private readonly ILocalizationService _localizationService;
        private readonly IMeasureService _measureService;
        private readonly IPermissionService _permissionService;
        private readonly ISettingService _settingService;
        private readonly IShippingByWeightAndSizeService _shippingByWeightAndSizeService;
        private readonly IShippingService _shippingService;
        private readonly IStateProvinceService _stateProvinceService;
        private readonly IStoreService _storeService;
        private readonly MeasureSettings _measureSettings;

        #endregion

        #region Ctor

        public ByWeightAndSizeController(CurrencySettings currencySettings,
            ByWeightAndSizeSettings byWeightAndSizeSettings,
            ICountryService countryService,
            ICurrencyService currencyService,
            ILocalizationService localizationService,
            IMeasureService measureService,
            IPermissionService permissionService,
            ISettingService settingService,
            IShippingByWeightAndSizeService shippingByWeightAndSizeService,
            IShippingService shippingService,
            IStateProvinceService stateProvinceService,
            IStoreService storeService,
            MeasureSettings measureSettings)
        {
            _currencySettings = currencySettings;
            _byWeightAndSizeSettings = byWeightAndSizeSettings;
            _countryService = countryService;
            _currencyService = currencyService;
            _localizationService = localizationService;
            _measureService = measureService;
            _permissionService = permissionService;
            _settingService = settingService;
            _shippingByWeightAndSizeService = shippingByWeightAndSizeService;
            _stateProvinceService = stateProvinceService;
            _shippingService = shippingService;
            _storeService = storeService;
            _measureSettings = measureSettings;
        }

        #endregion

        #region Methods

        public IActionResult Configure()
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageShippingSettings))
                return AccessDeniedView();

            var model = new ConfigurationModel
            {
                LimitMethodsToCreated = _byWeightAndSizeSettings.LimitMethodsToCreated,
                ShippingByWeightAndSizeEnabled = _byWeightAndSizeSettings.ShippingByWeightAndSizeEnabled
            };

            //stores
            model.AvailableStores.Add(new SelectListItem { Text = "*", Value = "0" });
            foreach (var store in _storeService.GetAllStores())
                model.AvailableStores.Add(new SelectListItem { Text = store.Name, Value = store.Id.ToString() });
            //warehouses
            model.AvailableWarehouses.Add(new SelectListItem { Text = "*", Value = "0" });
            foreach (var warehouses in _shippingService.GetAllWarehouses())
                model.AvailableWarehouses.Add(new SelectListItem { Text = warehouses.Name, Value = warehouses.Id.ToString() });
            //shipping methods
            foreach (var sm in _shippingService.GetAllShippingMethods())
                model.AvailableShippingMethods.Add(new SelectListItem { Text = sm.Name, Value = sm.Id.ToString() });
            //countries
            model.AvailableCountries.Add(new SelectListItem { Text = "*", Value = "0" });
            var countries = _countryService.GetAllCountries();
            foreach (var c in countries)
                model.AvailableCountries.Add(new SelectListItem { Text = c.Name, Value = c.Id.ToString() });
            //states
            model.AvailableStates.Add(new SelectListItem { Text = "*", Value = "0" });

            model.SetGridPageSize();

            return View("~/Plugins/Shipping.ByWeightAndSize/Views/Configure.cshtml", model);
        }

        [HttpPost]
        public IActionResult Configure(ConfigurationModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageShippingSettings))
                return Content("Access denied");

            //save settings
            _byWeightAndSizeSettings.LimitMethodsToCreated = model.LimitMethodsToCreated;
            _settingService.SaveSetting(_byWeightAndSizeSettings);

            return Json(new { Result = true });
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public IActionResult SaveMode(bool value)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageShippingSettings))
                return Content("Access denied");

            //save settings
            _byWeightAndSizeSettings.ShippingByWeightAndSizeEnabled = value;
            _settingService.SaveSetting(_byWeightAndSizeSettings);

            return Json(new { Result = true });
        }

        #region Fixed rate

        [HttpPost]
        public IActionResult FixedShippingRateList(ConfigurationModel searchModel)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageShippingSettings))
                return AccessDeniedDataTablesJson();

            var shippingMethods = _shippingService.GetAllShippingMethods().ToPagedList(searchModel);

            var gridModel = new FixedRateListModel().PrepareToGrid(searchModel, shippingMethods, () =>
            {
                return shippingMethods.Select(shippingMethod => new FixedRateModel
                {
                    ShippingMethodId = shippingMethod.Id,
                    ShippingMethodName = shippingMethod.Name,
                    Rate = _settingService.GetSettingByKey<decimal>(
                        string.Format(ByWeightAndSizeDefaults.FixedRateSettingsKey, shippingMethod.Id)),
                    TransitDays = _settingService.GetSettingByKey<int?>(
                        string.Format(ByWeightAndSizeDefaults.TransitDaysSettingsKey, shippingMethod.Id))
                });
            });

            return Json(gridModel);
        }

        [HttpPost]
        public IActionResult UpdateFixedShippingRate(FixedRateModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageShippingSettings))
                return Content("Access denied");

            _settingService.SetSetting(string.Format(ByWeightAndSizeDefaults.FixedRateSettingsKey, model.ShippingMethodId), model.Rate, 0, false);
            _settingService.SetSetting(string.Format(ByWeightAndSizeDefaults.TransitDaysSettingsKey, model.ShippingMethodId), model.TransitDays, 0, false);

            _settingService.ClearCache();

            return new NullJsonResult();
        }

        #endregion

        #region Rate by weight

        [HttpPost]
        public IActionResult RateByWeightAndSizeList(ConfigurationModel searchModel, ConfigurationModel filter)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageShippingSettings))
                return AccessDeniedDataTablesJson();

            //var records = _shippingByWeightAndSizeService.GetAll(command.Page - 1, command.PageSize);
            var records = _shippingByWeightAndSizeService.FindRecords(
              pageIndex: searchModel.Page - 1,
              pageSize: searchModel.PageSize,
              storeId: filter.SearchStoreId,
              warehouseId: filter.SearchWarehouseId,
              countryId: filter.SearchCountryId,
              stateProvinceId: filter.SearchStateProvinceId,
              zip: filter.SearchZip,
              shippingMethodId: filter.SearchShippingMethodId,
              weight: null,
              orderSubtotal: null
              );

            var gridModel = new ShippingByWeightAndSizeListModel().PrepareToGrid(searchModel, records, () =>
            {
                return records.Select(record =>
                {
                    var model = new ShippingByWeightAndSizeModel
                    {
                        Id = record.Id,
                        StoreId = record.StoreId,
                        StoreName = _storeService.GetStoreById(record.StoreId)?.Name ?? "*",
                        WarehouseId = record.WarehouseId,
                        WarehouseName = _shippingService.GetWarehouseById(record.WarehouseId)?.Name ?? "*",
                        ShippingMethodId = record.ShippingMethodId,
                        ShippingMethodName = _shippingService.GetShippingMethodById(record.ShippingMethodId)?.Name ??
                                             "Unavailable",
                        CountryId = record.CountryId,
                        CountryName = _countryService.GetCountryById(record.CountryId)?.Name ?? "*",
                        StateProvinceId = record.StateProvinceId,
                        StateProvinceName =
                            _stateProvinceService.GetStateProvinceById(record.StateProvinceId)?.Name ?? "*",
                        WeightFrom = record.WeightFrom,
                        WeightTo = record.WeightTo,
                        OrderSubtotalFrom = record.OrderSubtotalFrom,
                        OrderSubtotalTo = record.OrderSubtotalTo,
                        AdditionalFixedCost = record.AdditionalFixedCost,
                        PercentageRateOfSubtotal = record.PercentageRateOfSubtotal,
                        RatePerWeightUnit = record.RatePerWeightUnit,
                        LowerWeightLimit = record.LowerWeightLimit,
                        Zip = !string.IsNullOrEmpty(record.Zip) ? record.Zip : "*"
                    };

                    var htmlSb = new StringBuilder("<div>");
                    htmlSb.AppendFormat("{0}: {1}",
                        _localizationService.GetResource("Plugins.Shipping.ByWeightAndSize.Fields.WeightFrom"),
                        model.WeightFrom);
                    htmlSb.Append("<br />");
                    htmlSb.AppendFormat("{0}: {1}",
                        _localizationService.GetResource("Plugins.Shipping.ByWeightAndSize.Fields.WeightTo"),
                        model.WeightTo);
                    htmlSb.Append("<br />");
                    htmlSb.AppendFormat("{0}: {1}",
                        _localizationService.GetResource(
                            "Plugins.Shipping.ByWeightAndSize.Fields.OrderSubtotalFrom"), model.OrderSubtotalFrom);
                    htmlSb.Append("<br />");
                    htmlSb.AppendFormat("{0}: {1}",
                        _localizationService.GetResource(
                            "Plugins.Shipping.ByWeightAndSize.Fields.OrderSubtotalTo"), model.OrderSubtotalTo);
                    htmlSb.Append("<br />");
                    htmlSb.AppendFormat("{0}: {1}",
                        _localizationService.GetResource(
                            "Plugins.Shipping.ByWeightAndSize.Fields.AdditionalFixedCost"),
                        model.AdditionalFixedCost);
                    htmlSb.Append("<br />");
                    htmlSb.AppendFormat("{0}: {1}",
                        _localizationService.GetResource(
                            "Plugins.Shipping.ByWeightAndSize.Fields.RatePerWeightUnit"), model.RatePerWeightUnit);
                    htmlSb.Append("<br />");
                    htmlSb.AppendFormat("{0}: {1}",
                        _localizationService.GetResource(
                            "Plugins.Shipping.ByWeightAndSize.Fields.LowerWeightLimit"), model.LowerWeightLimit);
                    htmlSb.Append("<br />");
                    htmlSb.AppendFormat("{0}: {1}",
                        _localizationService.GetResource(
                            "Plugins.Shipping.ByWeightAndSize.Fields.PercentageRateOfSubtotal"),
                        model.PercentageRateOfSubtotal);

                    htmlSb.Append("</div>");
                    model.DataHtml = htmlSb.ToString();

                    return model;
                });
            });

            return Json(gridModel);
        }

        public IActionResult AddRateByWeightAndSizePopup()
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageShippingSettings))
                return AccessDeniedView();

            var model = new ShippingByWeightAndSizeModel
            {
                PrimaryStoreCurrencyCode = _currencyService.GetCurrencyById(_currencySettings.PrimaryStoreCurrencyId)?.CurrencyCode,
                BaseWeightIn = _measureService.GetMeasureWeightById(_measureSettings.BaseWeightId)?.Name,
                WeightTo = 1000000,
                OrderSubtotalTo = 1000000
            };

            var shippingMethods = _shippingService.GetAllShippingMethods();
            if (!shippingMethods.Any())
                return Content("No shipping methods can be loaded");

            //stores
            model.AvailableStores.Add(new SelectListItem { Text = "*", Value = "0" });
            foreach (var store in _storeService.GetAllStores())
                model.AvailableStores.Add(new SelectListItem { Text = store.Name, Value = store.Id.ToString() });
            //warehouses
            model.AvailableWarehouses.Add(new SelectListItem { Text = "*", Value = "0" });
            foreach (var warehouses in _shippingService.GetAllWarehouses())
                model.AvailableWarehouses.Add(new SelectListItem { Text = warehouses.Name, Value = warehouses.Id.ToString() });
            //shipping methods
            foreach (var sm in shippingMethods)
                model.AvailableShippingMethods.Add(new SelectListItem { Text = sm.Name, Value = sm.Id.ToString() });
            //countries
            model.AvailableCountries.Add(new SelectListItem { Text = "*", Value = "0" });
            var countries = _countryService.GetAllCountries(showHidden: true);
            foreach (var c in countries)
                model.AvailableCountries.Add(new SelectListItem { Text = c.Name, Value = c.Id.ToString() });
            //states
            model.AvailableStates.Add(new SelectListItem { Text = "*", Value = "0" });

            return View("~/Plugins/Shipping.ByWeightAndSize/Views/AddRateByWeightAndSizePopup.cshtml", model);
        }
        
        [HttpPost]
        public IActionResult AddRateByWeightAndSizePopup(ShippingByWeightAndSizeModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageShippingSettings))
                return AccessDeniedView();
            
            _shippingByWeightAndSizeService.InsertShippingByWeightRecord(new ShippingByWeightAndSizeRecord
            {
                StoreId = model.StoreId,
                WarehouseId = model.WarehouseId,
                CountryId = model.CountryId,
                StateProvinceId = model.StateProvinceId,
                Zip = model.Zip == "*" ? null : model.Zip,
                ShippingMethodId = model.ShippingMethodId,
                WeightFrom = model.WeightFrom,
                WeightTo = model.WeightTo,
                OrderSubtotalFrom = model.OrderSubtotalFrom,
                OrderSubtotalTo = model.OrderSubtotalTo,
                AdditionalFixedCost = model.AdditionalFixedCost,
                RatePerWeightUnit = model.RatePerWeightUnit,
                PercentageRateOfSubtotal = model.PercentageRateOfSubtotal,
                LowerWeightLimit = model.LowerWeightLimit,
                TransitDays = model.TransitDays
            });

            ViewBag.RefreshPage = true;

            return View("~/Plugins/Shipping.ByWeightAndSize/Views/AddRateByWeightAndSizePopup.cshtml", model);
        }
        
        public IActionResult EditRateByWeightAndSizePopup(int id)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageShippingSettings))
                return AccessDeniedView();

            var sbw = _shippingByWeightAndSizeService.GetById(id);
            if (sbw == null)
                //no record found with the specified id
                return RedirectToAction("Configure");

            var model = new ShippingByWeightAndSizeModel
            {
                Id = sbw.Id,
                StoreId = sbw.StoreId,
                WarehouseId = sbw.WarehouseId,
                CountryId = sbw.CountryId,
                StateProvinceId = sbw.StateProvinceId,
                Zip = sbw.Zip,
                ShippingMethodId = sbw.ShippingMethodId,
                WeightFrom = sbw.WeightFrom,
                WeightTo = sbw.WeightTo,
                OrderSubtotalFrom = sbw.OrderSubtotalFrom,
                OrderSubtotalTo = sbw.OrderSubtotalTo,
                AdditionalFixedCost = sbw.AdditionalFixedCost,
                PercentageRateOfSubtotal = sbw.PercentageRateOfSubtotal,
                RatePerWeightUnit = sbw.RatePerWeightUnit,
                LowerWeightLimit = sbw.LowerWeightLimit,
                PrimaryStoreCurrencyCode = _currencyService.GetCurrencyById(_currencySettings.PrimaryStoreCurrencyId)?.CurrencyCode,
                BaseWeightIn = _measureService.GetMeasureWeightById(_measureSettings.BaseWeightId)?.Name,
                TransitDays = sbw.TransitDays
            };

            var shippingMethods = _shippingService.GetAllShippingMethods();
            if (!shippingMethods.Any())
                return Content("No shipping methods can be loaded");

            var selectedStore = _storeService.GetStoreById(sbw.StoreId);
            var selectedWarehouse = _shippingService.GetWarehouseById(sbw.WarehouseId);
            var selectedShippingMethod = _shippingService.GetShippingMethodById(sbw.ShippingMethodId);
            var selectedCountry = _countryService.GetCountryById(sbw.CountryId);
            var selectedState = _stateProvinceService.GetStateProvinceById(sbw.StateProvinceId);
            //stores
            model.AvailableStores.Add(new SelectListItem { Text = "*", Value = "0" });
            foreach (var store in _storeService.GetAllStores())
                model.AvailableStores.Add(new SelectListItem { Text = store.Name, Value = store.Id.ToString(), Selected = (selectedStore != null && store.Id == selectedStore.Id) });
            //warehouses
            model.AvailableWarehouses.Add(new SelectListItem { Text = "*", Value = "0" });
            foreach (var warehouse in _shippingService.GetAllWarehouses())
                model.AvailableWarehouses.Add(new SelectListItem { Text = warehouse.Name, Value = warehouse.Id.ToString(), Selected = (selectedWarehouse != null && warehouse.Id == selectedWarehouse.Id) });
            //shipping methods
            foreach (var sm in shippingMethods)
                model.AvailableShippingMethods.Add(new SelectListItem { Text = sm.Name, Value = sm.Id.ToString(), Selected = (selectedShippingMethod != null && sm.Id == selectedShippingMethod.Id) });
            //countries
            model.AvailableCountries.Add(new SelectListItem { Text = "*", Value = "0" });
            var countries = _countryService.GetAllCountries(showHidden: true);
            foreach (var c in countries)
                model.AvailableCountries.Add(new SelectListItem { Text = c.Name, Value = c.Id.ToString(), Selected = (selectedCountry != null && c.Id == selectedCountry.Id) });
            //states
            var states = selectedCountry != null ? _stateProvinceService.GetStateProvincesByCountryId(selectedCountry.Id, showHidden: true).ToList() : new List<StateProvince>();
            model.AvailableStates.Add(new SelectListItem { Text = "*", Value = "0" });
            foreach (var s in states)
                model.AvailableStates.Add(new SelectListItem { Text = s.Name, Value = s.Id.ToString(), Selected = (selectedState != null && s.Id == selectedState.Id) });

            return View("~/Plugins/Shipping.ByWeightAndSize/Views/EditRateByWeightAndSizePopup.cshtml", model);
        }

        [HttpPost]
        public IActionResult EditRateByWeightAndSizePopup(ShippingByWeightAndSizeModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageShippingSettings))
                return AccessDeniedView();

            var sbw = _shippingByWeightAndSizeService.GetById(model.Id);
            if (sbw == null)
                //no record found with the specified id
                return RedirectToAction("Configure");

            sbw.StoreId = model.StoreId;
            sbw.WarehouseId = model.WarehouseId;
            sbw.CountryId = model.CountryId;
            sbw.StateProvinceId = model.StateProvinceId;
            sbw.Zip = model.Zip == "*" ? null : model.Zip;
            sbw.ShippingMethodId = model.ShippingMethodId;
            sbw.WeightFrom = model.WeightFrom;
            sbw.WeightTo = model.WeightTo;
            sbw.OrderSubtotalFrom = model.OrderSubtotalFrom;
            sbw.OrderSubtotalTo = model.OrderSubtotalTo;
            sbw.AdditionalFixedCost = model.AdditionalFixedCost;
            sbw.RatePerWeightUnit = model.RatePerWeightUnit;
            sbw.PercentageRateOfSubtotal = model.PercentageRateOfSubtotal;
            sbw.LowerWeightLimit = model.LowerWeightLimit;
            sbw.TransitDays = model.TransitDays;

            _shippingByWeightAndSizeService.UpdateShippingByWeightRecord(sbw);

            ViewBag.RefreshPage = true;

            return View("~/Plugins/Shipping.ByWeightAndSize/Views/EditRateByWeightAndSizePopup.cshtml", model);
        }

        [HttpPost]
        public IActionResult DeleteRateByWeightAndSize(int id)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageShippingSettings))
                return Content("Access denied");

            var sbw = _shippingByWeightAndSizeService.GetById(id);
            if (sbw != null)
                _shippingByWeightAndSizeService.DeleteShippingByWeightRecord(sbw);

            return new NullJsonResult();
        }

        #endregion

        #endregion
    }
}