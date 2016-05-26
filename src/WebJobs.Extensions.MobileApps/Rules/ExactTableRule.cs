// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.Rules;
using Microsoft.WindowsAzure.MobileServices;

namespace Microsoft.Azure.WebJobs.Extensions.MobileApps.Rules
{
    internal class ExactTableRule : BindingRule<MobileTableAttribute>
    {
        private MobileAppsConfiguration _config;

        public ExactTableRule(MobileAppsConfiguration config)
        {
            _config = config;
        }

        public override bool CanBind(MobileTableAttribute attribute, Type parameterType)
        {
            return parameterType == typeof(IMobileServiceTable);
        }

        public override Task<object> OnFunctionExecutingAsync(MobileTableAttribute attribute, Type parameterType, IDictionary<string, object> invocationState)
        {
            MobileTableContext context = _config.CreateContext(attribute);
            IMobileServiceTable table = context.Client.GetTable(context.ResolvedAttribute.TableName);

            return Task.FromResult<object>(table);
        }
    }
}
