#addin "Cake.XdtTransform"
#addin "Cake.Powershell"
#addin "Cake.Http"
#addin "Cake.Json"
#addin "Newtonsoft.Json"


#load "local:?path=CakeScripts/helper-methods.cake"
#load "local:?path=CakeScripts/xml-helpers.cake"


var target = Argument<string>("Target", "Default");
var configuration = new Configuration();
var cakeConsole = new CakeConsole();
var configJsonFile = "cake-config.json";
var unicornSyncScript = $"./scripts/Unicorn/Sync.ps1";

var deploymentRootPath = "";
var devSitecoreUserName = Argument("DEV_SITECORE_USERNAME", EnvironmentVariable("DEV_SITECORE_USERNAME"));
var devSitecorePassword = Argument("DEV_SITECORE_PASSWORD", EnvironmentVariable("DEV_SITECORE_PASSWORD"));


/*===============================================
================ MAIN TASKS =====================
===============================================*/

Setup(context =>
{
	cakeConsole.ForegroundColor = ConsoleColor.Yellow;	
    var configFile = new FilePath(configJsonFile);
    configuration = DeserializeJsonFromFile<Configuration>(configFile);

    if (target.Contains("WDP")){
         deploymentRootPath = $"{configuration.DeployFolder}\\{configuration.Version}\\xGenerator";
    }
    else{
    deploymentRootPath = configuration.WebsiteRoot;
    }
  
    if ((target.Contains("WDP") ) && 
    ((string.IsNullOrEmpty(devSitecorePassword)) || (string.IsNullOrEmpty(devSitecoreUserName)))){
        cakeConsole.WriteLine("");
        cakeConsole.WriteLine("");
        Warning("       ***********  WARNING  ***************        ");
        cakeConsole.WriteLine("");
        Warning("You have not supplied your dev.sitecore.com credentials.");
        Warning("Some of the build tasks selected require assets that are hosted on dev.sitecore.com.");
        Warning("If these assets have not previously been downloaded, the script will fail.");
        Warning("You can avoid this warning by supplying values for 'DEV_SITECORE_USERNAME' and 'DEV_SITECORE_PASSWORD' as environment variables or ScriptArgs");
        cakeConsole.WriteLine("");
        Information("Example: .\\build.ps1 -Target Build-WDP -ScriptArgs --DEV_SITECORE_USERNAME=your_user@email.com, --DEV_SITECORE_PASSWORD=<your-password>");
        cakeConsole.WriteLine("");
        Warning("       *************************************        ");
    }
});

Task("Default")
.WithCriteria(configuration != null)
.IsDependentOn("CleanBuildFolders")
.IsDependentOn("Modify-PublishSettings")
.IsDependentOn("Publish-Projects")
.IsDependentOn("Apply-Xml-Transform")
.IsDependentOn("Modify-Unicorn-Source-Folder")
.IsDependentOn("Sync-Unicorn");


/*===============================================
=========== Packaging - Main Tasks ==============
===============================================*/
Task("Build-WDP")
.WithCriteria(configuration != null)
.IsDependentOn("CleanAll")
.IsDependentOn("Publish-Projects")
.IsDependentOn("Publish-YML")
.IsDependentOn("Prepare-Transform-Files")
.IsDependentOn("Publish-Post-Steps")
.IsDependentOn("Create-WDP");




/*===============================================
================= SUB TASKS =====================
===============================================*/

Task("CleanAll")
.IsDependentOn("CleanBuildFolders")
.IsDependentOn("CleanDeployFolder");

Task("CleanBuildFolders").Does(() => {
    // Clean project build folders
    CleanDirectories($"{configuration.SourceFolder}/**/obj");
    CleanDirectories($"{configuration.SourceFolder}/**/bin");

});

