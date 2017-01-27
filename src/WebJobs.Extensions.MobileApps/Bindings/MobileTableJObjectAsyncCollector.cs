// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.MobileServices;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.WebJobs.Extensions.MobileApps
{
    internal class MobileTableJObjectAsyncCollector : IAsyncCollector<JObject>
    {
        private MobileTableContext _context;

        public MobileTableJObjectAsyncCollector(MobileTableContext context)
        {
            _context = context;
        }

        public async Task AddAsync(JObject item, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (string.IsNullOrEmpty(_context.ResolvedAttribute.TableName))
            {
                throw new InvalidOperationException("The table name must be specified.");
            }

            IMobileServiceTable table = _context.Client.GetTable(_context.ResolvedAttribute.TableName);
            await table.InsertAsync(item);
        }

        public Task FlushAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            // Mobile Services does not support batching.
            return Task.FromResult(0);
        }
    }
}