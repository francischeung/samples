#!/bin/bash

###
### This is sample code and is not intended to be used in production
###

# Adds user to Azure Active Directory group

helpFunction()
{
   echo ""
   echo "Usage: $0 -u userid -g groupname"
   echo -e "\t-u user's id or email address"
   echo -e "\t-g name of AAD group"
   
   exit 1 # Exit script after printing help
}

while getopts "u:g:" opt
do
   case "$opt" in
      m ) userid="$OPTARG" ;;
      g ) groupname="$OPTARG" ;;
      ? ) helpFunction ;; # Print helpFunction in case parameter is non-existent
   esac
done

# Print helpFunction in case parameters are empty
if [ -z "$userid" ] || [ -z "$groupname" ]
then
   echo "Some or all of the parameters are empty";
   helpFunction
fi

# Begin script in case all parameters are correct
#echo "$userid"
#echo "$groupname"

. serviceprincipalcreds.config

#echo "using the following credentials to login:" "$clientid" "$clientsecret" "$tenantid"
az login --service-principal -u "$clientid" -p "$clientsecret" --tenant "$tenantid"

userObjId=$(az ad user show --id "$userid" -o json --query objectId)
#echo "$userObjId"

az ad group member add --group $groupname --member-id $userObjId