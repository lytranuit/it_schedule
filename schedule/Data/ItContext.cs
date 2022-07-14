using it.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using it.Areas.Admin.Models;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.EntityFrameworkCore.ChangeTracking;
namespace it.Data
{
    public class ItContext : DbContext
    {
        public ItContext(DbContextOptions<ItContext> options) : base(options)
        {
        }

        public DbSet<UserModel> UserModel { get; set; }
        public DbSet<PlanModel> PlanModel { get; set; }
        public DbSet<PlanFileModel> PlanFileModel { get; set; }
        public DbSet<PlanFileEquipmentModel> PlanFileEquipmentModel { get; set; }
        public DbSet<ScheduleModel> ScheduleModel { get; set; }
        public DbSet<DepartmentModel> DepartmentModel { get; set; }
        public DbSet<EquipmentModel> EquipmentModel { get; set; }
        //public DbSet<User2Model> User2Model { get; set; }
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
