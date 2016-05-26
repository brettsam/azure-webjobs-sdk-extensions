// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using Microsoft.Azure.WebJobs.Extensions.Rules;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Host.Config;
using Microsoft.WindowsAzure.MobileServices;

namespace Microsoft.Azure.WebJobs.Extensions.MobileApps
{
    /// <summary>
    /// Defines the configuration options for the motile table binding.
    /// </summary>
    public class MobileAppsConfiguration : IExtensionConfigProvider
    {
        internal const string AzureWebJobsMobileAppUriName = "AzureWebJobsMobileAppUri";
        internal const string AzureWebJobsMobileAppApiKeyName = "AzureWebJobsMobileAppApiKey";
        internal readonly ConcurrentDictionary<string, IMobileServiceClient> ClientCache = new ConcurrentDictionary<string, IMobileServiceClient>();

        /// <summary>
        /// Constructs a new instance.
        /// </summary>
        public MobileAppsConfiguration()
        {
            this.ApiKey = GetSettingFromConfigOrEnvironment(AzureWebJobsMobileAppApiKeyName);

            string uriString = GetSettingFromConfigOrEnvironment(AzureWebJobsMobileAppUriName);

            // if not found, MobileAppUri must be set explicitly before using the config
            if (!string.IsNullOrEmpty(uriString))
            {
                this.MobileAppUri = new Uri(uriString);
            }

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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1804:RemoveUnusedLocals", MessageId = "itemProvider")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1804:RemoveUnusedLocals", MessageId = "outputProvider")]
        public void Initialize(ExtensionConfigContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            context.Config.AddService(this);

            IExtensionRegistry extensions = context.Config.GetService<IExtensionRegistry>();
            extensions.RegisterBindingRules<MobileTableAttribute>(context.Config);
        }

        internal static void ThrowIfGenericArgumentIsInvalid(MobileTableAttribute attribute, Type paramType)
        {
            // Assume IsQueryType or IsTableType has already run -- so we know there is only one argument
            Type argumentType = paramType.GetGenericArguments().Single();
            MobileAppUtility.ThrowIfInvalidItemType(attribute, argumentType);
        }

        internal static bool ThrowIfInvalidOutputItemType(MobileTableAttribute attribute, Type paramType)
        {
            // We explicitly allow object as a type to enable anonymous types, but TableName must be specified.
            if (paramType == typeof(object))
            {
                if (string.IsNullOrEmpty(attribute.TableName))
                {
                    throw new InvalidOperationException("A parameter of type 'object' must have table name specified.");
                }

                return true;
            }

            return MobileAppUtility.ThrowIfInvalidItemType(attribute, paramType);
        }

        internal object BindForOutput(MobileTableAttribute attribute, Type paramType)
        {
            MobileTableContext context = CreateContext(attribute);

            Type collectorType = typeof(MobileTableAsyncCollector<>).MakeGenericType(paramType);

            return Activator.CreateInstance(collectorType, context);
        }

        internal static Uri ResolveMobileAppUri(Uri defaultUri, string attributeUriString)
        {
            Uri resolvedUri = defaultUri;

            if (!string.IsNullOrEmpty(attributeUriString))
            {
                string resolvedUriString = GetSettingFromConfigOrEnvironment(attributeUriString);
                resolvedUri = new Uri(resolvedUriString);
            }

            return resolvedUri;
        }

        internal MobileTableContext CreateContext(MobileTableAttribute attribute)
        {
            Uri resolvedUri = ResolveMobileAppUri(MobileAppUri, attribute.MobileAppUri);
            string resolvedApiKey = ResolveApiKey(ApiKey, attribute.ApiKey);

            return new MobileTableContext
            {
                Client = GetClient(resolvedUri, resolvedApiKey),
                ResolvedAttribute = attribute
            };
        }

        internal static string ResolveApiKey(string defaultApiKey, string attributeApiKey)
        {
            string resolvedApiKey = defaultApiKey;

            // If the attribute specifies an empty string ApiKey, set the ApiKey to null.
            if (attributeApiKey == string.Empty)
            {
                resolvedApiKey = null;
            }
            else if (attributeApiKey != null)
            {
                resolvedApiKey = GetSettingFromConfigOrEnvironment(attributeApiKey);
            }

            return resolvedApiKey;
        }

        internal IMobileServiceClient GetClient(Uri mobileAppUri, string apiKey)
        {
            string key = GetCacheKey(mobileAppUri, apiKey);
            return ClientCache.GetOrAdd(key, (c) => CreateMobileServiceClient(ClientFactory, mobileAppUri, apiKey));
        }

        internal static string GetCacheKey(Uri mobileAppUri, string apiKey)
        {
            return string.Format("{0};{1}", mobileAppUri, apiKey);
        }

        internal static IMobileServiceClient CreateMobileServiceClient(IMobileServiceClientFactory factory, Uri mobileAppUri, string apiKey = null)
        {
            HttpMessageHandler[] handlers = null;
            if (!string.IsNullOrEmpty(apiKey))
            {
                handlers = new[] { new MobileServiceApiKeyHandler(apiKey) };
            }

            return factory.CreateClient(mobileAppUri, handlers);
        }

        internal static string GetSettingFromConfigOrEnvironment(string key)
        {
            string value = null;

            if (string.IsNullOrEmpty(value))
            {
                value = ConfigurationManager.AppSettings[key];
                if (string.IsNullOrEmpty(value))
                {
                    value = Environment.GetEnvironmentVariable(key);
                }
            }

            return value;
        }
    }
}