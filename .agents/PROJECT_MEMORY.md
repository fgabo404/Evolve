# Memoria operativa de Evolve

Esta página orienta la siguiente sesión; la [arquitectura](../GEvolve.Engine/WORLD_GENERATION_ARCHITECTURE.md) y el [roadmap](../GEvolve.Engine/WORLD_GENERATION_ROADMAP.md) conservan la descripción y el estado oficiales. Verificar allí cualquier dato que pueda haber cambiado.

## Estado y siguiente decisión

- **Rama de trabajo:** `worldgenerator`. No modificar `master` ni `develop` por inferencia.
- **Objetivo técnico activo:** preparar el alcance de H05-B3 cuando el usuario lo autorice, partiendo del ensamblaje cortical aprobado a 4r.
- **Último hito aprobado:** H05-B2 está aprobado y cerrado tras compilación, pruebas específicas, suite completa y auditoría confirmadas por el usuario. H05-A1/A2 y H05-B1 también están cerrados.
- **Siguiente etapa:** H05-B3, aún no iniciada; H05-B4/B5 y H06 tampoco han comenzado. La selección B1 original se conserva para regresión.
- **Próxima acción concreta:** esperar la autorización y el alcance de H05-B3; consultar entonces su tramo del roadmap antes de implementar.

## Contratos que condicionan cambios futuros

`WorldGenerationService` es el único orquestador oficial. La cuadrícula conserva costura horizontal E/O y límites abiertos N/S. Las 16 placas tectónicas universales son independientes de la corteza. Un fragmento cortical, una provincia, un sistema geológico y un continente emergido son conceptos distintos; corteza continental no equivale a tierra emergida. La generación es determinista para tamaño, tipo, semilla e intento definidos. `Continents` todavía no tiene superficie oficial y conserva `SurfaceGenerationPending`; la etapa cortical no se conecta al servicio.

El radio operativo de ensamblaje es **4r**; 5r, 6r y 8r siguen siendo diagnósticos. `Generate` usa núcleos correlacionados, `GenerateOriginalGeometry` conserva la selección histórica y `GenerateExperimentalCorrelated` ofrece acceso explícito a la correlacionada. `SharedCorticalModel`/`SharedCorticalState` usan `shared-cortex-1`; la persistencia anterior debe seguir siendo compatible. Proteger los 12 hashes tectónicos y 36 baselines geográficos aprobados. Las estrategias de Pangaea y Archipelagos siguen separadas; `SmallContinents` conserva su ruta histórica. La validación visual se realiza dentro del juego, sin exportar mapas.

## Problema abierto y forma de trabajo

La configuración aprobada conserva núcleos correlacionados, radio 4r, ocho fragmentos y ocho provincias, con enlaces sustentados por tectónica real y sin forzar acreción en todos los mundos. La validación de H05-B2 está cerrada; la integración al servicio oficial y la superficie siguen pendientes. El usuario ejecuta compilaciones y pruebas; Codex revisa estáticamente y no afirma resultados nuevos sin salida aportada.
