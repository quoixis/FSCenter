using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSCenter.Models
{
    [Table("visits")]
    public class Visit
    {
        [Key, Column("visit_id")] public int VisitId { get; set; }
        [Required, Column("membership_id")] public int MembershipId { get; set; }
        [Column("visit_date")] public string? VisitDate { get; set; }
        [Column("notes")] public string? Notes { get; set; }
        [ForeignKey("MembershipId")] public Membership Membership { get; set; } = null!;
    }
}
