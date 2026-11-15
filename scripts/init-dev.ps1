$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$target = Join-Path $root ".env"
if (Test-Path $target) { throw ".env already exists. It was not changed." }
function New-RandomSecret([int]$count) {
    $data = New-Object byte[] $count
    $rng = [Security.Cryptography.RandomNumberGenerator]::Create()
    try { $rng.GetBytes($data) } finally { $rng.Dispose() }
    return [Convert]::ToBase64String($data)
}
$text = @"
jwt_key=$(New-RandomSecret 48)
service_key=$(New-RandomSecret 32)
users_db_password=$(New-RandomSecret 24)
products_db_password=$(New-RandomSecret 24)
admin_email=admin@example.com
admin_password=Aa1!$(New-RandomSecret 24)
users_public_url=http://localhost:5081
"@
[IO.File]::WriteAllText($target, $text, (New-Object Text.UTF8Encoding($false)))
Write-Host ".env created. Local admin credentials are in this file. Do not commit it."
