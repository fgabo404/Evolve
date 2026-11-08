# Laboratorio de Pangea — panel lateral y flujo secuencial

**Rama:** `worldgeneration_pangea`. Implementación: `86a7effcf8d0425b4c669b588590cdb4a93397a3` — `feat(game): reorganize Pangaea laboratory sidebar and sequential flow`. Fuente de esta validación: `51ca6458b7445334df4961ce7903640959ca3490` — `fix(tests): isolate laboratory outputs and respect periodic camera anchors`. La segunda revisión añade salidas aisladas y la comprobación correcta del anclaje periódico; no cambia el código funcional de Game.

**Alcance:** reorganización de la experiencia del laboratorio. No cambia código de Engine, Definitions o State, ni sus fórmulas, configuración, RNG, topología, superficie, BASE, H04, geometría montañosa, aporte o FINAL. Los baselines históricos, guardados y paleta global se mantienen. P03-A.7.3, P03-A.7.4 y P03-B no se inician; P03-A continúa sin aprobación.

**Restricción visual:** Codex no abrió Evolve, no creó ventanas/contextos GL, no generó capturas/mapas ni utilizó automatización visual. La instancia que ya tenía abierta el usuario no fue inspeccionada ni cerrada. Los tests de controles, cámara y colores son aserciones de código, no una aprobación de apariencia, legibilidad, fluidez o calidad geográfica. La inspección visual queda exclusivamente al usuario.

## Estructura implementada

La columna izquierda fija reúne **GENERACION DE MUNDO LAB**, tipo mapa, tamaño, semilla, vista, generación manual, información de celda y salida, en ese orden. Los controles no ocupan ni cubren el mapa. Inspector y **SALIR AL MENÚ** permanecen abajo a la izquierda. Se reutilizan los componentes bitmap, colores UI, renderer hexagonal, cámara y overlays de Evolve.

| Resolución de ventana | Columna | Ancho del mapa | Proporción del ancho para mapa |
|---|---:|---:|---:|
| 640×480 | 240 | 400 | 62.5 % |
| 800×600 | 280 | 520 | 65 % |
| 1280×720 | 280 | 1000 | 78.125 % |
| 1920×1080 | 280 | 1640 | 85.417 % |

Estas cifras son geometría del layout, no mediciones visuales. El mapa ocupa toda la altura. En ventanas bajas, la rueda desplaza solo los controles cuando el puntero está en ellos; el inspector conserva desplazamiento propio para leer todos los campos sin reducir su letra. La rueda sobre el mapa hace zoom. Los hit targets se recortan al mismo viewport de controles usado para dibujar; menús desplegables permanecen dentro de la columna. La información prioriza CellId/coordenadas y BASE/APORTE/FINAL, conservando el resto de los detalles mediante scroll.

Tipo mapa ofrece solo **PANGEA - ÚNICO DISPONIBLE**. No se anuncian otros generadores como disponibles en el laboratorio. El acceso habitual **Crear mundo** permanece operativo.

## Secuencia de interacción, separada del motor

| Acción visible | Prerrequisitos de la UI | Operación oficial |
|---|---|---|
| 01 Generar placas y movimientos | Cuadrícula inicial completada | Plates→Motion, dos etapas originales |
| 02 Generar superficies | 01 completada | Surface |
| 03 Generar elevaciones | 02 completada | Base |
| 04 Generar sistemas montañosos | 03 completada | MountainSystems |
| 05 Generar elevaciones finales | 04 completada | Final |

Mientras la cuadrícula automática se prepara no se habilitan operaciones incompatibles. Después solo 01 está disponible; cada éxito habilita el paso siguiente. Las etapas previas completadas pueden repetirse. Pendiente, ejecutándose, completada, bloqueada y error reflejan estados del motor más el requisito de precedencia del controlador. El controlador rechaza también llamadas posteriores fuera de secuencia, no solo deshabilita botones.

Engine conserva su DAG original (por ejemplo, sistemas sigue sin depender numéricamente de BASE); el bloqueo secuencial es exclusivamente una política de Game autorizada en esta tarea. No se agregan fórmulas, generadores o dependencias al motor. Repetir BASE conserva la geometría de sistemas e invalida FINAL según el contrato existente; ninguna etapa puede ejecutarse mientras falta una previa vigente o hay trabajo activo.

