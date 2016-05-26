// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.WebJobs.Extensions.MobileApps
{
    internal static class MobileAppUtility
    {
        /// <summary>
        /// Evaluates whether the specified type is valid for use with mobile tables.
        /// If the type is <see cref="JObject"/>, then then a table name is required.
        /// If the type is not <see cref="JObject"/>, then it must contain a single public
        /// string 'Id' property.
        /// </summary>
        /// <param name="itemType">The type to evaluate.</param>
        /// <param name="tableName">The table name.</param>
        /// <returns></returns>
        public static bool IsValidItemType(Type itemType, string tableName)
        {
            // We cannot support a type of JObject without a TableName.
            if (itemType == typeof(JObject))
            {
                return !string.IsNullOrEmpty(tableName);
            }

            // POCO types must have a string id property (case insensitive).
            IEnumerable<PropertyInfo> idProperties = itemType.GetProperties()
                .Where(p => string.Equals("id", p.Name, StringComparison.OrdinalIgnoreCase) && p.PropertyType == typeof(string));

            if (idProperties.Count() != 1)
            {
                return false;
            }

            return true;
        }

        internal static bool ThrowIfInvalidItemType(MobileTableAttribute attribute, Type paramType)
        {
            if (!IsValidItemType(paramType, attribute.TableName))
            {
                throw new ArgumentException(string.Format("The type '{0}' cannot be used in a MobileTable binding. The type must either be 'JObject' or have a public string 'Id' property.", paramType.Name));
            }

            return true;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "paramType")]
        internal static void ValidateMobileAppUri(MobileAppsConfiguration config, MobileTableAttribute attribute, Type paramType)
        {
            if (config.MobileAppUri == null &&
                string.IsNullOrEmpty(attribute.MobileAppUri))
            {
                throw new InvalidOperationException(
                    string.Format(CultureInfo.CurrentCulture,
                    "The mobile app Uri must be set either via a '{0}' app setting, via the MobileTableAttribute.MobileAppUri property or via MobileAppsConfiguration.MobileAppUri.",
                    MobileAppsConfiguration.AzureWebJobsMobileAppUriName));
            }
        }
    }
}