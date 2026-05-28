using System;

namespace SRVS.Domain.Entities
{
    public class ResetRequest
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string SchoolId { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // Pending, Approved, Rejected
        public DateTime RequestedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
    }
}
