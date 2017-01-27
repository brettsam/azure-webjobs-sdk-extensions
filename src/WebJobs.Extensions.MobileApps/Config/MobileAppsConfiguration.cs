// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.MobileApps.Bindings;
using Microsoft.Azure.WebJobs.Extensions.MobileApps.Config;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Config;
using Microsoft.WindowsAzure.MobileServices;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.WebJobs.Extensions.MobileApps
{
    /// <summary>
    /// Defines the configuration options for the motile table binding.
    /// </summary>
    public class MobileAppsConfiguration : IExtensionConfigProvider
    {
        internal const string AzureWebJobsMobileAppUriName = "AzureWebJobsMobileAppUri";
        internal const string AzureWebJobsMobileAppApiKeyName = "AzureWebJobsMobileAppApiKey";

        private MobileTableContextFactory _contextFactory;

        /// <summary>
        /// Constructs a new instance.
        /// </summary>
        public MobileAppsConfiguration()
        {
            this.ClientFactory = new DefaultMobileServiceClientFactory();
        }

        /// <summary>
        /// Gets or sets the ApiKey to use with the Mobile App.
        /// </summary>
        public string ApiKey { get; set; }

        /// <summary>
        /// Gets or sets the mobile app URI.
        /// </summary>      
        public Uri MobileAppUri { get; set; }

        internal IMobileServiceClientFactory ClientFactory { get; set; }

        /// <inheritdoc />
        public void Initialize(ExtensionConfigContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            INameResolver nameResolver = context.Config.GetService<INameResolver>();
            IConverterManager converterManager = context.Config.GetService<IConverterManager>();
            converterManager.AddConverter<object, JObject>((o) => JObject.FromObject(o));

            // Set defaults, to be used if no other values are found:
            string defaultApiKey = nameResolver.Resolve(AzureWebJobsMobileAppApiKeyName);

            string uriString = nameResolver.Resolve(AzureWebJobsMobileAppUriName);
            Uri defaultMobileAppUri;
            Uri.TryCreate(uriString, UriKind.Absolute, out defaultMobileAppUri);

            _contextFactory = new MobileTableContextFactory(this, defaultMobileAppUri, defaultApiKey, nameResolver);
            BindingFactory factory = new BindingFactory(nameResolver, converterManager);

            IBindingProvider outputProvider = factory.BindToGenericAsyncCollector<MobileTableAttribute>(BindForPocoOutput, ThrowIfInvalidOutputItemType);
            IBindingProvider outputJObjectProvider = factory.BindToAsyncCollector<MobileTableAttribute, JObject>(BindForJObjectOutput);

            IBindingProvider clientProvider = factory.BindToInput<MobileTableAttribute, IMobileServiceClient>(false, typeof(MobileTableClientRule), _contextFactory);

            IBindingProvider queryProvider = factory.BindToInput<MobileTableAttribute, IMobileServiceTableQuery<OpenType>>(false, typeof(MobileTableQueryRule<>), _contextFactory);
            queryProvider = factory.AddFilter<MobileTableAttribute>(IsQueryType, queryProvider);

            IBindingProvider jObjectTableProvider = factory.BindToInput<MobileTableAttribute, IMobileServiceTable>(false, typeof(MobileTableJObjectRule), _contextFactory);

            IBindingProvider tableProvider = factory.BindToInput<MobileTableAttribute, IMobileServiceTable<OpenType>>(false, typeof(MobileTablePocoRule<>), _contextFactory);
            tableProvider = factory.AddFilter<MobileTableAttribute>(IsTableType, tableProvider);

            IBindingProvider itemProvider = factory.BindToGenericValueProvider<MobileTableAttribute>(BindForItemAsync);
            itemProvider = factory.AddFilter<MobileTableAttribute>(IsItemType, itemProvider);

            IExtensionRegistry extensions = context.Config.GetService<IExtensionRegistry>();
            extensions.RegisterBindingRules<MobileTableAttribute>((attr, t) => ValidateMobileAppUri(attr, t, defaultMobileAppUri), nameResolver, outputProvider, outputJObjectProvider, clientProvider, jObjectTableProvider, queryProvider, tableProvider, itemProvider);
        }

        internal static bool IsQueryType(MobileTableAttribute attribute, Type paramType)
        {
            if (paramType.IsGenericType &&
                paramType.GetGenericTypeDefinition() == typeof(IMobileServiceTableQuery<>))
            {
                Type tableType = paramType.GetGenericArguments().Single();
                ThrowIfInvalidItemType(attribute, tableType);

                return true;
            }

            return false;
        }

        internal bool IsTableType(MobileTableAttribute attribute, Type paramType)
        {
            // We will check if the argument is valid in a Validator
            if (paramType.IsGenericType &&
                paramType.GetGenericTypeDefinition() == typeof(IMobileServiceTable<>))
            {
                Type tableType = paramType.GetGenericArguments().Single();
                ThrowIfInvalidItemType(attribute, tableType);

                return true;
            }

            return false;
        }

        internal bool IsItemType(MobileTableAttribute attribute, Type paramType)
        {
            ThrowIfInvalidItemType(attribute, paramType);

            if (string.IsNullOrEmpty(attribute.Id))
            {
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, "'Id' must be set when using a parameter of type '{0}'.", paramType.Name));
            }

            return true;
        }

        internal static bool ThrowIfInvalidOutputItemType(MobileTableAttribute attribute, Type paramType)
        {
            // These will fall through to the JObject AsyncCollector
            if (paramType == typeof(object) || paramType == typeof(JObject))
            {
                return false;
            }

            return ThrowIfInvalidItemType(attribute, paramType);
        }

        internal static bool ThrowIfInvalidItemType(MobileTableAttribute attribute, Type paramType)
        {
            if (!MobileAppUtility.IsValidItemType(paramType, attribute.TableName))
            {
                throw new ArgumentException(string.Format("The type '{0}' cannot be used in a MobileTable binding. The type must either be 'JObject', 'object', or have a public string 'Id' property.", paramType.Name));
            }

            return true;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "paramType")]
        internal void ValidateMobileAppUri(MobileTableAttribute attribute, Type paramType, Uri defaultMobileAppUri)
        {
            if (MobileAppUri == null &&
                string.IsNullOrEmpty(attribute.MobileAppUriSetting) &&
                defaultMobileAppUri == null)
            {
                throw new InvalidOperationException(
                    string.Format(CultureInfo.CurrentCulture,
                    "The mobile app Uri must be set either via a '{0}' app setting, via the MobileTableAttribute.MobileAppUriSetting property or via MobileAppsConfiguration.MobileAppUri.",
                    AzureWebJobsMobileAppUriName));
            }
        }

        internal Task<IValueBinder> BindForItemAsync(MobileTableAttribute attribute, Type paramType)
        {
            MobileTableContext context = _contextFactory.CreateContext(attribute);

            Type genericType = typeof(MobileTableItemValueBinder<>).MakeGenericType(paramType);
            IValueBinder binder = (IValueBinder)Activator.CreateInstance(genericType, context);

            return Task.FromResult(binder);
        }

        internal MobileTableJObjectAsyncCollector BindForJObjectOutput(MobileTableAttribute attribute)
        {
            MobileTableContext context = _contextFactory.CreateContext(attribute);
            return new MobileTableJObjectAsyncCollector(context);
        }

        internal object BindForPocoOutput(MobileTableAttribute attribute, Type paramType)
        {
            MobileTableContext context = _contextFactory.CreateContext(attribute);

            Type collectorType = typeof(MobileTablePocoAsyncCollector<>).MakeGenericType(paramType);

            return Activator.CreateInstance(collectorType, context);
        }
    }
}