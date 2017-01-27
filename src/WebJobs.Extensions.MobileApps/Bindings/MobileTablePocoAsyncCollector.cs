// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.MobileServices;

namespace Microsoft.Azure.WebJobs.Extensions.MobileApps
{
    internal class MobileTablePocoAsyncCollector<T> : IAsyncCollector<T>
    {
        private MobileTableContext _context;

        public MobileTablePocoAsyncCollector(MobileTableContext context)
        {
            _context = context;
        }

        public async Task AddAsync(T item, CancellationToken cancellationToken = default(CancellationToken))
        {
            // If TableName is specified, add it to the internal table cache. Now items of this type
            // will operate on the specified TableName.
            if (!string.IsNullOrEmpty(_context.ResolvedAttribute.TableName))
            {
                _context.Client.AddToTableNameCache(item.GetType(), _context.ResolvedAttribute.TableName);
            }

            IMobileServiceTable<T> table = _context.Client.GetTable<T>();
            await table.InsertAsync(item);
        }

        public Task FlushAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            // Mobile Services does not support batching.
            return Task.FromResult(0);
        }
    }
}