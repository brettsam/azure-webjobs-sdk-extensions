// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Azure.WebJobs.Extensions.EasyTables;

namespace Microsoft.Azure.WebJobs
{
    /// <summary>
    /// Extension methods for EasyTable integration.
    /// </summary>
    public static class EasyTablesHostConfigurationExtensions
    {
        /// <summary>
        /// Enables use of the EasyTable extensions.
        /// </summary>
        /// <param name="config">The <see cref="JobHostConfiguration"/> to configure.</param>
        /// <param name="easyTablesConfig">The <see cref="EasyTableConfiguration"/> to use.</param>
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", Justification = "We explicitly need to register an EasyTableConfiguration.")]
        public static void UseEasyTables(this JobHostConfiguration config, EasyTableConfiguration easyTablesConfig = null)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (easyTablesConfig == null)
            {
                easyTablesConfig = new EasyTableConfiguration();
            }

            config.RegisterExtensionConfigProvider(easyTablesConfig);
        }
    }
}