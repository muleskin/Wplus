# Builds Wplus and puts one self-contained program file in publish\
# (no .NET install needed on the target machine).
$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot
dotnet publish Wplus.csproj -c Release -o publish\.tmp
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
New-Item -ItemType Directory -Force publish | Out-Null
Get-ChildItem publish -File | Remove-Item -Force
Copy-Item publish\.tmp\Wplus.exe publish\Wplus.exe -Force
Remove-Item publish\.tmp -Recurse -Force
Get-ChildItem publish | Select-Object Name, Length
