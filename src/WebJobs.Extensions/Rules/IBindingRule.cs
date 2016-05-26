// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.Rules
{
    internal interface IBindingRule<TAttribute>
    {
        bool CanBind(TAttribute attribute, Type parameterType);
        Task<object> OnFunctionExecutingAsync(TAttribute attribute, Type parameterType, IDictionary<string, object> invocationState);
        Task OnFunctionExecutedAsync(TAttribute attribute, Type parameterType, object item, IDictionary<string, object> invocationState);
    }
}
