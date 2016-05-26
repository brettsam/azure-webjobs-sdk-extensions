// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.Rules
{
    internal abstract class GenericBindingRule<TAttribute> : IBindingRule<TAttribute> where TAttribute : Attribute
    {
        private Type _ruleBinderType;
        private object[] _constructorParams;

        public GenericBindingRule(Type genericBindingRuleBinderType, params object[] constructorParams)
        {
            _ruleBinderType = genericBindingRuleBinderType;
            _constructorParams = constructorParams;

            // TODO: Some checks that the Type is okay.
        }

        public abstract bool CanBind(TAttribute attribute, Type parameterType);

        public Task<IBindingRuleBinder<TAttribute>> GetRuleBinderAsync(TAttribute attribute, Type parameterType)
        {
            Type genericRuleType = _ruleBinderType.MakeGenericType(parameterType);
            IBindingRuleBinder<TAttribute> ruleBinder = Activator.CreateInstance(genericRuleType, _constructorParams) as IBindingRuleBinder<TAttribute>;

            return Task.FromResult(ruleBinder);
        }
    }
}
