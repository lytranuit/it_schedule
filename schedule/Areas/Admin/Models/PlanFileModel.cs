using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace it.Areas.Admin.Models
{
    [Table("tbl_plan_file")]
    public class PlanFileModel
    {
        [Key]
        public int id { get; set; }

        public string department_name { get; set; }
        public string? department_name_en { get; set; }
        public string? file_url { get; set; }
        public int? plan_id { get; set; }

        public int? year { get; set; }
        public List<PlanFileEquipmentModel> equipments { get; set; }
        public DateTime created_at { get; set; }

        public DateTime? updated_at { get; set; }

        public DateTime? deleted_at { get; set; }
    }
}
