using it.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using it.Areas.Admin.Models;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Newtonsoft.Json;

namespace it.Data
{
	public class ItContext : DbContext
	{
		private IActionContextAccessor actionAccessor;
		private UserManager<UserModel> UserManager;
		public ItContext(DbContextOptions<ItContext> options, UserManager<UserModel> UserMgr, IActionContextAccessor ActionAccessor) : base(options)
		{
			actionAccessor = ActionAccessor;
			UserManager = UserMgr;

		}

		public DbSet<UserModel> UserModel { get; set; }
		public DbSet<PlanModel> PlanModel { get; set; }
		public DbSet<PlanFileModel> PlanFileModel { get; set; }
		public DbSet<PlanFileEquipmentModel> PlanFileEquipmentModel { get; set; }
		public DbSet<ScheduleModel> ScheduleModel { get; set; }
		public DbSet<DepartmentModel> DepartmentModel { get; set; }
		public DbSet<EquipmentModel> EquipmentModel { get; set; }
		public DbSet<UserEquipmentModel> UserEquipmentModel { get; set; }
		public DbSet<AuditTrailsModel> AuditTrailsModel { get; set; }
		//public override int SaveChanges()
		//{
		//    var audit = new Audit();
		//    audit.PreSaveChanges(this);
		//    var rowAffecteds = base.SaveChanges();
		//    audit.PostSaveChanges();

		//    base.SaveChanges();

		//    return rowAffecteds;
		//}
		//public DbSet<User2Model> User2Model { get; set; }
		//public virtual async Task<int> SaveChangesAsync()
		//{
		//    var audit = new Audit();
		//    audit.CreatedBy = "ZZZ Projects"; // Optional
		//    //OnBeforeSaveChanges();
		//    var result = await base.SaveChangesAsync();
		//    var entries = audit.Entries;
		//    return result;
		//}
		public override int SaveChanges()
		{
			OnBeforeSaveChanges();
			return base.SaveChanges();
		}
		private void OnBeforeSaveChanges()
		{
			ChangeTracker.DetectChanges();
			var auditEntries = new List<AuditEntry>();
			var user_http = actionAccessor.ActionContext.HttpContext.User;
			var user_id = UserManager.GetUserId(user_http);
			var changes = ChangeTracker.Entries();
			foreach (var entry in changes)
			{
				if (entry.Entity is AuditTrailsModel || entry.State == EntityState.Detached || entry.State == EntityState.Unchanged)
					continue;

				var auditEntry = new AuditEntry(entry);
				auditEntry.TableName = entry.Entity.GetType().Name;
				auditEntry.UserId = user_id;
				auditEntries.Add(auditEntry);
				foreach (var property in entry.Properties)
				{

					string propertyName = property.Metadata.Name;
					if (property.Metadata.IsPrimaryKey())
					{
						auditEntry.KeyValues[propertyName] = property.CurrentValue;
						continue;
					}
					switch (entry.State)
					{
						case EntityState.Added:
							auditEntry.AuditType = AuditType.Create;
							auditEntry.NewValues[propertyName] = property.CurrentValue;
							break;
						case EntityState.Deleted:
							auditEntry.AuditType = AuditType.Delete;
							auditEntry.OldValues[propertyName] = property.OriginalValue;
							break;
						case EntityState.Modified:
							if (property.IsModified)
							{
								var Original = entry.GetDatabaseValues().GetValue<object>(propertyName);
								var Current = property.CurrentValue;
								if (JsonConvert.SerializeObject(Original) == JsonConvert.SerializeObject(Current))
									continue;
								auditEntry.ChangedColumns.Add(propertyName);
								auditEntry.AuditType = AuditType.Update;
								auditEntry.OldValues[propertyName] = Original;
								auditEntry.NewValues[propertyName] = Current;

							}
							break;
					}

				}
			}
			foreach (var auditEntry in auditEntries)
			{
				AuditTrailsModel.Add(auditEntry.ToAudit());
			}
		}

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			//modelBuilder.Entity<IdentityUser>().ToTable("AspNetUsers");

			//modelBuilder.Entity<DocumentModel>().HasMany(l => l.Teams).WithOne().HasForeignKey("LeagueId");

		}
		protected override void ConfigureConventions(ModelConfigurationBuilder builder)
		{
		}
	}
}
