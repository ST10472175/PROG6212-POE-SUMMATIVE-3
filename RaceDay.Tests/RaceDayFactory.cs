using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RaceDay.Api.Data;

namespace RaceDay.Tests
{//start of namespace

    public class RaceDayFactory : WebApplicationFactory<Program>
    {//start of class

        //every factory gets its own empty database
        private readonly string _databaseName = "RaceDayTests-" + Guid.NewGuid().ToString();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {//start of method

            builder.UseEnvironment("Testing");

            //set the organiser code that the tests use
            builder.ConfigureAppConfiguration((context, config) =>
            {
                Dictionary<string, string> settings = new Dictionary<string, string>();
                settings["Registration:OrganiserCode"] = TestHelper.OrganiserCode;
                config.AddInMemoryCollection(settings);
            });

            //swap SQL Server for the in-memory database
            builder.ConfigureServices(services =>
            {
                // EF Core 8 does not expose IDbContextOptionsConfiguration<TContext>.
                // Remove the complete production context/options registration before
                // replacing SQL Server with InMemory. Removing only the typed options
                // can leave the original context registration in the test container.
                services.RemoveAll<RaceDayContext>();
                services.RemoveAll<DbContextOptions<RaceDayContext>>();
                services.RemoveAll<DbContextOptions>();

                // Register one isolated in-memory database for this test factory.
                services.AddDbContext<RaceDayContext>(options => options.UseInMemoryDatabase(_databaseName));
            });

        }//end of method

    }//end of class
}//end of namespace
