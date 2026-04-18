using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace EAHSA.Models
{
    [Table("sections")]
    public class Section : BaseModel
    {
        [PrimaryKey("id")]
        public int Id { get; set; }

        [Column("name")]
        public string SectionName { get; set; } = "";

        [Column("grade_level")]
        public int GradeLevel { get; set; }
    }
}