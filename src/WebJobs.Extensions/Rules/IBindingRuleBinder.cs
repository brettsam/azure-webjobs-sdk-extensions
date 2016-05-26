// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.Rules
{
    internal interface IBindingRuleBinder<TAttribute> where TAttribute : Attribute
    {
        Task<object> OnFunctionExecutingAsync(TAttribute attribute, Type parameterType);
        Task OnFunctionExecutedAsync(TAttribute attribute, Type parameterType, object item);
    }
}
