// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Net.Http;
using Microsoft.WindowsAzure.MobileServices;

namespace Microsoft.Azure.WebJobs.Extensions.MobileApps.Config
{
    internal class MobileTableContextFactory
    {
        internal readonly ConcurrentDictionary<string, IMobileServiceClient> ClientCache = new ConcurrentDictionary<string, IMobileServiceClient>();
        private MobileAppsConfiguration _config;
        private Uri _defaultMobileAppUri;
        private string _defaultApiKey;
        private INameResolver _nameResolver;

        public MobileTableContextFactory(MobileAppsConfiguration config, Uri defaultMobileAppUri, string defaultApiKey, INameResolver nameResolver)
        {
            _config = config;
            _defaultMobileAppUri = defaultMobileAppUri;
            _defaultApiKey = defaultApiKey;
            _nameResolver = nameResolver;
        }

        internal MobileTableContext CreateContext(MobileTableAttribute resolvedAttribute)
        {
            Uri resolvedUri = ResolveMobileAppUri(resolvedAttribute.MobileAppUriSetting);
            string resolvedApiKey = ResolveApiKey(resolvedAttribute.ApiKeySetting);

            return new MobileTableContext
            {
                Client = GetClient(resolvedUri, resolvedApiKey),
                ResolvedAttribute = resolvedAttribute
            };
        }

        internal IMobileServiceClient GetClient(Uri mobileAppUri, string apiKey)
        {
            string key = GetCacheKey(mobileAppUri, apiKey);
            return ClientCache.GetOrAdd(key, (c) => CreateMobileServiceClient(_config.ClientFactory, mobileAppUri, apiKey));
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

        internal Uri ResolveMobileAppUri(string attributeUriString)
        {
            // First, try the Attribute's Uri.
            Uri attributeUri;
            if (Uri.TryCreate(attributeUriString, UriKind.Absolute, out attributeUri))
            {
                return attributeUri;
            }

            // Second, try the config's Uri
            if (_config.MobileAppUri != null)
            {
                return _config.MobileAppUri;
            }

            // Finally, fall back to the default.
            return _defaultMobileAppUri;
        }

        internal string ResolveApiKey(string attributeApiKey)
        {
            // The behavior for ApiKey is unique, so we do not use the AutoResolve
            // functionality.
            // If an attribute sets the ApiKeySetting to an empty string,
            // that overwrites any default value and sets it to null.
            // If ApiKeySetting is null, it returns the default value.

            // First, if the key is an empty string, return null.
            if (attributeApiKey != null && attributeApiKey.Length == 0)
            {
                return null;
            }

            // Second, if it is anything other than null, return the resolved value
            if (attributeApiKey != null)
            {
                return _nameResolver.Resolve(attributeApiKey);
            }

            // Third, try the config's key
            if (!string.IsNullOrEmpty(_config.ApiKey))
            {
                return _config.ApiKey;
            }

            // Finally, fall back to the default.
            return _defaultApiKey;
        }
    }
}