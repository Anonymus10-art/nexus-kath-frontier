# Guía paso a paso — NEXUS KATH FRONTIER

## 1. Qué hará el instalador final

Tus amigos descargarán un solo archivo parecido a:

`NEXUS-KATH-FRONTIER-Setup-v0.1.0.exe`

Al abrirlo, instalará el launcher. En el primer uso, el botón **JUGAR AHORA** hará
lo siguiente:

1. Comprueba si existe Java 21 de 64 bits.
2. Si no existe, descarga un runtime privado Eclipse Temurin 21.
3. Inicia sesión con Microsoft.
4. Descarga Minecraft 1.21.1 desde los servicios oficiales.
5. Instala NeoForge 21.1.248.
6. Descarga los mods y las configuraciones de NEXUS KATH FRONTIER.
7. Verifica todos los archivos mediante SHA-256.
8. Abre Minecraft con la RAM configurada.

Las siguientes veces solo descargará los archivos nuevos o modificados.

## 2. Crear el repositorio de GitHub

1. En GitHub, crea un repositorio llamado `nexus-kath-frontier`.
2. Sube a ese repositorio todo el contenido de esta carpeta.
3. Abre `src/NexusKathFrontier.Launcher/appsettings.json`.
4. Cambia esta dirección:

   `https://github.com/OWNER/nexus-kath-frontier/releases/latest/download/manifest.json`

   Por la dirección que incluya tu usuario. Ejemplo:

   `https://github.com/Kathsitaa/nexus-kath-frontier/releases/latest/download/manifest.json`

## 3. Registrar el inicio de sesión Microsoft

El launcher necesita su propia aplicación pública de Microsoft; no guardes
contraseñas ni tokens dentro del código.

1. Abre el portal de Microsoft Entra y entra en **App registrations**.
2. Pulsa **New registration**.
3. Nombre: `NEXUS KATH FRONTIER Launcher`.
4. Permite cuentas personales de Microsoft/Xbox además de cuentas organizativas.
5. Registra la aplicación y copia el valor **Application (client) ID**.
6. En la configuración de autenticación de la aplicación, habilita el flujo de
   cliente público indicado por la documentación de CmlLib/MSAL.
7. Pega el identificador en `microsoftClientId` dentro de `appsettings.json`.

El Client ID no es una contraseña. No agregues secretos de cliente al launcher.

## 4. Agregar los mods

Copia únicamente los archivos del cliente dentro de `pack`:

- `pack/mods`: mods `.jar`.
- `pack/config`: configuraciones de los mods.
- `pack/resourcepacks`: paquetes de recursos opcionales.
- `pack/shaderpacks`: shaders opcionales.

No copies `saves`, cuentas, logs, capturas ni archivos oficiales de Minecraft.

## 5. Crear la primera actualización

Abre PowerShell en la raíz del proyecto y ejecuta:

```powershell
Set-ExecutionPolicy -Scope Process Bypass
.\tools\New-PackManifest.ps1 -Version "0.1.0" -Repository "TU-USUARIO/nexus-kath-frontier"
```

El script crea `release-assets`. Después:

1. En GitHub, abre **Releases** y elige **Draft a new release**.
2. Usa el tag `pack-0.1.0`.
3. Sube todos los archivos que estén dentro de `release-assets`.
4. Publica la release.

Para una actualización futura, cambia solo los mods necesarios y genera, por
ejemplo, la versión `0.1.1`. El launcher comparará los hashes y descargará solo
los cambios.

## 6. Compilar el instalador

### Opción recomendada: GitHub Actions

1. En el repositorio, abre la pestaña **Actions**.
2. Selecciona **Compilar instalador**.
3. Pulsa **Run workflow**.
4. Cuando termine, descarga el artefacto
   `NEXUS-KATH-FRONTIER-Installer`.

### Compilación en tu PC

Instala .NET 8 SDK e Inno Setup. Después ejecuta:

```powershell
.\tools\Build-Launcher.ps1
```

El resultado aparecerá en `artifacts\installer`.

## 7. Probar antes de compartir

Haz la primera prueba en un usuario nuevo de Windows o en otro equipo:

1. No debe pedir permisos de administrador.
2. Debe detectar o descargar Java 21.
3. Debe abrir el inicio de sesión Microsoft.
4. Debe mostrar la versión `0.1.0` del modpack.
5. Minecraft debe iniciar usando `neoforge-21.1.248`.
6. Debe aparecer el servidor `68.129.97.89:25565`.
7. El botón **REPARAR INSTALACIÓN** debe restaurar un mod eliminado.

## 8. Archivos instalados en cada PC

El launcher guarda su contenido en:

`%LOCALAPPDATA%\NexusKathFrontier`

Dentro estarán el juego, Java privado, caché, ajustes y logs. No modifica la
instalación normal de `.minecraft` del jugador.
