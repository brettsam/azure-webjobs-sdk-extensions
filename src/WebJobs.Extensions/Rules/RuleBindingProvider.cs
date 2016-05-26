// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Bindings;

namespace Microsoft.Azure.WebJobs.Extensions.Rules
{
    internal class RuleBindingProvider<TAttribute> : IBindingProvider where TAttribute : Attribute
    {
        private IBindingRule<TAttribute> _rule;
        private INameResolver _nameResolver;

        public RuleBindingProvider(IBindingRule<TAttribute> rule, INameResolver nameResolver)
        {
            _rule = rule;
            _nameResolver = nameResolver;
        }

        public Task<IBinding> TryCreateAsync(BindingProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            ParameterInfo parameter = context.Parameter;

            // If the attribute is not TAttribute, this provider will not work
            TAttribute attributeSource = parameter.GetCustomAttribute<TAttribute>(inherit: false);
            if (attributeSource == null)
            {
                return Task.FromResult<IBinding>(null);
            }

            // resolve static %% variables that we know now
            var cloner = new AttributeCloner<TAttribute>(attributeSource, _nameResolver);
            var attrNameResolved = cloner.GetNameResolvedAttribute();

            // This may do validation and throw too. 
            bool canBind = _rule.CanBind(attrNameResolved, parameter.ParameterType);

            if (!canBind)
            {
                return Task.FromResult<IBinding>(null);
            }

            IBinding binding = new RuleBinding<TAttribute>(_rule, cloner, parameter.ParameterType);
            return Task.FromResult(binding);
        }
    }
}
