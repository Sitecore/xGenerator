Param (
  [Parameter()]
  [string]$Registry = ""
  ,
  [Parameter(
    HelpMessage = "Process Isolation to use when building images")]
  [string]$IsolationMode = "hyperv"
  ,
  [Parameter(
    HelpMessage = "Windows image version")]
  [string]$WindowsVersion = "ltsc2019"
  ,
  [Parameter(
    HelpMessage = "Sitecore version")]
  [string]$SitecoreVersion = "10.2.0"
)

$ErrorActionPreference = "Stop";

Write-Host "Preparing your Sitecore Containers environment!" -ForegroundColor Green

################################################
# Retrieve and import SitecoreDockerTools module
################################################

# Check for Sitecore Gallery
Import-Module PowerShellGet
$SitecoreGallery = Get-PSRepository | Where-Object { $_.Name -eq "SitecoreGallery" }
if (-not $SitecoreGallery) {
  Write-Host "Adding Sitecore PowerShell Gallery..." -ForegroundColor Green
  Register-PSRepository -Name SitecoreGallery -SourceLocation https://sitecore.myget.org/F/sc-powershell/api/v2 -InstallationPolicy Trusted -Verbose
  $SitecoreGallery = Get-PSRepository -Name SitecoreGallery
}
else {
  Write-Host "Updating Sitecore PowerShell Gallery url..." -ForegroundColor Yellow
  Set-PSRepository -Name $SitecoreGallery.Name -Source "https://sitecore.myget.org/F/sc-powershell/api/v2"
}

#Install and Import SitecoreDockerTools
$dockerToolsVersion = "10.0.5"
Remove-Module SitecoreDockerTools -ErrorAction SilentlyContinue
if (-not (Get-InstalledModule -Name SitecoreDockerTools -RequiredVersion $dockerToolsVersion -ErrorAction SilentlyContinue)) {
  Write-Host "Installing SitecoreDockerTools..." -ForegroundColor Green
  Install-Module SitecoreDockerTools -RequiredVersion $dockerToolsVersion -Scope CurrentUser -Repository $SitecoreGallery.Name
}
Write-Host "Importing SitecoreDockerTools..." -ForegroundColor Green
Import-Module SitecoreDockerTools -RequiredVersion $dockerToolsVersion

###############################
# Populate the environment file
###############################

Write-Host "Populating required demo team .env file values..." -ForegroundColor Green

$NanoserverVersion = $(if ($WindowsVersion -eq "ltsc2019") { "1809" } else { $WindowsVersion })

Set-DockerComposeEnvFileVariable "REGISTRY" -Value $Registry
Set-DockerComposeEnvFileVariable "SITECORE_VERSION" -Value $SitecoreVersion
Set-DockerComposeEnvFileVariable "ISOLATION" -Value $IsolationMode
Set-DockerComposeEnvFileVariable "WINDOWSSERVERCORE_VERSION" -Value $WindowsVersion
Set-DockerComposeEnvFileVariable "NANOSERVER_VERSION" -Value $NanoserverVersion

Write-Host "Done!" -ForegroundColor Green
