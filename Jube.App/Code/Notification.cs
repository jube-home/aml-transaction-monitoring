/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.
 *
 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

namespace Jube.App.Code
{
    using System;
    using System.Collections.Generic;
    using DynamicEnvironment;
    using log4net;

    public class Notification
    {
        private readonly DynamicEnvironment dynamicEnvironment;
        private readonly ILog log;

        public Notification(ILog log, DynamicEnvironment dynamicEnvironment)
        {
            this.dynamicEnvironment = dynamicEnvironment;
            this.log = log;
        }

        public void Send(int notificationType, string notificationDestination, string notificationSubject,
            string notificationBody, Dictionary<string, string> values
        )
        {
            var notificationTokenization = new Tokenisation();

            var replacedNotificationDestination = notificationDestination;
            if (!String.IsNullOrEmpty(replacedNotificationDestination))
            {
                var notificationDestinationTokens =
                    notificationTokenization.ReturnTokens(replacedNotificationDestination);
                foreach (var notificationToken in notificationDestinationTokens)
                {
                    if (!values.TryGetValue(notificationToken, out var value))
                    {
                        continue;
                    }

                    var notificationReplaceToken = $"[@{notificationToken}@]";
                    replacedNotificationDestination =
                        replacedNotificationDestination.Replace(notificationReplaceToken,
                            value);
                }
            }

            var replacedNotificationSubject = notificationSubject;
            if (!String.IsNullOrEmpty(replacedNotificationSubject))
            {
                var notificationSubjectTokens = notificationTokenization.ReturnTokens(replacedNotificationSubject);
                foreach (var notificationToken in notificationSubjectTokens)
                {
                    if (!values.TryGetValue(notificationToken, out var value))
                    {
                        continue;
                    }

                    var notificationReplaceToken = $"[@{notificationToken}@]";
                    replacedNotificationSubject =
                        replacedNotificationSubject.Replace(notificationReplaceToken, value);
                }
            }

            var replacedNotificationBody = notificationBody;
            if (!String.IsNullOrEmpty(replacedNotificationBody))
            {
                var notificationBodyTokens = notificationTokenization.ReturnTokens(replacedNotificationBody);
                foreach (var notificationToken in notificationBodyTokens)
                {
                    if (!values.TryGetValue(notificationToken, out var value))
                    {
                        continue;
                    }

                    var notificationReplaceToken = $"[@{notificationToken}@]";
                    replacedNotificationBody =
                        replacedNotificationBody.Replace(notificationReplaceToken, value);
                }
            }

            if (notificationType == 1)
            {
                var sendMail = new SendMail(dynamicEnvironment, log);
                sendMail.Send(replacedNotificationDestination, replacedNotificationSubject,
                    replacedNotificationBody);
            }
            else
            {
                var sendSms = new SendSms(dynamicEnvironment, log);
                sendSms.Send(replacedNotificationDestination, replacedNotificationBody);
            }
        }
    }
}
