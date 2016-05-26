// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.Rules;
using Microsoft.WindowsAzure.MobileServices;

namespace Microsoft.Azure.WebJobs.Extensions.MobileApps.Rules
{
    internal class TableRule : InputBindingRule<MobileTableAttribute>
    {
        private MobileAppsConfiguration _config;

        public TableRule(MobileAppsConfiguration config)
        {
            _config = config;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "1")]
        protected override bool CanBind(MobileTableAttribute attribute, Type parameterType)
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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "1")]
        public override Task<object> OnFunctionExecutingAsync(MobileTableAttribute attribute, Type parameterType)
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

            MethodInfo getTableMethod = GetGenericTableMethod();
            MethodInfo getTableGenericMethod = getTableMethod.MakeGenericMethod(tableType);

            return Task.FromResult(getTableGenericMethod.Invoke(context.Client, null));
        }

        private static MethodInfo GetGenericTableMethod()
        {
            return typeof(IMobileServiceClient).GetMethods()
                .Where(m => m.IsGenericMethod && m.Name == "GetTable").Single();
        }
    }
}
