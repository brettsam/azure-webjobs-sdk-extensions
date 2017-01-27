// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.WebJobs.Extensions.MobileApps.Config;
using Microsoft.WindowsAzure.MobileServices;

namespace Microsoft.Azure.WebJobs.Extensions.MobileApps.Bindings
{
    internal class MobileTableClientRule
    {
        private MobileTableContextFactory _contextFactory;

        public MobileTableClientRule(MobileTableContextFactory contextFactory)
        {
            _contextFactory = contextFactory;
        }

        internal IMobileServiceClient Convert(MobileTableAttribute attribute)
        {
            MobileTableContext context = _contextFactory.CreateContext(attribute);
            return context.Client;
        }
    }
}
