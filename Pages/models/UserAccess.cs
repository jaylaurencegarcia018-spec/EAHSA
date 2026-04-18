using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace EAHSA.Models
{
    [Table("users")]
    public class UserAccess : BaseModel
    {
        [PrimaryKey("id", false)]
        public int Id { get; set; }

        [Column("email")]
        public string? Email { get; set; }

        [Column("password")]
        public string? Password { get; set; }

        [Column("allowedgrades")]
        public string? AllowedGrades { get; set; }

        [Column("allowedsections")]
        public string? AllowedSections { get; set; }

        [Column("role")]
        public string? Role { get; set; }
    }
}