$sourceFile = "C:\Users\dusti\Desktop\Track\TrackResources.lua";
$targetFile = "C:\Users\dusti\Desktop\Track\TrackResources_target.lua";

Start-Process -FilePath "TrackResourcesMerger.exe" -ArgumentList "$sourceFile","$targetFile","true"