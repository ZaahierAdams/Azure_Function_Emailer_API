using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EmailerAPI.Repository.Models
{
    public partial class EmailRecord
    {
        [StringLength(100)]
        public string Id { get; set; }
        public long? ConsumerId { get; set; }
        public string JobId { get; set; }
        [StringLength(10)]
        public string Status { get; set; }
        public DateTime? DateSent { get; set; }
        public string Recipents { get; set; }
        [Column("Recipents_CC")]
        public string RecipentsCc { get; set; }
        [Column("Recipents_BCC")]
        public string RecipentsBcc { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public string Footer { get; set; }

        [ForeignKey("ConsumerId")]
        [InverseProperty("EmailRecord")]
        public virtual Consumer Consumer { get; set; }
    }
}
