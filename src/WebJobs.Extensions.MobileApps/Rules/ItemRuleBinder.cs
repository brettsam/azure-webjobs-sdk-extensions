// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.Rules;
using Microsoft.WindowsAzure.MobileServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.WebJobs.Extensions.MobileApps.Rules
{
    internal class ItemRuleBinder<TItem> : IBindingRuleBinder<MobileTableAttribute>
    {
        private MobileAppsConfiguration _config;
        private JObject _originalItem;

        public ItemRuleBinder(MobileAppsConfiguration config)
        {
            _config = config;
        }

        public async Task<object> OnFunctionExecutingAsync(MobileTableAttribute attribute, Type parameterType)
        {
            object item = null;
            MobileTableContext context = _config.CreateContext(attribute);

            if (typeof(TItem) == typeof(JObject))
            {
                IMobileServiceTable table = context.Client.GetTable(context.ResolvedAttribute.TableName);
                await IgnoreNotFoundExceptionAsync(async () =>
                {
                    item = await table.LookupAsync(context.ResolvedAttribute.Id);
                    _originalItem = CloneItem(item);
                });
            }
            else
            {
                // If TableName is specified, add it to the internal table cache. Now items of this type
                // will operate on the specified TableName.
                if (!string.IsNullOrEmpty(context.ResolvedAttribute.TableName))
                {
                    context.Client.AddToTableNameCache(typeof(TItem), context.ResolvedAttribute.TableName);
                }

                IMobileServiceTable<TItem> table = context.Client.GetTable<TItem>();
                await IgnoreNotFoundExceptionAsync(async () =>
                {
                    item = await table.LookupAsync(context.ResolvedAttribute.Id);
                    _originalItem = CloneItem(item);
                });
            }

            return item;
        }

        public async Task OnFunctionExecutedAsync(MobileTableAttribute attribute, Type parameterType, object item)
        {
            JObject currentValue = null;
            bool isJObject = item.GetType() == typeof(JObject);

            if (isJObject)
            {
                currentValue = item as JObject;
            }
            else
            {
                currentValue = JObject.FromObject(item);
            }

            MobileTableContext context = _config.CreateContext(attribute);

            if (HasChanged(_originalItem, currentValue))
            {
                // make sure it's not the Id that has changed
                if (!string.Equals(GetId(_originalItem), GetId(currentValue), StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("Cannot update the 'Id' property.");
                }

                if (isJObject)
                {
                    IMobileServiceTable table = context.Client.GetTable(context.ResolvedAttribute.TableName);
                    await table.UpdateAsync((JObject)item);
                }
                else
                {
                    // If TableName is specified, add it to the internal table cache. Now items of this type
                    // will operate on the specified TableName.
                    if (!string.IsNullOrEmpty(context.ResolvedAttribute.TableName))
                    {
                        context.Client.AddToTableNameCache(item.GetType(), context.ResolvedAttribute.TableName);
                    }
                    IMobileServiceTable<TItem> table = context.Client.GetTable<TItem>();
                    await table.UpdateAsync((TItem)item);
                }
            }
        }

        internal static string GetId(JObject item)
        {
            JToken idToken = item.GetValue("id", StringComparison.OrdinalIgnoreCase);
            return idToken.ToString();
        }

        internal static bool HasChanged(JToken original, JToken current)
        {
            return !JToken.DeepEquals(original, current);
        }

        internal static JObject CloneItem(object item)
        {
            string serializedItem = JsonConvert.SerializeObject(item);
            return JObject.Parse(serializedItem);
        }

        private static async Task IgnoreNotFoundExceptionAsync(Func<Task> action)
        {
            try
            {
                await action();
            }
            catch (AggregateException ex)
            {
                foreach (Exception innerEx in ex.InnerExceptions)
                {
                    MobileServiceInvalidOperationException mobileEx =
                        innerEx as MobileServiceInvalidOperationException;
                    if (mobileEx == null ||
                        mobileEx.Response.StatusCode != System.Net.HttpStatusCode.NotFound)
                    {
                        throw innerEx;
                    }
                }
            }
        }
    }
}
