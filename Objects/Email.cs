using MimeKit;
using Newtonsoft.Json;
//using System.Text.Json.Serialization;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace EmailerAPI.Objects
{
    public class Email
    {
        
        [JsonProperty("appid")]
        public string AppId { get; set; }


        [JsonProperty("recipients")]
        //public List<MailboxAddress> Recipents { get; set; }
        public string[] Recipents { get; set; }


        [JsonProperty("cc")]
        public string[] Recipents_CC { get; set; }


        [JsonProperty("bcc")]
        public string[] Recipents_BCC { get; set; }


        [JsonProperty("subject")]
        [RegularExpression(@"^.{1,100}$")]
        public string Subject { get; set; }
  

        [JsonProperty("body")]
        [RegularExpression(@"^.{1,384000}$")]
        public string Body { get; set; }


        [JsonProperty("footer")]
        [RegularExpression(@"^.{1,255}$")]
        public string Footer { get; set; }

        //[JsonProperty("bodyformat")]
        //public string BodyFormat { get; set; }


        //[JsonIgnore]
        [Key]
        public string Id { get; set; }

        public string Status { get; set; }

        [JsonIgnore]
        public DateTime DateSent { get; set; }

        [JsonIgnore]
        public string JobId { get; set; }




    }
}
