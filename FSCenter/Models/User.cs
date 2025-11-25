using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSCenter.Models
{
    [Table("users")]
    public class User
    {
        [Key, Column("user_id")] public int UserId { get; set; }
        [Required, Column("username")] public string Username { get; set; } = "";
        [Required, Column("passwordh")] public string PasswordHash { get; set; } = "";
        [Required, Column("full_name")] public string FullName { get; set; } = "";
        [Required, Column("phone")] public string Phone { get; set; } = "";
        [Required, Column("email")] public string Email { get; set; } = "";
        [Column("created_at")] public string? CreatedAt { get; set; }
    }
}
