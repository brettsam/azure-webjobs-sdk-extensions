// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.MobileApps.Config;
using Microsoft.WindowsAzure.MobileServices;

namespace Microsoft.Azure.WebJobs.Extensions.MobileApps.Bindings
{
    internal class MobileTableJObjectRule
    {
        private MobileTableContextFactory _contextFactory;

        public MobileTableJObjectRule(MobileTableContextFactory contextFactory)
        {
            _contextFactory = contextFactory;
        }

        internal Task<IMobileServiceTable> Convert(MobileTableAttribute attribute)
        {
            MobileTableContext context = _contextFactory.CreateContext(attribute);
            IMobileServiceTable table = context.Client.GetTable(context.ResolvedAttribute.TableName);
            return Task.FromResult(table);
        }
    }
}
