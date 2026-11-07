# P03-A.7.2.1 — Controles y alturas del Laboratorio de Pangea

**Código validado:** `f862a0c265cd9c341a761c5c0606c6f107dc1f2b` — `feat(game): simplify Pangaea laboratory controls and elevation display`, rama `worldgeneration_pangea`. Base: `283d855874e4eb1c7f3bde71a94c35689aee08e3`. La documentación y los logs se publican después del commit de implementación para identificar exactamente el código ejecutado.

**Alcance:** únicamente controlador, controles y representación del laboratorio en Game, sus pruebas y documentación. No cambia código de Engine, Definitions ni State, parámetros, RNG geográfico, superficie, BASE, H04, sistemas, aporte ni FINAL. No se actualizan baselines o referencias. La paleta global y los guardados mantienen su comportamiento.

**Pendiente de aprobación visual del usuario.** Codex no ejecutó Game, gráficos smoke, ventanas, contextos GL, capturas, mapas exportados ni automatización visual. Las pruebas de valores/colores y geometría de controles son aserciones de código; no inspeccionan pixels ni aprueban apariencia o calidad geográfica. El portapapeles y la lectura real de números/dado quedan pendientes de uso manual.

## Cambios implementados

| Experiencia | Implementación |
|---|---|
| Inicio/reinicio | `Restart` crea/reinicia la sesión oficial y ejecuta automáticamente Grid mediante la API asíncrona existente; no hay botón de cuadrícula |
| 01 Placas y movimiento | Encadena Plates y Motion, con dos operaciones oficiales independientes y sus revisiones/hashes; muestra el resultado combinado al terminar H04 |
| 02 Superficie | Mismo resultado oficial; no exige BASE ni FINAL |
| 03 BASE | Sigue requiriendo exclusivamente superficie; conserva sistemas cuando se repite |
| 04 Sistemas | Requiere superficie/H04; sigue pudiendo ejecutarse antes de BASE |
| 05 FINAL | Misma composición oficial de BASE/sistemas |
| ALEATORIA con dado | Cuatro bytes criptográficos convertidos a int32 con signo en Game; texto inmediato, sin cambiar RNG del motor ni iniciar un mundo completo |
| Copiar/editar | Ctrl+C copia la semilla; Ctrl+V con foco reemplaza texto; teclado/retroceso mantienen validación de formato |
| MOSTRAR ALTURAS | Etiquetas numéricas opcionales desde campos existentes, visibles solo con escala ≥13 y hasta 1000 entradas en el rango de cámara |
| SUPERFICIE | Océano azul; tierra ≤90 color terrestre existente; tierra >90 blanca, usando FINAL vigente y después BASE |

El estado del botón agrupado procede de ambas etapas reales; su duración suma los tiempos de operación disponibles, sin presentarla como una medición del tiempo gráfico. Si placas falla o se cancela, no se encola H04. Cancelar durante H04 conserva la política cooperativa del motor. Confirmar un reinicio elimina la cola y abandona trabajos anteriores, impidiendo que una terminación vieja se publique. Si falla/cancela la cuadrícula automática, el usuario puede reintentar iniciando/reiniciando sesión.

El selector conserva placas sin flechas, movimiento/H04 con flechas, compresión, extensión, cizalla y las demás vistas vigentes. Se añadió únicamente una entrada visual de movimiento en Game; su relleno reutiliza la paleta de placas y los overlays existentes, sin calcular nuevas magnitudes. Los IDs anteriores de vistas/acciones se mantienen; las nuevas entradas se agregan al final.

`PangaeaMapData.Height` es la lectura común de campos para las etiquetas y el color de superficie. Devuelve null cuando el campo no existe, distinto de cero real. En BASE lee BASE; en APORTE lee aporte; en FINAL lee FINAL; en SUPERFICIE prioriza FINAL→BASE. No ofrece una altura inventada en cuadrícula, placas, esfuerzos o sistemas. La invalidación por revisión existente elimina FINAL antes de publicar otra vista; si H04/sistemas se repite, BASE permanece y se usa inmediatamente. Mientras BASE y FINAL están ausentes no se muestra blanco por elevación ni números.

