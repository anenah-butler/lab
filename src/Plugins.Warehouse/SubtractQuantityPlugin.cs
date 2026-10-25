using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using TALXIS.DevKit.Build.Dataverse.Plugin;

namespace Plugins.Warehouse
{
    [PluginStep("Create", "lab_warehousetransaction", PluginStage.PostOperation)]
    public class SubtractQuantityPlugin : PluginBase
    {
        public SubtractQuantityPlugin(string unsecureConfiguration, string secureConfiguration)
            : base(typeof(SubtractQuantityPlugin))
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
            if (!quantity.HasValue || itemReference == null || transactionType == null)
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

                int updatedQuantity;
                if (transactionType.Value == 100000000)
                {
                    updatedQuantity = available + quantity.Value;
                }
                else if (transactionType.Value == 100000001)
                {
                    updatedQuantity = available - quantity.Value;
                }
                else
                {
                    return;
                }

                var itemUpdate = new Entity("lab_warehouseitem", itemReference.Id);
                itemUpdate["lab_availablequantity"] = updatedQuantity;
                service.Update(itemUpdate);
            }
            catch (Exception exception)
            {
                localPluginContext.TracingService.Trace("Plugin exception: {0}", exception);
                throw;
            }
        }
    }
}
