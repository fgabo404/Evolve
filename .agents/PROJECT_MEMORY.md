# Memoria operativa de Evolve

Esta página orienta la siguiente sesión; la [arquitectura](../GEvolve.Engine/WORLD_GENERATION_ARCHITECTURE.md) y el [roadmap](../GEvolve.Engine/WORLD_GENERATION_ROADMAP.md) conservan la descripción y el estado oficiales. Verificar allí cualquier dato que pueda haber cambiado.

## Estado y siguiente decisión

- **Rama de trabajo:** `worldgenerator`. No modificar `master` ni `develop` por inferencia.
- **Objetivo técnico activo:** H05-B3 implementado con superficie preliminar explícita dentro de Evolve; pendiente de ejecución local y auditoría técnica/visual.
- **Último hito aprobado:** H05-B2 está aprobado y cerrado tras compilación, pruebas específicas, suite completa y auditoría confirmadas por el usuario. H05-A1/A2 y H05-B1 también están cerrados.
- **Siguiente etapa:** validar H05-B3; H05-B4/B5 y H06 no autorizados ni iniciados. B1 original permanece para regresión.
- **Próxima acción concreta:** recibir compilación, `--continents-surface`, suite completa y evaluación visual del usuario; no cerrar H05-B3 por inferencia.

## Contratos que condicionan cambios futuros

`WorldGenerationService` es el único orquestador oficial. La cuadrícula conserva costura horizontal E/O y límites abiertos N/S; hay 16 placas universales independientes de la corteza. Fragmento, provincia, sistema y continente emergido son conceptos distintos. `Generate(Continents)` conserva `SurfaceGenerationPending`; `GenerateContinentsSurfacePreview` compone B1 correlacionado → B2 4r → superficie determinista, sin relieve/clima/biomas y sin declarar `Ready`. La vista **SUPERFICIE PRELIMINAR** usa la cuadrícula existente; save/load conserva superficie y corteza sin regenerar.

El radio operativo de ensamblaje es **4r**; 5r, 6r y 8r siguen siendo diagnósticos. `Generate` usa núcleos correlacionados, `GenerateOriginalGeometry` conserva la selección histórica y `GenerateExperimentalCorrelated` ofrece acceso explícito a la correlacionada. `SharedCorticalModel`/`SharedCorticalState` usan `shared-cortex-1`; la persistencia anterior debe seguir siendo compatible. Proteger los 12 hashes tectónicos y 36 baselines geográficos aprobados. Las estrategias de Pangaea y Archipelagos siguen separadas; `SmallContinents` conserva su ruta histórica. La validación visual se realiza dentro del juego, sin exportar mapas.

## Problema abierto y forma de trabajo

La configuración aprobada conserva núcleos correlacionados, 4r, ocho fragmentos y ocho provincias, con enlaces tectónicos reales y sin acreción obligatoria. B3 usa afinidad existente, transición costera y efectos locales acotados, con nivel marino provisional 0.38; no impone 2–4 continentes ni hace emerger toda la corteza. Su calibración visual está pendiente. El usuario ejecuta compilaciones y pruebas; Codex revisa estáticamente y no afirma resultados nuevos sin salida aportada.
