# Credential Expiration Alert Proof of Concept
## This code is a proof of concept and should not deployed directly into production.

This code is intended to be deployed into an Azure Logic App. The Logic App needs to have a managed identity that is given read access to the [Azure Graph API](https://docs.microsoft.com/en-us/azure/app-service/scenario-secure-app-access-microsoft-graph-as-app?tabs=azure-cli%2Ccommand-line#grant-access-to-microsoft-graph). However make sure to update the script to give "Application.Read.All" role to the managed identity.