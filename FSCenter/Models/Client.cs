using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSCenter.Models
{
    [Table("clients")]
    public class Client
    {
        [Key, Column("client_id")] public int ClientId { get; set; }
        [Required, Column("full_name")] public string FullName { get; set; } = "";
        [Required, Column("phone")] public string Phone { get; set; } = "";
        [Column("email")] public string? Email { get; set; }
        [Column("age")] public int? Age { get; set; }
        [Column("address")] public string? Address { get; set; }
        [Column("registered_at")] public DateTime? RegisteredAt { get; set; }
        [Column("is_active")] public int IsActive { get; set; } = 1;
        public ICollection<Membership> Memberships { get; set; } = new List<Membership>();
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}
