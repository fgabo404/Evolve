# P03-A.7.2.3 — Alturas legibles dentro de hexágonos

**Commit validado:** `d507714956546652e8d7dc640057e998ca41ec65`, `fix(game): constrain elevation labels to readable hex cells`. Rama `worldgeneration_pangea`; base anterior `3f0738d944a9b32be7c3f74b57f8a742c1c8e80e`. Fuente limpia al comenzar la validación.

**Alcance:** solo representación de alturas en Game, pruebas y documentación. Se inspeccionaron `PangaeaMapData.cs` y `MapCamera2D.cs` y se conservaron sin cambios. No cambian panel lateral, botones, inspector, cámara, fuente bitmap, paleta global, orquestación, generadores, datos, fórmulas ni baselines históricos. P03-A.7.3 y P03-B no iniciados. P03-A no aprobado.

**Pendiente de aprobación visual exclusiva del usuario.** Codex no abrió Evolve, no creó ventanas/contextos GL, no capturó imágenes ni automatizó una evaluación gráfica. Las pruebas de fuente, posición y contraste son funciones puras sobre números y polígonos, no comprobaciones de pixels renderizados ni aprobación de apariencia.

## Causa y solución

La implementación anterior usaba un umbral fijo de escala, un stride de celdas y hasta 600 muestras a distancia, con fuente fija y desplazamiento vertical aproximado. Eso permitía números aunque el hexágono no pudiera contenerlos. Se eliminan `MinimumScale`, stride y el límite/muestreo simplificado. No se sustituyen por otro número arbitrario de zoom.

`UiRenderer.MeasureTextInk` lee los píxeles encendidos de los mismos glifos y el mismo avance de seis unidades que utiliza `Text`. No instancia UiRenderer ni recursos gráficos. La fuente, sus glifos y el comportamiento del método de dibujo previo permanecen intactos; solo se añade medición estática. Se cachean los textos/medidas de byte 0–255 para no formatear o medir repetidamente por celda.

Ejemplos de medidas reales a tamaño unitario:

| Texto | Margen izquierdo del glifo | Ancho de tinta | Alto de tinta |
|---|---:|---:|---:|
| 0 | 0 | 5 | 7 |
| 1 | 1 | 3 | 7 |
| 10 | 1 | 10 | 7 |
| 100 | 1 | 16 | 7 |

Se considera el espacio completo ocupado por los rectángulos del bitmap, incluidos sus extremos. El origen se calcula restando al centro del hexágono el centro de esa caja escalada. El dibujo usa ese origen con `centered: false`; así se centran horizontal y verticalmente los píxeles reales, incluyendo números estrechos y sus márgenes laterales. Se elimina el desplazamiento aproximado `p.Y - 5`.

Para un hexágono flat-top de radio proyectado `r`, el rectángulo centrado de texto, aumentado con 2 px de margen, debe satisfacer:

```text
halfHeight <= sqrt(3) * r / 2
sqrt(3) * halfWidth + halfHeight <= sqrt(3) * r
```

Se obtiene de esas restricciones el tamaño máximo del píxel bitmap, limitado al tamaño preferido 1.5. El viewport impone además el ancho/alto disponible alrededor del centro. Si el tamaño resultante es menor que el mínimo legible 1.25, no se crea etiqueta. El umbral es de tamaño de letra real y texto concreto: por ejemplo, a igual radio una cifra puede caber mientras tres no. No es un umbral fijo de cámara.

El rectángulo completo con margen queda dentro del hexágono convexo y del viewport. Eso impide que el texto sobresalga o invada el de una celda vecina. Se recorren todas las celdas candidatas visibles con incremento uno, incluyendo copias E/O, sin stride, muestras, agregados, promedios ni corte por cantidad. A distancia donde ni el glifo más estrecho cabe legible, se retorna una lista vacía antes de recorrer celdas. Las celdas/cajas fuera del mapa visible se descartan antes de preparar su etiqueta.

El contraste blanco/negro se escoge a partir del color real de la capa, usando su luminancia lineal y la opción de mayor contraste, en lugar de inferirlo solo por el valor de altura. No cambia los colores del terreno. La apariencia final, incluidos overlays y condiciones reales de pantalla, requiere revisión del usuario.

