using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace it.Areas.Admin.Models
{
    [Table("tbl_plan")]
    public class PlanModel
    {
        [Key]
        public int id { get; set; }

        public string name { get; set; }
        public string? name_en { get; set; }
        public string? code { get; set; }
        public int? department_id { get; set; }
        public DateTime? created_at { get; set; }

        public DateTime? updated_at { get; set; }

        public DateTime? deleted_at { get; set; }
    }
}
