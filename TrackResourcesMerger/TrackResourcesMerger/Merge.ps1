$sourceFile = "<Path-to>\TrackResources.lua";
$targetFile = "<Path-to>\TrackResources_target.lua";

Start-Process -FilePath "TrackResourcesMerger.exe" -ArgumentList "$sourceFile","$targetFile","true"