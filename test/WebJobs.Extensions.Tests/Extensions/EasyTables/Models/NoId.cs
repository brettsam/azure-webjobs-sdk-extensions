// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Azure.WebJobs.Extensions.Tests.EasyTables
{
    [SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Justification = "This class is used for testing via reflection.")]
    internal class NoId
    {
        public string Data { get; set; }
    }
}