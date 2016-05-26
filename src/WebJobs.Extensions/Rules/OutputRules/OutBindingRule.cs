// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Rules;

internal class OutBindingRule<TAttribute, TMessage> : IBindingRule<TAttribute> where TAttribute : Attribute
{
    private OutputBindingRule<TAttribute> _outputRule;

    public OutBindingRule(OutputBindingRule<TAttribute> outputRule)
    {
        _outputRule = outputRule;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "1")]
    public bool CanBind(TAttribute attribute, Type parameterType)
    {
        // never called.
        return true;
    }

    public async Task OnFunctionExecutedAsync(TAttribute attribute, Type parameterType, object item, IDictionary<string, object> invocationState)
    {
        if (item == null)
        {
            // Nothing set
            return;
        }

        TMessage message = (TMessage)item;
        Type messageType = OutputBindingRule<TAttribute>.GetAsyncCollectorCoreType(parameterType);
        IAsyncCollector<TMessage> collector = _outputRule.GetAsyncCollector(attribute, messageType) as IAsyncCollector<TMessage>;

        await collector.AddAsync(message);
        await collector.FlushAsync();
    }

    public Task<object> OnFunctionExecutingAsync(TAttribute attribute, Type parameterType, IDictionary<string, object> invocationState)
    {
        return Task.FromResult<object>(null);
    }
}
