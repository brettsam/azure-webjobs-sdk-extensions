// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.Rules;
using Microsoft.WindowsAzure.MobileServices;

namespace Microsoft.Azure.WebJobs.Extensions.MobileApps.Rules
{
    internal class ExactTableRule : IBindingRule<MobileTableAttribute>
    {
        private MobileAppsConfiguration _config;

        public ExactTableRule(MobileAppsConfiguration config)
        {
            _config = config;
        }

        public bool CanBind(MobileTableAttribute attribute, Type parameterType)
        {
            return parameterType == typeof(IMobileServiceTable);
        }

        public Task<object> OnFunctionExecutingAsync(MobileTableAttribute attribute, Type parameterType, IDictionary<string, object> invocationState)
        {
            MobileTableContext context = _config.CreateContext(attribute);
            IMobileServiceTable table = context.Client.GetTable(context.ResolvedAttribute.TableName);

            return Task.FromResult<object>(table);
        }

        public Task OnFunctionExecutedAsync(MobileTableAttribute attribute, Type parameterType, object item, IDictionary<string, object> invocationState)
        {
            return Task.FromResult(0);
        }
    }
}
