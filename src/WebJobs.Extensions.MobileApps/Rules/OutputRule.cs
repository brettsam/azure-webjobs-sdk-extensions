// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Azure.WebJobs.Extensions.Rules;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.WebJobs.Extensions.MobileApps.Rules
{
    internal class OutputRule : OutputBindingRule<MobileTableAttribute>
    {
        private MobileAppsConfiguration _config;

        public OutputRule(MobileAppsConfiguration config)
        {
            _config = config;
        }

        public override bool CanBindToMessage(MobileTableAttribute attribute, Type messageType)
        {
            return messageType == typeof(JObject) || messageType == typeof(object);
        }

        public override object GetAsyncCollector(MobileTableAttribute attribute, Type messageType)
        {
            MobileTableContext context = _config.CreateContext(attribute);
            Type collectorType = typeof(MobileTableAsyncCollector<>).MakeGenericType(messageType);
            return Activator.CreateInstance(collectorType, context);
        }
    }
}
