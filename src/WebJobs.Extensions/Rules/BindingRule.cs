// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.Rules
{
    internal abstract class BindingRule<TAttribute> : IBindingRule<TAttribute>
    {
        public abstract bool CanBind(TAttribute attribute, Type parameterType);

        public abstract Task<object> OnFunctionExecutingAsync(TAttribute attribute, Type parameterType, IDictionary<string, object> invocationState);

        public virtual Task OnFunctionExecutedAsync(TAttribute attribute, Type parameterType, object item, IDictionary<string, object> invocationState)
        {
            return Task.FromResult(0);
        }
    }
}
