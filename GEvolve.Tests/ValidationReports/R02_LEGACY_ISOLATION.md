# R02 — Desconexión del legado y laboratorio de cuadrícula

Fecha UTC de revisión: 2026-11-09T15:13:31.2368802Z. Base autorizada: 43222f5f8c2e6f26089c29f02e46bbc3c0428b45, decisión R02 del director.

**Implementado y revisado estáticamente. R02 NO aprobado.** No se ejecutaron .NET, compilación, suites, perfiles, juego, GL, capturas ni pruebas visuales. La resolución final de tipos/ensamblados y apariencia requieren validación local del usuario, auditor y director.

[Roadmap único](../../GEvolve.Engine/World/WorldGeneration/ROADMAP.md), [guía activa](../../GEvolve.Game/GRID_LABORATORY.md).

## Decisión y aislamiento

- Engine: Compile Remove World/worldgenerationold/**/*.cs. Los 34 archivos C# y su documentación se conservan intactos. No hay proyecto alternativo, opción LegacyEnabled ni fallback que los reactive.
- Se excluye también Geometry/HexDemoWorld.cs, el antiguo demo de alturas sintéticas fuera de worldgenerationold. Se conserva su archivo; Game no lo usa. Grid/, HexCoord y geometría común no se modifican.
- Game: Compile Remove Legacy/**/*.cs. Sus 16 fuentes anteriores se archivan byte a byte bajo Legacy, incluidos Program, GameSession, PangaeaLaboratory/Window, layouts, renderer, datos/capas/paletas/comparador. No quedan rutas activas de creación, regeneración, resize de mundos geográficos ni etapas antiguas.
- Se mantienen activos únicamente los componentes gráficos/de edición genéricos UiRenderer, ShaderProgram, MapCamera2D y TextFieldEditor, con blobs idénticos. La referencia directa Game→State se retira al dejar de usar estado persistible; State y sus contratos siguen intactos y referenciados por Engine/Tests.
- Tests: EnableDefaultCompileItems=false, Compile Include Active/**/*.cs. Las 32 fuentes C# históricas permanecen en sus rutas originales, incluido el Program antiguo, fuera de la compilación. Los seis archivos de baselines/referencias y sus recursos conservan el contenido previo.
- El runner oficial acepta exclusivamente --all-suites y --grid-laboratory; rechaza flags históricos con código 2. --all-suites ahora identifica explícitamente el alcance R02 (cuatro suites), NO la suite geográfica antigua.
- La versión anterior de commands.ps1 se copia sin cambios a Tests/Legacy. Los snapshots de informes no cambian. El script activo admite solo GridLaboratory y captura build+suite nueva, con privacidad, streams íntegros, códigos reales, omisión si build falla y artefactos aislados. No inicia Game ni ofrece modos geográficos.

Las fuentes archivadas contienen referencias históricas por diseño, pero no se incluyen en Compile ni se invocan mediante reflexión. Las únicas cadenas antiguas de los tests nuevos son comprobaciones de ausencia en metadatos de ensamblados; no son dependencias compiladas ni invocaciones del legado.

## Flujo activo

GameSession solo permite menú, pantalla de creación no disponible y laboratorio de cuadrícula. No tiene GeneratedWorld, LandmassState, servicio de generación ni guardado de mundo parcial. GridLaboratory posee GridSize, texto/valor int32 de semilla, error de edición y HexGrid. La semilla es metadato de sesión; no cambia celdas ni colores y no constituye un generador ficticio.

Al entrar: Small/2026 y cuadrícula neutral. Tamaño reconstruye HexGrid con dimensiones oficiales y ajusta cámara; semilla mantiene geometría, zoom y desplazamiento. Rueda/arrastre solo sobre el área de mapa; foco y Ctrl+A/C/X/V del editor existente solo actúan sobre el campo. Entradas inválidas/out-of-range se rechazan y texto parcial no produce datos geográficos. Salir/Escape libera la sesión y recursos; reentrar restaura valores iniciales.

