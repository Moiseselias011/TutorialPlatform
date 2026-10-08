#Requires -Version 5.1

<#
    Arranca la plataforma de una sola pasada: SQL Server Express, el backend en
    el puerto 5099 y el túnel ngrok, y espera hasta que las dos URLs respondan.

        powershell -NoProfile -ExecutionPolicy Bypass -File .\arrancar.ps1

    Pensado para el arranque de después de reiniciar el PC: en ese momento
    mueren el proceso dotnet y el de ngrok. SQL Server Express suele arrancar
    solo con el sistema, pero si no lo está, este script no puede levantarlo
    sin elevación y lo avisa en pantalla.

    Es inocuo si todo ya está corriendo: comprueba antes de lanzar nada, así
    que se puede ejecutar tantas veces como haga falta sin duplicar procesos.

    -SoloEstado   solo informa de lo que hay; no arranca ni toca nada.
#>

param(
    [switch]$SoloEstado
)

$dirApi  = Join-Path $PSScriptRoot 'backend\TutorialPlatform.Api'
$puerto  = 5099
$api     = "http://localhost:$puerto"
$dirLog  = Join-Path $env:LOCALAPPDATA 'Temp\opencode'
$log     = Join-Path $dirLog 'backend.log'
$logErr  = Join-Path $dirLog 'backend.err.log'


# ---------- utilidades ----------

# ¿Contesta esa URL? Un 401 o un 403 también cuentan: lo que se necesita saber
# es si el servidor está vivo, no si nos deja pasar.
function Responde($url) {
    try {
        $r = Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 5
        return ([int]$r.StatusCode -ge 200 -and [int]$r.StatusCode -lt 400)
    } catch {
        return ($null -ne $_.Exception.Response)
    }
}

# URL pública del túnel, leída de la API local de ngrok (4040): es lo único
# fiable, porque la dirección depende de la cuenta y no conviene hardcodearla.
function Tunel-Publico {
    try {
        $r = Invoke-WebRequest -Uri 'http://localhost:4040/api/tunnels' -UseBasicParsing -TimeoutSec 5
        $d = $r.Content | ConvertFrom-Json
        foreach ($t in $d.tunnels) { if ($t.public_url -like 'https://*') { return $t.public_url } }
        foreach ($t in $d.tunnels) { if ($t.public_url) { return $t.public_url } }
    } catch { }
    return $null
}

function Ruta-Ngrok {
    $enPath = Get-Command 'ngrok.exe' -ErrorAction SilentlyContinue
    if ($enPath) { return $enPath.Source }
    $paquete = Get-ChildItem (Join-Path $env:LOCALAPPDATA 'Microsoft\WinGet\Packages') `
        -Filter 'ngrok.exe' -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($paquete) { return $paquete.FullName }
    return $null
}


Write-Host '== Arranque de la plataforma ==' -ForegroundColor Cyan

# ---------- 1. SQL Server Express ----------
# Se intenta levantar por si acaso; sin elevación sale la excepción y se
# explica, porque ese caso solo lo puede resolver la persona a mano.
$sqlNombre = 'MSSQL$SQLEXPRESS'
$sql = Get-Service -Name $sqlNombre -ErrorAction SilentlyContinue
if ($null -eq $sql) {
    Write-Host ('  [?] No encuentro el servicio ' + $sqlNombre + '.') -ForegroundColor Yellow
} elseif ($sql.Status -eq 'Running') {
    Write-Host '  [ok] SQL Server Express en marcha.' -ForegroundColor Green
} elseif ($SoloEstado) {
    Write-Host '  [!!] SQL Server Express PARADO.' -ForegroundColor Yellow
} else {
    try {
        Start-Service -Name $sqlNombre
        Write-Host '  [ok] SQL Server Express levantado.' -ForegroundColor Green
    } catch {
        Write-Host ('  [!!] SQL Express esta parado y no puedo levantarlo sin elevacion: ' + $_.Exception.Message) -ForegroundColor Yellow
        Write-Host '       Abre services.msc y arranca MSSQL$SQLEXPRESS a mano.' -ForegroundColor Yellow
    }
}

# ---------- 2. Backend en 5099 ----------
# Primero se mira el puerto: si ya escucha, hay una instancia viva y lanzar
# otra solo produciría el error MSB3027 en el siguiente build.
$escucha = Get-NetTCPConnection -State Listen -LocalPort $puerto -ErrorAction SilentlyContinue
if ($escucha) {
    $pidBueno = @($escucha)[0].OwningProcess
    Write-Host ('  [ok] El backend ya escucha en ' + $puerto + ' (PID ' + $pidBueno + ').') -ForegroundColor Green
} elseif ($SoloEstado) {
    Write-Host ('  [!!] El backend NO escucha en el puerto ' + $puerto + '.') -ForegroundColor Yellow
} else {
    $dotnet = Get-Command 'dotnet' -ErrorAction SilentlyContinue
    if (-not $dotnet) {
        Write-Host '  [X] No encuentro dotnet en el PATH. Falta el SDK de .NET.' -ForegroundColor Red
    } elseif (-not (Test-Path $dirApi)) {
        Write-Host ('  [X] No existe el proyecto del backend: ' + $dirApi) -ForegroundColor Red
    } else {
        if (-not (Test-Path $dirLog)) { $dirLog = $env:TEMP; $log = Join-Path $dirLog 'backend.log'; $logErr = Join-Path $dirLog 'backend.err.log' }
        Start-Process -FilePath 'dotnet' -ArgumentList 'run','--launch-profile','http' `
            -WorkingDirectory $dirApi -RedirectStandardOutput $log -RedirectStandardError $logErr
        Write-Host ('  ..  Backend lanzando (registro: ' + $log + ').') -ForegroundColor DarkGray
    }
}