## Campos e inspector conservados

| Vista | Valor |
|---|---|
| BASE | BASE |
| APORTE | Aporte existente |
| FINAL | FINAL |
| SUPERFICIE | FINAL vigente; en su ausencia BASE |
| Otras vistas disponibles | Altura vigente FINAL→BASE |

Las lecturas continúan en `PangaeaMapData.Height`, sin modificaciones. Un campo ausente no produce texto; cero produce `0` si es legible. El panel de celda mantiene BASE/APORTE/FINAL exactos independientemente de que aparezca texto sobre el mapa. No cambia el blanco >90 de superficie ni la invalidación por revisiones existente.

## Pruebas automatizadas

La suite existente `--pangaea-laboratory` llama a `PangaeaHeightLabelTests` en los cuatro tamaños oficiales. No se crea una suite de generación paralela ni una ventana. Los registros `LAB_LABELS_PASS` completos están en [pangaea-laboratory.log](pangaea-laboratory.log).

- Medidas literales de glifos reales 0/1/10/100 y escalado exacto, incluyendo el margen de la cifra 1.
- Zoom lejano: cero etiquetas en cada vista, sin subconjuntos espaciados.
- Zoom legible: campos individuales exactos y cobertura de todas las celdas que caben al tamaño mínimo. No hay stride ni límite de muestras.
- Cada esquina de la caja aumentada con margen está dentro de los seis lados del hexágono. El test usa productos cruzados de un polígono, de forma independiente a las desigualdades del código de producción.
- Centro de tinta reconstruido desde el origen que recibe Text y la medida real coincide en ambos ejes con la proyección del hexágono.
- Rectángulos de tinta de etiquetas cercanas sin intersección; ningún texto generado fuera del viewport.
- Contraste de negro/blanco con fondos claros/oscuros y contraste numérico de los colores actuales de todas las vistas.
- Desplazamiento real de cámara, cambios de zoom, copias E/O y filas válidas de los límites N/S.
- Valores 0, 1, 10, 90 y 100 en distintos radios; una cifra cabe a un radio donde tres se ocultan. Los fixtures solo existen en la prueba de representación.
- BASE/APORTE/FINAL exactos; campos ausentes sin números ficticios; inspector idéntico antes/después del zoom aunque las etiquetas estén ocultas.
- Los contratos previos de flujo, reinicios, viewport/panel, cancelación, caché, blanco >90 y equivalencia manual/automática siguen ejecutándose.

La suite `--pangaea-staged` mantiene la referencia independiente de **28 mundos Pangea y 12 controles**. La general conserva las pruebas de Game y laboratorio y los baselines históricos. El runner no publica un total individual de aserciones; no se inventa ese conteo.

## Archivos modificados

| Archivo | Cambio |
|---|---|
| `GEvolve.Game/Graphics/PangaeaHeightLabels.cs` | Ajuste por dimensiones reales, centrado, culling, contraste; eliminación de muestreo |
| `GEvolve.Game/Graphics/UiRenderer.cs` | Medición estática de tinta; sin cambiar Text, glifos o recursos |
| `GEvolve.Game/PangaeaLaboratoryWindow.cs` | Consume origen, tamaño y color preparados para la etiqueta |
| `GEvolve.Tests/PangaeaHeightLabelTests.cs` | Nuevas pruebas puras de fuente, geometría y datos |
| `GEvolve.Tests/PangaeaLaboratoryTests.cs` | Integra pruebas y verifica independencia del inspector respecto del zoom |
| `GEvolve.Game/PANGAEA_LABORATORY.md` | Sustituye regla antigua de números espaciados por ajuste por celda |
| `GEvolve.Engine/PANGAEA_ROADMAP.md` | Estado P03-A.7.2.3 e informe enlazado |
| `GEvolve.Engine/WORLD_GENERATION_ARCHITECTURE.md` | Regla de representación vigente |
| `GEvolve.Engine/WORLD_GENERATION_ROADMAP.md` | Seguimiento de etiquetas legibles |
| `.agents/PROJECT_MEMORY.md` | Sustituye la regla obsoleta de muestreo |
| `GEvolve.Tests/ValidationReports/README.md` | Procedimiento de validación y cobertura |
| Esta carpeta: `REPORT.md`, `SUMMARY.md`, `build.log`, `pangaea-laboratory.log`, `pangaea-staged.log`, `all-suites.log`, `commands.ps1` | Evidencia íntegra de ejecución |

