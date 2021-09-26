using Microsoft.Extensions.Configuration;
using NUnit.Framework;

namespace CosmosDbTest
{
   [SetUpFixture]
   public static class TestSuite
   {
      static TestSuite()
      {
         var builder = new ConfigurationBuilder();
         const string environmentName = "Local";

         builder
            // .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: true)
            // .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true)
            ;

         Configuration = builder.Build();
      }

      public static readonly IConfiguration Configuration;
   }
}
