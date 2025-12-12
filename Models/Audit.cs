using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ark.Models
{
    public enum AuditState
    {
        Create,
        Update,
        Delete,
    }

    [Table("Audit")]
    public class Audit
    {
        [Key]
        [Column("Id")]
        public long Id { get; set; }
        [Column("UserName")]
        public string UserName { get; set; } = null!;
        [Column("DateTime")]
        public DateTime DateTime { get; set; }
        [Column("TableName")]
        public string TableName { get; set; } = null!;
        [Column("State")]
        public string State { get; set; } = null!;
        [Column("Keys")]
        public string Keys { get; set; } = null!;
        [Column("OldValues")]
        public string? OldValues { get; set; }
        [Column("NewValues")]
        public string? NewValues { get; set; }
    }
}
