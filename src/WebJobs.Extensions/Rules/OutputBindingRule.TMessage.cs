// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace Microsoft.Azure.WebJobs.Extensions.Rules
{
    /// <summary>    
    /// </summary>
    /// <typeparam name="TAttribute"></typeparam>
    /// <typeparam name="TMessage"></typeparam>
    public abstract class OutputBindingRule<TAttribute, TMessage> : OutputBindingRule<TAttribute> where TAttribute : Attribute
    {
        /// <summary>        
        /// </summary>
        /// <param name="attribute"></param>
        /// <param name="messageType"></param>
        /// <returns></returns>
        public override bool CanBindToMessage(TAttribute attribute, Type messageType)
        {
            return messageType == typeof(TMessage);
        }

        /// <summary>
        /// </summary>
        /// <param name="attribute"></param>
        /// <returns></returns>
        public abstract IAsyncCollector<TMessage> GetAsyncCollector(TAttribute attribute);

        /// <summary>        
        /// </summary>
        /// <param name="attribute"></param>
        /// <param name="parameterType"></param>
        /// <returns></returns>
        public override object GetAsyncCollector(TAttribute attribute, Type parameterType)
        {
            return GetAsyncCollector(attribute, parameterType);
        }
    }
}
