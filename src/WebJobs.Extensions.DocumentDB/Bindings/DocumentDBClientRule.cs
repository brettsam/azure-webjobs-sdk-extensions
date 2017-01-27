// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.Documents.Client;

namespace Microsoft.Azure.WebJobs.Extensions.DocumentDB.Bindings
{
    internal class DocumentDBClientRule
    {
        private DocumentDBContextFactory _contextFactory;

        public DocumentDBClientRule(DocumentDBContextFactory contextFactory)
        {
            _contextFactory = contextFactory;
        }

        internal DocumentClient Convert(DocumentDBAttribute attribute)
        {
            DocumentDBContext context = _contextFactory.CreateContext(attribute);
            return context.Service.GetClient();
        }
    }
}
