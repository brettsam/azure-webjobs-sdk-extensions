// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.Rules;
using Microsoft.WindowsAzure.MobileServices;

namespace Microsoft.Azure.WebJobs.Extensions.MobileApps.Rules
{
    internal class TableRule : IBindingRule<MobileTableAttribute>
    {
        private MobileAppsConfiguration _config;

        public TableRule(MobileAppsConfiguration config)
        {
            _config = config;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "1")]
        public virtual bool CanBind(MobileTableAttribute attribute, Type parameterType)
        {
            // We will check if the argument is valid in a Validator
            if (parameterType.IsGenericType &&
                parameterType.GetGenericTypeDefinition() == typeof(IMobileServiceTable<>))
            {
                Type tableType = parameterType.GetGenericArguments().Single();
                MobileAppUtility.ThrowIfInvalidItemType(attribute, tableType);

                return true;
            }

            return false;
        }

        public Task OnFunctionExecutedAsync(MobileTableAttribute attribute, Type parameterType, object item, IDictionary<string, object> invocationState)
        {
            return Task.FromResult(0);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "1")]
        public virtual Task<object> OnFunctionExecutingAsync(MobileTableAttribute attribute, Type parameterType, IDictionary<string, object> invocationState)
        {
            MobileTableContext context = _config.CreateContext(attribute);

            // Assume that the Filter has already run.
            Type tableType = parameterType.GetGenericArguments().Single();

            // If TableName is specified, add it to the internal table cache. Now items of this type
            // will operate on the specified TableName.
            if (!string.IsNullOrEmpty(context.ResolvedAttribute.TableName))
            {
                context.Client.AddToTableNameCache(tableType, context.ResolvedAttribute.TableName);
            }

            MethodInfo getTableMethod = typeof(IMobileServiceClient).GetMethods()
                .Where(m => m.IsGenericMethod && m.Name == "GetTable").Single();
            MethodInfo getTableGenericMethod = getTableMethod.MakeGenericMethod(tableType);

            return Task.FromResult(getTableGenericMethod.Invoke(context.Client, null));
        }
    }
}
