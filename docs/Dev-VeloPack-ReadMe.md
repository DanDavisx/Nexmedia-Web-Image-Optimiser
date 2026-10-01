### **\[Stable vs Prerelease Updates]**



The VeloPack update source is configured roughly as:





internal static GithubSource CreateUpdateSource()

&#x20;       => new("https://github.com/DanDavisx/Nexmedia-Web-Image-Optimiser", null, false);"





The important final argument controls prereleases. Stable build: false. This means only stable releases. Switching the value to true means stable releases + prereleases.





### **\[Installing VeloPack CLI]**



This project uses VeloPack for automatic updating.



If working on a new machine:

dotnet tool install -g vpk --version 1.2.158





### **\[Publishing a prerelease version with VeloPack]**



\# Commit and push changes to GitHub.



\# Set the app version to: 0.x.x-dev.x in the .Desktop.csproj



\# Go to Visual Studio CLI in the repo root and:



\# Add personal access token

$env:VPK\_TOKEN = "github\_pat\_..." 



\# Delete old publish folder

Remove-Item .\\publish -Recurse -Force -ErrorAction SilentlyContinue             



\# Download previous release

vpk download github `

&#x20;   --repoUrl https://github.com/DanDavisx/Nexmedia-Web-Image-Optimiser `

&#x20;   --pre                                                                       



\# Publish application

dotnet publish .\\src\\NexMedia.WebImageOptimiser.Desktop\\NexMedia.WebImageOptimiser.Desktop.csproj `

&#x20;   -c Release `

&#x20;   -r win-x64 `

&#x20;   --self-contained true `

&#x20;   -o .\\publish



\# Launch executable and test

.\\publish\\NexMedia.WebImageOptimiser.exe                                        



\# Package application

vpk pack `

&#x20;   --packId NexMedia.WebImageOptimiser `

&#x20;   --packVersion 0.1.x-dev.x `

&#x20;   --packDir .\\publish `

&#x20;   --mainExe NexMedia.WebImageOptimiser.Desktop.exe `

&#x20;   --packTitle "Nexmedia Web Image Optimiser"                                  



\# Upload to GitHub with --pre tag 

vpk upload github `

&#x20;   --repoUrl https://github.com/DanDavisx/Nexmedia-Web-Image-Optimiser `

&#x20;   --publish `

&#x20;   --pre `

&#x20;   --releaseName "Nexmedia Web Image Optimiser 0.1.x-dev.x" `

&#x20;   --tag v0.1.x-dev.x                                                         



\# Remove token from this PowerShell session

Remove-Item Env:VPK\_TOKEN



### **\[Publishing a stable release version with VeloPack]**



\# Commit and push changes to GitHub.



\# Set the app version to: 0.x.x in the .Desktop.csproj



\# Go to Visual Studio CLI in root directory.



\# Add personal access token

$env:VPK\_TOKEN = "github\_pat\_..."



\# Publish application

dotnet publish .\\src\\NexMedia.WebImageOptimiser.Desktop\\NexMedia.WebImageOptimiser.Desktop.csproj `

&#x20;   -c Release `

&#x20;   -r win-x64 `

&#x20;   --self-contained true `

&#x20;   -o .\\publish



\# Clear old local release packages

Remove-Item .\\Releases -Recurse -Force -ErrorAction SilentlyContinue



\# Pack

vpk pack `

&#x20;   --packId NexMedia.WebImageOptimiser `

&#x20;   --packVersion 0.1.0 `

&#x20;   --packDir .\\publish `

&#x20;   --mainExe NexMedia.WebImageOptimiser.Desktop.exe `

&#x20;   --packTitle "NexMedia Web Image Optimiser"



\# Upload + publish stable GitHub release

vpk upload github `

&#x20;   --repoUrl https://github.com/DanDavisx/Nexmedia-Web-Image-Optimiser `

&#x20;   --publish `

&#x20;   --releaseName "Nexmedia Web Image Optimiser 0.1.0" `

&#x20;   --tag v0.1.0



\# Remove token from this PowerShell session

Remove-Item Env:VPK\_TOKEN



