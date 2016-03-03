// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.ServiceBus;
using Microsoft.WindowsAzure.MobileServices;
using Newtonsoft.Json.Linq;
using System.Text;

namespace Microsoft.Azure.WebJobs.Extensions.EasyTables
{
    internal class EasyTableAttributeBindingProvider : IBindingProvider
    {
        private EasyTableConfiguration _easyTableConfig;
        private JobHostConfiguration _jobHostConfig;
        private INameResolver _nameResolver;

        public EasyTableAttributeBindingProvider(JobHostConfiguration config, EasyTableConfiguration easyTableConfig, INameResolver nameResolver)
        {
            _jobHostConfig = config;
            _easyTableConfig = easyTableConfig;
            _nameResolver = nameResolver;
        }

        public Task<IBinding> TryCreateAsync(BindingProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            ParameterInfo parameter = context.Parameter;
            EasyTableAttribute attribute = parameter.GetCustomAttribute<EasyTableAttribute>(inherit: false);
            if (attribute == null)
            {
                return Task.FromResult<IBinding>(null);
            }

            if (_easyTableConfig.MobileAppUri == null)
            {
                throw new InvalidOperationException(
                    string.Format(CultureInfo.CurrentCulture,
                    "The Easy Tables Uri must be set either via a '{0}' app setting or directly in code via EasyTableConfiguration.MobileAppUri.",
                    EasyTableConfiguration.AzureWebJobsMobileAppUriName));
            }
            
            IConverterManager converterManager = _jobHostConfig.GetOrCreateConverterManager();
            converterManager.AddConverter<byte[], JObject>((bytes) =>
            {
                string jsonStr = Encoding.UTF8.GetString(bytes);
                return JObject.Parse(jsonStr);
            });

            EasyTableContext easyTableContext = CreateContext(_easyTableConfig, attribute, _nameResolver);

            IBindingProvider compositeProvider = new CompositeBindingProvider(new IBindingProvider[]
            {
                new EasyTableOutputBindingProvider(_jobHostConfig, easyTableContext),
                new EasyTableQueryBinding(context.Parameter, easyTableContext),
                new EasyTableTableBinding(parameter, easyTableContext),
                new EasyTableItemBinding(parameter, easyTableContext, context)
            });

            return compositeProvider.TryCreateAsync(context);
        }

        internal static EasyTableContext CreateContext(EasyTableConfiguration config, EasyTableAttribute attribute, INameResolver resolver)
        {
            return new EasyTableContext
            {
                Config = config,
                Client = new MobileServiceClient(config.MobileAppUri),
                ResolvedId = Resolve(attribute.Id, resolver),
                ResolvedTableName = Resolve(attribute.TableName, resolver)
            };
        }

        private static string Resolve(string value, INameResolver resolver)
        {
            if (resolver == null)
            {
                return value;
            }

            return resolver.ResolveWholeString(value);
        }
    }
}