El dado genera un int32 nuevo, distinto del anterior, y elimina inmediatamente sesión, trabajo/cola, capas, estados y selección. Reinicia solo la cuadrícula; no hay confirmación ni generación completa implícita. Cambiar tamaño tiene el mismo reinicio inmediato. Editar semilla borra datos antes de aceptar una sesión nueva; un texto incompleto/fuera de int32 no habilita generación y requiere iniciar con texto válido. Una revisión local de sesión impide reutilizar recursos entre configuraciones aunque los contadores internos comiencen de nuevo.

El trabajo abandonado conserva observación/limpieza de sus tareas, pero pierde su ruta de publicación. Cancelar sigue siendo cooperativo; si H04 aún está en cola, no se inicia tras cancelar. Los datos parciales no se convierten en partidas guardables.

## Mapa, cámara y alturas integradas

`MapCamera2D.FitViewport` define el rectángulo real del mapa, con origen X igual al ancho de la columna y Y=0. Proyección del renderer y transformaciones de selección/zoom usan ese mismo origen. El `Fit` anterior conserva X=0, Y=58 y su tamaño original para el flujo tradicional. La proyección añade el desplazamiento horizontal/vertical de viewport, sin cambiar centros, forma o topología de los hexágonos.

Las alturas se solicitan siempre, sin botón ni interruptor. BASE usa BASE; APORTE usa aporte; FINAL usa FINAL; otras vistas usan FINAL vigente y después BASE. Sin un campo disponible no se inventa cero. Con escala ≥13 se anotan todas las celdas del rango visible, sin el antiguo corte por 1000 entradas. Al alejarse se espacian valores reales, hasta 600 anotaciones simplificadas; no se agregan ni promedian alturas. Los IDs de copias E/O siguen correspondiendo a su celda lógica.

SUPERFICIE conserva océano azul, tierra normal hasta 90 y blanco solo para tierra >90. Usa FINAL vigente o BASE y deja de leer FINAL inmediatamente cuando se invalida. La misma entrada visual almacenada alimenta colores, etiquetas e inspección; no hay snapshots o hashes por frame.

## Evidencia técnica y ejecución aislada

La [primera compilación normal](../P03-A721-sidebar_20261108_013206845Z_e1030014/REPORT.md) falló por DLLs bloqueadas por el juego ya abierto. No fue un fallo de los algoritmos ni se ejecutaron suites dependientes. Se preservan sus errores y advertencias. No se cerró el juego para continuar.

La ayuda local del SDK .NET 10.0.402 confirma `--artifacts-path` tanto en build como en run. `commands.ps1 -IsolatedArtifacts` pasa la misma carpeta relativa única a ambos comandos, bajo `GEvolve.Tests/bin/ValidationArtifacts/<runId>`. Esta carpeta está cubierta por la regla existente `**/bin/`; no se publican binarios/intermedios. No se necesitan dependencias externas. La repetición crea otra carpeta en vez de sobrescribir DLLs del proceso abierto.

Se conservan stdout/stderr y códigos individuales en [SUMMARY.md](SUMMARY.md), [build.log](build.log), [pangaea-laboratory.log](pangaea-laboratory.log), [pangaea-staged.log](pangaea-staged.log), [all-suites.log](all-suites.log) y [commands.ps1](commands.ps1). Las fechas de ejecución son UTC según el procedimiento de reportes; su fecha local inicial en Guatemala es 2026-11-07.

La suite del laboratorio comprueba cuatro tamaños: bloqueo secuencial y rechazo sin mutación, diferencia explícita UI/DAG original, siete operaciones reales, equivalencia con automático, repetición/invalidation, selección/inspección, reinicio inmediato manual/aleatorio/tamaño, descarte de trabajos/colas antiguos, cancelación/recuperación, caché sin snapshots ociosos, límites y accesibilidad del panel/menús/scroll, mayoría de espacio para mapa, origen y anclaje periódico de cámara, retorno al viewport tradicional, alturas densas/espaciadas y blanco >90. No comprueba pixels.

La suite por etapas conserva la referencia independiente de 28 mundos Pangea y 12 controles de otros tipos. La general incluye Game y laboratorio. El runner no emite un conteo individual de aserciones: no se inventa ese total. Los fallos históricos no autorizan actualizar sus baselines.

## Archivos modificados

