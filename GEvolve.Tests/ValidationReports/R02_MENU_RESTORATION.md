# R02 — Restauración del menú y navegación original

Base: d513d88c6616abf0828eebe35b7da3bcb7351446, decisión y regresión de UI documentadas por el director. Rama worldgeneration. **Implementado y revisado estáticamente; compilación, suites posteriores al cambio, evaluación visual, auditoría y aprobación pendientes. R02 NO aprobado.** Codex no ejecutó .NET, juego, capturas ni automatización visual.

## Interfaz restaurada

Se consultaron exclusivamente como referencia de presentación Legacy/Program.cs (RenderMenu/DrawButtons) y Legacy/Screens/ScreenLayout.cs. No se importan, invocan ni compilan esas fuentes.

- Inicio con Evolve/version y Civilization Simulator, fondo en dos franjas, botones centrados con orden **Crear mundo, Laboratorio, Opciones y Salir**, dimensiones adaptables, sombras, bordes y realce de puntero conforme a la composición anterior.
- Crear mundo conserva el formulario con tamaño, tipo de mapa, semilla editable/aleatoria, Generar mundo y Volver. Los selectores ofrecen los cuatro tamaños y los tres tipos visibles de Definitions; son solo preferencias de interfaz, no configuración de motor o partida.
- Generar mundo está deshabilitado y excluido de hit testing, sin handler ni API de generación. Aviso permanente: generación no disponible en R02. No hay servicio, tarea de generación, fallback, estado geográfico, cuadrícula de creación o persistencia nuevos.
- Laboratorio abre el GridLaboratory nuevo sin modificaciones de su modelo, renderer, cámara o geometría. Solo cuadrícula neutral, semilla, tamaño, zoom y desplazamiento. Las preferencias del formulario no se trasladan a la cuadrícula ni añaden capas.
- Opciones recupera la pantalla informativa «Opciones avanzadas pendientes» y Volver. No inventa ajustes ni datos persistentes.
- Volver/Escape descartan el formulario o la sesión de cuadrícula; Escape cierra primero un selector abierto. Salir solicita el cierre normal desde el menú, sin crear una ventana en las pruebas.

GameSession modela destinos y salida; WorldCreationForm guarda exclusivamente Size/Type/SeedText y CanGenerate=false. El editor existente se comparte mediante foco y callbacks entre campo de creación y laboratorio. GridLaboratoryLayout comparte dibujo/hit testing y descarta botones deshabilitados; sus dropdowns solo existen en CreateWorld. No hay nueva arquitectura de generación ni cambios a R03.

## Pruebas preparadas, no ejecutadas

MenuNavigation se añade como quinta suite oficial, junto a Grid, Laboratory, TextEditing y LegacyIsolation. Las cuatro suites anteriores tienen evidencia local previa al cambio, tal como registra el roadmap; esos PASS no certifican esta restauración.

MenuNavigationTests cubre:

1. Orden, etiquetas y acceso a las cuatro opciones del menú; centrado, no solapamiento y límites en 640×480, 800×600, 1280×720 y 1920×1080.
2. Presencia de todos los controles de creación, dropdowns y prioridad de clics; Generar deshabilitado y sin destino de clic.
3. Navegación Crear mundo/Volver, Opciones/Volver, Laboratorio/Volver y solicitud de salida; guardas para no sustituir pantallas activas desde métodos exclusivos del menú.
4. Preferencias de formulario, validación int32 positiva/negativa, rechazo de texto/rango inválidos, pegado con TextFieldEditor sin duplicación y semilla aleatoria sin generación.
5. Separación entre creación y laboratorio, descarte de preferencias al volver y valores iniciales independientes al reentrar.

Se adapta la aserción de navegación de Laboratory a la pantalla de formulario visible, manteniendo la comprobación de no generación. Las pruebas de aislamiento y las fuentes geográficas históricas no se modifican. No se abre GameWindow ni contexto GL en los tests.

## Conservación del aislamiento

Revisión estática: diez fuentes manuales activas de Game, cero referencias a namespaces/tipos de Engine.World, WorldGenerationService, GeneratedWorld, PangaeaGeneration o comparador. Los nombres Pangea/Continentes/Archipiélagos son valores de Definitions para el formulario, no generadores activos.

Sin cambios en las exclusiones de Engine/Game/Tests ni en el script de validación; se conserva la corrección de sus tres inicializaciones. Sin cambios en worldgenerationold, Game/Legacy, Tests/Legacy, Definitions, State, Grid, guardados o baselines. GridLaboratory.cs, HexGridRenderer.cs, MapCamera2D.cs, UiRenderer.cs, ShaderProgram.cs y TextFieldEditor.cs conservan sus blobs anteriores. No hay etapas, capas, alturas o comparador histórico activos.

XML de proyectos y sintaxis PowerShell revisados estáticamente; git diff --check revisado. No se infiere compilación C# ni aprobación visual de esas comprobaciones. La fidelidad visual, entrega de eventos, cierre de ventana y fluidez corresponden al usuario dentro de Evolve.

## Archivos modificados

- GEvolve.Game/GameSession.cs
- GEvolve.Game/WorldCreationForm.cs (nuevo)
- GEvolve.Game/Screens/GridLaboratoryLayout.cs
- GEvolve.Game/Program.cs
- GEvolve.Tests/Active/MenuNavigationTests.cs (nuevo)
- GEvolve.Tests/Active/GridLaboratoryTests.cs
- GEvolve.Tests/Active/Program.cs
- GEvolve.Game/GRID_LABORATORY.md
- GEvolve.Engine/World/WorldGeneration/README.md
- GEvolve.Engine/World/WorldGeneration/ROADMAP.md
- .agents/PROJECT_MEMORY.md
- GEvolve.Tests/ValidationReports/README.md
- GEvolve.Tests/ValidationReports/R02_MENU_RESTORATION.md (este informe)

## Validación local pendiente

Desde la raíz en worldgeneration:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\GEvolve.Tests\ValidationReports\commands.ps1 -ValidationSet GridLaboratory -Label R02-menu -IsolatedArtifacts
```

Captura compilación y las cinco suites oficiales sin ventana. Los informes locales ajenos y los resultados históricos se conservan sin modificaciones ni inclusión en este commit. Los anteriores 17 PASS/2 FAIL geográficos siguen siendo históricos y no se consideran resueltos por las suites R02.

Revisión interactiva exclusiva del usuario, después de compilar:

```powershell
dotnet run --project GEvolve.Game
```

Revisar diseño inicial, las cuatro opciones, formulario y Generar deshabilitado, selección/pegado, Opciones/Volver, Escape, entrada/salida del laboratorio y navegación de cuadrícula; cerrar normalmente. No ejecutar modos gráficos automatizados. Auditor y director deciden la aprobación de R02.
