using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSCenter.Models
{
    [Table("rooms")]
    public class Room
    {
        [Key, Column("room_id")] public int RoomId { get; set; }
        [Required, Column("room_number")] public string RoomNumber { get; set; } = "";
        [Required, Column("name")] public string Name { get; set; } = "";
        [Column("area")] public double? Area { get; set; }
        [Column("capacity")] public int? Capacity { get; set; }
        [Column("status")] public string Status { get; set; } = "Вільна";
        [Column("description")] public string? Description { get; set; }
        public ICollection<Club> Clubs { get; set; } = new List<Club>();
    }
}
