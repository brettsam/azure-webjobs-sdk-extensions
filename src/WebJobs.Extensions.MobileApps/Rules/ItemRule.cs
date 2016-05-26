// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.Rules;

namespace Microsoft.Azure.WebJobs.Extensions.MobileApps.Rules
{
    internal class ItemRule : IBindingRule<MobileTableAttribute>
    {
        private MobileAppsConfiguration _config;

        public ItemRule(MobileAppsConfiguration config)
        {
            _config = config;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "1")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0")]
        public Task<IBindingRuleBinder<MobileTableAttribute>> GetRuleBinderAsync(MobileTableAttribute attribute, Type parameterType)
        {
            // this is the final rule, so if the type is okay, return true
            MobileAppUtility.ThrowIfInvalidItemType(attribute, parameterType);

            if (string.IsNullOrEmpty(attribute.Id))
            {
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, "'Id' must be set when using a parameter of type '{0}'.", parameterType.Name));
            }

            Type genericRule = typeof(ItemRuleBinder<>).MakeGenericType(parameterType);
            IBindingRuleBinder<MobileTableAttribute> binder = Activator.CreateInstance(genericRule, _config) as IBindingRuleBinder<MobileTableAttribute>;
            return Task.FromResult(binder);
        }
    }
}
