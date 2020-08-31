# IPAddressUtilization Proof of Concept


## This code is a proof of concept and should not deployed directly into production.

This Azure function is timer triggered and needs the following app settings:
* SubscriptionIds (Comma delimited list of Azure subscription Ids)
* LogAnalyticsWorkspaceId (Id of Log Analytics Workspace)
* LogAnalyticsWorkspaceSharedKey (Key for Log Analytics Workspace)

This Azure function requires that its system managed identity is enabled and that the "User Access Administrator" role be granted to this identity.