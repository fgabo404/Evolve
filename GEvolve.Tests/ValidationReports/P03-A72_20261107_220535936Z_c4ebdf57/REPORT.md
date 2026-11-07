# P03-A.7.2 — Interfaz manual de Pangea

**Fuente validada:** `e231bbf26f9ba81865ea9bd2158838771c639780` — `feat(game): add interactive Pangaea generation laboratory`, rama `worldgeneration_pangea`. Base anterior: `c1abdbc6a7d0b906fe5f5c1ae28e690712eec265` (P03-A.7.1). La validación identifica el código antes del commit operativo de informes/documentación.

**Alcance:** interfaz interna, control de sesión, entrada visual parcial e inspección. Ningún cambio de código en Engine, Definitions o State; las fórmulas, parámetros, intentos, RNG, geografía y baselines permanecen intactos. P03-A.7.3–7.4 no implementados; P03-A no aprobado y P03-B no iniciado.

**Restricción visual:** Codex no ejecutó GEvolve.Game, gráficos smoke, contextos GL, capturas, imágenes de mapas ni automatización visual. Las pruebas numéricas de layout y contratos del controlador son comprobaciones de código; no acreditan apariencia, legibilidad en pantalla, fluidez ni calidad geográfica. La entrega queda pendiente de aprobación visual exclusiva del usuario.

## Arquitectura e integración

La entrada nueva **LABORATORIO DE PANGEA** convive con **Crear mundo**, que conserva la generación automática. `GameSession` aporta el estado de pantalla; el laboratorio posee su controlador y sesión temporal propios. No coloca resultados parciales en `GameState` ni cambia la persistencia.

`PangaeaLaboratory` usa la fábrica oficial de P03-A.7.1 y `ExecuteAsync`. La ventana procesa sus terminaciones con `Update()`: el trabajo de generación y las copias de snapshots quedan fuera de su hilo, y la GPU pertenece a EvolveWindow. Los botones consultan estados/CanExecute del motor y el grafo público, sin imponer una secuencia adicional.

Al repetir una etapa se retiran de inmediato su revisión anterior y los descendientes afectados. Solo se conservan campos completados con revisión coincidente. Esto cubre también una operación que termina antes de consultar sus estados: su resultado anterior no se reutiliza mientras se espera la copia nueva. Las revisiones publicadas actualizan la entrada visual almacenada; los frames ociosos no solicitan snapshots ni hashes.

`PangaeaMapData` representa exclusivamente los campos existentes. `HexGridRenderer` conserva sus hexágonos, buffers, cámara, shader y overlays, y admite esa entrada parcial además del constructor previo para mundos completos. No hay un segundo renderer ni un LandmassState con alturas o biomas ficticios. Los colores de placas y esfuerzos y la escala BASE/APORTE/FINAL usan las paletas existentes. Superficie muestra exclusivamente tierra/océano; sistemas utiliza sus propietarios reales y ejes/anchuras existentes, sin exigir FINAL.

El formulario guarda un borrador. Cambiar semilla/tamaño oculta las capas, deshabilita operaciones e indica cambios pendientes; **INICIAR / REINICIAR** comunica la pérdida de capas antes de confirmar. Cancelar restaura la configuración activa. Confirmar reinicia Engine, limpia la selección y abandona el trabajo anterior. Sus tareas se observan/liberan sin una ruta para publicar en la sesión actual.

## Etapas y dependencias

| Operación | Prerrequisitos | Vista al terminar | Repetir invalida |
|---|---|---|---|
| 01 Cuadrícula | Configuración | Hexágonos sin atributos | 02–07 |
| 02 Placas | 01 | 16 placas | 03–07 |
| 03 Movimiento/H04 | 02 | Placas y flechas; esfuerzos seleccionables | 06–07 |
| 04 Superficie | 02 | Tierra/océano, sin alturas | 05–07 |
| 05 BASE | 04 | Alturas BASE | 07; conserva 06 |
| 06 Sistemas | 03 + 04 | Corredores, ejes y anchuras | 07; conserva 05 |
| 07 FINAL | 05 + 06 | Alturas FINAL; APORTE disponible | 07 |

