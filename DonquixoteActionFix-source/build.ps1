param(
    [string]$Sts2Dir = "E:\SteamLibrary\steamapps\common\Slay the Spire 2",
    [string]$DonquixoteModDir = "C:\Users\HP\Downloads\3764112372____beta___Don_Quixote-______________Limbus_Company_______________________________________\3764112372 【beta】Don Quixote-堂吉诃德 [Limbus Company] _【边狱巴士】【边狱公司】"
)

$ErrorActionPreference = "Stop"
$project = Join-Path $PSScriptRoot "DonquixoteActionFix.csproj"
dotnet build $project --configuration Release -p:Sts2Dir="$Sts2Dir" -p:DonquixoteModDir="$DonquixoteModDir"
if ($LASTEXITCODE -ne 0) {
    throw "Mod build failed with exit code $LASTEXITCODE"
}

Write-Host "Standalone mod staged in: $(Join-Path $PSScriptRoot 'dist\DonquixoteActionFix')"
