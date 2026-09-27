<#
.SYNOPSIS
    Crea o actualiza la base de datos de PixMarket a partir del modelo de EF Core.

.DESCRIPTION
    La API ya hace esto sola al arrancar (PixMarketAPI/Data/InicializadorBaseDatos.cs).
    Este script es para hacerlo cuando quieras, sin depender de la API:

        - crea la base y las tablas si no existen;
        - agrega las columnas, indices y claves foraneas que falten;
        - ensancha las columnas de texto que queden cortas;
        - inserta el administrador y la configuracion de la tienda si estan vacias.

    Nunca borra ni modifica datos existentes, asi que se puede ejecutar las veces
    que haga falta.

.PARAMETER Conexion
    Cadena de conexion de MySQL. Si no se indica, se busca en este orden:
    1) la variable de entorno ConnectionStrings__DefaultConnection;
    2) ConnectionStrings:DefaultConnection en PixMarketAPI/appsettings.json.

.EXAMPLE
    .\Scripts\actualizar-bd.ps1

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File .\Scripts\actualizar-bd.ps1

.EXAMPLE
    .\Scripts\actualizar-bd.ps1 -Conexion "Server=localhost;Database=PixMarket;User=root;Password=***"

.EXAMPLE
    .\Scripts\actualizar-bd.ps1 -CifrarContrasenas

.NOTES
    Con -CifrarContrasenas se convierte a hash (PBKDF2) las contrasenas que
    quedaron guardadas en texto plano. No hace falta usarlo: la API recifra
    sola la contrasena de cada usuario en su primer inicio de sesion. Sirve
    para no dejar ninguna clave legible en la base de datos.

    Si Windows bloquea la ejecucion de scripts ("no se puede cargar el archivo
    ... porque la ejecucion de scripts esta deshabilitada"), usa la segunda
    forma con -ExecutionPolicy Bypass, o habilitala para tu usuario con:
        Set-ExecutionPolicy -Scope CurrentUser RemoteSigned
#>
[CmdletBinding()]
param(
    [string]$Conexion,
    [ValidateSet('Debug', 'Release')]
    [string]$Configuracion = 'Debug',

    # Convierte a hash las contrasenas que quedaron en texto plano en la base.
    [switch]$CifrarContrasenas
)

$ErrorActionPreference = 'Stop'

# --- Rutas -------------------------------------------------------------------

$raiz      = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$proyecto  = Join-Path $raiz 'PixMarketAPI\PixMarketAPI.csproj'
$carpeta   = Join-Path $raiz 'PixMarketAPI'
$libreria  = Join-Path $raiz "PixMarketAPI\bin\$Configuracion\net8.0\PixMarketAPI.dll"

function Escribir([string]$Color, [string]$Texto) {
    Write-Host $Texto -ForegroundColor $Color
}

# --- 1. Cadena de conexion ---------------------------------------------------

if ([string]::IsNullOrWhiteSpace($Conexion)) {
    $Conexion = $env:ConnectionStrings__DefaultConnection
}

if ([string]::IsNullOrWhiteSpace($Conexion)) {
    $archivo = Join-Path $carpeta 'appsettings.json'

    if (Test-Path $archivo) {
        $config = Get-Content $archivo -Raw | ConvertFrom-Json
        $Conexion = $config.ConnectionStrings.DefaultConnection
    }
}

if ([string]::IsNullOrWhiteSpace($Conexion)) {
    Escribir 'Red' 'ERROR: no se encontro la cadena de conexion.'
    Escribir 'Yellow' '  Indicala con -Conexion, define la variable de entorno'
    Escribir 'Yellow' '  ConnectionStrings__DefaultConnection, o ponela en'
    Escribir 'Yellow' '  PixMarketAPI/appsettings.json.'
    exit 1
}

# No mostrar la clave de la base en la consola.
$resumen = $Conexion -replace '(?i)(Password|Pwd)\s*=\s*[^;]*', '$1=***'
Escribir 'Cyan' "Conexion: $resumen"

# --- 2. Compilar -------------------------------------------------------------

Escribir 'Cyan' 'Compilando la API...'

& dotnet build $proyecto -c $Configuracion --nologo | Out-Host
if ($LASTEXITCODE -ne 0) {
    # Visual Studio puede tener abierto el .exe y bloquearlo.
    Escribir 'Yellow' 'No se pudo compilar. Reintento sin generar el ejecutable (.exe)...'

    & dotnet build $proyecto -c $Configuracion --nologo -p:UseAppHost=false | Out-Host
    if ($LASTEXITCODE -ne 0) {
        Escribir 'Red' 'ERROR: la API no compila. Revisa los errores de arriba.'
        exit 1
    }
}

if (-not (Test-Path $libreria)) {
    Escribir 'Red' "ERROR: no se encontro $libreria"
    exit 1
}

# --- 3. Sincronizar la base de datos ----------------------------------------

Escribir 'Cyan' 'Sincronizando la base de datos...'
Write-Host ''

$env:ConnectionStrings__DefaultConnection = $Conexion

# Se ejecuta dentro de la carpeta de la API para que encuentre appsettings.json.
Push-Location $carpeta
try {
    $argumentos = if ($CifrarContrasenas) { '--cifrar-contrasenas' } else { '--actualizar-bd' }

    & dotnet $libreria $argumentos
    $codigo = $LASTEXITCODE
}
finally {
    Pop-Location
}

Write-Host ''

if ($codigo -ne 0) {
    Escribir 'Red' "ERROR: la actualizacion fallo (codigo $codigo)."
    Escribir 'Yellow' 'Revisa que el servicio de MySQL este iniciado y que la'
    Escribir 'Yellow' 'cadena de conexion sea correcta.'
    exit $codigo
}

Escribir 'Green' 'Base de datos lista. Ya puedes iniciar la API.'
exit 0

