Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName PresentationFramework
<#
Oculus Add Script
Version 0.3
Revised 9/9/19 - Ben Kellermann
Revised 9/14/19 - Jon Carewick
This script allows one to easily add any installed app/game to Oclulus without having to manually launch it within Oculus first

Changelog
- 0.3: Added integration to Oculus Home Icon Changer (read .conf, etc) & required assemply type ref at top of script & other minor changes
- 0.2: Removed dependency on external files / Added prompt to overwrite or quit if manifest already exists / changed working path to $env:TEMP
- 0.1: Initial script

Note About oculusHomeIconChanger
This script was originally written to work in conjunction with OHIC.  If currently using it, please edit this script prior to use
and change the path assigned to the $OHIC variable (Line #37) to the path where you have OculusHomeIconChanger.exe located on your PC

Instructions
1. Launch PowerShell / PowerShell ISE
2. If needed, execute the below string to allow scripts to be run from your system
   set-executionpolicy unrestricted
3. Execute the below string to start the script
   ./AddtoOculus.ps1
4. When prompted, browse to and select the app/game's primary EXE
   (This isn't always the sole EXE in the root of the app/game folder)
5. If a manifest already exists, choose "Yes" to overwrite the existing one or "No" to stop the script 
6. When promtped, choose whether or not the app/game is sourced from Steam (Choosing "Yes" simply adds the launch parameter "-vrmode oculus")
7. When prompted, choose whether or not you wish to restart the Oculus service 
   (It must be restarted in order for the addition to appear.  If planning to use OculusHomeIconChanger, I suggest choosing "No" then restarting
   service from within OHIC once images are set)

Future Plans
- Compile to standalone EXE
- Hopefully migrate with OculusHomeIconChanger

Note about Oculus Home Icon Changer integration:
This script now requires some params, like this:
powershell.exe -Executionpolicy bypass -file .\AddToOculus.ps1 "C:\Program Files (x86)\Oculus\CoreData\Manifests" "C:\Program Files (x86)\Oculus\CoreData\Software\StoreAssets"
This is handled automatically with the "Add new" button in OHIC If no params added, uses default in script below

#>
#Prompt to Select Game's Primary EXE
$FileBrowser = New-Object System.Windows.Forms.OpenFileDialog -Property @{ 
    InitialDirectory = [Environment]::GetFolderPath('Desktop') 
    Filter = 'Executable (*.exe)|*.exe'
}
$dialog = $FileBrowser.ShowDialog()
#If Dialog is cancelled, exit script
if($dialog -ne 'Ok')
{
	exit
}

#Set Variables
$WorkingPath = "$env:TEMP\OHICAddNew"
$OCManifests = "C:\Program Files\Oculus\CoreData\Manifests"
$OCStoreAssets = "C:\Program Files\Oculus\CoreData\Software\StoreAssets"
$OHIC = "C:\Program Files (x86)\OculusHomeIconChanger\OculusHomeIconChanger.exe"
$File = $FileBrowser.SafeFileName
$FileName = $File.replace(".exe","")
$VREXE = $FileBrowser.Filename
$VREXE1 = $VREXE.replace(":\","_")
$VREXE2 = $VREXE1.replace("\","_")
$VREXEDouble = $VREXE.replace("\","\\")
$VREXE3 = $VREXE2.replace(".exe","")
$VREXECan = $VREXE3.replace(" ","")
$VREXEJSON = $VREXECan + ".json"
$VREXEAssets = $VREXECan + "_assets"

#Create Working directory inside of %TEMP% folder:
New-Item -ItemType "directory" -Path "$WorkingPath"

#Grab 2 arguments if exist as new $OCManifests and $OCStoreAssets values
if($args[0].length -gt 0 -And $args[1].length -gt 0)
{
	$OCManifests = $args[0]
	$OCStoreAssets = $args[1]
}

if (Test-Path "$OCManifests\$VREXECan.json" -PathType Leaf) { 
    $msgBoxInput =  [System.Windows.MessageBox]::Show('Manifest already exists.  Overwrite?','Overwrite','YesNo','Question')
    switch  ($msgBoxInput) {
    'Yes' {
    Remove-Item "$OCManifests\$VREXECan.json" -Force
    Remove-Item "$OCManifests\$VREXEAssets.json" -Force
    Remove-Item "$OCStoreAssets\$VREXEAssets" -Recurse -Force
    }
    'No' {
        #Toast notification showing completion
        Add-Type -AssemblyName System.Windows.Forms 
        $global:balloon = New-Object System.Windows.Forms.NotifyIcon
           $path = (Get-Process -id $pid).Path
        $balloon.Icon = [System.Drawing.Icon]::ExtractAssociatedIcon($path) 
        $balloon.BalloonTipIcon = [System.Windows.Forms.ToolTipIcon]::Warning 
        $balloon.BalloonTipText = 'The script was cancelled due to existing manifest'
        $balloon.BalloonTipTitle = "Add to Oculus Script" 
        $balloon.Visible = $true 
        $balloon.ShowBalloonTip(5000)
        exit
     }
} 
}

#Create Base .json    
New-Item "$WorkingPath\$VREXECan.json" -Force
Add-Content "$WorkingPath\$VREXECan.json" '{"canonicalName":"variable1","displayName":"variable2","files":{"variable3":""},"firewallExceptionsRequired":false,"isCore":false,"launchFile":"variable4","launchParameters":"variable5","manifestVersion":0,"packageType":"APP","thirdParty":true,"version":"1","versionCode":1}"'
((Get-Content -path "$WorkingPath\$VREXECan.json" -Raw) -replace 'variable1',$VREXECan) | Set-Content -Path "$WorkingPath\$VREXECan.json"
((Get-Content -path "$WorkingPath\$VREXECan.json" -Raw) -replace 'variable2',$FileName) | Set-Content -Path "$WorkingPath\$VREXECan.json"
((Get-Content -path "$WorkingPath\$VREXECan.json" -Raw) -replace 'variable3',$VREXEDouble) | Set-Content -Path "$WorkingPath\$VREXECan.json"
((Get-Content -path "$WorkingPath\$VREXECan.json" -Raw) -replace 'variable4',$VREXEDouble) | Set-Content -Path "$WorkingPath\$VREXECan.json"

#Prompt asking if app/game being added is Steam-based.  If so, it adds "-vrmode oculus" to launch parameters in .json else leaves it empty
$msgBoxInput =  [System.Windows.MessageBox]::Show('Is this a Steam Game/App?','Game  input','YesNo','Question')
  switch  ($msgBoxInput) {
  'Yes' {
  ((Get-Content -path "$WorkingPath\$VREXECan.json" -Raw) -replace 'variable5',' -vrmode oculus') | Set-Content -Path "$WorkingPath\$VREXECan.json"
  }
  'No' {
  ((Get-Content -path "$WorkingPath\$VREXECan.json" -Raw) -replace 'variable5',' DFCTR.exe') | Set-Content -Path "$WorkingPath\$VREXECan.json"
  }
  }

#Create assets.json
New-Item "$WorkingPath\$VREXEAssets.json" -Force
Add-Content "$WorkingPath\$VREXEAssets.json" '{
  "dominantColor": "#BEA519",
  "files": {
    "original.jpg": "daa5972168f70b84f39dd66cc54a24fbe7bde6f3a6ee1f42da9cdf168a489320",
    "cover_square_image.jpg": "9d5eba15a05bf5e3fa3847fc1c00c0a2807303f0188ac769c49c7852d39388b8",
    "icon_image.jpg": "46acec14a788def1813031b277a87d16ece28043b0be0353519eb2170c36a42c",
    "cover_landscape_image.jpg": "97d5c79bcb57accc563fb21ec56b808581c1c9550ac2d536e0c97c6f3e7cf8b4",
    "cover_landscape_image_large.png": "d02c7e2062cdaca773932b3b3b08e5673ae1be5a1fbc5acde4d2a7d4c0589ad9",
    "small_landscape_image.jpg": "b9a9a8e74aa1d446279e2fbe8f574ad5b800f7fe8217d1a8d275ea8e94a7e9ff",
    "logo_transparent_image.png": "e31a5769a9805bcf1bd3fbc1b594e68f1036f9ac68631a6bd3a0747a0e21c944"
  },
  "packageType": "ASSET_BUNDLE",
  "isCore": false,
  "appId": null,
  "canonicalName": "variable1",
  "launchFile": null,
  "launchParameters": null,
  "launchFile2D": null,
  "launchParameters2D": null,
  "version": "1567185292",
  "versionCode": 1567185292,
  "redistributables": null,
  "firewallExceptionsRequired": false,
  "thirdParty": true,
  "manifestVersion": 0
}'
((Get-Content -path "$WorkingPath\$VREXEAssets.json" -Raw) -replace 'variable1',$VREXEAssets) | Set-Content -Path "$WorkingPath\$VREXEAssets.json"

#Create StoreAssets Folder
New-Item "$WorkingPath\$VREXEAssets" -ItemType directory -Force

#Move Items to Correct paths
Move-Item -Path "$($WorkingPath)\*.json" -Destination "$($OCManifests)" -force
Move-Item -Path "$($WorkingPath)\$VREXEAssets" -Destination "$($OCStoreAssets)" -force

#Toast notification showing completion
Add-Type -AssemblyName System.Windows.Forms 
$global:balloon = New-Object System.Windows.Forms.NotifyIcon
$path = (Get-Process -id $pid).Path
$balloon.Icon = [System.Drawing.Icon]::ExtractAssociatedIcon($path) 
$balloon.BalloonTipIcon = [System.Windows.Forms.ToolTipIcon]::Warning 
$balloon.BalloonTipText = 'The script completed successfully!'
$balloon.BalloonTipTitle = "Add to Oculus Script" 
$balloon.Visible = $true 
$balloon.ShowBalloonTip(5000)

#Remove working directory 
Remove-Item "$WorkingPath" -Recurse

#output the json filename to be read by OHIC C#
write-host "[ParseWithThis]"
write-host "$($VREXEJSON)"