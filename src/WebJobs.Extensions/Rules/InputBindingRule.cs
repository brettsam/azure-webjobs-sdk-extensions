// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.Rules
{
    internal abstract class InputBindingRule<TAttribute> : IBindingRule<TAttribute> where TAttribute : Attribute
    {
        protected abstract bool CanBind(TAttribute attribute, Type parameterType);

        public abstract Task<object> OnFunctionExecutingAsync(TAttribute attribute, Type parameterType);

        Task<IBindingRuleBinder<TAttribute>> IBindingRule<TAttribute>.GetRuleBinderAsync(TAttribute attribute, Type parameterType)
        {
            IBindingRuleBinder<TAttribute> binder = null;

            if (!CanBind(attribute, parameterType))
            {
                return Task.FromResult(binder);
            }

            binder = new InputBindingRuleBinder<TAttribute>(this);
            return Task.FromResult(binder);
        }
    }
}
