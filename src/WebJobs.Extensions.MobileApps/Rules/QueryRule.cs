// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.MobileServices;

namespace Microsoft.Azure.WebJobs.Extensions.MobileApps.Rules
{
    internal class QueryRule : TableRule
    {
        public QueryRule(MobileAppsConfiguration config) : base(config)
        {
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "1")]
        protected override bool CanBind(MobileTableAttribute attribute, Type parameterType)
        {
            if (parameterType.IsGenericType &&
                parameterType.GetGenericTypeDefinition() == typeof(IMobileServiceTableQuery<>))
            {
                Type tableType = parameterType.GetGenericArguments().Single();
                MobileAppUtility.ThrowIfInvalidItemType(attribute, tableType);

                return true;
            }

            return false;
        }

        public override async Task<object> OnFunctionExecutingAsync(MobileTableAttribute attribute, Type parameterType)
        {
            object table = await base.OnFunctionExecutingAsync(attribute, parameterType);
            MethodInfo createQueryMethod = table.GetType().GetMethod("CreateQuery");

            return createQueryMethod.Invoke(table, null);
        }
    }
}
