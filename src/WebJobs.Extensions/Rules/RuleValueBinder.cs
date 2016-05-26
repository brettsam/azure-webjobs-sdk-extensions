// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Bindings;

namespace Microsoft.Azure.WebJobs.Extensions.Rules
{
    internal class RuleValueBinder<TAttribute> : IValueBinder where TAttribute : Attribute
    {
        private IBindingRuleBinder<TAttribute> _ruleBinder;
        private TAttribute _resolvedAttribute;
        private Type _parameterType;
        private string _invokeString;

        public RuleValueBinder(IBindingRuleBinder<TAttribute> ruleBinder, TAttribute resolvedAttribute, Type parameterType, string invokeString)
        {
            _ruleBinder = ruleBinder;
            _resolvedAttribute = resolvedAttribute;
            _parameterType = parameterType;
            _invokeString = invokeString;
        }

        public Type Type
        {
            get
            {
                return _parameterType;
            }
        }

        public object GetValue()
        {
            return _ruleBinder.OnFunctionExecutingAsync(_resolvedAttribute, _parameterType).Result;
        }

        public Task SetValueAsync(object value, CancellationToken cancellationToken)
        {
            return _ruleBinder.OnFunctionExecutedAsync(_resolvedAttribute, _parameterType, value);
        }

        public string ToInvokeString()
        {
            return _invokeString;
        }
    }
}
