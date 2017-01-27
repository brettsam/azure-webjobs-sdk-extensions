// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.MobileApps.Config;
using Microsoft.WindowsAzure.MobileServices;

namespace Microsoft.Azure.WebJobs.Extensions.MobileApps.Bindings
{
    internal class MobileTableQueryRule<T>
    {
        private MobileTableContextFactory _contextFactory;

        public MobileTableQueryRule(MobileTableContextFactory contextFactory)
        {
            _contextFactory = contextFactory;
        }

        internal async Task<IMobileServiceTableQuery<T>> Convert(MobileTableAttribute attribute)
        {
            // The Table POCO rule already knows how to get the table
            MobileTablePocoRule<T> tablePocoRule = new MobileTablePocoRule<T>(_contextFactory);
            IMobileServiceTable<T> table = await tablePocoRule.Convert(attribute);

            return table.CreateQuery();
        }
    }
}
