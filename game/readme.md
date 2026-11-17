# game

Contiene la interfaz gráfica, la presentación del mundo, los menús y la interacción con el jugador.

No implementa reglas ni cálculos de simulación.

## UI-01 — Proyecto gráfico mínimo

Proyecto ejecutable C# / .NET 10 con OpenTK 4.9.4. Abre una ventana
redimensionable titulada `Evolve`, de 1280 × 720 píxeles iniciales, con fondo
azul oscuro sólido. Requiere el SDK de .NET 10 y un controlador gráfico con
soporte para OpenGL 3.3.

`EvolveEntryPoint.cs` inicia la aplicación y garantiza la disposición de la ventana al
terminar el bucle. `EvolveWindow.cs` contiene únicamente la configuración de la
ventana y el repintado del fondo. El cierre nativo (X o Alt+F4 en Windows)
finaliza el bucle; OpenTK libera la ventana y su contexto mediante `Dispose`.
No se crean recursos gráficos adicionales ni referencias a otros módulos.

The root `VERSION` file supplies all game version metadata through
`Evolve.Game.csproj`. `GameVersion.Display` reads `AssemblyInformationalVersion`
to preserve the exact prefix and leading zeros; numeric assembly and file
versions are normalized independently. Rebuild after changing `VERSION`.
Visible version text and menu presentation belong to UI-02.

### Validación del responsable / Auditor

Desde la raíz del repositorio, en PowerShell:

```powershell
dotnet restore .\game\Evolve.Game.csproj
dotnet build .\game\Evolve.Game.csproj --configuration Release --no-restore
dotnet run --project .\game\Evolve.Game.csproj --configuration Release --no-build
```

Comprobar que la ventana se abre con el título y el fondo indicados, que el
fondo cubre el área al redimensionar, minimizar y restaurar, y que X y Alt+F4
cierran la aplicación limpiamente (en ejecuciones separadas). Tras cada cierre,
`$LASTEXITCODE` debe ser `0` y no debe quedar un proceso de Evolve activo.

La compilación, la ejecución y el resultado visual quedan pendientes de
validación por el Auditor y aprobación del responsable. Esta entrega se limita
a UI-01 y no cambia el estado del roadmap.
