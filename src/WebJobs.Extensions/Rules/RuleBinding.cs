// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Protocols;

namespace Microsoft.Azure.WebJobs.Extensions.Rules
{
    internal class RuleBinding<TAttribute> : IBinding where TAttribute : Attribute
    {
        private IBindingRule<TAttribute> _rule;
        private AttributeCloner<TAttribute> _cloner;
        private Type _parameterType;

        public RuleBinding(IBindingRule<TAttribute> rule, AttributeCloner<TAttribute> cloner, Type parameterType)
        {
            _rule = rule;
            _cloner = cloner;
            _parameterType = parameterType;
        }

        public bool FromAttribute
        {
            get
            {
                return true;
            }
        }

        public async Task<IValueProvider> BindAsync(BindingContext context)
        {
            var attrResolved = await _cloner.ResolveFromBindingData(context);
            return new RuleValueBinder<TAttribute>(_rule, attrResolved, _parameterType, _cloner.GetInvokeString(attrResolved));
        }

        public async Task<IValueProvider> BindAsync(object value, ValueBindingContext context)
        {
            var str = value as string;
            if (str != null)
            {
                // Called when we invoke from dashboard. 
                // str --> attribute --> obj 
                var resolvedAttr = await _cloner.ResolveFromInvokeString(str);
                return new RuleValueBinder<TAttribute>(_rule, resolvedAttr, _parameterType, _cloner.GetInvokeString(resolvedAttr));
            }
            else
            {
                // Passed a direct object, such as JobHost.Call 
                throw new NotImplementedException();
            }
        }

        public ParameterDescriptor ToParameterDescriptor()
        {
            return new ParameterDescriptor();
        }
    }
}
