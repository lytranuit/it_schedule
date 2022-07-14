using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace it.Areas.Admin.Models
{
    [Table("tbl_plan_file_equipment")]
    public class PlanFileEquipmentModel
    {
        [Key]
        public int id { get; set; }

        public int? equipment_id { get; set; }

        public int? plan_file_id { get; set; }
        [ForeignKey("plan_file_id")]
        public virtual PlanFileModel plan_file { get; set; }

    }
}