`PangaeaMapData.cs`, `MapCamera2D.cs`, `PangaeaLaboratory.cs`, layout, botones, Engine/State/Definitions y baselines no cambian. El swap ajeno `GEvolve.Tests/.BiomeTests.cs.swp` permanece intacto.

## Reproducción y logs

Desde la raíz:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\GEvolve.Tests\ValidationReports\commands.ps1 -ValidationSet LaboratoryPangaea -Label P03-A723 -IsolatedArtifacts
```

Se utilizan salidas aisladas bajo bin ignorado, sin reemplazar DLLs de un juego abierto ni iniciarlo/cerrarlo. [SUMMARY.md](SUMMARY.md) registra UTC, SDK, fuente, códigos y duraciones. Salida completa: [build.log](build.log), [pangaea-laboratory.log](pangaea-laboratory.log), [pangaea-staged.log](pangaea-staged.log), [all-suites.log](all-suites.log). [commands.ps1](commands.ps1) es la copia exacta usada; captura ambos streams y conserva errores. Si build falla, omite dependientes.

Para inspección exclusiva del usuario: cerrar una instancia anterior si sigue abierta, ejecutar `dotnet run --project GEvolve.Game`, generar las capas y acercar/alejar el zoom. Confirmar números dentro de sus celdas, centrados y con contraste, desaparición completa cuando no caben, campos activos e inspector a cualquier distancia. Codex no ejecuta este procedimiento ni declara aprobada su apariencia.

## Resultados

Ejecución UTC: **2026-11-08 02:59:45.937–03:06:57.739** (inicio local Guatemala: 2026-11-07 20:59:45.937). SDK **10.0.402**, fuente limpia `d507714956546652e8d7dc640057e998ca41ec65`. Todos los comandos usan el `--artifacts-path` aislado que registra SUMMARY.md.

| Validación | Estado | Código | Segundos |
|---|---|---:|---:|
| Build | PASS | 0 | 11.556 |
| Laboratorio | PASS | 0 | 15.840 |
| Equivalencia por etapas | PASS | 0 | 71.329 |
| General | FAIL | 1 | 332.258 |

Build: **0 advertencias y 0 errores**. Los cuatro registros `LAB_LABELS_PASS` (Small, Medium, Large, Enormous) confirman fuente, ajuste geométrico, centrado de tinta, culling, todas las celdas elegibles/sin muestras, contraste, ausencia de solapamientos, pan/zoom y límites/costura. Laboratorio y Game también pasan dentro de la general.

Equivalencia original/automático/manual: **28 Pangea + 12 controles PASS**. Los 28 registros `STAGED_EQ` coinciden además exactamente con el reporte previo del panel lateral; ningún hash cambia.

General: **16 suites PASS / 2 FAIL**. TectonicMotion conserva cuatro discrepancias de elevación y ContinentalGeology doce. Los 16 registros históricos `elevation: expected=..., actual=...` coinciden exactamente con el informe anterior. No se cambian baselines ni se presenta la general como aprobada. No aparecen nuevas diferencias geográficas en estas comprobaciones.

La revisión de archivos publicados no detectó patrones de rutas personales ni credenciales. Solo se versionan código, pruebas, documentación y logs; no binarios ni mapas. Se mantiene la política de conservación existente de informes y resultados fallidos.

Después de completar la ejecución se eliminó exclusivamente su carpeta de artefactos aislados, tras verificar la ruta absoluta bajo `GEvolve.Tests/bin/ValidationArtifacts`. No se tocaron las salidas normales de Game ni los reportes. La repetición genera artefactos nuevos bajo otro ID UTC.

**P03-A.7.2.3 implementado y validado técnicamente, pendiente de aprobación visual exclusiva del usuario.** No se aprueban apariencia, fluidez ni calidad geográfica. P03-A no aprobado y P03-A.7.3 no iniciado.
