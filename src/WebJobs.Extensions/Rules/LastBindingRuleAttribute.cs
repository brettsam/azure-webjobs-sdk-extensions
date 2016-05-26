// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace Microsoft.Azure.WebJobs.Extensions.Rules
{
    /// <summary>
    /// Apply to a BindingRule to indicate this is the last one to be run. Only one is allowed per Attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class LastBindingRuleAttribute : Attribute
    {
    }
}
