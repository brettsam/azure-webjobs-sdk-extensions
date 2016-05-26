// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Bindings;

namespace Microsoft.Azure.WebJobs.Extensions.Rules
{
    internal class RuleBindingProvider<TAttribute> : IBindingProvider where TAttribute : Attribute
    {
        private Type _ruleType;
        private JobHostConfiguration _config;

        public RuleBindingProvider(Type ruleType, JobHostConfiguration config)
        {
            _ruleType = ruleType;
            _config = config;
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

            Type ruleType = _ruleType;

            // use the parameter to close the generic type
            if (_ruleType.ContainsGenericParameters)
            {
                ruleType = _ruleType.MakeGenericType(parameter.ParameterType);
            }

            ParameterInfo[] parameters = ruleType.GetConstructors().Single().GetParameters();

            // try to fulfill the parameters from services
            IEnumerable<object> parameterValues = parameters.Select(p => _config.GetService(p.ParameterType));

            IBindingRule<TAttribute> rule = Activator.CreateInstance(ruleType, parameterValues.ToArray()) as IBindingRule<TAttribute>;

            // resolve static %% variables that we know now
            var cloner = new AttributeCloner<TAttribute>(attributeSource, _config.GetService<INameResolver>());
            var attrNameResolved = cloner.GetNameResolvedAttribute();

            // This may do validation and throw too. 
            bool canBind = rule.CanBind(attrNameResolved, parameter.ParameterType);

            if (!canBind)
            {
                return Task.FromResult<IBinding>(null);
            }

            IBinding binding = new RuleBinding<TAttribute>(rule, cloner, parameter.ParameterType);
            return Task.FromResult(binding);
        }
    }
}
