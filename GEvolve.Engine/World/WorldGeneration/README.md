# WorldGeneration — Generador oficial de mundos de Evolve

Esta área sigue sin algoritmos. R02 reinicia el flujo activo con la cuadrícula compartida; no implementa R03 ni un generador alternativo.

- **Único plan activo:** [ROADMAP.md](ROADMAP.md).
- **Archivo histórico:** [../worldgenerationold/](../worldgenerationold/), preservado y excluido explícitamente de Engine. No puede usarse como fallback.
- **Infraestructura común:** GEvolve.Engine/Grid/ y su geometría, sin cambios. Definitions, State y guardados históricos conservan sus contratos.
- **Game:** laboratorio vacío con tamaño, semilla de sesión, zoom y desplazamiento. Crear mundo indica no disponible. No hay etapas, capas o comparador geográficos.
- **Pruebas oficiales:** GEvolve.Tests/Active/; las pruebas históricas se conservan en sus archivos originales fuera de Compile. Los resultados anteriores corresponden al legado.

R00 y R01 fueron cerrados por el director con sus excepciones históricas registradas en el roadmap. **R02 implementado y revisado estáticamente; compilación, pruebas locales, revisión visual, auditoría y aprobación pendientes.** [Alcance y evidencia estática](../../../GEvolve.Tests/ValidationReports/R02_LEGACY_ISOLATION.md).

La presentación usa directamente HexGrid como dato espacial temporal, sin un snapshot geográfico ni un estado guardable. El contrato arquitectónico de snapshot para R03 sigue pendiente de definición y aprobación. No se duplica PangaeaGenerationSession.

**Reglas:** ninguna ruta activa invoca el archivo histórico. Solo se rescatarán piezas concretas después de revisión y autorización explícitas; nunca mediante una opción de compilación o ejecución que reactive el motor anterior.