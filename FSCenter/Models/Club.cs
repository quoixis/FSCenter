using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSCenter.Models
{
    [Table("clubs")]
    public class Club
    {
        [Key, Column("club_id")] public int ClubId { get; set; }
        [Required, Column("name")] public string Name { get; set; } = "";
        [Column("description")] public string? Description { get; set; }
        [Column("trainer_id")] public int? TrainerId { get; set; }
        [Column("room_id")] public int? RoomId { get; set; }
        [Column("schedule")] public string? Schedule { get; set; }
        [Required, Column("price8sessions")] public double Price8Sessions { get; set; }
        [Required, Column("price12sessions")] public double Price12Sessions { get; set; }
        [Column("is_active")] public int IsActive { get; set; } = 1;
        [ForeignKey("TrainerId")] public Trainer? Trainer { get; set; }
        [ForeignKey("RoomId")] public Room? Room { get; set; }
        public ICollection<Membership> Memberships { get; set; } = new List<Membership>();
    }
}
