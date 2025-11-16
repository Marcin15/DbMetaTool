$exe = Get-ChildItem -Path $PSScriptRoot -Recurse -Filter "DbMetaTool.exe" | Select-Object -First 1
if ($exe) {
    & $exe.FullName
} else {
    Write-Host "DbMetaTool.exe not found"
}