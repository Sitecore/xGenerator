#addin nuget:?package=Cake.XdtTransform&version=0.16.0
#addin nuget:?package=Cake.Powershell&version=0.4.8
#addin nuget:?package=Cake.Http&version=0.7.0
#addin nuget:?package=Cake.Json&version=4.0.0
#addin nuget:?package=Newtonsoft.Json&version=11.0.2

#load "local:?path=CakeScripts/helper-methods.cake"

var target = Argument<string>("Target", "Default");
var configuration = new Configuration();
var cakeConsole = new CakeConsole();
var configJsonFile = "cake-config.json";
var unicornSyncScript = $"./scripts/Unicorn/Sync.ps1";

/*===============================================
================ MAIN TASKS =====================
===============================================*/

Setup(context =>
{
	cakeConsole.ForegroundColor = ConsoleColor.Yellow;	
    var configFile = new FilePath(configJsonFile);
    configuration = DeserializeJsonFromFile<Configuration>(configFile);
});

Task("Default")
.WithCriteria(configuration != null)
.IsDependentOn("Clean")
.IsDependentOn("Modify-PublishSettings")
.IsDependentOn("Publish-Projects")
.IsDependentOn("Apply-Xml-Transform")
.IsDependentOn("Modify-Unicorn-Source-Folder")
.IsDependentOn("Sync-Unicorn");


/*===============================================
================= SUB TASKS =====================
===============================================*/

Task("Clean").Does(() => {
    CleanDirectories($"{configuration.SourceFolder}/**/obj");
    CleanDirectories($"{configuration.SourceFolder}/**/bin");
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
    var exmGen = $"{configuration.SourceFolder}\\ExperienceGenerator.Exm";

    PublishProject(colossus, configuration.WebsiteRoot);
    PublishProject(xgen, configuration.WebsiteRoot);
    PublishProject(exmGen, configuration.WebsiteRoot);
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

    var authenticationFile = new FilePath($"{configuration.WebsiteRoot}/App_config/Include/Unicorn/Unicorn.zSharedSecret.config");
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

RunTarget(target);
