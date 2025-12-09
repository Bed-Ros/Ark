using System;
using System.Runtime.Serialization;

namespace Ark.Models
{
    public enum AuditState
    {
        Create,
        Update,
        Delete,
    }

    public class Audit
    {
        public long Id { get; set; }
        public string UserName { get; set; } = null!;
        public DateTime DateTime { get; set; }
        public string TableName { get; set; } = null!;
        public string State { get; set; } = null!;
        public string Keys { get; set; } = null!;
        public string? OldValues { get; set; }
        public string? NewValues { get; set; }
    }
}
