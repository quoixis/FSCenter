using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSCenter.Models
{
    [Table("memberships")]
    public class Membership
    {
        [Key, Column("membership_id")] public int MembershipId { get; set; }
        [Required, Column("client_id")] public int ClientId { get; set; }
        [Required, Column("club_id")] public int ClubId { get; set; }
        [Required, Column("sessions_total")] public int SessionsTotal { get; set; }
        [Required, Column("sessions_remaining")] public int SessionsRemaining { get; set; }
        [Required, Column("start_date")] public string StartDate { get; set; } = "";
        [Column("expiry_date")] public string? ExpiryDate { get; set; }
        [Column("status")] public string Status { get; set; } = "Активний";
        [ForeignKey("ClientId")] public Client Client { get; set; } = null!;
        [ForeignKey("ClubId")] public Club Club { get; set; } = null!;
        public ICollection<Visit> Visits { get; set; } = new List<Visit>();
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}
