using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSCenter.Models
{
    [Table("trainers")]
    public class Trainer
    {
        [Key, Column("trainer_id")] public int TrainerId { get; set; }
        [Required, Column("full_name")] public string FullName { get; set; } = "";
        [Required, Column("phone")] public string Phone { get; set; } = "";
        [Column("email")] public string? Email { get; set; }
        [Column("specialization")] public string? Specialization { get; set; }
        [Column("hire_date")] public string? HireDate { get; set; }
        [Column("is_active")] public int IsActive { get; set; } = 1;
        public ICollection<Club> Clubs { get; set; } = new List<Club>();
    }
}
