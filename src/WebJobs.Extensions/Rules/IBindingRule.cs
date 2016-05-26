﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.Rules
{
    internal interface IBindingRule<TAttribute> where TAttribute : Attribute
    {
        Task<IBindingRuleBinder<TAttribute>> GetRuleBinderAsync(TAttribute attribute, Type parameterType);
    }
}