Estados visibles: pendiente, disponible, generando, completada, invalidada y error. Los botones completados pueden repetirse; se muestran motivos de indisponibilidad al pasar el cursor. Se informa duración real y error. Cancelar solicita descarte cooperativo; los bucles internos pueden terminar antes de observar la solicitud.

La inspección admite cualquier celda desde cuadrícula y distingue campos pendientes, invalidados, en generación y cero real. Muestra placa, movimiento y esfuerzos, tierra/océano, BASE, sistema, anchura si es eje, APORTE y FINAL cuando existen. No se fabrica aporte vertical al terminar sistemas: ese dato solo existe después de FINAL.

## Evidencia automatizada

[SUMMARY.md](SUMMARY.md) conserva inicio/fin UTC, SDK, plataforma, fuente limpia, estados, duraciones y códigos individuales. Logs íntegros: [build](build.log), [laboratorio](pangaea-laboratory.log), [equivalencia por etapas](pangaea-staged.log), [suite general](all-suites.log). La copia [commands.ps1](commands.ps1) reproduce esta ejecución y nunca inicia el juego interactivo.

La suite de laboratorio no crea ventana ni contexto GL. Comprueba:

- Apertura desde menú y mantenimiento del acceso automático.
- Cuatro tamaños oficiales; siete operaciones reales y vistas disponibles tras cada terminación.
- Superficie antes de H04 y sistemas antes de BASE: dependencias no lineales.
- Entradas parciales sin placas, superficie, BASE ni FINAL inventados; rechazo de vistas ausentes.
- Selección antes de superficie, cero real en océano y etiquetas pendiente/invalidado.
- Cien actualizaciones ociosas conservando referencias, revisión y contador de snapshots.
- Repetición de BASE conservando revisión/hash/influencia de sistemas y retirando FINAL inmediatamente.
- Igualdad de superficie, BASE y FINAL frente a la ruta automática en los cuatro tamaños.
- Cambio de tamaño/semilla, confirmación/cancelación y ausencia de capas bajo configuración nueva.
- Reinicio con trabajo anterior activo, descarte de terminaciones abandonadas, cancelación/reintento y error/recuperación.
- Límites numéricos compartidos por dibujo/hit testing a 640×480, 800×600, 1280×720 y 1920×1080. Estas aserciones no constituyen una inspección visual.

`--pangaea-staged` conserva las siete semillas × cuatro tamaños y 12 controles de otros tipos frente a la referencia independiente anterior a la refactorización, además de invalidación, persistencia y aislamiento. La suite general incluye las pruebas de Game y del laboratorio. No se actualizan los baselines históricos de TectonicMotion y ContinentalGeology.

## Archivos modificados

| Área | Archivos |
|---|---|
| Controlador y pantalla | `GEvolve.Game/PangaeaLaboratory.cs`, `PangaeaLaboratoryWindow.cs`, `GameSession.cs`, `Program.cs` |
| Entrada gráfica y renderer compartido | `GEvolve.Game/Graphics/PangaeaMapData.cs`, `HexGridRenderer.cs`, `MapViewMode.cs` |
| Controles | `GEvolve.Game/Screens/PangaeaLaboratoryLayout.cs`, `ScreenLayout.cs` |
| Pruebas | `GEvolve.Tests/PangaeaLaboratoryTests.cs`, `GameTests.cs`, `Program.cs` |
| Procedimiento | `GEvolve.Tests/ValidationReports/commands.ps1`, `README.md`, esta carpeta |
| Documentación | `GEvolve.Game/PANGAEA_LABORATORY.md`, roadmaps, arquitectura, API por etapas y memoria operativa |

## Repetir la validación técnica

