// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.Rules
{
    /// <summary>    
    /// </summary>
    /// <typeparam name="TAttribute"></typeparam>
    public interface IBindingRule<TAttribute> where TAttribute : Attribute
    {
        /// <summary>
        /// Called to set up 'parameter routing'.
        /// </summary>
        /// <param name="attribute">The fully resolved attribute for this function parameter.</param>
        /// <param name="parameterType">The parameter type.</param>
        /// <returns> True if the attribute/type pair is supported by this Rule. False if not. If false, the host will check the next rule.
        /// Throw an exception to abort indexing. </returns>
        bool CanBind(TAttribute attribute, Type parameterType);

        /// <summary>
        /// Called before the user's function has been called. Provides an opportunity to create the object for the function parameter.
        /// </summary>
        /// <param name="attribute">The fully resolved attribute for this function parameter.</param>
        /// <param name="parameterType">The parameter type.</param>
        /// <param name="invocationState">A state bag created per-invocation. The same state bag is shared between the Executing and Executed methods.</param>
        /// <returns>The object to use for the function's parameter</returns>
        Task<object> OnFunctionExecutingAsync(TAttribute attribute, Type parameterType, IDictionary<string, object> invocationState);

        /// <summary>
        /// Called after the user's function has been called. Provides an opportunity to perform an action on the function parameter object after the function exits.
        /// </summary>
        /// <param name="attribute">The fully resolved attribute for this function parameter.</param>
        /// <param name="parameterType">The parameter type.</param>
        /// <param name="item">The object.</param>
        /// <param name="invocationState">A state bag created per-invocation. The same state bag is shared between the Executing and Executed methods.</param>
        /// <returns>The completion Task.</returns>
        Task OnFunctionExecutedAsync(TAttribute attribute, Type parameterType, object item, IDictionary<string, object> invocationState);
    }
}
