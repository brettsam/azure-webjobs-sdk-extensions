﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.ServiceBus;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.WebJobs.Extensions.EasyTables
{
    /// <summary>
    /// Provides an <see cref="IBinding"/> for valid output parameters decorated with
    /// an <see cref="EasyTableAttribute"/>.
    /// </summary>
    /// <remarks>
    /// The method parameter type can be one of the following:
    /// <list type="bullet">
    /// <item><description><see cref="ICollector{T}"/>, where T is either <see cref="JObject"/> or any type with a public string Id property.</description></item>
    /// <item><description><see cref="IAsyncCollector{T}"/>, where T is either <see cref="JObject"/> or any type with a public string Id property.</description></item>
    /// <item><description>out <see cref="JObject"/></description></item>
    /// <item><description>out <see cref="JObject"/>[]</description></item>
    /// <item><description>out T, where T is any Type with a public string Id property</description></item>
    /// <item><description>out T[], where T is any Type with a public string Id property</description></item>
    /// </list>
    /// </remarks>
    internal class EasyTableOutputBindingProvider : IBindingProvider
    {
        private EasyTableContext _easyTableContext;
        private JobHostConfiguration _jobHostConfig;

        public EasyTableOutputBindingProvider(JobHostConfiguration jobHostConfig, EasyTableContext easyTableContext)
        {
            _jobHostConfig = jobHostConfig;
            _easyTableContext = easyTableContext;
        }

        public Task<IBinding> TryCreateAsync(BindingProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            ParameterInfo parameter = context.Parameter;

            // this is a special case for scripting
            if (parameter.ParameterType == typeof(IAsyncCollector<byte[]>))
            {
                IBinding binding = GenericBinder.BindCollector<JObject, EasyTableContext>(parameter, _jobHostConfig.GetOrCreateConverterManager(),
                    (easyTableContext, valueBindingContext) => new EasyTableAsyncCollector<JObject>(easyTableContext), string.Empty,
                    (s) => _easyTableContext);

                return Task.FromResult<IBinding>(binding);
            }

            if (IsValidOutType(parameter.ParameterType) ||
                IsValidCollectorType(parameter.ParameterType))
            {
                return CreateBinding(parameter);
            }

            return Task.FromResult<IBinding>(null);
        }

        internal Task<IBinding> CreateBinding(ParameterInfo parameter)
        {
            Type coreType = EasyTableUtility.GetCoreType(parameter.ParameterType);

            IBinding genericBinding = GenericBinder.BindGenericCollector(parameter, typeof(EasyTableAsyncCollector<>), coreType,
                _jobHostConfig.GetOrCreateConverterManager(), (s) => _easyTableContext);
            return Task.FromResult(genericBinding);
        }

        internal static bool IsValidOutType(Type paramType)
        {
            if (paramType.IsByRef)
            {
                Type coreType = paramType.GetElementType();
                if (coreType.IsArray)
                {
                    coreType = coreType.GetElementType();
                }

                if (EasyTableUtility.IsValidItemType(coreType))
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool IsValidCollectorType(Type paramType)
        {
            if (paramType.IsGenericType)
            {
                Type genericType = paramType.GetGenericTypeDefinition();
                if (genericType == typeof(ICollector<>) || genericType == typeof(IAsyncCollector<>))
                {
                    if (EasyTableUtility.IsCoreTypeValidItemType(paramType))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}