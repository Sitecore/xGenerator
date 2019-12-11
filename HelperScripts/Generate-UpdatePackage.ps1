<#

.SYNOPSIS
This script generates a Sitecore update package out of the xGenerator build output

.PARAMETER ConfigurationFile
A cake-config.json file

#>

Param(
    [parameter(Mandatory = $true)]
    [string] $ConfigurationFile
)

###########################
# Find configuration files
###########################

Import-Module "$($PSScriptRoot)\ProcessConfigFile\ProcessConfigFile.psm1" -Force

$configuration = ProcessConfigFile -Config $ConfigurationFile
$config = $configuration.cakeConfig
$assetsFolder = $configuration.assetsFolder
$buildFolder = $configuration.buildFolder

################################################################
# Prepare folders for update package generation and triggers it
################################################################

Function Process-UpdatePackage([PSObject] $Configuration, [String] $FolderString, $assetsfolder) {

    # Get the output folder path

    $targetFolderName = (Get-Item -Path $FolderString).Name
    $sourceFolder = (Get-Item -Path $FolderString).FullName

    # Create a target folder that will host the generated .update package file

    if (!(Test-Path -Path $([IO.Path]::Combine($assetsfolder, $targetFolderName)))) {
        Write-Host "Creating" $([IO.Path]::Combine($assetsfolder, $targetFolderName))
        New-Item -ItemType Directory -Force -Path $([IO.Path]::Combine($assetsfolder, $targetFolderName))        
            
    }

    $updateFile = Join-Path $([IO.Path]::Combine($assetsfolder, $targetFolderName)) "$($targetFolderName).update"
    GenerateUpdatePackage -configFile $Configuration -argSourcePackagingFolder $sourceFolder -argOutputPackageFile $updateFile
}

###############################
# Generate the Update packages
###############################

Function GenerateUpdatePackage() {

    Param(
        [parameter(Mandatory = $true)]
        [String] $configFile,
        [String] $argSourcePackagingFolder,
        [String] $argOutputPackageFile

    )

    Set-PSRepository -Name PSGallery -InstallationPolicy Trusted
    Install-Module -Name Sitecore.Courier

    New-CourierPackage -Target $($argSourcePackagingFolder) -Output $($argOutputPackageFile) -SerializationProvider "Rainbow" -IncludeFiles $true
	
}

#####################################
# Clean up and prepare for packaging
#####################################

Function Clean-Up([PSObject] $Configuration, [String] $FolderString) {

    # Clean Assemblies

    $AssembliesToRemove = @("Sitecore.*.dll", "Unicorn*.dll", "Rainbow*.dll", "Kamsar*.dll", "Microsoft.*.dll", "HtmlAgilityPack.dll", "ICSharpCode.SharpZipLib.dll", "Lucene.Net.*", "Mvp.Xml.dll", "Newtonsoft.Json.dll", "Owin.dll", "Remotion.Linq.dll", "System.*.dll")
    $AssembliesToKeep = @("Sitecore.HabitatHome.*", "Sitecore.DataExchange.*", "Microsoft.Owin.Security.Facebook.dll", "Microsoft.Owin.Security.MicrosoftAccount.dll", "Microsoft.Owin.Security.OpenIdConnect.dll")

    Get-ChildItem $FolderString -Include $AssembliesToRemove -Exclude $AssembliesToKeep -Recurse | ForEach-Object($_) { Remove-Item $_.FullName }

    # Clean Configs Configs

    $ConfigsToRemove = @("*.Serialization*.config", "Unicorn*.config*", "Rainbow*.config")  

    Get-ChildItem $FolderString -Include $ConfigsToRemove -Recurse | ForEach-Object($_) { Remove-Item $_.FullName }

    # Clean configurations in bin

    $BinFolder = $([IO.Path]::Combine($FolderString, "bin"))

    $BinConfigsToRemove = @("*.config", "*.xdt")  
       

    Get-ChildItem $BinFolder -Include $BinConfigsToRemove -Recurse | ForEach-Object($_) { Remove-Item $_.FullName }

    # Clean Empty Folders

    Get-ChildItem $FolderString -recurse | 

    Where-Object { $_.PSIsContainer -and @(Get-ChildItem -Lit $_.Fullname -r | Where-Object {!$_.PSIsContainer}).Length -eq 0 } |

    Remove-Item -recurse    
}


#$rootFolder = Get-ChildItem (Join-Path $buildFolder *)
$rootFolder = $buildFolder
#Prepare Packages

ForEach ($folder in $rootFolder) {
    Clean-Up -Configuration $config -FolderString (Get-Item -Path $folder).FullName

    Process-UpdatePackage -Configuration $config -FolderString $folder -assetsfolder $assetsfolder

}