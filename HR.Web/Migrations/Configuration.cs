using System.Data.Entity.Migrations;

namespace HR.Web.Migrations
{
    internal sealed class Configuration : DbMigrationsConfiguration<HR.Web.Data.HrContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = true;
            AutomaticMigrationDataLossAllowed = false;
        }

        protected override void Seed(HR.Web.Data.HrContext context)
        {
            // Seed method left intentionally empty — Global.asax performs demo seeding after migrations.
        }
    }
}
