// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Bindings;

namespace Microsoft.Azure.WebJobs.Extensions.Rules
{
    internal class RuleValueBinder<TAttribute> : IValueBinder where TAttribute : Attribute
    {
        private IBindingRule<TAttribute> _rule;
        private TAttribute _resolvedAttribute;
        private Type _parameterType;
        private string _invokeString;
        private IDictionary<string, object> _invocationState;

        public RuleValueBinder(IBindingRule<TAttribute> rule, TAttribute resolvedAttribute, Type parameterType, string invokeString)
        {
            _rule = rule;
            _resolvedAttribute = resolvedAttribute;
            _parameterType = parameterType;
            _invokeString = invokeString;
            _invocationState = new Dictionary<string, object>();
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
            return _rule.OnFunctionExecutingAsync(_resolvedAttribute, _parameterType, _invocationState).Result;
        }

        public Task SetValueAsync(object value, CancellationToken cancellationToken)
        {
            return _rule.OnFunctionExecutedAsync(_resolvedAttribute, _parameterType, value, _invocationState);
        }

        public string ToInvokeString()
        {
            return _invokeString;
        }
    }
}
