# GEvolve.Definitions — Reconstrucción de catálogos

Los cuatro catálogos que se reconstruyen en la rama `definitions` son:

| Catálogo nuevo (activo) | Archivo anterior (solo referencia) |
|---|---|
| `Biomas/biomes.json` | `Biomas/old/biomes.json` |
| `Resources/fauna.json` | `Resources/old/fauna.json` |
| `Resources/flora.json` | `Resources/old/flora.json` |
| `Resources/minerals.json` | `Resources/old/minerals.json` |

El catálogo activo `biomes.json` recupera los ocho biomas históricos y conserva 21 subbiomas: todos con `id` y `name`. **Los 5 subbiomas de Bosque tienen bloques `spawn` declarativos**; los otros 16 aún no tienen `spawn`. Única excepción respecto al histórico: Montaña tiene solo Montaña rocosa (`mountain_rocky`) y Cumbres (`mountain_summits`). Los catálogos activos de fauna, flora y minerales siguen vacíos. No se deben copiar automáticamente fichas anteriores ni inventar parámetros sin aprobar.

## Regla acordada para biomas

La jerarquía conceptual es **Bioma → Subbioma → requisitos de spawn**, con condiciones ambientales definidas en JSON. La estructura `spawn.required` (condiciones obligatorias) y `spawn.preferred` (bandas favorables) está aplicada a los cinco bosques activos. Es un contrato inicial **pendiente de revisión y tipado C#**; se distingue la duración de la estación cálida entre templado y boreal y la saturación del suelo para pantano arbolado. `forest_subtropical` se ha retirado del catálogo activo, sin reutilizar su ID. El nuevo `forest_subalpine` es la franja forestal por debajo del límite local de árboles (`distanceBelowLocalTreelineMeters`); su cálculo no está implementado y requiere nuevo contrato tipado de Engine. La nueva definición se transformará posteriormente en modelos C# tipados; no se generará código dinámicamente desde el JSON.

## Compatibilidad de transición

El generador histórico de Pangea continúa utilizando el `biomes.json` **archivado** a través del recurso integrado `Evolve.biomes.json`. Esto es temporal: el catálogo activo nuevo no debe entregarse a `BiomeCatalog.Default` hasta que se acuerde e implemente el nuevo contrato del motor. La carga genérica `DefinitionCatalog.Load` excluye `old/`.

Los recursos y tecnologías actuales conservan vínculos a entidades del diseño anterior; esos vínculos se revisarán al incorporar nuevas entidades. La wiki usa los archivos activos en reconstrucción (ocho biomas y 21 subbiomas, con `spawn` en los cinco bosques y sin entidades biológicas/geológicas); las pruebas anteriores basadas en cantidades históricas deben adaptarse; no deben confundirse con una validación de los nuevos contenidos.

**Plan de trabajo:** [ROADMAP.md](ROADMAP.md).
