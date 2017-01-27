// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.WebJobs.Extensions.DocumentDB.Bindings;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Config;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.WebJobs.Extensions.DocumentDB
{
    /// <summary>
    /// Defines the configuration options for the DocumentDB binding.
    /// </summary>
    public class DocumentDBConfiguration : IExtensionConfigProvider
    {
        internal const string AzureWebJobsDocumentDBConnectionStringName = "AzureWebJobsDocumentDBConnectionString";

        private DocumentDBContextFactory _contextFactory;

        /// <summary>
        /// Constructs a new instance.
        /// </summary>
        public DocumentDBConfiguration()
        {
            DocumentDBServiceFactory = new DefaultDocumentDBServiceFactory();
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

            INameResolver nameResolver = context.Config.GetService<INameResolver>();
            IConverterManager converterManager = context.Config.GetService<IConverterManager>();
            converterManager.AddConverterBuilder<SingleType, Document, DocumentDBAttribute>(typeof(DocumentDBPocoConverter<>));

            // Use this if there is no other connection string set.
            string defaultConnectionString = nameResolver.Resolve(AzureWebJobsDocumentDBConnectionStringName);

            _contextFactory = new DocumentDBContextFactory(this, defaultConnectionString, context.Trace);

            BindingFactory factory = new BindingFactory(nameResolver, converterManager);
            IBindingProvider outputProvider = factory.BindToAsyncCollector<DocumentDBAttribute, Document>((attr) => BindForOutput(attr));

            IBindingProvider clientProvider = factory.BindToInput<DocumentDBAttribute, DocumentClient>(false, typeof(DocumentDBClientRule), _contextFactory);

            IBindingProvider itemProvider = factory.BindToGenericValueProvider<DocumentDBAttribute>((attr, t) => BindForItemAsync(attr, t));

            IExtensionRegistry extensions = context.Config.GetService<IExtensionRegistry>();
            extensions.RegisterBindingRules<DocumentDBAttribute>((attr, t) => ValidateConnection(attr, defaultConnectionString), nameResolver, outputProvider, clientProvider, itemProvider);
        }

        internal void ValidateConnection(DocumentDBAttribute attribute, string defaultConnectionString)
        {
            if (string.IsNullOrEmpty(ConnectionString) &&
                string.IsNullOrEmpty(attribute.ConnectionStringSetting) &&
                string.IsNullOrEmpty(defaultConnectionString))
            {
                throw new InvalidOperationException(
                    string.Format(CultureInfo.CurrentCulture,
                    "The DocumentDB connection string must be set either via a '{0}' app setting, via the DocumentDBAttribute.ConnectionStringSetting property or via DocumentDBConfiguration.ConnectionString.",
                    AzureWebJobsDocumentDBConnectionStringName));
            }
        }

        internal DocumentDBAsyncCollector BindForOutput(DocumentDBAttribute attribute)
        {
            DocumentDBContext context = _contextFactory.CreateContext(attribute);
            return new DocumentDBAsyncCollector(context);
        }

        internal Task<IValueBinder> BindForItemAsync(DocumentDBAttribute attribute, Type type)
        {
            DocumentDBContext context = _contextFactory.CreateContext(attribute);

            Type genericType = typeof(DocumentDBItemValueBinder<>).MakeGenericType(type);
            IValueBinder binder = (IValueBinder)Activator.CreateInstance(genericType, context);

            return Task.FromResult(binder);
        }

        internal class SingleType : OpenType
        {
            public override bool IsMatch(Type type)
            {               
                if (type == null)
                {
                    throw new ArgumentNullException(nameof(type));
                } 

                return !type.IsArray;
            }
        }

        internal class DocumentDBPocoConverter<T> where T : class
        {
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
            public Document Convert(T value)
            {
                Document doc = value as Document;
                if (doc != null)
                {
                    return doc;
                }

                JObject jobject = null;
                if (value is string)
                {
                    jobject = JObject.Parse(value.ToString());
                }
                else
                {
                    jobject = JObject.FromObject(value);
                }

                return jobject.ToObject<Document>();
            }
        }
    }
}