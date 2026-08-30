# NEXUS KATH FRONTIER Launcher

Launcher nativo de Windows para el modpack **NEXUS KATH FRONTIER**.

## Funciones incluidas

- Interfaz WPF tecnológica y futurista.
- Inicio de sesión oficial con Microsoft (Minecraft Java comprado).
- Descarga de los archivos oficiales de Minecraft 1.21.1.
- Detección automática de Java 21 y runtime Temurin privado si hace falta.
- Instalación automática de NeoForge 21.1.248.
- Actualización diferencial del modpack desde GitHub Releases.
- Verificación SHA-256 antes de instalar cada archivo.
- Reparación de archivos faltantes o modificados.
- Ajuste de RAM entre 4 y 16 GB.
- Agrega automáticamente `68.129.97.89:25565` a la lista de servidores.
- Instalador `.exe` para Windows x64 mediante Inno Setup.
- Compilación automática mediante GitHub Actions.

## Carpetas importantes

| Ruta | Uso |
| --- | --- |
| `src/NexusKathFrontier.Launcher` | Código y diseño del launcher |
| `pack` | Mods y configuración que se publicarán |
| `tools/New-PackManifest.ps1` | Genera manifiesto, hashes y assets |
| `tools/Build-Launcher.ps1` | Compila launcher e instalador en Windows |
| `installer/setup.iss` | Configuración del instalador `.exe` |
| `.github/workflows` | Compilación automática en GitHub |

## Estado de esta versión

Esta es la base funcional **0.1.0**. Antes de distribuirla hay que sustituir dos
valores provisionales en `appsettings.json`:

1. `OWNER` por el propietario y repositorio reales de GitHub.
2. `YOUR-AZURE-APP-CLIENT-ID` por el identificador de la aplicación Microsoft.

Lee `GUIA-PASO-A-PASO.md` para terminar la configuración.

## Importante

El proyecto no incluye ni redistribuye Minecraft. El launcher descarga los
archivos oficiales y exige una cuenta Microsoft que posea Minecraft: Java Edition.
