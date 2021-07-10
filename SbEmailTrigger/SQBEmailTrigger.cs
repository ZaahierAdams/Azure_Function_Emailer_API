using System;
using System.Data;
using System.Data.SqlClient;
using EmailerAPI.Objects;
// Developed: Zaahier Adams
// https://github.com/ZaahierAdams 

using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using MimeKit;
using MailKit;
using MailKit.Security;
using MailKit.Net.Smtp;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using SendGrid.Helpers.Mail;
using SendGrid;
using System.Net.Http.Headers;
using System.Linq;
using EmailerAPI.Repository.Models;

namespace EmailerAPI
{

    public class SBQEmailTrigger
    {
        [FunctionName("SBQEmailTrigger")]
        public async /*static*/ void Run(
            [ServiceBusTrigger("!!!_FUNCTION_APP_!!!", Connection = "!!!_SERVICE_BUS_CONNECTION_STRING_HERE_!!!")] string inputSBQueue,
            ILogger log)
        {
            log.LogInformation($" - C# ServiceBus queue trigger function processed message: {inputSBQueue}");
            Email message = JsonConvert.DeserializeObject<Email>(inputSBQueue);
            await SendGridEmailAsync(message, log);
        }


        // Sendgrid method
        public async Task SendGridEmailAsync(Email message, ILogger log)
        {

            var mailMessage = CreateSendGridMessage(message);

            var response = await SendGrid(mailMessage, log);

            TimeZoneInfo cstZone = TimeZoneInfo.FindSystemTimeZoneById("!!!_TIME_ZONE_HERE_!!!");
            DateTime timeUtc = DateTime.UtcNow;
            DateTime cstTime = TimeZoneInfo.ConvertTimeFromUtc(timeUtc, cstZone);
            message.DateSent = cstTime;

            IEnumerable<string> values;

            string jobid = default;
            try
            {
                if (response.Headers.TryGetValues("X-Message-ID", out values))
                {
                    string session = values.First();
                    message.JobId = session;
                }
                jobid = response.StatusCode.ToString();
            }
            catch
            {
            }
            message.Status = jobid;


            // Update email record in dbo
            int maxAttempts = 6;
            int wait = 10000; //milliseconds

            for (var i = 0; i < maxAttempts + 1; i++)
            {
                try
                {
                    using var ctx = new TestdbContext();

                    ctx.Database.AutoTransactionsEnabled = true;
                    var emailrec = ctx.EmailRecord.Find(message.Id);

                    emailrec.Status = message.Status;
                    emailrec.DateSent = message.DateSent;
                    emailrec.JobId = message.JobId;

                    var RecorUpdates = ctx.EmailRecord.Attach(emailrec);
                    RecorUpdates.State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                    var result = await ctx.SaveChangesAsync();

                    log.LogInformation($" - Wrote {result} enteries to EmailTable");

                    break;
                }
                catch
                {
                    log.LogInformation($" - No record found!");
                    System.Threading.Thread.Sleep(wait);
                }
            }

        }



        // Sendgrid method - SendGrid object 
        private SendGridMessage CreateSendGridMessage(Email email)
        {
            if (email.Subject == null) throw new ArgumentException("No Email Subject Provided");
            if (email.Body == null) throw new ArgumentException("No Email Body Provided");

            List<EmailAddress> recepients = new List<EmailAddress>();
            List<EmailAddress> recepients_CC = new List<EmailAddress>();
            List<EmailAddress> recepients_BCC = new List<EmailAddress>();


            if (email.Recipents != null)
            {
                foreach (string email_add in email.Recipents)
                {
                    recepients.Add(new EmailAddress(email_add, email_add));
                }
            }
            else
            {
                throw new ArgumentException("No recepients provided");
            }
            if (email.Recipents_CC != null)
            {
                foreach (string email_add in email.Recipents_CC)
                {
                    recepients_CC.Add(new EmailAddress(email_add, email_add));
                }
            }
            if (email.Recipents_BCC != null)
            {
                foreach (string email_add in email.Recipents_BCC)
                {
                    recepients_BCC.Add(new EmailAddress(email_add, email_add));
                }
            }


            var emailMessage = new SendGridMessage();
            emailMessage.From = new EmailAddress("!!!_SENDER_EMAIL_HERE_!!!", "!!!_NAME_!!!");


            emailMessage.AddTos(recepients.Distinct().ToList());
            if (recepients_CC.Count != 0)
            {
                emailMessage.AddCcs(recepients_CC.Distinct().ToList());
            }
            if (recepients_BCC.Count != 0)
            {
                emailMessage.AddBccs(recepients_BCC.Distinct().ToList());
            }

            emailMessage.Subject = email.Subject;
            emailMessage.PlainTextContent = email.Body;
            emailMessage.HtmlContent = email.Body;


            if (email.Footer != null)
            {
                emailMessage.SetFooterSetting(true, email.Footer, email.Footer);
            }

            return emailMessage;
        }



        // Sendgrid method - send email 
        private async Task<Response> SendGrid(SendGridMessage mailMessage, ILogger log)
        {
            Response response = default;
            try
            {
                var client = new SendGridClient("!!!_SENDGRID_USER_SECRET_HERE_!!!");
                response = await client.SendEmailAsync(mailMessage);

                HttpHeaders headers = response.Headers;



            }
            catch
            {

                return response;
                throw;
            }
            finally
            {
            }
            log.LogInformation($"C# ServiceBus queue trigger function processed message: {mailMessage}");
            return response;
        }

        // SMTP method
        public async Task SendEmailAsync(Email message, ILogger log)
        {
            var mailMessage = CreateEmailMessage(message);

            await SendAsync(mailMessage, log);
        }

        // SMTP method - Email Message Object 
        private MimeMessage CreateEmailMessage(Email email)
        {
            List<MailboxAddress> recepients = new List<MailboxAddress>();
            foreach (string email_add in email.Recipents)
            {
                recepients.Add(new MailboxAddress(email_add, email_add));
            }
            var emailMessage = new MimeMessage();
            emailMessage.From.Add(new MailboxAddress("!!!_SENDER_EMAIL_ADDRESS_HERE_!!!"));
            emailMessage.To.AddRange(recepients);
            emailMessage.Subject = email.Subject;
            emailMessage.Body = new TextPart("html") { Text = email.Body };
            return emailMessage;
        }

        // SMTP method - Send Email 
        private async Task SendAsync(MimeMessage mailMessage, ILogger log)
        {
            using (var client = new SmtpClient())
            {
                try
                {

                    client.CheckCertificateRevocation = false;

                    await client.ConnectAsync("!!!_SMTP_SERVICE_HERE_!!!", 465, true /*SecureSocketOptions.SslOnConnect*/);
                    //client.AuthenticationMechanisms.Remove("XOAUTH2");
                    await client.AuthenticateAsync("!!!_SENDER_EMAIL_ADDRESS_HERE_!!!", "!!!_PASSWORD_HERE_!!!");
                    await client.SendAsync(mailMessage);
                }
                catch
                {
                    //log an error message or throw an exception, or both.
                    throw;
                }
                finally
                {
                    await client.DisconnectAsync(true);
                    client.Dispose();
                }


                log.LogInformation($"C# ServiceBus queue trigger function processed message: {mailMessage}");

            }
        }


    }
}