| Área | Archivos |
|---|---|
| Controlador y pantalla | `GEvolve.Game/PangaeaLaboratory.cs`, `PangaeaLaboratoryWindow.cs`, `Program.cs` |
| Composición/hit testing | `GEvolve.Game/Screens/PangaeaLaboratoryLayout.cs` |
| Origen del mapa y overlays | `GEvolve.Game/Graphics/MapCamera2D.cs`, `HexGridRenderer.cs` |
| Alturas de campos vigentes | `GEvolve.Game/Graphics/PangaeaMapData.cs`, `PangaeaHeightLabels.cs` |
| Pruebas | `GEvolve.Tests/PangaeaLaboratoryTests.cs` |
| Captura aislada | `GEvolve.Tests/ValidationReports/commands.ps1`, `README.md` |
| Guía/estado | `GEvolve.Game/PANGAEA_LABORATORY.md`, `GEvolve.Engine/PANGAEA_ROADMAP.md`, `WORLD_GENERATION_ARCHITECTURE.md`, `WORLD_GENERATION_ROADMAP.md`, `.agents/PROJECT_MEMORY.md` |
| Evidencia | Esta carpeta y el reporte anterior del bloqueo de archivos |

No se toca el swap ajeno `GEvolve.Tests/.BiomeTests.cs.swp`. No se versionan compilados ni artefactos gráficos.

## Repetición y revisión manual

Validación técnica, desde la raíz:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\GEvolve.Tests\ValidationReports\commands.ps1 -ValidationSet LaboratoryPangaea -Label P03-A721-sidebar -IsolatedArtifacts
```

Para cargar la implementación nueva en la aplicación, el usuario debe cerrar su instancia anterior y ejecutar manualmente `dotnet run --project GEvolve.Game`. Los artefactos aislados de validación no sustituyen el proceso gráfico que ya estaba abierto. Abrir el laboratorio, generar 01→05, elegir capas, comprobar la semilla aleatoria y la selección; inspeccionar alturas, scroll y distribución del mapa en las resoluciones admitidas. Cerrar normalmente. Codex no ejecuta ese procedimiento.

Preparación/subida de buffers y construcción de texto siguen requiriendo el hilo gráfico. Con zoom legible se generan todas las etiquetas visibles; su coste gráfico y legibilidad requieren evaluación manual. No se declara rendimiento gráfico aprobado. Tipo limitado explícitamente a Pangea; sin guardado de estados parciales ni comparación avanzada P03-A.7.3.

## Resultado de la ejecución

Ejecución UTC: **2026-11-08 01:33:48.837–01:41:47.616** (inicio local: 2026-11-07 19:33:48.837, Guatemala). SDK **10.0.402**; fuente limpia al inicio: `51ca6458b7445334df4961ce7903640959ca3490`. Todos los comandos de esta tabla usan el mismo `--artifacts-path` aislado indicado en SUMMARY.md.

| Validación | Estado | Código | Segundos |
|---|---|---:|---:|
| Build | PASS | 0 | 8.051 |
| Laboratorio | PASS | 0 | 16.094 |
| Equivalencia por etapas | PASS | 0 | 69.085 |
| General | FAIL | 1 | 384.815 |

Build: **0 advertencias y 0 errores**. Laboratorio: los cuatro tamaños pasan controles, flujo, configuración, cámara y campos numéricos. Equivalencia: **28 Pangea + 12 controles PASS**. Los 28 registros `STAGED_EQ` también coinciden exactamente con P03-A.7.2.1, sin diferencias en sus hashes.

General: **16 suites PASS / 2 FAIL**. Game, PangaeaStaged y PangaeaLaboratory pasan dentro de la general. TectonicMotion conserva sus cuatro diferencias de elevación y ContinentalGeology sus doce. Los 16 registros históricos `elevation: expected=..., actual=...` coinciden exactamente con el informe anterior. No se reemplazaron baselines ni se presenta la general como aprobada. El fallo de copia de la compilación normal inicial se conserva aparte y quedó superado con salidas aisladas, sin cerrar el juego.

La revisión de los archivos publicados no detectó patrones de rutas personales ni credenciales. Se conservan los dos reportes por contener evidencia distinta (bloqueo de entorno y ejecución completa). Solo texto/Markdown/PowerShell queda versionado; ningún binario o mapa exportado.

Tras terminar se eliminó únicamente el árbol de artefactos aislados de esta ejecución, comprobando su ruta absoluta bajo `GEvolve.Tests/bin/ValidationArtifacts`. No se tocaron las salidas normales del juego ni reportes. Repetir el script genera un árbol nuevo con otro ID UTC.

**Rediseño implementado y validado técnicamente; pendiente de aprobación visual del usuario.** No se certifican legibilidad, apariencia o fluidez por estas pruebas. P03-A no aprobado; P03-A.7.3 y P03-B no iniciados.
