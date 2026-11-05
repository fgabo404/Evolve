# Memoria operativa de Evolve

Esta página orienta la siguiente sesión; la [arquitectura](../GEvolve.Engine/WORLD_GENERATION_ARCHITECTURE.md) y el [roadmap](../GEvolve.Engine/WORLD_GENERATION_ROADMAP.md) conservan la descripción y el estado oficiales. Verificar allí cualquier dato que pueda haber cambiado.

## Estado y siguiente decisión

- **Rama de trabajo:** `worldgenerator`. No modificar `master` ni `develop` por inferencia.
- **Objetivo técnico activo:** validar la adopción de núcleos correlacionados como valor predeterminado de `ContinentsCorticalFragmentGenerator.Generate`, conservando el ensamblaje B2 a 4r.
- **Últimos hitos aprobados:** H05-A1, H05-A2 y H05-B1 están aprobados y cerrados. El roadmap también registra los hitos anteriores cerrados.
- **Hito pendiente:** H05-B2 permanece abierto hasta validar localmente el nuevo valor predeterminado y auditar el diff. H05-B3/B4/B5 y H06 no han comenzado. La selección B1 original se conserva para regresión.
- **Próxima acción concreta:** el usuario ejecuta compilación, `--correlated-cortex`, `--continents-assembly` y suite completa; después, el auditor revisa los resultados de la adopción antes de cerrar H05-B2.

## Contratos que condicionan cambios futuros

`WorldGenerationService` es el único orquestador oficial. La cuadrícula conserva costura horizontal E/O y límites abiertos N/S. Las 16 placas tectónicas universales son independientes de la corteza. Un fragmento cortical, una provincia, un sistema geológico y un continente emergido son conceptos distintos; corteza continental no equivale a tierra emergida. La generación es determinista para tamaño, tipo, semilla e intento definidos. `Continents` todavía no tiene superficie oficial y conserva `SurfaceGenerationPending`; la etapa cortical no se conecta al servicio.

El radio operativo de ensamblaje es **4r**; 5r, 6r y 8r siguen siendo diagnósticos. `Generate` usa núcleos correlacionados, `GenerateOriginalGeometry` conserva la selección histórica y `GenerateExperimentalCorrelated` ofrece acceso explícito a la correlacionada. `SharedCorticalModel`/`SharedCorticalState` usan `shared-cortex-1`; la persistencia anterior debe seguir siendo compatible. Proteger los 12 hashes tectónicos y 36 baselines geográficos aprobados. Las estrategias de Pangaea y Archipelagos siguen separadas; `SmallContinents` conserva su ruta histórica. La validación visual se realiza dentro del juego, sin exportar mapas.

## Problema abierto y forma de trabajo

La decisión arquitectónica adopta la geometría correlacionada solo en el generador cortical, sin forzar enlaces en escenarios que carezcan de evidencia tectónica suficiente. El estado del servicio oficial y la superficie no cambian. Falta validar localmente el nuevo valor predeterminado y revisar el diff antes de cerrar H05-B2. El usuario ejecuta compilaciones y pruebas; Codex revisa estáticamente y no afirma resultados nuevos sin salida aportada.
