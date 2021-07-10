using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EmailerAPI.Repository.Models
{
    public partial class Consumer
    {
        public Consumer()
        {
            EmailRecord = new HashSet<EmailRecord>();
        }

        public long Id { get; set; }
        [Required]
        [StringLength(250)]
        public string ConsumerIdentifier { get; set; }
        [Required]
        [StringLength(250)]
        public string ConsumerName { get; set; }
        [StringLength(128)]
        public string Password { get; set; }
        [StringLength(20)]
        public string ConsumerShortname { get; set; }
        public bool IsDeleted { get; set; }
        public int Version { get; set; }
        public bool AllowSms { get; set; }
        public bool AllowEmail { get; set; }

        [InverseProperty("Consumer")]
        public virtual ICollection<EmailRecord> EmailRecord { get; set; }
    }
}
