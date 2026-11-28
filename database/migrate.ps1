<#
.SYNOPSIS
  Tạo database FoMart (nếu chưa có) và chạy các migration chưa áp dụng, theo thứ tự tên file.

.EXAMPLE
  ./database/migrate.ps1 -Docker -Seed                    # container trong docker-compose.yml (đọc .env)
  ./database/migrate.ps1                                  # SQL Server cài trên Windows, Windows auth
  ./database/migrate.ps1 -Seed                            # kèm dữ liệu mẫu dev
  ./database/migrate.ps1 -Server "localhost,1433" -User sa -Password "..." -Database FoMart

.NOTES
  - Cần sqlcmd. Mỗi migration chạy với -b (dừng ở lỗi đầu tiên) và tự bọc transaction.
  - Đã áp dụng thì không chạy lại; sửa nội dung file đã áp dụng sẽ bị báo lỗi checksum.
    Muốn thay đổi schema: thêm file Vxxx__mo_ta.sql mới, không sửa file cũ.
#>
[CmdletBinding()]
param(
    [string]$Server = 'localhost',
    [string]$Database = 'FoMart',
    [string]$User,
    [string]$Password,
    [switch]$Seed,
    [switch]$Docker
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot

if ($Docker) {
    # Đọc MSSQL_SA_PASSWORD và MSSQL_PORT từ .env ở thư mục gốc repo.
    $envFile = Join-Path (Split-Path $root -Parent) '.env'
    if (-not (Test-Path $envFile)) { throw "Không thấy $envFile. Sao chép .env.example thành .env và đặt mật khẩu." }
    $dotenv = @{}
    Get-Content $envFile | Where-Object { $_ -match '^\s*([A-Z_]+)\s*=\s*(.*)\s*$' } |
        ForEach-Object { $dotenv[$Matches[1]] = $Matches[2].Trim('"') }
    $port = if ($dotenv['MSSQL_PORT']) { $dotenv['MSSQL_PORT'] } else { '14330' }
    $Server = "localhost,$port"
    $User = 'sa'
    $Password = $dotenv['MSSQL_SA_PASSWORD']
}

function Invoke-Sql {
    param([string]$File, [string]$Query, [string]$Db = $Database)
    $sqlArgs = @('-S', $Server, '-d', $Db, '-b', '-C', '-I', '-f', '65001', '-h', '-1', '-W')
    if ($User) { $sqlArgs += @('-U', $User, '-P', $Password) } else { $sqlArgs += '-E' }
    if ($File) { $sqlArgs += @('-i', $File) } else { $sqlArgs += @('-Q', "SET NOCOUNT ON; $Query") }
    $out = & sqlcmd @sqlArgs
    if ($LASTEXITCODE -ne 0) { throw "sqlcmd failed ($LASTEXITCODE): $File$Query`n$($out -join "`n")" }
    return $out
}

Write-Host "Database: $Database @ $Server"

# 00_create_database.sql khai báo tên database bằng biến @DatabaseName; thay theo -Database rồi chạy trên master.
$createScript = [IO.File]::ReadAllText((Join-Path $root '00_create_database.sql'), [Text.Encoding]::UTF8)
$pattern = "(DECLARE @DatabaseName SYSNAME = N')[^']*(')"
if ($createScript -notmatch $pattern) { throw "Không tìm thấy dòng 'DECLARE @DatabaseName SYSNAME = N''...''' trong 00_create_database.sql." }
$createScript = [regex]::Replace($createScript, $pattern, "`${1}$($Database.Replace("'", "''"))`${2}")
$tempCreate = Join-Path ([IO.Path]::GetTempPath()) "fomart-create-$([guid]::NewGuid().ToString('N')).sql"
[IO.File]::WriteAllText($tempCreate, $createScript, (New-Object Text.UTF8Encoding $false))
try {
    # Ngay sau khi tạo database, ALTER DATABASE có thể deadlock với tác vụ nền của SQL Server: thử lại.
    for ($attempt = 1; ; $attempt++) {
        try { Invoke-Sql -File $tempCreate -Db 'master' | Out-Null; break }
        catch { if ($attempt -ge 5 -or $_.Exception.Message -notmatch 'deadlock') { throw }; Start-Sleep -Seconds $attempt }
    }
}
finally { Remove-Item $tempCreate -ErrorAction SilentlyContinue }

# Bảng ghi các migration đã chạy (nằm trong database đích).
Invoke-Sql -Query @'
IF OBJECT_ID(N'dbo.schema_versions', N'U') IS NULL
CREATE TABLE dbo.schema_versions (
    version     VARCHAR(50)   NOT NULL CONSTRAINT PK_schema_versions PRIMARY KEY,
    script_name NVARCHAR(260) NOT NULL,
    checksum    CHAR(64)      NOT NULL,
    applied_at  DATETIME2(3)  NOT NULL CONSTRAINT DF_schema_versions_applied_at DEFAULT SYSUTCDATETIME()
);
'@ | Out-Null

$applied = @{}
Invoke-Sql -Query 'SELECT version + ''|'' + checksum FROM dbo.schema_versions' |
    Where-Object { $_ -match '\|' } |
    ForEach-Object { $v, $c = $_.Trim().Split('|'); $applied[$v] = $c }

# Checksum SHA-256 của nội dung sau khi đổi CRLF về LF: Git trên Windows có thể checkout file với CRLF,
# chỉ khác ký tự xuống dòng thì không coi là "đã sửa migration". Checksum đã lưu trước đây là của bản LF nên vẫn khớp.
function Get-ScriptChecksum([string]$Path) {
    $text = [IO.File]::ReadAllText($Path, [Text.Encoding]::UTF8) -replace "`r`n", "`n"
    $sha = [Security.Cryptography.SHA256]::Create()
    try { return -join ($sha.ComputeHash([Text.UTF8Encoding]::new($false).GetBytes($text)) | ForEach-Object { $_.ToString('x2') }) }
    finally { $sha.Dispose() }
}

$scripts = @(Get-ChildItem (Join-Path $root 'migrations') -Filter 'V*__*.sql' | Sort-Object Name)
if ($Seed) { $scripts += Get-ChildItem (Join-Path $root 'seed') -Filter '*.sql' | Sort-Object Name }

foreach ($s in $scripts) {
    $isSeed = $s.DirectoryName -like '*seed'
    $version = if ($isSeed) { "seed:$($s.BaseName)" } else { ($s.Name -split "__")[0] }
    $checksum = Get-ScriptChecksum $s.FullName

    if ($applied.ContainsKey($version)) {
        if ($applied[$version] -ne $checksum -and -not $isSeed) {
            throw "Migration $($s.Name) đã áp dụng nhưng nội dung đã bị sửa. Hãy tạo migration mới thay vì sửa file cũ."
        }
        Write-Host "  = $($s.Name) (đã áp dụng)"
        continue
    }

    Write-Host "  + $($s.Name)"
    Invoke-Sql -File $s.FullName | ForEach-Object { if ($_) { Write-Host "    $_" } }
    Invoke-Sql -Query "INSERT INTO dbo.schema_versions (version, script_name, checksum) VALUES (N'$version', N'$($s.Name)', '$checksum')" | Out-Null
}

Write-Host 'Xong.'
