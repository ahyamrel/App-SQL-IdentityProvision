$vmObjectId = "920f42e2-969d-44b2-9aa4-6b0810233678"

connect-azuread
$graph = Get-AzureADServicePrincipal -Filter "AppId eq '00000003-0000-0000-c000-000000000000'"
$userReadWriteAllPermission = $graph.AppRoles `
    | where Value -Like "User.ReadWrite.All" `
    | Select-Object -First 1

$msi = Get-AzureADServicePrincipal -ObjectId $vmObjectId

New-AzureADServiceAppRoleAssignment -Id $userReadWriteAllPermission.Id -ObjectId $msi.ObjectId -PrincipalId $msi.ObjectId -ResourceId $graph.ObjectId 