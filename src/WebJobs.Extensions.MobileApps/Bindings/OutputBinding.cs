using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.MobileApps.Bindings
{
    internal class OutputBinding
    {
        internal static bool ThrowIfInvalidOutputItemType(MobileTableAttribute attribute, Type paramType)
        {
            // We explicitly allow object as a type to enable anonymous types, but TableName must be specified.
            if (paramType == typeof(object))
            {
                if (string.IsNullOrEmpty(attribute.TableName))
                {
                    throw new InvalidOperationException("A parameter of type 'object' must have table name specified.");
                }

                return true;
            }

            return ThrowIfInvalidItemType(attribute, paramType);
        }
    }
}
