using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace it.Areas.Admin.Models
{
	[Table("tbl_schedule")]
	public class ScheduleModel
	{
		[Key]
		public int id { get; set; }

		public string title { get; set; }
		public string? description { get; set; }
		public int? recurrenceID { get; set; }
		public string? recurrenceException { get; set; }
		public string? recurrenceRule { get; set; }
		public int? department_id { get; set; }
		public int? equipment_id { get; set; }
		public int? plan_id { get; set; }
		public DateTimeOffset? end_time { get; set; }
		public DateTimeOffset? start_time { get; set; }
		public string? user_id_created { get; set; }

		public string? type_plan { get; set; }
		public DateTime? date_plan { get; set; }
		public DateTime? created_at { get; set; }

		public DateTime? updated_at { get; set; }

		public DateTime? deleted_at { get; set; }
	}
}
