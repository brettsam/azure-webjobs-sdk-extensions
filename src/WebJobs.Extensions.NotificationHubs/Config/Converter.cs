﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.Azure.NotificationHubs;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.WebJobs.Extensions.NotificationHubs
{
    internal static class Converter
    {
        public static IConverterManager AddNotificationHubConverters(this IConverterManager converterManager)
        {
            converterManager.AddConverter<TemplateNotification, Notification>(templateNotification => templateNotification);
            converterManager.AddConverter<string, Notification>(messageProperties => BuildTemplateNotificationFromJsonString(messageProperties));
            converterManager.AddConverter<string, Notification, NotificationHubAttribute>((notificationAsString, notificationHubAttr) => BuildNotificationFromString(notificationAsString, notificationHubAttr.Platform));
            return converterManager;
        }

        internal static TemplateNotification BuildTemplateNotificationFromJsonString(string messageProperties)
        {
            JObject jobj = JObject.Parse(messageProperties);
            Dictionary<string, string> templateProperties = jobj.ToObject<Dictionary<string, string>>();
            return new TemplateNotification(templateProperties);
        }

        internal static TemplateNotification BuildTemplateNotificationFromDictionary(IDictionary<string, string> templateProperties)
        {
            return new TemplateNotification(templateProperties);
        }

        internal static Notification BuildNotificationFromString(string notificationAsString, NotificationPlatform platform)
        {
            Notification notification = null;
            if (platform == 0)
            {
                JObject jobj = JObject.Parse(notificationAsString);
                Dictionary<string, string> templateProperties = jobj.ToObject<Dictionary<string, string>>();
                notification = new TemplateNotification(templateProperties);
            }
            else
            {
                switch (platform)
                {
                    case NotificationPlatform.Wns:
                        notification = new WindowsNotification(notificationAsString);
                        break;
                    case NotificationPlatform.Apns:
                        notification = new AppleNotification(notificationAsString);
                        break;
                    case NotificationPlatform.Gcm:
                        notification = new GcmNotification(notificationAsString);
                        break;
                    case NotificationPlatform.Adm:
                        notification = new AdmNotification(notificationAsString);
                        break;
                    case NotificationPlatform.Mpns:
                        notification = new MpnsNotification(notificationAsString);
                        break;
                }
            }

            return notification;
        }
    }
}
