function ProcessConfigFile {
    <#
.SYNOPSIS
Processes json configuration files

.DESCRIPTION
Converts cake-config.json, assets.json, and azureuser-config.json configs to powershell objects.
Specifices the file path of various folders and config files.
Specifices version number and topology name.
The script then returns all these as an array.

.PARAMETER ConfigurationFile
A cake-config.json file

#>
    [CmdletBinding()]
    Param(
        [parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [Alias ("Config")]
        [string] $ConfigurationFile
    )
    ####################################
    # Find and process cake-config.json
    ####################################
    $configuration = @{}

    if (!(Test-Path $ConfigurationFile)) {
        Write-Host "Configuration file '$($ConfigurationFile)' not found." -ForegroundColor Red
        Write-Host  "Please ensure there is a cake-config.json configuration file at '$($ConfigurationFile)'" -ForegroundColor Red
        Exit 1
    }

    $configuration.cakeConfig = Get-Content -Raw $ConfigurationFile |  ConvertFrom-Json
		
    if (!$configuration.cakeConfig) {
        throw "Error trying to load configuration!"
    } 
    $cakeConfig = $configuration.cakeConfig

    # Note the selected topology and assign the correct project path

    ###############################
    # Find and process assets.json
    ###############################
		
     [string] $assetsConfigFile = $([io.path]::combine($cakeConfig.ProjectFolder, 'assets.json'))

     if (!(Test-Path $assetsConfigFile)) {
         Write-Host "Assets file '$($assetsConfigFile)' not found." -ForegroundColor Red
         Write-Host  "Please ensure there is a assets.json file at '$($assetsConfigFile)'" -ForegroundColor Red
         Exit 1
     }
		
     $configuration.assetsConfigFile = $assetsConfigFile
     $configuration.assets = Get-Content -Raw $assetsConfigFile |  ConvertFrom-Json

     if (!$configuration.assets) {
         throw "Error trying to load Assest File!"
     } 

    #########################################
    # Find and process azureuser-config.json
    #########################################
    # if ($cakeConfig.DeploymentTarget -eq "Azure") {

    #     [string] $azureuserConfigFile = $([io.path]::combine($topologyPath, 'azureuser-config.json'))

    #     if (!(Test-Path $azureuserConfigFile)) {
    #         Write-Host "azureuser-config file '$($azureuserConfigFile)' not found." -ForegroundColor Red
    #         Write-Host  "Please ensure there is a user-config.json configuration file at '$($azureuserConfigFile)'" -ForegroundColor Red
    #         Exit 1
    #     }

    #     $configuration.azureUserConfigFile = $azureuserConfigFile
    #     $configuration.azureUserConfig = Get-Content -Raw $azureuserConfigFile |  ConvertFrom-Json
	
    #     if (!$configuration.azureUserConfig) {
    #         throw "Error trying to load azureuser-config.json!"
    #     }
    # }


    # Determine deployment target
    # $deploymentTarget = $configuration.cakeConfig.DeploymentTarget
    # if ($deploymentTarget -eq "Local" -or $deploymentTarget -eq "OnPrem") {
    #     $deploymentTarget = "OnPrem"
    # }
    # else {
    #     $deploymentTarget = "Cloud"
    # }
    # Specifcy Asset Folder Location
    $assetsfolder = $([io.path]::combine($configuration.cakeConfig.DeployFolder, $configuration.cakeConfig.version , 'assets'))
    $buildFolder = $([io.path]::combine($configuration.cakeConfig.DeployFolder, $configuration.cakeConfig.version, 'xGenerator'))
    $configuration.assetsFolder = $assetsfolder
    $configuration.buildFolder = $buildFolder


    ###########################################
    # Find and process xGenerator-parameters.json
    ###########################################
    
    [string] $xGeneratorParamsConfigFile = $([io.path]::combine($cakeConfig.ProjectFolder, 'WdpComponents', 'xGenerator-parameters.json'))
    if (!(Test-Path $xGeneratorParamsConfigFile)) {
        Write-Host "xGenerator-parameters file '$($xGeneratorParamsConfigFile)' not found." -ForegroundColor Red
        Write-Host  "Please ensure there is a xGenerator-parameters.json configuration file at '$($xGeneratorParamsConfigFile)'" -ForegroundColor Red
        Exit 1
    }
    $xGeneratorParamsConfig = Get-Content -Raw $xGeneratorParamsConfigFile |  ConvertFrom-Json
    if (!$xGeneratorParamsConfig) {
        throw "Error trying to load xGenerator-parameters.json!"
    }

    $configuration.xGeneratorParamsConfig = $xGeneratorParamsConfig
    $configuration.xGeneratorParamsConfigFile = $xGeneratorParamsConfigFile

    return $configuration
}