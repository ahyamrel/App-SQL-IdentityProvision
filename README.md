# License-Retention
Code documantion to be completed..

## Syncronization process
![Syncronization process](https://i.imgur.com/nSNz91J.jpgif)

*	Authentication is based on Azure managed identity (passwordless).
*	User creation/update and mail notifications are based on Microsoft Graph.
*	SQL is Azure SQL.
*	Users created or updated will be stamped “ClickSync” in the state attribute.
*	Logs are written to Sync_Log table.


## Application Configuration
* Configure VM with AAD identity

![Configure the machine with AAD identity](https://i.imgur.com/z4iwORN.jpg)

* Assign permissions to Microsoft Graph to manage users

```powershell
$vmObjectId = "920f42e2-969d-44b2-9aa4-6b0810233678"
Connect-AzureAD
$graph = Get-AzureADServicePrincipal -Filter "AppId eq '00000003-0000-0000-c000-000000000000'"
$userReadWriteAllPermission = $graph.AppRoles `
    | where Value -Like "User.ReadWrite.All" `
    | Select-Object -First 1

$msi = Get-AzureADServicePrincipal -ObjectId $vmObjectId

New-AzureADServiceAppRoleAssignment -Id $userReadWriteAllPermission.Id -ObjectId $msi.ObjectId -PrincipalId $msi.ObjectId -ResourceId $graph.ObjectId  

```

* Assign permissions to Microsoft Graph to send emails

```powershell
$vmObjectId = "920f42e2-969d-44b2-9aa4-6b0810233678"
Connect-AzureAD
$graph = Get-AzureADServicePrincipal -Filter "AppId eq '00000003-0000-0000-c000-000000000000'"
$mailSend = $graph.AppRoles `
    | where Value -Like "Mail.Send" `
    | Select-Object -First 1

$msi = Get-AzureADServicePrincipal -ObjectId $vmObjectId

New-AzureADServiceAppRoleAssignment -Id $mailSend.Id -ObjectId $msi.ObjectId -PrincipalId $msi.ObjectId -ResourceId $graph.ObjectId 

```

* Assign permissions to Microsoft Graph to manage group membership

```powershell
$vmObjectId = "920f42e2-969d-44b2-9aa4-6b0810233678"
$graph = Get-AzureADServicePrincipal -Filter "AppId eq '00000003-0000-0000-c000-000000000000'"
$permissions = @("GroupMember.ReadWrite.All", "Group.ReadWrite.All", "Directory.ReadWrite.All")
$msi = Get-AzureADServicePrincipal -ObjectId $vmObjectId
foreach($permission in $permissions){
    $aPermission = $graph.AppRoles | where Value -Like $permission | Select-Object -First 1
    New-AzureADServiceAppRoleAssignment -Id $aPermission.Id -ObjectId $msi.ObjectId -PrincipalId $msi.ObjectId -ResourceId $graph.ObjectId 
} 

```

* Configure AAD admin in the SQL server 

![Configure AAD admin in the SQL server ](https://i.imgur.com/DRkKgNR.png_)

* Connect to the SQL server using the AAD admin and give permissions to the VM

```sql
CREATE USER clicksrv FROM EXTERNAL PROVIDER
ALTER ROLE db_datareader ADD MEMBER clicksrv
ALTER ROLE db_datawriter ADD MEMBER clicksrv

```

## Troubleshooting
To be added

## Test results
To be added
