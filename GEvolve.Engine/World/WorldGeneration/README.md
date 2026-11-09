# WorldGeneration — Generador oficial de mundos de Evolve

Esta es el área de construcción del generador de mundos. Inicialmente **no contiene algoritmos**: se añadirán módulos cuando sus contratos estén diseñados, implementados y auditados.

- **Plan activo único:** [ROADMAP.md](ROADMAP.md).
- **Referencia histórica:** [../worldgenerationold/](../worldgenerationold/) con el generador anterior, cuyos namespaces y dependencias se conservan temporalmente.
- **Infraestructura común:** `GEvolve.Engine/Grid/`; contratos del juego en Definitions/State y visualización en Game.
- **Laboratorio:** se conserva el de Pangea; se integrará por etapas al nuevo motor.

Organización futura, solo cuando exista código: `Core`, `Tectonics`, `Crust/Strategies`, `Relief`, `Surface`, `Climate`, `Hydrology`, `Biomes`, `Ecology`, `Validation`, `Diagnostics`.

**Reglas:** no importar algoritmos antiguos sin revisión; no recrear estados equivalentes innecesarios; cada etapa declara dependencias y produce datos verificables; el generador nuevo no llama a `worldgenerationold`.

**R00–R01:** [verificación Git y estática](../../../GEvolve.Tests/ValidationReports/R00-R01_GIT_AUDIT.md). La igualdad de blobs y la inclusión de fuentes se revisan sin ejecutar .NET; compilación, regresiones y evaluación visual siguen pendientes del usuario, además de revisión/aprobación. Los resultados antiguos del laboratorio no certifican el motor nuevo.
