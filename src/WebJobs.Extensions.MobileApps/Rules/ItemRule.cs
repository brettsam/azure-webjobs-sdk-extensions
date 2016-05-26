// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.Rules;
using Microsoft.WindowsAzure.MobileServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.WebJobs.Extensions.MobileApps.Rules
{
    [LastBindingRule]
    internal class ItemRule<TItem> : IBindingRule<MobileTableAttribute>
    {
        private MobileAppsConfiguration _config;
        private const string OriginalItemKey = "OriginalItem";

        public ItemRule(MobileAppsConfiguration config)
        {
            _config = config;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "1")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0")]
        public bool CanBind(MobileTableAttribute attribute, Type parameterType)
        {
            // this is the final rule, so if the type is okay, return true
            MobileAppUtility.ThrowIfInvalidItemType(attribute, parameterType);

            if (string.IsNullOrEmpty(attribute.Id))
            {
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, "'Id' must be set when using a parameter of type '{0}'.", parameterType.Name));
            }

            return true;
        }

        public async Task OnFunctionExecutedAsync(MobileTableAttribute attribute, Type parameterType, object item, IDictionary<string, object> invocationState)
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
            JObject originalItem = invocationState[OriginalItemKey] as JObject;

            if (HasChanged(originalItem, currentValue))
            {
                // make sure it's not the Id that has changed
                if (!string.Equals(GetId(originalItem), GetId(currentValue), StringComparison.Ordinal))
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

        public async Task<object> OnFunctionExecutingAsync(MobileTableAttribute attribute, Type parameterType, IDictionary<string, object> invocationState)
        {
            object item = null;
            MobileTableContext context = _config.CreateContext(attribute);

            if (typeof(TItem) == typeof(JObject))
            {
                IMobileServiceTable table = context.Client.GetTable(context.ResolvedAttribute.TableName);
                await IgnoreNotFoundExceptionAsync(async () =>
                {
                    item = await table.LookupAsync(context.ResolvedAttribute.Id);
                    invocationState[OriginalItemKey] = CloneItem(item);
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
                    invocationState[OriginalItemKey] = CloneItem(item);
                });
            }

            return item;
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
