<#
.SYNOPSIS
Create SCWDP Packages from update packages

.DESCRIPTION
This script prepares Web Deploy Package (WDP) creation, by reading through configuration files and by looking for 
pre-existing mandatory files for the WDP creation process. 
During the WDP generation process, a 3rd party zip library is used (Ionic Zip) to zip up and help generate the Sitecore
Cargo Payload (SCCPL) packages.

.PARAMETER ConfigurationFile
A cake-config.json file

#>

#######################
# Mandatory parameters
#######################

Param(
    [parameter(Mandatory = $true)]
    [String] $ConfigurationFile
)

###########################
# Find configuration files
###########################

Import-Module "$($PSScriptRoot)\ProcessConfigFile\ProcessConfigFile.psm1" -Force

$configuration = ProcessConfigFile -Config $ConfigurationFile
$config = $configuration.cakeConfig
$assetconfig = $configuration.assets
$assetsFolder = $configuration.assetsFolder
$buildFolder = $configuration.buildFolder


###########################
# Clear WDPs from File Names
###########################

Function CleanUp {
    Param(
        [String] $RootFolder,
        [String] $DotNetZipPath		
    )

    [System.Reflection.Assembly]::LoadFrom($DotNetZipPath)
    $encoding = [System.Text.Encoding]::GetEncoding(65001)

    $WDPs = Get-ChildItem -Path $RootFolder -Recurse -Include "*.scwdp.zip"
	
    ForEach ($WDP in $WDPs) {  
	
        $ZipFile = New-Object Ionic.Zip.ZipFile($encoding)
        $ZipFile = [Ionic.Zip.ZIPFile]::Read($WDP.FullName)
        $ZipFile.RemoveSelectedEntries("Content/Website/temp/*")
        $ZipFile.Save();   
        $ZipFile.Dispose(); 
	
    }
}

##################################################################
# 3rd Party Ionic Zip function - helping create the SCCPL package
##################################################################

Function Zip {

    Param(
        [String] $FolderToZip,
        [String] $ZipFilePath,
        [String] $DotNetZipPath
    )

    # load Ionic.Zip.dll 
  
    [System.Reflection.Assembly]::LoadFrom($DotNetZipPath)
    $Encoding = [System.Text.Encoding]::GetEncoding(65001)
    $ZipFile = New-Object Ionic.Zip.ZipFile($Encoding)

    $ZipFile.AddDirectory($FolderToZip) | Out-Null

    If (!(Test-Path (Split-Path $ZipFilePath -Parent))) {

        mkdir (Split-Path $ZipFilePath -parent)

    }

    Write-Host "Saving zip file from $FolderToZip"
    $ZipFile.Save($ZipFilePath)
    $ZipFile.Dispose()
    Write-Host "Saved..."

}

Function Create-CargoPayload {
    Param(
        [parameter(Mandatory = $true)]
        [String]$CargoName,
        [parameter(Mandatory = $true)]
        [alias("Cargofolder")]
        [String]$OutputCargoFolder,
        [String]$XdtSourceFolder,
        [parameter(Mandatory = $true)]
        [String]$ZipAssemblyPath
    )

    if (!(Test-Path $OutputCargoFolder)) {
        Write-Host $OutputCargoFolder "Folder does not exist"
        Write-Host "Creating" $OutputCargoFolder "Folder"
        New-Item -Path $OutputCargoFolder -ItemType Directory -Force
    }

    $WrkingCargoFldrSafeZone = Join-path $OutputCargoFolder "temp"

    if (!(Test-Path $($WrkingCargoFldrSafeZone))) {
        $WrkingCargoFldrSafeZone = New-Item -Path $(Join-path $OutputCargoFolder "temp") -ItemType Directory -Force
    }

    New-Item -Path $(Join-Path $WrkingCargoFldrSafeZone "CopyToRoot") -ItemType Directory -Force
    New-Item -Path $(Join-Path $WrkingCargoFldrSafeZone "CopyToWebsite") -ItemType Directory -Force
    New-Item -Path $(Join-Path $WrkingCargoFldrSafeZone "IOActions") -ItemType Directory -Force
    $XdtsPath = New-Item -Path $(Join-Path $WrkingCargoFldrSafeZone "Xdts") -ItemType Directory -Force
    $WorkingZipFilePath = Join-Path $WrkingCargoFldrSafeZone $($CargoName + ".zip")

    if ($CargoName -like "*.sccpl") {
        $OutputCargoFilePath = Join-path $OutputCargoFolder $Cargoname
    }
    else {
        $OutputCargoFilePath = Join-path $OutputCargoFolder $($Cargoname + ".sccpl")
    }

    Write-Host "Creating" $OutputCargoFilePath

    if ($XdtSourceFolder) {
        # Gather xdt files

        $files = Get-ChildItem -Path $XdtSourceFolder -Filter "*.xdt" -Recurse
        ForEach ($file in $files) {

            $currentFolder = $file.Directory.ToString()
            [String]$replacementPath = $currentFolder -replace [Regex]::Escape($XdtSourceFolder), ($XdtsPath.FullName)
            [System.IO.DirectoryInfo]$destination = $replacementPath
            if (($destination.FullName -ine $XdtsPath.FullName) -and (!(Test-Path -Path $destination))) {
        
                New-Item -Path $destination -ItemType Directory

            }

            Copy-Item -Path $file.FullName -Destination $destination -Force -ErrorVariable capturedErrors -ErrorAction SilentlyContinue

        }

    }

    # Zip up all Cargo Payload folders using Ionic Zip
    Zip -FolderToZip $WrkingCargoFldrSafeZone -ZipFilePath $WorkingZipFilePath -DotNetZipPath $ZipAssemblyPath

    # Move and rename the zipped file to .sccpl - create the Sitecore Cargo Payload file
	
    Write-Host "Converting" $WorkingZipFilePath "to sccpl"
    Move-Item -Path $WorkingZipFilePath -Destination $OutputCargoFilePath -Force | Out-Null

    # Clean up Working folder

    Remove-Item -Path $WrkingCargoFldrSafeZone -Recurse -Force

    Write-Host "Creation of" $OutputCargoFilePath "Complete" -ForegroundColor Green
}

