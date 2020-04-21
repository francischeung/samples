using System;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(UserOnboardFunctionVSCode.Startup))]

namespace UserOnboardFunctionVSCode
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            var serviceProvider = builder.Services.BuildServiceProvider();
            var configuration = serviceProvider.GetService<IConfiguration>();

            builder.Services.AddHttpClient("RoleGroupMapping", c => 
            {
                c.BaseAddress = new Uri(configuration["RoleGroupMappingConfigUrl"]);
            });

        }
    }
}