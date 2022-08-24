using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace it.Areas.Admin.Models
{
	[Table("tbl_user_equipment")]
	public class UserEquipmentModel
	{
		[Key]
		public int id { get; set; }

		public int? equipment_id { get; set; }

		public string? user_id { get; set; }
		[ForeignKey("user_id")]
		public virtual UserModel user { get; set; }

	}
}
