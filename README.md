# Experience Generator

[![Build status](https://ci.appveyor.com/api/projects/status/b610w5q4qosfyh7r/branch/master?svg=true)](https://ci.appveyor.com/project/jeanfrancoislarente/xgenerator/branch/master)


Generate "realistically looking" traffic for the Sitecore Experience Database (xDB) with configurable patterns, including:

 - Trends over time
 - Identified contacts with multiple visits
 - Bounce rate
 - Geo location
 - Landing pages
 - Channels
 - Referrers
 - Internal and external search
 - Outcomes
 - Campaigns

# Experience Profile Generator

Generate visits for Sitecore contacts (xProfile) with configurable settings:

 - Contact information
 - Visit pages
 - Recency
 - Outcomes
 - Geo location
 - Goals
 - Channel

#Solution structure:

**ExperienceGenerator.Client** – SPEAK v1 applications to manipulate with configuration of created data. 
Includes Experience generator and Experience Profile generator tools

**ExperienceGenerator** – core of SPEAK applications.
Consists of:
*	configuration parsers (for Experience generator and Experience Profile generator tools)
*	Databases of request data (location, device distribution, user agents, search engines etc.)
*	Request parameters randomized factories.
	
**Colossus** – contains of math randomizer tools, basic web request generator and parser tools.
Contains tools that allow serialize\deserialize all XGenerator data to custom web request header.

**Colossus.Integration** – provide integrations with Sitecore. 
Adds custom handlers to Sitecore pipelines to be able patch current request based on received data from Colossus request headers.
Contains walkers to visit sitecore pages with required behavior strategy:
*	Strict walker – uses predefined strict pages list, used in xProfile Generator
* Random walker – opens random page or landing page. According to required count of visits\bounces parses html output, extracts `<a href="..."/>` elements with relative hyperlink path and choose random one to visit if possible.
Both walkers adds page event data if required from configuration.

## General processing flow for XGenerator:
1.	User opens XGenerator tools, performs job configuration.
2.	User clicks “Start” button
3.	ExperienceGenerator.Client pass received configuration to ExperienceGenerator parser
4.	ExperienceGenerator parser creates set of segments with request variables based on configuration and pass them to XGenerator JobManager
5.	JobManager starts invoking requests based on behavior configuration: XProfile behavior simulator or XAnalytics behavior simulator.
6.	Sitecore receives requests as is, setups analytics tracker by calling own pipelines
7.	Colossus.Intergration processors executed inside Sitecore pipelines to patch analytics tracker with current customizations of request

## Installation Instructions
In order to deploy the assets, you need either Visual Studio 2017 or MSBuild Tools for Visual Studio 2017.
Installation instructions assume using **PowerShell 5.1** in _**administrative**_ mode.

The following is a list of default values / assumptions for install locations

- **Project location:**	    	`c:\projects\xgenerator\`
- **Instance Url:**				`xgenerator.dev.local`
- **Website Root:**				`c:\inetpub\wwwroot\xgenerator.dev.local`

If you do **not want to use the default settings**, you need to adjust the appropriate values in `cake-config.json` file:

- **ProjectFolder**
- **InstanceUrl**
- **WebsiteRoot**

The cake script will automatically create a publishSettings.targets.user file with the value of the InstanceUrl specified in the cake-config.json file.

- Run **`.\build.ps1`**

## Package Building Instructions
- Building the installation package requires Sitecore Rocks.
- The package definition is in ExperienceGenerator.Client\ExperienceGenerator.package. You need to point it to a Sitecore install with Experience Generator installed to build the package.

## Extension points:
*	Variable Factories in XGenParser class that fill each request with variables based on configuration (device, date, duration, channel, campaign etc.) allow us setup new factory to fill new request variable
*	Processors in Colossus.Integration library patches all requests with provided request variables.
*	XGenParser – class that converts configuration to set of requests. Executes all request variable factories.

THIS MODULE IS PROVIDED ON AN "AS IS" BASIS, WITHOUT SUPPORT, WARRANTIES OR CONDITIONS OF ANY KIND.
