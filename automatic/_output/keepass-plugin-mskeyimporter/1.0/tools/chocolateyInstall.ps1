$packageName = 'keepass-plugin-mskeyimporter'
$typName = 'MicrosoftKeyImporterPlugin.dll'
$packageSearch = 'KeePass Password Safe'
$url = 'https://github.com/downloads/svickn/MicrosoftKeyImporterPlugin/MicrosoftKeyImporterPlugin-v1.0.zip'
$checksum = '93fde6d55fa256799337168ad924a0831e595278'
$checksumType = 'sha1'
try {
# search registry for location of installed KeePass
$regPath = Get-ItemProperty -Path @('HKLM:\Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\*',
                                    'HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\*',
                                    'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\*') `
                            -ErrorAction:SilentlyContinue `
           | Where-Object {$_.DisplayName -like "$packageSearch*" `
                           -and `
                           $_.DisplayVersion -ge 2.0 `
                           -and `
                           $_.DisplayVersion -lt 3.0 } `
           | ForEach-Object { $_.InstallLocation }
$installPath = $regPath
# search $env:ChocolateyBinRoot for portable install
if (! $installPath) {
  Write-Verbose "$($packageSearch) not found installed."
  $binRoot = Get-BinRoot
  $portPath = Join-Path $binRoot "keepass"
  $installPath = Get-ChildItem -Directory $portPath* -ErrorAction SilentlyContinue
}
if (! $installPath) {
  Write-Verbose "$($packageSearch) not found in $($env:ChocolateyBinRoot)"
  throw "$($packageSearch) location could not be found."
}
$pluginPath = (Get-ChildItem -Directory $installPath\Plugin*).FullName
if ($pluginPath.Count -eq 0) {
  $pluginPath = Join-Path $installPath "Plugins"
  [System.IO.Directory]::CreateDirectory($pluginPath)
}
# download and extract zip into tools dir
$toolsDir = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"
Install-ChocolateyZipPackage -PackageName "$packageName" `
                             -Url "$url" `
                             -UnzipLocation "$toolsDir" `
                             -Checksum "$checksum" `
                             -ChecksumType "$checksumType"
# rename PLGX file so it is clear which plugins are managed via choco
$typPlugin = Join-Path $toolsDir $typName
$chocoPlugin = Join-Path $pluginPath "$($packageName).dll"
Move-Item -Path $typPlugin -Destination $chocoPlugin -Force
if ( Get-Process -Name "KeePass" `
                 -ErrorAction SilentlyContinue ) {
  Write-Warning "$($packageSearch) is currently running. Plugin will be available at next restart of $($packageSearch)." 
} else {
  Write-Host "$($packageName) will be loaded the next time KeePass is started."
  Write-Host "Please note this plugin may require additional configuration. Look for a new entry in KeePass' Menu>Tools"
}} catch {
  throw $_.Exception
}