Task("CleanDeployFolder").Does(() => {
var folderBase = $"{configuration.Version}\\xGenerator";
    // Clean deployment folders
     string[] folders = { $"\\{folderBase}" };

    foreach (string folder in folders)
    {
        Information($"Cleaning: {folder}");
        if (DirectoryExists($"{configuration.DeployFolder}{folder}"))
        {
            try
            {
                CleanDirectories($"{configuration.DeployFolder}{folder}");
            } catch
            {
                Console.BackgroundColor = ConsoleColor.Red;
                Console.WriteLine($"The folder under path \'{configuration.DeployFolder}{folder}\' is still in use by a process. Exiting...");
                Console.ResetColor();
                Environment.Exit(0);
            }
        }
    }
});

Task("Build-Solution")
.Does(() => {
    MSBuild(configuration.SolutionFile, cfg => InitializeMSBuildSettings(cfg));
});

Task("Publish-Projects")
.IsDependentOn("Build-Solution")
.Does(() => {
    var colossus = $"{configuration.SourceFolder}\\Colossus.Integration";
    var xgen = $"{configuration.SourceFolder}\\ExperienceGenerator.Client";
    var exmGen = $"{configuration.SourceFolder}\\ExeprienceGenerator.Exm";
    
    PublishProject(colossus, deploymentRootPath);
    PublishProject(xgen, deploymentRootPath);
    PublishProject(exmGen, deploymentRootPath);
});

Task("Apply-Xml-Transform").Does(() => {
    Transform(configuration.SourceFolder);
});

Task("Modify-Unicorn-Source-Folder").Does(() => {
    var zzzDevSettingsFile = File($"{configuration.WebsiteRoot}/App_config/Include/ExperienceGenerator/zExperienceGenerator.DevSettings.config");
    
	var rootXPath = "configuration/sitecore/sc.variable[@name='{0}']/@value";
    var sourceFolderXPath = string.Format(rootXPath, "experienceGeneratorSource");
    var directoryPath = MakeAbsolute(new DirectoryPath(configuration.ProjectFolder)).FullPath;

    var xmlSetting = new XmlPokeSettings {
        Namespaces = new Dictionary<string, string> {
            {"patch", @"http://www.sitecore.net/xmlconfig/"}
        }
    };
    XmlPoke(zzzDevSettingsFile, sourceFolderXPath, directoryPath, xmlSetting);
});

Task("Modify-PublishSettings").Does(() => {
    var publishSettingsOriginal = File($"{configuration.SourceFolder}/publishsettings.targets");
    var destination = $"{configuration.SourceFolder}/publishsettings.targets.user";

    CopyFile(publishSettingsOriginal,destination);

	var importXPath = "/ns:Project/ns:Import";

    var publishUrlPath = "/ns:Project/ns:PropertyGroup/ns:publishUrl";

    var xmlSetting = new XmlPokeSettings {
        Namespaces = new Dictionary<string, string> {
            {"ns", @"http://schemas.microsoft.com/developer/msbuild/2003"}
        }
    };
    XmlPoke(destination,importXPath,null,xmlSetting);
    XmlPoke(destination,publishUrlPath,$"{configuration.InstanceUrl}",xmlSetting);
});

Task("Sync-Unicorn").Does(() => {
    var unicornUrl = configuration.InstanceUrl + "unicorn.aspx";
    Information("Sync Unicorn items from url: " + unicornUrl);

    var authenticationFile = new FilePath($"{configuration.WebsiteRoot}/App_config/Include/Unicorn.SharedSecret.config");
    var xPath = "/configuration/sitecore/unicorn/authenticationProvider/SharedSecret";

    string sharedSecret = XmlPeek(authenticationFile, xPath);

    
    StartPowershellFile(unicornSyncScript, new PowershellSettings()
                                                        .SetFormatOutput()
                                                        .SetLogOutput()
                                                        .WithArguments(args => {
                                                            args.Append("secret", sharedSecret)
                                                                .Append("url", unicornUrl);
                                                        }));
});



