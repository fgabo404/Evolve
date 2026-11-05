# Memoria operativa de Evolve

Esta página orienta la siguiente sesión; la [arquitectura](../GEvolve.Engine/WORLD_GENERATION_ARCHITECTURE.md) y el [roadmap](../GEvolve.Engine/WORLD_GENERATION_ROADMAP.md) conservan la descripción y el estado oficiales. Verificar allí cualquier dato que pueda haber cambiado.

## Estado y siguiente decisión

- **Rama de trabajo:** `worldgenerator`. No modificar `master` ni `develop` por inferencia.
- **Objetivo técnico activo:** H05-C1 experimental implementado: formación multiescala y comparación A/C. Pendiente de compilación, suite y evaluación visual del usuario. A permanece operativa/persistible.
- **Último hito aprobado:** H05-B2 está aprobado y cerrado tras compilación, pruebas específicas, suite completa y auditoría confirmadas por el usuario. H05-A1/A2 y H05-B1 también están cerrados.
- **Siguiente etapa:** validar C1 dentro de H05-B3, que sigue abierto; no adoptar C ni iniciar C2, H05-B4/B5 o H06. B1/B2/B3 son referencias operativas intactas.
- **Próxima acción concreta:** recibir compilación, `--multiscale-cortex`, suite completa y evaluación A/C. El usuario confirmó la batería A/B de `93b13380015310dc843854f3160ddc4ea5a7c0e7`; B no fue adoptada: solo cambió 94/705450 celdas y no resolvió los bloques.

## Contratos que condicionan cambios futuros

`WorldGenerationService` es el único orquestador oficial. La cuadrícula conserva costura horizontal E/O y límites abiertos N/S; hay 16 placas universales independientes de la corteza. Fragmento, provincia, sistema y continente emergido son conceptos distintos. `Generate(Continents)` conserva `SurfaceGenerationPending`; `GenerateContinentsSurfacePreview` compone B1 correlacionado → B2 4r → superficie determinista, sin relieve/clima/biomas y sin declarar `Ready`. La vista **SUPERFICIE PRELIMINAR** usa la cuadrícula existente; save/load conserva superficie y corteza sin regenerar.

El radio operativo de ensamblaje es **4r**; 5r, 6r y 8r siguen siendo diagnósticos. `Generate` usa núcleos correlacionados, `GenerateOriginalGeometry` conserva la selección histórica y `GenerateExperimentalCorrelated` ofrece acceso explícito a la correlacionada. `SharedCorticalModel`/`SharedCorticalState` usan `shared-cortex-1`; la persistencia anterior debe seguir siendo compatible. Proteger los 12 hashes tectónicos y 36 baselines geográficos aprobados. Las estrategias de Pangaea y Archipelagos siguen separadas; `SmallContinents` conserva su ruta histórica. La validación visual se realiza dentro del juego, sin exportar mapas.

## Problema abierto y forma de trabajo

La referencia A conserva B1 correlacionado, B2 4r y B3 0.38. C1 prueba ocho orígenes jerárquicos, K=2,3,4 con tres candidatos por K, presupuesto cortical A 80/20, campos anisotrópicos y trazas contiguas. Sus criterios provisionales son 2–4 principales ≥3% del mapa, dominancia ≥60% y revisión separada de masas 1–3%, cuellos y conexiones sin soporte. Los criterios no pintan ni eliminan tierra. `CompareContinentsGeometryPreview` comparte la tectónica y recalcula B2/B3 sobre C; `DisplayLandmass` muestra C con su corteza propia, mientras `State.Landmass` guarda A. Un rechazo geográfico es un resultado legítimo, no una aprobación ni una excusa para forzar conexiones. C1 no tiene rifting físico completo ni emergencia C2. El usuario ejecuta todas las validaciones.
