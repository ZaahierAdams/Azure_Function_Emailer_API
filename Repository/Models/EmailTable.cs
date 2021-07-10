using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EmailerAPI.Repository.Models
{
    public partial class EmailTable
    {
        [StringLength(10)]
        public string AppId { get; set; }
        public string Recipents { get; set; }
        [Column("Recipents_CC")]
        public string RecipentsCc { get; set; }
        [Column("Recipents_BCC")]
        public string RecipentsBcc { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public string Footer { get; set; }
        [StringLength(100)]
        public string Id { get; set; }
        [StringLength(10)]
        public string Status { get; set; }
        public DateTime? DateSent { get; set; }
        public string JobId { get; set; }
    }
}
