# GEvolve.Definitions — Reconstrucción de catálogos

Los cuatro catálogos que se reconstruyen en la rama `definitions` son:

| Catálogo nuevo (activo) | Archivo anterior (solo referencia) |
|---|---|
| `Biomas/biomes.json` | `Biomas/old/biomes.json` |
| `Resources/fauna.json` | `Resources/old/fauna.json` |
| `Resources/flora.json` | `Resources/old/flora.json` |
| `Resources/minerals.json` | `Resources/old/minerals.json` |

El catálogo activo `biomes.json` contiene ocho biomas principales con `id`, `name` y `subbiomes: []`; los catálogos activos de fauna, flora y minerales siguen vacíos. No se deben copiar automáticamente fichas anteriores ni inventar parámetros sin aprobar.

## Regla acordada para biomas

La jerarquía conceptual es **Bioma → Subbioma → requisitos de spawn**, con condiciones ambientales definidas en JSON. La estructura detallada de `spawn`, las unidades, las reglas obligatorias y las preferencias todavía se diseñarán y aprobarán. La nueva definición se transformará posteriormente en modelos C# tipados; no se generará código dinámicamente desde el JSON.

## Compatibilidad de transición

El generador histórico de Pangea continúa utilizando el `biomes.json` **archivado** a través del recurso integrado `Evolve.biomes.json`. Esto es temporal: el catálogo activo nuevo no debe entregarse a `BiomeCatalog.Default` hasta que se acuerde e implemente el nuevo contrato del motor. La carga genérica `DefinitionCatalog.Load` excluye `old/`.

Los recursos y tecnologías actuales conservan vínculos a entidades del diseño anterior; esos vínculos se revisarán al incorporar nuevas entidades. La wiki usa los archivos activos en reconstrucción (ocho biomas, sin subbiomas ni entidades biológicas/geológicas); las pruebas anteriores basadas en cantidades históricas deben adaptarse; no deben confundirse con una validación de los nuevos contenidos.

**Plan de trabajo:** [ROADMAP.md](ROADMAP.md).