`PangaeaHeightLabels` prepara solo anotaciones desde la entrada visual almacenada. Respeta los IDs con costura E/O y las filas válidas; no reconstruye cuadrícula, no copia snapshots, no calcula hashes ni genera alturas. La ventana posiciona/corta el texto con la cámara y el renderer/UI existentes. La supresión por zoom/cantidad evita texto masivo a distancia; acercar más si el rango supera 1000. Se conserva la política de snapshots por revisión de P03-A.7.2.

## Pruebas de código

La suite existente `--pangaea-laboratory` ahora cubre:

- Grid automática al iniciar/reiniciar, sus siete estados internos y la ausencia de sus botones separados.
- Exactamente cinco acciones visibles, numeración y controles nuevos dentro de límites numéricos a 640×480, 800×600, 1280×720 y 1920×1080.
- Placas→H04 secuenciales, referencia de revisión de entrada real, vistas individuales y repetición agrupada conservando Grid e invalidando descendientes.
- Cancelación antes de encadenar H04 y recuperación; reinicio con trabajo activo y descarte de resultados obsoletos.
- Dependencias originales: superficie/BASE pueden calcularse sin H04 en la API interna; sistemas requiere H04/superficie y puede preceder a BASE.
- 100 semillas aleatorias válidas int32, edición de límites int.MinValue/int.MaxValue, texto inmediato y ninguna generación implícita; mismas reglas de confirmación/cancelación que edición manual.
- Etiquetas BASE/APORTE/FINAL/SUPERFICIE con valores exactos; IDs envueltos; supresión con interruptor/zoom/cantidad; ningún número en vistas o campos ausentes.
- Casos controlados 90 y 91, océano y cero, ausencia de alturas y prioridad de FINAL sobre BASE. Se prueban funciones puras sin GPU; los fixtures de frontera no modifican los mundos del motor.
- Todos los colores de superficie en los cuatro mundos reales satisfacen exactamente tierra y FINAL>90; invalidar FINAL vuelve inmediatamente a BASE en cada celda.
- Repetir BASE conserva sistemas, oculta alturas invalidadas y recompone exactamente el mismo resultado; superficie/BASE/FINAL coinciden con el automático en los cuatro tamaños.
- Caché por revisión, 100 actualizaciones ociosas sin snapshots nuevos, datos parciales sin campos ficticios e inspección pendiente/invalidado/cero real.

`--pangaea-staged` conserva la referencia independiente de 28 Pangea (siete semillas × cuatro tamaños) y 12 controles de otros tipos, además de invalidación, persistencia y aislamiento. `--all-suites` incluye las pruebas de Game, laboratorio, elevaciones y cordilleras. Los dos fallos históricos de elevación no autorizan reemplazar sus baselines.

No se declara un total individual de aserciones: el runner no lo emite. El número de muestras aleatorias es cobertura técnica de formato/flujo, no una prueba estadística de calidad de aleatoriedad.

## Reproducción e informes

