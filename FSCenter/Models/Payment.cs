using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSCenter.Models
{
    [Table("payments")]
    public class Payment
    {
        [Key, Column("payment_id")] public int PaymentId { get; set; }
        [Required, Column("client_id")] public int ClientId { get; set; }
        [Column("membership_id")] public int? MembershipId { get; set; }
        [Required, Column("amount")] public double Amount { get; set; }
        [Column("payment_date")] public string? PaymentDate { get; set; }
        [Required, Column("payment_method")] public string PaymentMethod { get; set; } = "";
        [Required, Column("description")] public string Description { get; set; } = "";
        [ForeignKey("ClientId")] public Client Client { get; set; } = null!;
        [ForeignKey("MembershipId")] public Membership? Membership { get; set; }
    }
}
