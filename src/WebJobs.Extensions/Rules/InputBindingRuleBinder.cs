// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.Rules
{
    internal class InputBindingRuleBinder<TAttribute> : IBindingRuleBinder<TAttribute> where TAttribute : Attribute
    {
        private InputBindingRule<TAttribute> _rule;

        public InputBindingRuleBinder(InputBindingRule<TAttribute> rule)
        {
            _rule = rule;
        }

        public Task OnFunctionExecutedAsync(TAttribute attribute, Type parameterType, object item)
        {
            return Task.FromResult(0);
        }

        public Task<object> OnFunctionExecutingAsync(TAttribute attribute, Type parameterType)
        {
            return _rule.OnFunctionExecutingAsync(attribute, parameterType);
        }
    }
}