################################
# Create the Web Deploy Package
################################

Function Create-WDP {

    Param(
        [String] $RootFolder, 
        [String] $SitecoreCloudModulePath, 
        [String] $JsonConfigFilename, 
        [String] $XmlParameterFilename, 
        [String] $SccplCargoFilename, 
        [String] $IonicZip,
        [String] $foldername,
        $assetJSONconfig,
        $configurationJson,
        [String] $XdtSrcFolder
    )

    <#
.SYNOPSIS
Create SCWDP packages

.DESCRIPTION
Is called by Prepare-wdp. Ties together several functions for the prupose of generating a SCWDP

.PARAMETER RootFolder 
is the physical path on the filesystem to the source folder for WDP operations that will contain the WDP JSON configuration file, 
the WDP XML parameters file and the folder with the module packages
The typical structure that should be followed is:

    \RootFolder\module_name_module.json
    \RootFolder\module_name_parameters.xml
    \RootFolder\SourcePackage\module_installation_package.zip( or .update)

.PARAMETER SitecoreCloudModulePath 
provides the path to the Sitecore.Cloud.Cmdlets.psm1 Azure Toolkit Powershell module (usually under \SAT\tools)

.PARAMETER JsonConfigFilename 
is the name of your WDP JSON configuration file

.PARAMETER XmlParameterFilename 
is the name of your XML parameter file (must match the name that is provided inside the JSON config)

.PARAMETER SccplCargoFilename 
is the name of your Sitecore Cargo Payload package (must match the name that is provided inside the JSON config)

.PARAMETER IonicZip 
is the path to Ionic's zipping library

.Example
 Create-WDP -RootFolder "C:\_deployment\website_packaged_test" `
            -SitecoreCloudModulePath "C:\Deploy\9.1.0\XPSingle\assets\Sitecore Azure Toolkit\tools\Sitecore.Cloud.Cmdlets.psm1" `
            -JsonConfigFilename "website_config" `
            -XmlParameterFilename "website_parameters" `
            -SccplCargoFilename "website_cargo" `
            -IonicZip ".\Sitecore Azure Toolkit\tools\DotNetZip.dll"

.Example
 Create-WDP -RootFolder "C:\Deploy\Modules\DEF" `
            -SitecoreCloudModulePath "C:\Deploy\9.1.0\XPSingle\assets\Sitecore Azure Toolkit\tools\Sitecore.Cloud.Cmdlets.psm1" `
            -JsonConfigFilename "def_config" `
            -XmlParameterFilename "def_parameters" `
            -SccplCargoFilename "def_cargo" `
            -IonicZip ".\Sitecore Azure Toolkit\tools\DotNetZip.dll"
#>

    # Create empty folder structures for the WDP work

    [string] $DestinationFolderPath = New-Item -Path "$($RootFolder)\WDPWorkFolder\WDP" -ItemType Directory -Force

    # WDP Components folder and sub-folders creation

    $ComponentsFolderPath = New-Item -Path "$($RootFolder)\WDPWorkFolder\Components" -ItemType Directory -Force
    $CargoPayloadFolderPath = New-Item -Path $(Join-Path $ComponentsFolderPath "CargoPayloads") -ItemType Directory -Force
    $AdditionalWdpContentsFolderPath = New-Item -Path "$($ComponentsFolderPath)\AdditionalFiles" -ItemType Directory -Force
    $JsonConfigFolderPath = New-Item -Path "$($ComponentsFolderPath)\Configs" -ItemType Directory -Force
    $ParameterXmlFolderPath = New-Item -Path "$($ComponentsFolderPath)\MsDeployXmls" -ItemType Directory -Force

    # Provide the required files for WDP

    $JsonConfigFilenamePath = Get-ChildItem -Path $JsonConfigFilename

    [String] $ConfigFilePath = "$($JsonConfigFolderPath)\$($JsonConfigFilenamePath.Name)"

    # Copy the parameters.xml file over to the target ParameterXml folder

    Copy-Item -Path $XmlParameterFilename -Destination $ParameterXmlFolderPath.FullName -Force

    # Copy the config.json file over to the target Config folder

    Copy-Item -Path $JsonConfigFilename -Destination $ConfigFilePath -Force

    # Create Cargo Payload(s)

    Create-CargoPayload -CargoName $($SccplCargoFilename + "_embeded") -Cargofolder $CargoPayloadFolderPath.FullName -XdtSourceFolder $XdtSrcFolder -ZipAssemblyPath $IonicZip

    #Create-CargoPayload -CargoName $($SccplCargoFilename) -Cargofolder $CargoPayloadFolderPath.FullName -ZipAssemblyPath $IonicZip

    # Build the WDP file

    Import-Module $SitecoreCloudModulePath -Verbose
    Start-SitecoreAzureModulePackaging -SourceFolderPath $RootFolder `
        -DestinationFolderPath $DestinationFolderPath `
        -CargoPayloadFolderPath $CargoPayloadFolderPath.FullName `
        -AdditionalWdpContentsFolderPath $AdditionalWdpContentsFolderPath.FullName `
        -ParameterXmlFolderPath $ParameterXmlFolderPath.FullName `
        -ConfigFilePath $ConfigFilePath `
        -Verbose

	
    CleanUp -RootFolder $RootFolder -DotNetZipPath $IonicZip

}

