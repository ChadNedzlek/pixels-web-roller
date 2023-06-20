Push-Location $PSScriptRoot/..
dotnet publish -c Release ./PixelsWeb/PixelsWeb.csproj
if ($LASTEXITCODE) {
    Write-Error "Failed to execute dotnet publish, stopping..."
    exit 1
}
Write-Host "Preparing to rsync"
cd $PSScriptRoot/
wsl ./sync-files.sh
Pop-Location