El renderer es exclusivamente espacial: geometría flat-top, instancias de centros visibles, gris neutro fijo, bordes, wrap E/O y límites N/S de la cámara. No tiene alturas ni etiquetas de elevación, océanos/superficie, placas/movimientos/esfuerzos, cordilleras, clima, biomas, recursos, inspector geográfico o comparador. No se normalizan ni fabrican campos ausentes.

R03 no se implementa: no hay nuevo orquestador ni snapshot geográfico duplicado. La presentación consume HexGrid directamente; el contrato mínimo de snapshot de R03 sigue pendiente del arquitecto.

## Preservación de fuentes de Game

Base: 43222f5. Cada ruta actual es GEvolve.Game/Legacy/ seguida de la ruta relativa de la tabla. Se comprobó el blob original con git rev-parse y el archivo archivado con git hash-object (sin -w).

| Ruta relativa anterior | Blob idéntico en el archivo |
|---|---|
| GameSession.cs | 9ec756c674a96b42587e33d2703e6cfa9592e397 |
| Graphics/BiomePalette.cs | 19fb085735aa4028e59d792b190610fd93749d3c |
| Graphics/ContinentalGeologyPalette.cs | c8e227bcd0bd591b33c65e98240f9a68d828c4eb |
| Graphics/HexGridRenderer.cs | a84b448873789196c24778d7116aba885b611453 |
| Graphics/MapViewMode.cs | c7dfdcdfbbc367932953853a527dedb4b14e7bca |
| Graphics/PangaeaHeightLabels.cs | 57aae5a47ee2dda2ce54e90a27591bae16d666a4 |
| Graphics/PangaeaMapData.cs | 88d1a88c683dc5f77891ab0d1c0fe3fb5586ce83 |
| Graphics/PlatePalette.cs | 4e550e0e41a504b7b572253abdd1317098f53b1c |
| Graphics/RainfallPalette.cs | 9829e991764b0af1395afff91c18b061a5d190d0 |
| Graphics/TectonicStressPalette.cs | b8b371e17a9cfecb1549d065fa46bc8fba1ae002 |
| Graphics/ThermalPalette.cs | 996beb5622982333056a1e9fd611d94406ada89b |
| PangaeaLaboratory.cs | 8631942290911649c1e8c2dd906b55889a79f5f9 |
| PangaeaLaboratoryWindow.cs | cf1a79f10c758e6d59220434ec79c3d828a1cb7d |
| Program.cs | 7c2f82212be361be2ab3d6a86fc7a07c2d68a6c4 |
| Screens/PangaeaLaboratoryLayout.cs | 1ee120ef8852d857302965f5e071f289944025fa |
| Screens/ScreenLayout.cs | 37b04ac30b2cbe817f3c6251cd98f428993ab79a |

## Revisión estática realizada

- Exclusiones verificadas leyendo XML de Engine/Game/Tests: 34 C# de worldgenerationold, demo sintético y 16 C# de Game archivado fuera del build; Tests solo Active.
- Nueve fuentes manuales activas de Game y cinco de Engine, descontando Legacy/worldgenerationold/bin/obj y demo excluido: cero referencias a namespaces/tipos geográficos antiguos. La inspección considera los conjuntos definidos por el proyecto; no confunde archivos históricos conservados con fuentes activas.
- 16/16 archivos C# de Game archivados con hash idéntico; cuatro utilidades compartidas intactas; 38/38 archivos históricos de pruebas y recursos/baselines sin cambios. La copia del script histórico es exacta.
- Cero modificaciones en Definitions, State, Engine/Grid y Engine/World/worldgenerationold frente al commit base; ningún JSON o formato de guardado cambia.
- Parser PowerShell del script nuevo y XML de proyectos revisados sin ejecución de sus comandos. La compilación efectiva de SDK, el shader y los tests NO se consideran aprobados por esta revisión.
- Los archivos locales ajenos .BiomeTests.cs.swp y ValidationReports/R00-R01_20261109_041729114Z_167839a8 se conservan fuera del commit. Se mantiene master en 710f9fc56ecdfd18b96210967acf0fdf9f9ba284; publicación solo en worldgeneration y avance rápido de develop.

## Pruebas oficiales nuevas y pruebas afectadas

