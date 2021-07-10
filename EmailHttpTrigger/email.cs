// Developed: Zaahier Adams
// https://github.com/ZaahierAdams 

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using EmailerAPI.Objects;
using System.Data;
using System.Linq;
using EmailerAPI.Repository.Models;
using System.Collections.Generic;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using System.Net;
using Microsoft.OpenApi.Models;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;

namespace EmailerAPI
{


    public static class email
    {


        [OpenApiOperation(operationId: "email", 
            tags: new[] { "Email" }, 
            Summary = "TMSS Email sender", 
            Description = "Sends Emails", 
            Visibility = OpenApiVisibilityType.Important)]

        [OpenApiRequestBody("application/json", typeof(Email))]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.Accepted, contentType: "text/plain", bodyType: typeof(string),
            Summary = "Accepted",
            Description = "Request has been accepted for further processing.")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/plain", bodyType: typeof(string), 
            Summary = "Success", 
            Description = "Your request was received successfully")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "text/plain", bodyType: typeof(string),
            Summary = "Bad Request",
            Description = "Invalid input")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.Unauthorized, contentType: "text/plain", bodyType: typeof(string),
            Summary = "Unauthorized",
            Description = "Requested resource requires authentication")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.RequestTimeout, contentType: "text/plain", bodyType: typeof(string),
            Summary = "Unauthorizedt",
            Description = "Client did not send a request within the time the server was expecting the request.")]

        [FunctionName("email")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, /*"get", */"post", Route = null)] HttpRequest req,
            [ServiceBus("!!!_FUNCTION_APP_!!!", Connection = "!!!_SQB_CONNECTION_STRING_!!!")] ICollector<string> outputSBQueue,
            ILogger log)
        {

            // Log request
            log.LogInformation(" - C# HTTP trigger function processed a request.");


            // Response message sent to client 
            string responseMessage;

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();


            // Desearialize JSON into Email.cs model 
            try
            {
                log.LogInformation(" - Deserializing Json");
                log.LogInformation(requestBody);
                Email data = JsonConvert.DeserializeObject<Email>(requestBody);

                if (data == null) throw new ArgumentException("Request could not be deserialized");

                if (String.IsNullOrEmpty(requestBody))
                {
                    responseMessage = "No valid message or invalid message format";
                    var response = new BadRequestObjectResult(responseMessage);
                    return response;
                }


                // Model validation
                log.LogInformation(" - Checking validity of all fields");
                if (data.Recipents.Length > 0
                    && !String.IsNullOrEmpty(data.Subject)
                    && !String.IsNullOrEmpty(data.Body)
                    && ValidateAppId(data.AppId, log))
                {

                    // Check the validy of all Email addresses provided 
                    // If a single email is invalid entire operation breaks
                    List<string[]> EmailArraysList = new List<string[]>();
                    EmailArraysList.Add(data.Recipents);
                    if (data.Recipents_CC != null)
                    {
                        EmailArraysList.Add(data.Recipents_CC);
                    }
                    if (data.Recipents_BCC != null)
                    {
                        EmailArraysList.Add(data.Recipents_BCC);
                    }
                    foreach (var EmailArray in EmailArraysList)
                    {
                        foreach (var EmailAddress in EmailArray)
                        {
                            if (!IsValidEmail(EmailAddress))
                            {
                                var response = InvalidEmailProtocol(log);
                                return response;
                            }
                        }
                    }
                }

                else
                {
                    responseMessage = "JSON invalid.";
                    var response = new BadRequestObjectResult(responseMessage);
                    response.StatusCode = 460;
                    return response;
                }


            }
            catch (Exception ex)
            {
                // Add more information 
                responseMessage = "Missing or invalid query string parameters";
                var response = new BadRequestObjectResult(responseMessage);
                return response;
            }

            // Add an id to Email Job Record
            string RecordId = Guid.NewGuid().ToString();
            Email Data_ = JsonConvert.DeserializeObject<Email>(requestBody);
            Data_.Id = RecordId;
            string UpdatedData = JsonConvert.SerializeObject(Data_);


            // Save record of job to db
            string statusDescrip = "Pending";
            try
            {
                using var ctx = new TestdbContext();
                ctx.Database.AutoTransactionsEnabled = true;
                var ConsumerRecord = ctx.Consumer.Find(Convert.ToInt64(Data_.AppId));
                EmailRecord newEmailRecord = new EmailRecord()
                {
                    Id = RecordId,
                    Consumer = ConsumerRecord,
                    ConsumerId = Convert.ToInt64(Data_.AppId),

                    Recipents = String.Join(",", Data_.Recipents),
                    RecipentsCc = String.Join(",", Data_.Recipents_CC),
                    RecipentsBcc = String.Join(",", Data_.Recipents_BCC),
                    Subject = Data_.Subject,
                    Body = Data_.Body,
                    Footer = Data_.Footer,

                    Status = statusDescrip,
                };

                // Async.
                //await ctx.EmailRecord.AddAsync(newEmailRecord);
                //var result = ctx.SaveChangesAsync();

                // Sync.
                ctx.EmailRecord.Add(newEmailRecord);
                var result = ctx.SaveChanges();

                log.LogInformation($" - Wrote {statusDescrip} email record to dbo");
            }
            catch
            {
                log.LogInformation($" - FAILED to write {statusDescrip} email record to dbo");
            }

            responseMessage = "Your request was received successfully";
            outputSBQueue.Add(UpdatedData);
            log.LogInformation(" - Added request to SBQ");

            return new OkObjectResult(responseMessage);

        }


        // Method - Verify ConsumerID 
        public static bool ValidateAppId(string appId, ILogger log)
        {
            try
            {
                using var ctx = new TestdbContext();
                log.LogInformation(" - Successfully connected to DATABASE_NAME");
                log.LogInformation($" - Validating Consumer Id {appId}");
                var result = from Consumer_Record in ctx.Consumer
                             where Consumer_Record.Id.ToString() == appId
                             select Consumer_Record;

                // Consumer Id found
                if (result != null)
                {
                    log.LogInformation($" - Consumer Id {appId} Found");
                    return true;
                }

                // Cosumer Id NOT found
                else
                {
                    log.LogInformation($" - Consumer Id {appId} NOT Found!");
                    return false;
                }
            }
            catch (Exception ex)
            {
                log.LogInformation(ex.Message);
                return false;
            }
        }


        // Method - Validate Email Format 
        static bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        // Method - Protocol for Invalid Email
        public static BadRequestObjectResult InvalidEmailProtocol(ILogger log)
        {
            log.LogInformation(" - Found invalid email address");
            string responseMessage = "Invalid Email address found! Operation was closed.";
            var response = new BadRequestObjectResult(responseMessage);
            response.StatusCode = 461;
            return response;
        }
    }
}
