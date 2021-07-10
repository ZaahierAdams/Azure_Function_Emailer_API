# Bulk Emailer API Sendgrid

## About
*Azure function* API for sending emails using *Sendgrid* service (or) *MimeKit*. 
- **Web service:** Microsoft Azure 
- **Framework:** ASP.NET Core 

## How it works 
- An HTTP Triggered function ```email.cs``` processes request and saves a dbo as “Pending” 
- *Service Bus Queue Trigger* ``` SQBEmailTrigger.cs``` picks up the message and sends email to either *Sendgrid* (or) uses *MimeKit* to send an email. Currently it set to use *Sendgrid’s* service
- Updates the dbo with returned status of job by *Sendgrid*.  

## Setting up
### Configurations:
There are plenty of configurations required: 
- Configure **Service Bus Queue** in:
	- HTTPTrigger function
	- SBQTrigger function (and corresponding ```function.json``` file)
- ```local.settings.json```
- Sendgrid and/or MimeKit details in SBQTrigger function 
- Reconfigure code to utilize your own scaffolded DB Context

### Running locally:
See ```EmailerAPI.csproj``` for required NuGet packages

### Testing 
See ```swagger.json``` for required API endpoints