Active/GridLaboratoryTests.cs define:
1. Grid: cuatro dimensiones oficiales, identidades/direcciones, reproducción exacta, vecinos simétricos, seam E/O y bordes N/S abiertos.
2. Laboratory: creación explícitamente no disponible sin fallback; tamaños/semilla int32, errores y reinicio/reentrada; geometría independiente de semilla; límites del layout en 1280×720/800×600/640×480; zoom/pan sin alterar datos.
3. TextEditing: pegado positivo/negativo, copiar/cortar/seleccionar/sustituir, rechazo de rango inválido, Home/End/Backspace/Delete, foco y ausencia de duplicación de texto.
4. LegacyIsolation: comprobación por metadatos de que el servicio histórico, demo y controladores/comparador Pangea no forman parte de los ensamblados oficiales. No instancia ventanas/renderers ni GL.

Quedan fuera de ejecución las pruebas anteriores de World con demo sintético, Grid con HexTopology histórico, Landmass/ContinentalGeology, tectónica/placas/contactos, pendientes/temperatura/precipitación/biomas, todas las de Pangea y Game/TextFieldEditing vinculadas a controladores previos. Sus fuentes no se alteran para que pasen; se conserva su contexto, sus hashes y los informes existentes.

Los 17 PASS/2 FAIL históricos siguen siendo deuda/evidencia histórica. Un futuro PASS de las cuatro suites R02 no corrige esos fallos ni valida geografía; los reportes nuevos lo explicitan.

## Archivos y referencias modificados

- Engine: GEvolve.Engine.csproj (exclusiones), World/WorldGeneration/README.md y ROADMAP.md (estado R02, sin aprobación).
- Game: GEvolve.Game.csproj, GameSession.cs, Program.cs, GridLaboratory.cs, Screens/GridLaboratoryLayout.cs y Graphics/HexGridRenderer.cs; las 16 rutas anteriores pasan a Legacy según tabla. Legacy/README.md explica la exclusión.
- Game: GRID_LABORATORY.md nueva y PANGAEA_LABORATORY.md marcada histórica.
- Tests: GEvolve.Tests.csproj, Active/Program.cs y Active/GridLaboratoryTests.cs; Legacy/commands.ps1 y README.md; ValidationReports/commands.ps1, README.md y este informe.
- Operación: .agents/PROJECT_MEMORY.md y skills evolve-architecture/evolve-worldgen/evolve-testing alineadas con la desconexión, sin autorizaciones antiguas de ejecución.
- Referencias activas eliminadas: GEvolve.Engine.World.WorldGenerationService, PangaeaGenerationSession/Snapshot/Stage, GeneratedWorld, PangaeaElevationDiagnostics, PangaeaBaseComparison/Variant, tipos de tectónica/biomas, PangaeaMapData/HeightLabels/MapViewMode y antiguo ScreenLayout. Todos permanecen donde corresponda en código archivado o State histórico, sin dependencias activas desde Game.
- No se cambian algoritmos originales, Grid, catálogos, baselines, guardados ni contenido de fuentes históricas.

## Validación pendiente del usuario

Desde la raíz, rama worldgeneration:

~~~powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .GEvolve.TestsValidationReportscommands.ps1 -ValidationSet GridLaboratory -Label R02 -IsolatedArtifacts
~~~

Equivalentes directos, sin captura automática:

~~~powershell
dotnet build
dotnet run --project GEvolve.Tests -- --all-suites
~~~

La revisión visual manual corresponde únicamente al usuario:

~~~powershell
dotnet run --project GEvolve.Game
~~~

Verificar menú y no disponibilidad de Crear mundo, cuadrícula neutral en cuatro tamaños, edición/pegado de semillas, zoom/pan, salida/reentrada y ausencia completa de etapas/capas antiguas. Cerrar la ventana normalmente. No hay --graphics-smoke ni automatización visual.

No se certifican frames, pico de memoria, eventos nativos, compilación o apariencia. El auditor revisará el alcance y el director aprobará R02; el snapshot de R03 permanece pendiente.
## Inventario completo del diff

Las rutas con D se retiran del flujo original y se conservan en Legacy según la tabla de hashes; ninguna fuente histórica se pierde. M/A corresponden a configuración, fuentes activas o documentación. Estado revisado antes del commit:

