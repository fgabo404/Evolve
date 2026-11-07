# P03-A.7.2 — Laboratorio de Pangea

Implementación de interfaz y contratos de presentación. La aprobación visual corresponde exclusivamente al usuario. Las comprobaciones automatizadas no crean ventanas, contextos GL ni capturas. No cambian los algoritmos, los parámetros P03-A.6 ni los contratos guardables.

## Abrir y usar (ejecución exclusiva del usuario)

Desde la raíz, `dotnet run --project GEvolve.Game`. Elegir **LABORATORIO DE PANGEA** en el menú principal. **Crear mundo** conserva el flujo automático habitual.

1. Elegir Pequeño, Mediano, Grande o Enorme. Pangea es fijo. Editar la semilla con el teclado y retroceso; pulsar **INICIAR / REINICIAR SESIÓN**.
2. Si existen capas o trabajo activo, confirmar **SÍ, BORRAR CAPAS** o **CONSERVAR SESIÓN**. Los cambios de formulario ocultan el mapa hasta aplicar o cancelar; nunca se muestra un mapa anterior bajo otra configuración.
3. Ejecutar los siete botones: cuadrícula, placas, movimiento/H04, superficie, BASE, sistemas y FINAL. Cada terminación selecciona su vista correspondiente. Los botones deshabilitados explican al pasar el cursor qué dependencia falta. No es obligatorio seguir el orden numérico: superficie puede preceder a H04 y sistemas puede preceder a BASE.
4. El selector **VER** ofrece únicamente capas vigentes: cuadrícula, placas/movimiento, compresión, extensión, cizalla, superficie, BASE, relieve/sistemas, APORTE y FINAL. Los sistemas muestran influencia existente, eje y anchura de sus segmentos antes de componer FINAL. La banda de anchura representa la anchura del eje; el relleno del corredor procede de los propietarios calculados por Engine.
5. Seleccionar un hexágono para inspeccionar sus campos. El panel distingue PENDIENTE, GENERANDO, INVALIDADO, ERROR y el cero real. La aportación vertical no existe hasta FINAL, aunque la geometría de sistemas ya esté disponible. Rueda para zoom; arrastrar para desplazar; la selección respeta la costura E/O.
6. Repetir una etapa con su mismo botón. BASE conserva sistemas e invalida FINAL. H04 conserva superficie/BASE e invalida sistemas/FINAL. Reiniciar descarta todas las capas y trabajos anteriores. **CANCELAR OPERACIÓN** solicita descarte cooperativo: el cálculo interno puede terminar antes de observar la cancelación.
7. **MENÚ** o Escape abandona la sesión temporal. No se exporta un estado incompleto a GameState ni a un guardado.

## Arquitectura

- `PangaeaLaboratory`: controlador de presentación en Game, sin dependencia de ventana o GL. Usa directamente `WorldGenerationService.CreatePangaeaSession`, `ExecuteAsync` y snapshots de P03-A.7.1. El hilo de ventana solo inicia operaciones, aplica sus terminaciones y administra recursos gráficos. Generación y copia de snapshots ocurren fuera de ese hilo.
- El formulario mantiene un borrador explícito. Confirmar reinicia Engine, abandona el trabajo anterior y borra datos seleccionables. Una terminación abandonada se observa y libera, pero no tiene camino de publicación al controlador nuevo.
- Al iniciar una operación se conservan solo resultados completados cuya revisión sigue coincidiendo; los descendientes y la revisión repetida dejan de ser entradas renderizables inmediatamente. El estado de botones procede del grafo real y `PangaeaStageInfo`, no de una secuencia artificial.
- `PangaeaMapData`: entrada visual opcional, sin fabricar LandmassState, alturas, biomas o ceros. Es una vista sobre un snapshot desligado; consulta campos disponibles y prepara etiquetas. No ejecuta generación ni reconstruye geografía.
- `HexGridRenderer`: mismo renderer, geometría, shader, cámara, instanciado y overlays. El constructor previo para mundos completos sigue operativo. El laboratorio ofrece una segunda entrada explícita de capas parciales. Las paletas de placas/esfuerzos y escala BASE/APORTE/FINAL se conservan; superficie usa los colores existentes de tierra/océano.
- `PangaeaLaboratoryWindow`: integración parcial de EvolveWindow, configuración, controles, estado, duración, selector de capas e inspección. `PangaeaLaboratoryLayout` comparte coordenadas entre dibujo y hit testing.
- No hay snapshots ni hashes por frame. Las terminaciones publican una representación por revisión. El renderer conserva buffers y topología hasta cambiar la revisión de cuadrícula/configuración; los colores se actualizan cuando cambian datos, vista o cámara. `Update()` ocioso conserva la misma referencia de mapa.

## Validación técnica reproducible

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\GEvolve.Tests\ValidationReports\commands.ps1 -ValidationSet LaboratoryPangaea -Label P03-A72
```

Ejecuta build, `--pangaea-laboratory`, `--pangaea-staged` y `--all-suites`, con salida completa y códigos individuales. La suite de laboratorio verifica controlador, grafo, entradas parciales y límites numéricos del layout; no comprueba apariencia, pixels ni calidad geográfica. La suite por etapas conserva la referencia independiente de 28 mundos y 12 controles. Los dos fallos históricos de elevación en la suite general no autorizan cambiar sus baselines.

## Límites y siguiente entrega

La copia de cada snapshot es proporcional al tamaño de sus capas; se realiza por operación, nunca por frame. Preparar/subir buffers y dibujar overlays todavía requiere el hilo gráfico; no se presenta su fluidez como validada. La cancelación no interrumpe bucles internos del generador. Los campos temporales no permiten guardar partidas parciales. Las anchuras se representan con bandas de segmento, mientras la cobertura real se muestra por influencia existente; no se recalculan corredores para dibujarlos.

P03-A.7.2 queda pendiente de inspección/aprobación visual del usuario, incluido Enorme y las resoluciones admitidas. P03-A.7.3 añadirá comparación ANTES/DESPUÉS, diferencias y diagnósticos visibles bajo un alcance posterior; no está implementada. P03-A.7.4 permanece planificado. P03-A no aprobado; P03-B no iniciado.
