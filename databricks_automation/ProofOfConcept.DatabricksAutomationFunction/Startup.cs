using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using ProofOfConcept.DatabricksAutomationFunction.Services;

[assembly: FunctionsStartup(typeof(ProofOfConcept.DatabricksAutomationFunction.Startup))]

namespace ProofOfConcept.DatabricksAutomationFunction
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddSingleton<IUserGroupRepository, UserGroupRepository>();
            builder.Services.AddSingleton<IDatabricksWorkspaceConfigRepository, DatabricksWorkspaceLocalConfigRepository>();
            builder.Services.AddSingleton<IDatabricksAutomationService, DatabricksAutomationService>();
        }
    }
}