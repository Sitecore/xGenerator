# escape=`

ARG BASE_IMAGE
ARG BUILD_IMAGE

FROM ${BUILD_IMAGE} as build

ARG INTERNAL_NUGET_SOURCE
ARG INTERNAL_NUGET_SOURCE_USERNAME="VSTS"
ARG INTERNAL_NUGET_SOURCE_PASSWORD

SHELL ["powershell", "-Command", "$ErrorActionPreference = 'Stop'; $ProgressPreference = 'SilentlyContinue';"]

WORKDIR /project
COPY src/ ./src
COPY ExperienceGenerator.sln .
COPY nuget.config .
COPY docker/scripts/Add-InternalNugetFeed.ps1 .

RUN .\Add-InternalNugetFeed.ps1

RUN msbuild /m:1 /p:Configuration="Release"  /p:DeployOnBuild=true /p:DeployDefaultTarget=WebPublish /p:WebPublishMethod=FileSystem /p:DeleteExistingFiles=false /p:publishUrl=C:\out\xGenerator /p:BuildProjectReferences=true /target:Build "ExperienceGenerator.sln" /restore

FROM ${BUILD_IMAGE} as data

SHELL ["powershell", "-Command", "$ErrorActionPreference = 'Stop'; $ProgressPreference = 'SilentlyContinue';"]

COPY docker/scripts/Packaging/ /packaging

COPY /src/serialization /items

# Install latest PackageProvider (required for Sitecore.Courier)
RUN Install-PackageProvider -Name NuGet -MinimumVersion 2.8.5.201 -Force
RUN .\packaging\generate-update-package.ps1 -target /items -output /out/db

FROM ${BASE_IMAGE} as solution

COPY --from=build /out/xgenerator /module/cm/content
COPY --from=data /out/db /module/db