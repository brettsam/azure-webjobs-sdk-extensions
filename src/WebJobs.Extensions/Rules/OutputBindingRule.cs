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
    public abstract class OutputBindingRule<TAttribute> : IBindingRule<TAttribute> where TAttribute : Attribute
    {
        private IBindingRule<TAttribute> _innerRule;

        bool IBindingRule<TAttribute>.CanBind(TAttribute attribute, Type parameterType)
        {
            Type typeMessage = GetAsyncCollectorCoreType(parameterType);

            if (typeMessage == null)
            {
                // incompatible type. Skip. 
                return false;
            }

            if (CanBindToMessage(attribute, typeMessage))
            {
                _innerRule = GetInnerRule(parameterType);
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="attribute"></param>
        /// <param name="parameterType"></param>
        /// <returns></returns>
        public abstract object GetAsyncCollector(TAttribute attribute, Type parameterType);

        Task IBindingRule<TAttribute>.OnFunctionExecutedAsync(TAttribute attribute, Type parameterType, object item, IDictionary<string, object> invocationState)
        {
            return _innerRule.OnFunctionExecutedAsync(attribute, parameterType, item, invocationState);
        }

        Task<object> IBindingRule<TAttribute>.OnFunctionExecutingAsync(TAttribute attribute, Type parameterType, IDictionary<string, object> invocationState)
        {
            return _innerRule.OnFunctionExecutingAsync(attribute, parameterType, invocationState);
        }

        /// <summary>        
        /// </summary>
        /// <param name="attribute"></param>
        /// <param name="messageType"></param>
        /// <returns></returns>
        public abstract bool CanBindToMessage(TAttribute attribute, Type messageType);

        internal static Type GetAsyncCollectorCoreType(Type parameterType)
        {
            if (parameterType.IsGenericType)
            {
                var genericType = parameterType.GetGenericTypeDefinition();
                var elementType = parameterType.GetGenericArguments()[0];

                if (genericType == typeof(IAsyncCollector<>))
                {
                    return elementType;
                }
                else if (genericType == typeof(ICollector<>))
                {
                    return elementType;
                }

                return null;
            }
            else
            {
                if (parameterType.IsByRef)
                {
                    var inner = parameterType.GetElementType(); // strip off the byref type

                    if (inner.IsArray)
                    {
                        var elementType = inner.GetElementType();
                        return elementType;
                    }
                    return inner;
                }
                return null;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1804:RemoveUnusedLocals", MessageId = "elementType")]
        private IBindingRule<TAttribute> GetInnerRule(Type parameterType)
        {
            if (parameterType.IsGenericType)
            {
                var genericType = parameterType.GetGenericTypeDefinition();
                var elementType = parameterType.GetGenericArguments()[0];

                if (genericType == typeof(IAsyncCollector<>))
                {
                    return null;
                }
                else if (genericType == typeof(ICollector<>))
                {
                    return null;
                }

                return null;
            }
            else
            {
                if (parameterType.IsByRef)
                {
                    var inner = parameterType.GetElementType(); // strip off the byref type

                    if (inner.IsArray)
                    {
                        var elementType = inner.GetElementType();
                        Type outArrayRuleType = typeof(OutArrayBindingRule<,>).MakeGenericType(typeof(TAttribute), elementType);
                        return Activator.CreateInstance(outArrayRuleType, this) as IBindingRule<TAttribute>;
                    }

                    Type outRuleType = typeof(OutBindingRule<,>).MakeGenericType(typeof(TAttribute), inner);
                    return Activator.CreateInstance(outRuleType, this) as IBindingRule<TAttribute>;
                }
                return null;
            }
        }
    }
}