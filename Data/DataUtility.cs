using AddressBook.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AddressBook.Data
{
    public static class DataUtility
    {
        public static string GetConnectionString(IConfiguration configuration)
        {
            string? connectionString = configuration.GetConnectionString("DefaultConnection");
            string? databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");

            return string.IsNullOrEmpty(databaseUrl) ? connectionString! : BuildConnectionString(databaseUrl);
        }
        private static string BuildConnectionString(string databaseUrl)
        {
            var databaseUri = new Uri(databaseUrl);
            var userInfo = databaseUri.UserInfo.Split(':');
            var builder = new NpgsqlConnectionStringBuilder()
            {
                Host = databaseUri.Host,
                Port = databaseUri.Port,
                Username = userInfo[0],
                Password = userInfo[1],
                Database = databaseUri.LocalPath.TrimStart('/'),
                SslMode = SslMode.Require,
                TrustServerCertificate = true
            };
            return builder.ToString();
        }

        public static async Task ManageDataAsync(IServiceProvider svcProvider)
        {
            var dbContextSvc = svcProvider.GetRequiredService<ApplicationDbContext>();
            var userManagerSvc = svcProvider.GetRequiredService<UserManager<AppUser>>();
            var configurationSvc = svcProvider.GetRequiredService<IConfiguration>();

            //Align the database by checking Migration
            await dbContextSvc.Database.MigrateAsync();

            //Seed Demo User(s)
            await SeedDemoUsersAsync(userManagerSvc, configurationSvc);
        }

        //Demo Users Seed Method
        private static async Task SeedDemoUsersAsync(UserManager<AppUser> userManagerSvc, IConfiguration configuration)
        {
            string? demoEmail = configuration["DemoLoginEmail"] ?? Environment.GetEnvironmentVariable("DemoLoginEmail");
            string? demoPassword = configuration["DemoLoginPassword"] ?? Environment.GetEnvironmentVariable("DemoLoginPassword");

            AppUser demoUser = new AppUser()
            {
                UserName = demoEmail,
                Email = demoEmail,
                FirstName = "Demo",
                LastName = "User",
                EmailConfirmed = true
            };

            try
            {
                AppUser? user = await userManagerSvc.FindByEmailAsync(demoUser.Email!);

                if(user == null)
                {
                    await userManagerSvc.CreateAsync(demoUser,demoPassword!);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("*************  ERROR  *************");
                Console.WriteLine("Error Seeding Demo Login User.");
                Console.WriteLine(ex.Message);
                Console.WriteLine("***********************************");
                throw;
            }
        }
    }
}
