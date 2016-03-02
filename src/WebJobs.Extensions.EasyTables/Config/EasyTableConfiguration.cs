// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Configuration;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Config;

namespace Microsoft.Azure.WebJobs.Extensions.EasyTables
{
    /// <summary>
    /// Defines the configuration options for the EasyTable binding.
    /// </summary>
    public class EasyTableConfiguration : IExtensionConfigProvider
    {
        internal const string AzureWebJobsMobileAppUriName = "AzureWebJobsMobileAppUri";

        /// <summary>
        /// Constructs a new instance.
        /// </summary>
        public EasyTableConfiguration()
        {
            string uriString = ConfigurationManager.AppSettings[AzureWebJobsMobileAppUriName];
            if (string.IsNullOrEmpty(uriString))
            {
                uriString = Environment.GetEnvironmentVariable(AzureWebJobsMobileAppUriName);
            }

            // if not found, MobileAppUri must be set explicitly before using the config
            if (!string.IsNullOrEmpty(uriString))
            {
                this.MobileAppUri = new Uri(uriString);
            }
        }

        /// <summary>
        /// Gets or sets the Mobile App URI for the Easy Table.
        /// </summary>      
        public Uri MobileAppUri { get; set; }

        public void Initialize(ExtensionConfigContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            // Register our extension binding providers
            IExtensionRegistry extensions = context.Config.GetService<IExtensionRegistry>();
            extensions.RegisterExtension<IBindingProvider>(
                new EasyTableAttributeBindingProvider(context.Config, this, context.Config.NameResolver));
        }
    }
}