~~~text
M	.agents/PROJECT_MEMORY.md
M	.agents/skills/evolve-architecture/SKILL.md
M	.agents/skills/evolve-testing/SKILL.md
M	.agents/skills/evolve-worldgen/SKILL.md
M	GEvolve.Engine/GEvolve.Engine.csproj
M	GEvolve.Engine/World/WorldGeneration/README.md
M	GEvolve.Engine/World/WorldGeneration/ROADMAP.md
M	GEvolve.Game/GEvolve.Game.csproj
A	GEvolve.Game/GRID_LABORATORY.md
M	GEvolve.Game/GameSession.cs
D	GEvolve.Game/Graphics/BiomePalette.cs
D	GEvolve.Game/Graphics/ContinentalGeologyPalette.cs
M	GEvolve.Game/Graphics/HexGridRenderer.cs
D	GEvolve.Game/Graphics/MapViewMode.cs
D	GEvolve.Game/Graphics/PangaeaHeightLabels.cs
D	GEvolve.Game/Graphics/PangaeaMapData.cs
D	GEvolve.Game/Graphics/PlatePalette.cs
D	GEvolve.Game/Graphics/RainfallPalette.cs
D	GEvolve.Game/Graphics/TectonicStressPalette.cs
D	GEvolve.Game/Graphics/ThermalPalette.cs
A	GEvolve.Game/GridLaboratory.cs
A	GEvolve.Game/Legacy/GameSession.cs
A	GEvolve.Game/Legacy/Graphics/BiomePalette.cs
A	GEvolve.Game/Legacy/Graphics/ContinentalGeologyPalette.cs
A	GEvolve.Game/Legacy/Graphics/HexGridRenderer.cs
A	GEvolve.Game/Legacy/Graphics/MapViewMode.cs
A	GEvolve.Game/Legacy/Graphics/PangaeaHeightLabels.cs
A	GEvolve.Game/Legacy/Graphics/PangaeaMapData.cs
A	GEvolve.Game/Legacy/Graphics/PlatePalette.cs
A	GEvolve.Game/Legacy/Graphics/RainfallPalette.cs
A	GEvolve.Game/Legacy/Graphics/TectonicStressPalette.cs
A	GEvolve.Game/Legacy/Graphics/ThermalPalette.cs
A	GEvolve.Game/Legacy/PangaeaLaboratory.cs
A	GEvolve.Game/Legacy/PangaeaLaboratoryWindow.cs
A	GEvolve.Game/Legacy/Program.cs
A	GEvolve.Game/Legacy/README.md
A	GEvolve.Game/Legacy/Screens/PangaeaLaboratoryLayout.cs
A	GEvolve.Game/Legacy/Screens/ScreenLayout.cs
M	GEvolve.Game/PANGAEA_LABORATORY.md
D	GEvolve.Game/PangaeaLaboratory.cs
D	GEvolve.Game/PangaeaLaboratoryWindow.cs
M	GEvolve.Game/Program.cs
A	GEvolve.Game/Screens/GridLaboratoryLayout.cs
D	GEvolve.Game/Screens/PangaeaLaboratoryLayout.cs
D	GEvolve.Game/Screens/ScreenLayout.cs
A	GEvolve.Tests/Active/GridLaboratoryTests.cs
A	GEvolve.Tests/Active/Program.cs
M	GEvolve.Tests/GEvolve.Tests.csproj
A	GEvolve.Tests/Legacy/README.md
A	GEvolve.Tests/Legacy/commands.ps1
A	GEvolve.Tests/ValidationReports/R02_LEGACY_ISOLATION.md
M	GEvolve.Tests/ValidationReports/README.md
M	GEvolve.Tests/ValidationReports/commands.ps1
~~~

Comprobaciones finales: 31 enlaces relativos revisados, cero rotos; cero errores de sintaxis PowerShell/XML y de git diff --check. El swap mantiene SHA256 0E965BA31D551ADAB8F57FA61EC946A866B0D3C4F2A820A13734A95338DB68BF. Ninguna compilación/prueba ejecutada.
