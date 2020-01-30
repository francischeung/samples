# Adds user (member-id) to Azure Active Directory group

#!/bin/bash

helpFunction()
{
   echo ""
   echo "Usage: $0 -m memberid -g groupname"
   echo -e "\t-m user's memberid"
   echo -e "\t-g name of AAD group"
   
   exit 1 # Exit script after printing help
}

while getopts "m:g:" opt
do
   case "$opt" in
      m ) memberid="$OPTARG" ;;
      g ) groupname="$OPTARG" ;;
      ? ) helpFunction ;; # Print helpFunction in case parameter is non-existent
   esac
done

# Print helpFunction in case parameters are empty
if [ -z "$memberid" ] || [ -z "$groupname" ]
then
   echo "Some or all of the parameters are empty";
   helpFunction
fi

# Begin script in case all parameters are correct
#echo "$memberid"
#echo "$groupname"
az ad group member add --group $groupname --member-id $memberid