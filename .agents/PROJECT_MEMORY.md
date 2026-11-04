# Memoria operativa de Evolve

Esta página orienta la siguiente sesión; la [arquitectura](../GEvolve.Engine/WORLD_GENERATION_ARCHITECTURE.md) y el [roadmap](../GEvolve.Engine/WORLD_GENERATION_ROADMAP.md) conservan la descripción y el estado oficiales. Verificar allí cualquier dato que pueda haber cambiado.

## Estado y siguiente decisión

- **Rama de trabajo:** `worldgenerator`. No modificar `master` ni `develop` por inferencia.
- **Objetivo técnico activo:** evaluar la calibración geológica de H05-B2 para `Continents`, especialmente el ensamblaje de fragmentos y la distribución de núcleos corticales. La variante correlacionada es un experimento aislado; aún no es la estrategia operativa.
- **Últimos hitos aprobados:** H05-A1, H05-A2 y H05-B1 están aprobados y cerrados. El roadmap también registra los hitos anteriores cerrados.
- **Hito pendiente:** H05-B2 permanece abierto. H05-B3/B4/B5 y H06 no han comenzado. No declarar aprobada la calibración ni cambiar la geometría B1 o el radio B2 sin decisión arquitectónica.
- **Próxima acción concreta:** el usuario ejecuta la compilación, `--correlated-cortex` y la suite completa; después, el auditor compara los 12 escenarios y decide si aprobar, ajustar o descartar la alternativa. Codex debe esperar esa evidencia antes de promover un algoritmo experimental.

## Contratos que condicionan cambios futuros

`WorldGenerationService` es el único orquestador oficial. La cuadrícula conserva costura horizontal E/O y límites abiertos N/S. Las 16 placas tectónicas universales son independientes de la corteza. Un fragmento cortical, una provincia, un sistema geológico y un continente emergido son conceptos distintos; corteza continental no equivale a tierra emergida. La generación es determinista para tamaño, tipo, semilla e intento definidos. `Continents` todavía no tiene superficie oficial y conserva `SurfaceGenerationPending`; los experimentos H05-B2 no se conectan al servicio.

El radio operativo de ensamblaje es **4r**. Los radios 5r, 6r y 8r y la distribución correlacionada de núcleos solo sirven para diagnóstico. `SharedCorticalModel`/`SharedCorticalState` usan `shared-cortex-1`; la persistencia anterior debe seguir siendo compatible. Proteger los 12 hashes tectónicos y 36 baselines geográficos aprobados. Las estrategias de Pangaea y Archipelagos siguen separadas; `SmallContinents` conserva su ruta histórica. La validación visual se realiza dentro del juego, sin exportar mapas.

## Problema abierto y forma de trabajo

Los núcleos separados y el crecimiento acotado de B1 dejan pocos pares próximos; aumentar el radio crea más vínculos, pero su número no prueba validez geológica. La calibración debe basarse en contactos tectónicos reales, distancias, movimiento normal y continuidad, no en una cuota de enlaces. El usuario ejecuta compilaciones, pruebas y mediciones; Codex implementa y revisa estáticamente. Entregar comandos exactos y no afirmar resultados nuevos sin salida aportada por el usuario.
