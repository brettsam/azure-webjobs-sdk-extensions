// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Concurrent;
using Microsoft.Azure.WebJobs.Host;

namespace Microsoft.Azure.WebJobs.Extensions.DocumentDB
{
    internal class DocumentDBContextFactory
    {
        internal readonly ConcurrentDictionary<string, IDocumentDBService> ClientCache = new ConcurrentDictionary<string, IDocumentDBService>();

        private DocumentDBConfiguration _config;
        private string _defaultConnectionString;
        private TraceWriter _trace;

        public DocumentDBContextFactory(DocumentDBConfiguration config, string defaultConnectionString, TraceWriter trace)
        {
            _config = config;
            _defaultConnectionString = defaultConnectionString;
            _trace = trace;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "attribute")]
        internal DocumentDBContext CreateContext(DocumentDBAttribute attribute)
        {
            string resolvedConnectionString = ResolveConnectionString(attribute.ConnectionStringSetting);

            IDocumentDBService service = GetService(resolvedConnectionString);

            return new DocumentDBContext
            {
                Service = service,
                Trace = _trace,
                ResolvedAttribute = attribute
            };
        }

        internal IDocumentDBService GetService(string connectionString)
        {
            return ClientCache.GetOrAdd(connectionString, (c) => _config.DocumentDBServiceFactory.CreateService(c));
        }

        internal string ResolveConnectionString(string attributeConnectionString)
        {
            // First, try the Attribute's string.
            if (!string.IsNullOrEmpty(attributeConnectionString))
            {
                return attributeConnectionString;
            }

            // Second, try the config's ConnectionString
            if (!string.IsNullOrEmpty(_config.ConnectionString))
            {
                return _config.ConnectionString;
            }

            // Finally, fall back to the default.
            return _defaultConnectionString;
        }
    }
}