using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace EAHSA.Models
{
    [Table("attendance")]
    public class Attendance : BaseModel
    {
        [PrimaryKey("id", false)]
        public int Id { get; set; }

        [Column("gradelevel")]
        public string? GradeLevel { get; set; }

        [Column("section")]
        public string? Section { get; set; }

        [Column("gender")]
        public string? Gender { get; set; }

        [Column("name")]
        public string? Name { get; set; }

        [Column("lrn")]
        public string? LRN { get; set; }

        [Column("status")]
        public string? Status { get; set; }

        [Column("photo")]
        public string? Photo { get; set; }

        [Column("date")]
        public DateTime Date { get; set; }
    }
}