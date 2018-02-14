// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using Microsoft.Azure.Documents;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Bindings.Path;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.WebJobs.Extensions.CosmosDB
{
    internal class CosmosDBSqlResolutionPolicy : IResolutionPolicy
    {
        public string TemplateBind(PropertyInfo propInfo, Attribute resolvedAttribute, BindingTemplate bindingTemplate, IReadOnlyDictionary<string, object> bindingData)
        {
            if (bindingTemplate == null)
            {
                throw new ArgumentNullException(nameof(bindingTemplate));
            }

            if (bindingData == null)
            {
                throw new ArgumentNullException(nameof(bindingData));
            }

            if (!(resolvedAttribute is CosmosDBAttribute cosmosDBAttribute))
            {
                throw new NotSupportedException($"This policy is only supported for {nameof(CosmosDBAttribute)}.");
            }

            // build a SqlParameterCollection for each parameter            
            SqlParameterCollection paramCollection = new SqlParameterCollection();

            // also build up a dictionary replacing '{token}' with '@token' 
            IDictionary<string, object> replacements = new Dictionary<string, object>();
            foreach (var tokenName in bindingTemplate.TokenStrings.Distinct())
            {
                string sqlTokenName = "@" + EscapeSqlParameterName(tokenName);

                // We need to construct a JObject that nests like the token and will return our
                // new SqlParameter as a replacement.                
                UpdateReplacementData(replacements, tokenName, sqlTokenName);

                // Resolve the token from the bindingData. This will resolve tokens like "a.b.c" automatically.
                string tokenValue = BindingTemplate.ResolveToken(tokenName, bindingData);

                paramCollection.Add(new SqlParameter(sqlTokenName, tokenValue));
            }

            cosmosDBAttribute.SqlQueryParameters = paramCollection;

            return bindingTemplate.Bind(new ReadOnlyDictionary<string, object>(replacements));
        }

        private string EscapeSqlParameterName(string name)
        {
            // periods and hyphens are not allowed in SqlParameter names
            const char underscore = '_';
            return name.Replace('.', underscore).Replace('-', underscore);
        }

        internal static void UpdateReplacementData(IDictionary<string, object> replacements, string tokenString, string dataValue)
        {
            string[] parts = tokenString.Split('.');

            // Token has only one part. No need for nested objects.
            if (parts.Length == 1)
            {
                replacements[tokenString] = dataValue;
                return;
            }

            JObject rootJObject = null;
            if (!replacements.TryGetValue(parts[0], out object root))
            {
                root = new JObject();
                replacements[parts[0]] = root;
            }

            rootJObject = root as JObject;

            JToken current = rootJObject;
            for (int i = 1; i < parts.Length - 1; i++)
            {
                JToken previous = current;
                current = new JObject();
                previous[parts[i]] = current;
            }

            // set the value at the innermost level.
            current[parts.Last()] = dataValue;
        }
    }
}