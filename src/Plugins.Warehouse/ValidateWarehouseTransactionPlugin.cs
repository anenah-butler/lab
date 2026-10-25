using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using TALXIS.DevKit.Build.Dataverse.Plugin;

namespace Plugins.Warehouse
{
    [PluginStep("Create", "lab_warehousetransaction", PluginStage.PreValidation)]
    public class ValidateWarehouseTransactionPlugin : PluginBase
    {
        public ValidateWarehouseTransactionPlugin(string unsecureConfiguration, string secureConfiguration)
            : base(typeof(ValidateWarehouseTransactionPlugin))
        {
        }

        protected override void ExecuteDataversePlugin(ILocalPluginContext localPluginContext)
        {
            if (localPluginContext == null)
            {
                throw new ArgumentNullException(nameof(localPluginContext));
            }

            var context = localPluginContext.PluginExecutionContext;
            if (!context.InputParameters.Contains("Target"))
            {
                return;
            }

            var target = context.InputParameters["Target"] as Entity;
            if (target == null || target.LogicalName != "lab_warehousetransaction")
            {
                return;
            }

            if (!target.Contains("lab_quantity") ||
                !target.Contains("lab_itemid") ||
                !target.Contains("lab_transactiontype"))
            {
                return;
            }

            var quantity = target.GetAttributeValue<int?>("lab_quantity");
            var itemReference = target.GetAttributeValue<EntityReference>("lab_itemid");
            var transactionType = target.GetAttributeValue<OptionSetValue>("lab_transactiontype");
            if (!quantity.HasValue || itemReference == null || transactionType == null ||
                transactionType.Value != 100000001)
            {
                return;
            }

            try
            {
                var service = localPluginContext.OrgSvcFactory.CreateOrganizationService(context.UserId);
                var item = service.Retrieve(
                    "lab_warehouseitem",
                    itemReference.Id,
                    new ColumnSet("lab_availablequantity"));
                var available = item == null
                    ? 0
                    : item.GetAttributeValue<int?>("lab_availablequantity") ?? 0;

                if (quantity.Value > available)
                {
                    throw new InvalidPluginExecutionException(
                        $"Not enough product in stock. Available: {available}, requested: {quantity.Value}.");
                }
            }
            catch (InvalidPluginExecutionException)
            {
                throw;
            }
            catch (Exception exception)
            {
                localPluginContext.TracingService.Trace("Plugin exception: {0}", exception);
                throw;
            }
        }
    }
}
