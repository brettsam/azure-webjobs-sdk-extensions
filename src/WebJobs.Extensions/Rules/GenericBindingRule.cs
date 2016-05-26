// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.Rules
{
    internal class GenericBindingRule<TAttribute> : IBindingRule<TAttribute>
    {
        private Type _ruleType;
        private object[] _constructorParams;
        private IBindingRule<TAttribute> innerRule;

        public GenericBindingRule(Type genericBindingRuleType, params object[] constructorParams)
        {
            _ruleType = genericBindingRuleType;
            _constructorParams = constructorParams;

            // TODO: Some checks that the Type is okay.
        }

        public bool CanBind(TAttribute attribute, Type parameterType)
        {
            Type genericRuleType = _ruleType.MakeGenericType(parameterType);
            innerRule = Activator.CreateInstance(genericRuleType, _constructorParams) as IBindingRule<TAttribute>;

            return innerRule.CanBind(attribute, parameterType);
        }

        public Task<object> OnFunctionExecutingAsync(TAttribute attribute, Type parameterType, IDictionary<string, object> invocationState)
        {
            return innerRule.OnFunctionExecutingAsync(attribute, parameterType, invocationState);
        }

        public Task OnFunctionExecutedAsync(TAttribute attribute, Type parameterType, object item, IDictionary<string, object> invocationState)
        {
            return innerRule.OnFunctionExecutedAsync(attribute, parameterType, item, invocationState);
        }
    }
}