########################################################################################
# WDP preparation function which sets up initial components required by the WDP process
########################################################################################

Function Prepare-WDP ($configJson, $assetsConfigJson, $assetsFolder) {

    # Assign values to required working folder paths
    
    [String] $xGeneratorWdpFolder = $([IO.Path]::Combine($configJson.ProjectFolder, 'WdpComponents'))
    [String] $SitecoreCloudModule = $([IO.Path]::combine($assetsFolder, 'Sitecore Azure Toolkit', 'tools', 'Sitecore.Cloud.Cmdlets.psm1'))
    [String] $IonicZipPath = $([IO.Path]::combine($assetsFolder, 'Sitecore Azure Toolkit', 'tools', 'DotNetZip.dll'))


    # Prepare the files and paths for the Habitat Home WDP creation

    ForEach ($folder in (Get-ChildItem -Path "$($assetsFolder)" | Where-Object {$_.name -eq "xGenerator"})) {

        # Do a check if the WDP package already exists and if not, proceed with package generation

        [String] $xGeneratorWdpTarget = "$($folder.FullName)\WDPWorkFolder\WDP"
        If ((Test-Path -Path $xGeneratorWdpTarget) -eq $False) {

            # Fetch the json and xml files needed for the WDP package generation and start the WDP package creation process

            Get-ChildItem -Path "$($xGeneratorWdpFolder)\*" -Include "xGenerator_config.json" | ForEach-Object { $WDPJsonFile = $_.FullName }
            Get-ChildItem -Path "$($xGeneratorWdpFolder)\*" -Include "xGenerator_parameters.xml" | ForEach-Object { $WDPXMLFile = $_.FullName }
            [String] $SccplCargoName = -join ($folder.Name, "_cargo")
            Create-WDP -RootFolder $folder.FullName `
                -SitecoreCloudModulePath $SitecoreCloudModule `
                -JsonConfigFilename $WDPJsonFile `
                -XmlParameterFilename $WDPXMLFile `
                -SccplCargoFilename $SccplCargoName `
                -IonicZip $IonicZipPath `
                -foldername $folder.Name `
                -assetJSONconfig $assetsConfigJson `
                -XdtSrcFolder $(Join-Path $buildFolder "xGenerator")
					
        }
        else {
			
            Write-Host "Skipping WDP generation - there's already a WDP package, present at $($xGeneratorWdpTarget)" -ForegroundColor Yellow
			
        }

			

    }

    
}

#######################################
# Call in the WDP preparation function
#######################################

Prepare-WDP -configJson $config -assetsConfigJson $assetconfig -assetsFolder $assetsFolder