/*===============================================
============ Packaging Tasks ====================
===============================================*/
Task("Create-WDP")
.IsDependentOn("Prepare-Environment")
.IsDependentOn("Generate-UpdatePackage")
.IsDependentOn("Generate-WDP");

Task("Publish-YML").Does(() => {

	var serializationFilesFilter = $@"{configuration.ProjectFolder}\**\*.yml";
    var destination = $@"{deploymentRootPath}\App_Data";

    if (!DirectoryExists(destination)){
        CreateFolder(destination);
    }

    try
    {
        var files = GetFiles(serializationFilesFilter).Select(x=>x.FullPath).ToList();

        CopyFiles(files , destination, preserveFolderStructure: true);
    }
    catch (System.Exception ex)
    {
        WriteError(ex.Message);
    }


});


Task("Generate-UpdatePackage").Does(() => {
	StartPowershellFile ($"{configuration.ProjectFolder}\\HelperScripts\\Generate-UpdatePackage.ps1", args =>
        {
            args.AppendQuoted($"{configuration.ProjectFolder}\\cake-config.json");
        });
		});

Task("Generate-WDP").Does(() => {
	StartPowershellFile ($"{configuration.ProjectFolder}\\HelperScripts\\Generate-WDP.ps1", args =>
        {
            args.AppendQuoted($"{configuration.ProjectFolder}\\cake-config.json");
        });
		});

Task("Prepare-Environment").Does(() => {
	StartPowershellFile ($"{configuration.ProjectFolder}\\HelperScripts\\Prepare-Environment.ps1", args => {
        args.AppendQuoted($"{configuration.ProjectFolder}\\cake-config.json");
        args.AppendSecret(devSitecoreUserName);
        args.AppendSecret(devSitecorePassword);
        });
    });    



/*===============================================
=============== Utility Tasks ===================
===============================================*/

Task("Prepare-Transform-Files").Does(()=>{
    
    var destination = $@"{deploymentRootPath}\xGenerator";  
     var colossus = $"{configuration.SourceFolder}\\Colossus.Integration";
    var xgen = $"{configuration.SourceFolder}\\ExperienceGenerator.Client";
    var exmGen = $"{configuration.SourceFolder}\\ExeprienceGenerator.Exm";

    var layers = new string[] { colossus, xgen, exmGen};
  
    foreach(var layer in layers)
    {
        var xdtFiles = GetTransformFiles(layer);
        
        List<string> files;
        
        files = xdtFiles.Select(x => x.FullPath).Where(x=>!x.Contains(".azure")).ToList();

        foreach (var file in files)
        {
            FilePath xdtFilePath = (FilePath)file;
            
            var fileToTransform = Regex.Replace(xdtFilePath.FullPath, ".+code/(.+/*.xdt)", "$1");
            fileToTransform = Regex.Replace(fileToTransform, ".sc-internal", "");

            FilePath sourceTransform = $"{destination}\\{fileToTransform}";
            
            if (!FileExists(sourceTransform)){
                CreateFolder(sourceTransform.GetDirectory().FullPath);
                CopyFile(xdtFilePath.FullPath,sourceTransform);
            }
            else {
                MergeFile(sourceTransform.FullPath	    // Source File
                        , xdtFilePath.FullPath			// Tranforms file (*.xdt)
                        , sourceTransform.FullPath);		// Target File
            }
        }
    }
});

Task("Publish-Post-Steps").Does(() => {

	var serializationFilesFilter = $@"{configuration.ProjectFolder}\**\*.poststep";
    var destination = $@"{deploymentRootPath}\App_Data\poststeps";

    if (!DirectoryExists(destination))
    {
        CreateFolder(destination);
    }

    try
    {
        var files = GetFiles(serializationFilesFilter).Select(x=>x.FullPath).ToList();

        CopyFiles(files, destination, preserveFolderStructure: false);
    }
    catch (System.Exception ex)
    {
        WriteError(ex.Message);
    }


});
RunTarget(target);
