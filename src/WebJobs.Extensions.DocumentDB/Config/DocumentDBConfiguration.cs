// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Configuration;
using System.Threading.Tasks;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Config;

namespace Microsoft.Azure.WebJobs.Extensions.DocumentDB
{
    /// <summary>
    /// Defines the configuration options for the DocumentDB binding.
    /// </summary>
    public class DocumentDBConfiguration : IExtensionConfigProvider
    {
        internal const string AzureWebJobsDocumentDBConnectionStringName = "AzureWebJobsDocumentDBConnectionString";

        /// <summary>
        /// Constructs a new instance.
        /// </summary>
        public DocumentDBConfiguration()
        {
            this.ConnectionString = GetSettingFromConfigOrEnvironment(AzureWebJobsDocumentDBConnectionStringName);
            this.DocumentDBServiceFactory = new DefaultDocumentDBServiceFactory();
        }

        internal IDocumentDBServiceFactory DocumentDBServiceFactory { get; set; }

        /// <summary>
        /// Gets or sets the DocumentDB connection string.
        /// </summary>
        public string ConnectionString { get; set; }

        /// <inheritdoc />
        public void Initialize(ExtensionConfigContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            var nameResolver = context.Config.GetService<INameResolver>();
            var cm = context.Config.GetService<IConverterManager>();

            var bf = new BindingFactory(nameResolver, cm);
            var outputRule = bf.BindToGenericAsyncCollector<DocumentDBAttribute, DocumentDBContext>(typeof(DocumentDBAsyncCollector<>), (attr) =>
            {
                return new DocumentDBContext
                {
                    Service = this.DocumentDBServiceFactory.CreateService(this.ConnectionString),
                    CreateIfNotExists = attr.CreateIfNotExists,
                    ResolvedCollectionName = attr.CollectionName,
                    ResolvedDatabaseName = attr.DatabaseName,
                    ResolvedId = attr.Id,
                    ResolvedPartitionKey = attr.PartitionKey,
                    CollectionThroughput = attr.CollectionThroughput,
                    MaxThrottleRetries = 10,
                    Trace = context.Trace
                };
            });

            var clientRule = bf.BindToExactType<DocumentDBAttribute, DocumentClient>((attr) => this.DocumentDBServiceFactory.CreateService(this.ConnectionString).GetClient());
            var itemRule = bf.BindToGenericItem<DocumentDBAttribute>((attr, t) =>
            {
                DocumentDBContext documentDBContext = new DocumentDBContext
                {
                    Service = this.DocumentDBServiceFactory.CreateService(this.ConnectionString),
                    CreateIfNotExists = attr.CreateIfNotExists,
                    ResolvedCollectionName = attr.CollectionName,
                    ResolvedDatabaseName = attr.DatabaseName,
                    ResolvedId = attr.Id,
                    ResolvedPartitionKey = attr.PartitionKey,
                    CollectionThroughput = attr.CollectionThroughput,
                    MaxThrottleRetries = 10,
                    Trace = context.Trace
                };

                Type genericType = typeof(DocumentDBItemValueBinder<>).MakeGenericType(t);
                var binder = (IValueBinder)Activator.CreateInstance(genericType, documentDBContext);

                return Task.FromResult(binder);
            });

            IExtensionRegistry extensions = context.Config.GetService<IExtensionRegistry>();
            extensions.RegisterBindingRules<DocumentDBAttribute>(outputRule, clientRule, itemRule);
        }

        internal static string GetSettingFromConfigOrEnvironment(string key)
        {
            string value = null;

            if (string.IsNullOrEmpty(value))
            {
                ConnectionStringSettings connectionString = ConfigurationManager.ConnectionStrings[key];
                if (connectionString != null)
                {
                    value = connectionString.ConnectionString;
                }

                if (string.IsNullOrEmpty(value))
                {
                    value = ConfigurationManager.AppSettings[key];
                }

                if (string.IsNullOrEmpty(value))
                {
                    value = Environment.GetEnvironmentVariable(key);
                }
            }

            return value;
        }
    }
}
