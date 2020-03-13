using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
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
        public async Task Run([TimerTrigger("0 */1 * * * *")]TimerInfo myTimer, ExecutionContext executionContext, ILogger log)
        {
            log.LogInformation($"ProofOfConcept.DatabricksAutomationFunction TimerTrigger function executed at: {DateTime.Now}");
            
            var workspaceConfigurations = await databricksWorkspaceConfigRepository.GetWorkspaceConfigurationsAsync(executionContext);

            foreach (var workspaceConfiguration in workspaceConfigurations)
            {
                var groups = new List<Group>();

                foreach (var groupConfiguration in workspaceConfiguration.groups)
                {
                    var group = await userGroupRepository.GetGroupMembershipAsync(groupConfiguration.AADGroupName);
                    databricksAutomationService.SynchronizeGroup(groupConfiguration, group);

                    groups.Add(group);
                }
                databricksAutomationService.RemoveOrphanUsers(databricksAutomationService.GetUserList(groups), workspaceConfiguration);
            }
        }
    }
}
