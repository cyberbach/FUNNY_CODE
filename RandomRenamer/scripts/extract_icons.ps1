$publishExe = "..\bin\Release\net8.0-windows\win-x64\publish\MP3RandomRenamer.exe"
$binExe = "..\bin\Release\net8.0-windows\win-x64\MP3RandomRenamer.exe"
Add-Type -AssemblyName System.Drawing
function Save-Icon($path,$out){
  $full = Join-Path $PSScriptRoot $path
  if(Test-Path $full){
    $ico = [System.Drawing.Icon]::ExtractAssociatedIcon($full)
    if($ico -ne $null){
      $bmp = $ico.ToBitmap()
      $bmp.Save($out,[System.Drawing.Imaging.ImageFormat]::Png)
      Write-Output "Saved: $out"
    } else {
      Write-Output "No icon extracted from $full"
    }
  } else { Write-Output "File not found: $full" }
}

Save-Icon $publishExe (Join-Path $PSScriptRoot "..\icon_from_publish.png")
Save-Icon $binExe (Join-Path $PSScriptRoot "..\icon_from_bin.png")
