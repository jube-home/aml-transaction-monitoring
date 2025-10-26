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

namespace Jube.Engine.Helpers
{
    using System;
    using System.Net;
    using System.Net.Mail;
    using DynamicEnvironment;
    using log4net;

    public static class SendMail
    {
        public static void Send(string toEmail, string subject, string body, ILog log,
            DynamicEnvironment jubeEnvironment)
        {
            try
            {
                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"SMTP Client: Message received with email of {toEmail} subject of {subject} and body of {body} a connection will now be made to the SMTP server.");
                }

                var smtpServer = new SmtpClient
                {
                    UseDefaultCredentials = jubeEnvironment.AppSettings("SMTPUseDefaultCredentials")
                        .Equals("True", StringComparison.OrdinalIgnoreCase),
                    Credentials = new NetworkCredential(jubeEnvironment.AppSettings("SMTPUser"),
                        jubeEnvironment.AppSettings("SMTPPassword")),
                    Port = Int32.Parse(jubeEnvironment.AppSettings("SMTPPort")),
                    EnableSsl = jubeEnvironment.AppSettings("SMTPEnableSsl")
                        .Equals("True", StringComparison.OrdinalIgnoreCase),
                    Host = jubeEnvironment.AppSettings("SMTPHost")
                };

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"SMTP Client: The SMTP Server has been configured with parameters UseDefaultCredentials: {smtpServer.UseDefaultCredentials}, Port {smtpServer.Port}, EnableSsl {smtpServer.EnableSsl}, Host {smtpServer.Host} and user {jubeEnvironment.AppSettings("SMTPUser")}.");
                }

                var email = new MailMessage
                {
                    From = new MailAddress(jubeEnvironment.AppSettings("SMTPFrom"))
                };
                email.To.Add(toEmail);
                email.Subject = subject;
                email.IsBodyHtml = false;
                email.Body = body;

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"SMTP Client:  The message has been compiled containing to address of {toEmail}, subject of {subject}, message body of {body},  set html {email.IsBodyHtml} and is being sent from {jubeEnvironment.AppSettings("SMTPFrom")}.");
                }

                smtpServer.Send(email);

                if (log.IsInfoEnabled)
                {
                    log.Info("SMTP Client: Message has been sent.");
                }
            }
            catch (Exception ex)
            {
                log.Error($"SMTP Client: A message has failed to be sent with exception {ex}.");
            }
        }
    }
}
