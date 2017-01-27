// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.MobileApps.Config;
using Microsoft.WindowsAzure.MobileServices;

namespace Microsoft.Azure.WebJobs.Extensions.MobileApps.Bindings
{
    internal class MobileTablePocoRule<T>
    {
        private MobileTableContextFactory _contextFactory;

        public MobileTablePocoRule(MobileTableContextFactory contextFactory)
        {
            _contextFactory = contextFactory;
        }

        internal Task<IMobileServiceTable<T>> Convert(MobileTableAttribute attribute)
        {
            MobileTableContext context = _contextFactory.CreateContext(attribute);

            // If TableName is specified, add it to the internal table cache. Now items of this type
            // will operate on the specified TableName.
            if (!string.IsNullOrEmpty(context.ResolvedAttribute.TableName))
            {
                context.Client.AddToTableNameCache(typeof(T), context.ResolvedAttribute.TableName);
            }

            IMobileServiceTable<T> table = context.Client.GetTable<T>();

            return Task.FromResult(table);
        }
    }
}
