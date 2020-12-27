using Nop.Core;
using Nop.Plugin.Shipping.ByWeightAndSize.Domain;

namespace Nop.Plugin.Shipping.ByWeightAndSize.Services
{
    /// <summary>
    /// Represents service shipping by weight service
    /// </summary>
    public partial interface IShippingByWeightAndSizeService
    {
        /// <summary>
        /// Get all shipping by weight records
        /// </summary>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>List of the shipping by weight record</returns>
        IPagedList<ShippingByWeightAndSizeRecord> GetAll(int pageIndex = 0, int pageSize = int.MaxValue);

        /// <summary>
        /// Get a shipping by weight record by passed parameters
        /// </summary>
        /// <param name="shippingMethodId">Shipping method identifier</param>
        /// <param name="storeId">Store identifier</param>
        /// <param name="warehouseId">Warehouse identifier</param>
        /// <param name="countryId">Country identifier</param>
        /// <param name="stateProvinceId">State identifier</param>
        /// <param name="zip">Zip postal code</param>
        /// <param name="weight">Weight</param>
        /// <param name="orderSubtotal">Order subtotal</param>
        /// <returns>Shipping by weight record</returns>
        ShippingByWeightAndSizeRecord FindRecords(int shippingMethodId, int storeId, int warehouseId,  
            int countryId, int stateProvinceId, string zip, decimal weight, decimal orderSubtotal);

        /// <summary>
        /// Filter Shipping Weight Records
        /// </summary>
        /// <param name="shippingMethodId">Shipping method identifier</param>
        /// <param name="storeId">Store identifier</param>
        /// <param name="warehouseId">Warehouse identifier</param>
        /// <param name="countryId">Country identifier</param>
        /// <param name="stateProvinceId">State identifier</param>
        /// <param name="zip">Zip postal code</param>
        /// <param name="weight">Weight</param>
        /// <param name="orderSubtotal">Order subtotal</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>List of the shipping by weight record</returns>
        IPagedList<ShippingByWeightAndSizeRecord> FindRecords(int shippingMethodId, int storeId, int warehouseId,
            int countryId, int stateProvinceId, string zip, decimal? weight, decimal? orderSubtotal, int pageIndex, int pageSize);

        /// <summary>
        /// Get a shipping by weight record by identifier
        /// </summary>
        /// <param name="shippingByWeightAndSizeRecordId">Record identifier</param>
        /// <returns>Shipping by weight record</returns>
        ShippingByWeightAndSizeRecord GetById(int shippingByWeightAndSizeRecordId);

        /// <summary>
        /// Insert the shipping by weight record
        /// </summary>
        /// <param name="shippingByWeightAndSizeRecord">Shipping by weight record</param>
        void InsertShippingByWeightRecord(ShippingByWeightAndSizeRecord shippingByWeightAndSizeRecord);

        /// <summary>
        /// Update the shipping by weight record
        /// </summary>
        /// <param name="shippingByWeightAndSizeRecord">Shipping by weight record</param>
        void UpdateShippingByWeightRecord(ShippingByWeightAndSizeRecord shippingByWeightAndSizeRecord);

        /// <summary>
        /// Delete the shipping by weight record
        /// </summary>
        /// <param name="shippingByWeightAndSizeRecord">Shipping by weight record</param>
        void DeleteShippingByWeightRecord(ShippingByWeightAndSizeRecord shippingByWeightAndSizeRecord);
    }
}