Desde la raíz:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\GEvolve.Tests\ValidationReports\commands.ps1 -ValidationSet LaboratoryPangaea -Label P03-A721
```

[SUMMARY.md](SUMMARY.md) contiene UTC, SDK, fuente, estados, duraciones, códigos y excepciones. Salida completa: [build.log](build.log), [pangaea-laboratory.log](pangaea-laboratory.log), [pangaea-staged.log](pangaea-staged.log), [all-suites.log](all-suites.log). [commands.ps1](commands.ps1) conserva el script de esta ejecución. Si build falla se omiten dependientes; se conservan fallos y ambos streams. Los logs se revisan antes de publicar y siguen la política de conservación existente.

## Archivos modificados

- `GEvolve.Game/PangaeaLaboratory.cs`: Grid automática, secuencia de operaciones, semilla aleatoria, estado agrupado y preferencia de etiquetas.
- `GEvolve.Game/PangaeaLaboratoryWindow.cs`, `Program.cs`: controles, dado, anotaciones, selección de vistas y portapapeles.
- `GEvolve.Game/Graphics/PangaeaMapData.cs`, `MapViewMode.cs`, nuevo `PangaeaHeightLabels.cs`: lectura numérica y clasificación visual exclusiva del laboratorio.
- `GEvolve.Game/Screens/PangaeaLaboratoryLayout.cs`, `ScreenLayout.cs`: cinco acciones, semilla aleatoria y checkbox sin cambiar IDs existentes.
- `GEvolve.Tests/PangaeaLaboratoryTests.cs`, `ValidationReports/commands.ps1`, `README.md`: contratos automatizados y publicación de evidencia.
- Guía `GEvolve.Game/PANGAEA_LABORATORY.md`, roadmap, arquitectura, memoria e informes.

## Validación manual exclusiva del usuario

Ejecutar `dotnet run --project GEvolve.Game`, elegir **LABORATORIO DE PANGEA**. Iniciar sesión y esperar cuadrícula automática. Pulsar **01 PLACAS Y MOVIMIENTO**, después superficie, BASE/sistemas en el orden disponible y FINAL. Usar **VER** para cada capa. Pulsar **ALEATORIA**, copiar con Ctrl+C y comprobar confirmación de reinicio. Activar **MOSTRAR ALTURAS** y acercar zoom; inspeccionar BASE/APORTE/FINAL. En superficie, observar blanco únicamente por altura >90 y la selección FINAL→BASE cuando FINAL se invalide.

La legibilidad, fluidez de overlays, icono de dado y portapapeles nativo no se certifican con ejecución sin ventana. Los estados parciales siguen sin guardarse como partidas. P03-A.7.2.1 queda pendiente de aprobación visual; P03-A no aprobado. P03-A.7.3, P03-A.7.4 y P03-B no se inician.

## Resultados de esta ejecución

Ejecución UTC: **2026-11-07 22:35:47.254–22:45:37.171**, SDK **10.0.402**, fuente limpia al inicio: `f862a0c265cd9c341a761c5c0606c6f107dc1f2b`.

| Comando | Estado | Código | Segundos |
|---|---|---:|---:|
| `dotnet build` | PASS | 0 | 10.561 |
| `dotnet run --project GEvolve.Tests -- --pangaea-laboratory` | PASS | 0 | 18.716 |
| `dotnet run --project GEvolve.Tests -- --pangaea-staged` | PASS | 0 | 67.738 |
| `dotnet run --project GEvolve.Tests -- --all-suites` | FAIL | 1 | 491.763 |

Laboratorio: los cuatro tamaños pasan el flujo y las nuevas comprobaciones de representación. La equivalencia original/automático/manual pasa en **28 Pangea y 12 controles**. Los 28 registros `STAGED_EQ` coinciden además línea por línea con P03-A.7.2, sin cambios en sus hashes.

General: **16 suites PASS / 2 FAIL**. Game, PangaeaLaboratory y PangaeaStaged pasan también dentro de la general. TectonicMotion conserva cuatro diferencias de elevación (2026 por tamaño); ContinentalGeology conserva doce (2026, 42 y -731 por tamaño). Los 16 registros históricos `elevation: expected=..., actual=...` coinciden exactamente con P03-A.7.2. Baselines intactos; no se presenta la general como aprobada.

La primera compilación no tuvo errores y sí una advertencia **CS8602** en la prueba que recorre BASE retenida tras invalidar H04/FINAL. Se conserva íntegra en los logs. Se corrigió en `5bfbfc96bcb896615679d78dabe091f60d308164` — `fix(tests): guard retained laboratory base before inspection`: una guarda explícita confirma el campo y lo almacena antes de recorrerlo. No se silencia nullable ni se cambia una aserción de resultado. Entre esos commits no cambia ningún archivo C# funcional de Game/Engine/State/Definitions; únicamente esa guarda de pruebas, el selector operativo de validación y documentación.

La compilación y el laboratorio se repiten sobre ese commit mediante `-ValidationSet LaboratoryControls -Label P03-A721-nullable`. [Resumen de la comprobación posterior](../P03-A721-nullable_20261107_224554334Z_1be5ccd6/SUMMARY.md). Esa ejecución acotada no se presenta como una repetición de equivalencia/general; los resultados amplios de este informe siguen identificados por su SHA original. Se conservan ambas carpetas por contener evidencia distinta y una advertencia corregida.

La aprobación visual sigue pendiente exclusivamente del usuario. No se ejecutó la aplicación gráfica ni se aprobó apariencia, fluidez o calidad geográfica.

**Comprobación posterior terminada:** sobre `5bfbfc96bcb896615679d78dabe091f60d308164`, build **PASS**, código 0, **20.093 s**, **0 advertencias y 0 errores**; laboratorio **PASS**, código 0, **27.922 s**. La advertencia inicial queda corregida con evidencia ejecutada. La suite general conserva los dos fallos históricos descritos, no corregidos ni ocultados.
