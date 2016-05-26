// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Host.Bindings;

namespace Microsoft.Azure.WebJobs.Extensions.Rules
{
    internal static class RegistryExtensions
    {
        public static void RegisterBindingRules<TAttribute>(this IExtensionRegistry registry, INameResolver resolver, params IBindingRule<TAttribute>[] rules) where TAttribute : Attribute
        {
            var providers = rules.Select(r => new RuleBindingProvider<TAttribute>(r, resolver));
            var all = new GenericCompositeBindingProvider<TAttribute>(providers);
            registry.RegisterExtension<IBindingProvider>(all);
        }
    }
}