# ---------- 3. Túnel ngrok ----------
$ngrok = Get-Process -Name 'ngrok' -ErrorAction SilentlyContinue
if ($ngrok) {
    Write-Host '  [ok] ngrok ya esta corriendo.' -ForegroundColor Green
} elseif ($SoloEstado) {
    Write-Host '  [!!] ngrok NO esta corriendo.' -ForegroundColor Yellow
} else {
    $rutaNgrok = Ruta-Ngrok
    if (-not $rutaNgrok) {
        Write-Host '  [X] No encuentro ngrok.exe. Instalalo con: winget install Ngrok.Ngrok' -ForegroundColor Red
    } else {
        Start-Process -FilePath $rutaNgrok -ArgumentList 'http',$puerto
        Write-Host '  ..  ngrok lanzado.' -ForegroundColor DarkGray
    }
}

# ---------- 4. Espera a que respondan ----------
$apiOk = $false
$urlPublica = $null

if (-not $SoloEstado) {
    Write-Host '  ..  Esperando a que respondan (max 90 s)...' -ForegroundColor DarkGray

    $fin = (Get-Date).AddSeconds(90)
    while ((Get-Date) -lt $fin) {
        if (Responde "$api/api/technologies?domain=programacion") { $apiOk = $true; break }
        Start-Sleep -Milliseconds 1000
    }

    $fin = (Get-Date).AddSeconds(45)
    while ((Get-Date) -lt $fin) {
        $urlPublica = Tunel-Publico
        if ($urlPublica) { break }
        Start-Sleep -Milliseconds 1000
    }
} else {
    $apiOk = Responde "$api/api/technologies?domain=programacion"
    $urlPublica = Tunel-Publico
}

# ---------- resumen ----------
Write-Host ''
Write-Host '== Estado ==' -ForegroundColor Cyan
$sqlEstado = if ($null -ne $sql) { $sql.Status } else { 'desconocido' }
Write-Host ('  SQL      : ' + $sqlEstado)
if ($apiOk) {
    Write-Host ('  API      : respondiendo en ' + $api) -ForegroundColor Green
} else {
    Write-Host ('  API      : SIN RESPUESTA en ' + $api) -ForegroundColor Red
}
if ($urlPublica) {
    Write-Host ('  Publico  : ' + $urlPublica) -ForegroundColor Green
} else {
    Write-Host '  Publico  : sin túnel (ngrok no contesta en el 4040)' -ForegroundColor Red
}

# Si la API no responde, lo más útil es enseñar lo que dijo el proceso: casi
# siempre ahí está el motivo (build fallido, puerto ocupado, BD caída).
if (-not $apiOk) {
    if (Test-Path $log) {
        Write-Host ''
        Write-Host ('Últimas líneas de ' + $log + ':') -ForegroundColor DarkGray
        Get-Content $log -Tail 12 | ForEach-Object { Write-Host ('    ' + $_) -ForegroundColor DarkGray }
    }
    if ((Test-Path $logErr) -and ((Get-Item $logErr).Length -gt 0)) {
        Write-Host '  Errores:' -ForegroundColor DarkGray
        Get-Content $logErr -Tail 8 | ForEach-Object { Write-Host ('    ' + $_) -ForegroundColor DarkGray }
    }
}

if ($apiOk -and $urlPublica) { exit 0 }
exit 1
