using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using ProofOfConcept.DatabricksAutomationFunction.Models;
using ProofOfConcept.DatabricksAutomationFunction.Services;

namespace ProofOfConcept.DatabricksAutomationFunction
{
    public class TimerTrigger
    {
        private readonly IUserGroupRepository userGroupRepository;
        private readonly IDatabricksWorkspaceConfigRepository databricksWorkspaceConfigRepository;
        private readonly IDatabricksAutomationService databricksAutomationService;

        public TimerTrigger(IUserGroupRepository userGroupRepository,
                            IDatabricksWorkspaceConfigRepository databricksWorkspaceConfigRepository,
                            IDatabricksAutomationService databricksAutomationService)
        {
            this.userGroupRepository = userGroupRepository;
            this.databricksWorkspaceConfigRepository = databricksWorkspaceConfigRepository;
            this.databricksAutomationService = databricksAutomationService;
        }

        [FunctionName("TimerTrigger")]
        public async Task Run([TimerTrigger("0 */5 * * * *")]TimerInfo myTimer, ExecutionContext executionContext, ILogger log)
        {
            log.LogInformation($"ProofOfConcept.DatabricksAutomationFunction TimerTrigger function executed at: {DateTime.Now}");
            
            var workspaceConfigurations = await databricksWorkspaceConfigRepository.GetWorkspaceConfigurationsAsync();

            foreach (var workspaceConfiguration in workspaceConfigurations)
            {
                var aadGroups = new List<AADGroup>();

                foreach (var groupConfiguration in workspaceConfiguration.groups)
                {
                    var aadGroup = await userGroupRepository.GetGroupMembershipAsync(groupConfiguration.AADGroupName);
                    await databricksAutomationService.SynchronizeGroupAsync(groupConfiguration, aadGroup);

                    aadGroups.Add(aadGroup);
                }
                await databricksAutomationService.RemoveOrphanUsersAsync(databricksAutomationService.GetFlatUserList(aadGroups), workspaceConfiguration);
            }
        }
    }
}
