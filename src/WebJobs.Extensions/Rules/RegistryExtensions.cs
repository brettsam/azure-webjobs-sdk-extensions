// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Host.Bindings;

namespace Microsoft.Azure.WebJobs.Extensions.Rules
{
    /// <summary>
    /// </summary>
    public static class RegistryExtensions
    {
        /// <summary>
        /// </summary>
        /// <typeparam name="TAttribute"></typeparam>
        /// <param name="registry"></param>
        /// <param name="config"></param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter")]
        public static void RegisterBindingRules<TAttribute>(this IExtensionRegistry registry, JobHostConfiguration config) where TAttribute : Attribute
        {
            List<Type> ruleTypes = Assembly.GetCallingAssembly().GetTypes()
                .Where(t => t.GetInterfaces().Contains(typeof(IBindingRule<TAttribute>))).ToList();

            Type lastRuleType = ruleTypes.Where(t => t.GetCustomAttribute<LastBindingRuleAttribute>() != null).SingleOrDefault();

            if (lastRuleType != null)
            {
                ruleTypes.Remove(lastRuleType);
                ruleTypes.Add(lastRuleType);
            }

            // any output rules need to be moved to the front
            IEnumerable<Type> outputRuleTypes = ruleTypes.Where(t => typeof(OutputBindingRule<TAttribute>).IsAssignableFrom(t));
            ruleTypes.RemoveAll(r => outputRuleTypes.Contains(r));
            ruleTypes.InsertRange(0, outputRuleTypes);

            var providers = ruleTypes.Select(r => new RuleBindingProvider<TAttribute>(r, config));
            var all = new GenericCompositeBindingProvider<TAttribute>(providers);
            registry.RegisterExtension<IBindingProvider>(all);
        }
    }
}