Desde la raíz:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\GEvolve.Tests\ValidationReports\commands.ps1 -ValidationSet LaboratoryPangaea -Label P03-A72
```

Se conservan stdout/stderr completos en secciones separadas, códigos reales y fallos; si build falla se omiten dependientes. El script crea una carpeta UTC nueva, revisa patrones de información sensible y no publica por sí mismo. Se aplica la política de conservación existente: conservar informes de hito/fallo/referencia; no borrar automáticamente.

## Uso manual, límites y próximo paso

Guía completa: [PANGAEA_LABORATORY.md](../../../GEvolve.Game/PANGAEA_LABORATORY.md). Exclusivamente el usuario ejecuta `dotnet run --project GEvolve.Game`, abre **LABORATORIO DE PANGEA**, elige tamaño/semilla e inicia/confirma la sesión. Pulsar 01–07 respetando botones disponibles; **VER** cambia entre capas vigentes. Rueda: zoom; arrastrar: cámara; clic: celda. Repetir BASE y comprobar FINAL invalidada; confirmar cambio de semilla/tamaño; cerrar normalmente.

La copia de snapshots tiene coste proporcional a los datos, una vez por terminación. La preparación/subida de buffers y el dibujo de overlays permanecen en el hilo gráfico; su fluidez y legibilidad quedan pendientes de inspección del usuario. La banda de anchura sigue el valor por segmento del eje; el relleno del corredor proviene de propietarios ya calculados y no de un generador visual nuevo. Los estados parciales no permiten guardar partidas. La cancelación no es una interrupción instantánea.

**Próximo P03-A.7.3, solo planificado:** comparación ANTES/DESPUÉS, diferencias, hashes/estadísticas visibles e invalidación visible enriquecida, usando las mismas revisiones. Requiere alcance posterior; no expone ahora parámetros geológicos ni ejecuta la auditoría integral P03-A.7.4.

## Resultado de esta ejecución

Ejecución UTC: **2026-11-07 22:05:35.936–22:17:17.312**, SDK **10.0.402**, fuente limpia al iniciar: `e231bbf26f9ba81865ea9bd2158838771c639780`.

| Comando | Estado | Código | Segundos |
|---|---|---:|---:|
| `dotnet build` | PASS | 0 | 14.174 |
| `dotnet run --project GEvolve.Tests -- --pangaea-laboratory` | PASS | 0 | 19.281 |
| `dotnet run --project GEvolve.Tests -- --pangaea-staged` | PASS | 0 | 114.728 |
| `dotnet run --project GEvolve.Tests -- --all-suites` | FAIL | 1 | 551.947 |

Compilación: **0 errores y 0 advertencias**. Laboratorio: los cuatro tamaños pasan las siete etapas y sus comprobaciones de contrato. La equivalencia por etapas pasa en **28 mundos Pangea + 12 controles**. Los 28 registros `STAGED_EQ` coinciden además línea por línea con P03-A.7.1; no cambia ninguno de sus hashes.

General: **16 suites PASS / 2 FAIL**. Game y PangaeaLaboratory pasan también dentro de la ejecución general. TectonicMotion conserva cuatro diferencias de elevación (2026 por tamaño); ContinentalGeology conserva doce (2026, 42 y -731 por tamaño). Los 16 registros `elevation: expected=..., actual=...` coinciden exactamente con los del informe P03-A.7.1. No aparecen diferencias nuevas de esos baselines ni se actualizan sus datos. No se presenta la suite general como aprobada. El runner no publica un total individual de aserciones; no se inventa ese conteo.

La revisión de los archivos publicados no detectó patrones de rutas personales o credenciales. No se publican bin/obj, ejecutables, temporales ni el swap ajeno. Se conserva un informe canónico de este commit/SDK; los dos logs preliminares exitosos y redundantes se descartaron tras revisar sus resultados y capturar la ejecución canónica. Los informes históricos importantes siguen intactos.

**P03-A.7.2 implementado y validado técnicamente, pendiente de aprobación visual del usuario.** No se certifican apariencia, fluidez ni calidad geográfica; P03-A sigue sin aprobación.
