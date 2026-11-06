# Memoria operativa de Evolve

Consulta primero [arquitectura](../GEvolve.Engine/WORLD_GENERATION_ARCHITECTURE.md) y [roadmap](../GEvolve.Engine/WORLD_GENERATION_ROADMAP.md). Esta memoria es un resumen de trabajo, no una autorización para introducir funcionalidades.

## Prioridad y estado

- **Rama:** `worldgeneration_pangea`. P03-A.2 validado técnicamente; P03-A.3 rechazado visualmente. P03-A.4: build/elevaciones/cordilleras PASS, suite general con dos fallos históricos de elevación (baselines intactos). P03-A.5: build y auditoría de cinco semillas Large PASS con autorización específica de ejecución; estado completo idéntico con/sin traza. [Informe](../GEvolve.Tests/ValidationReports/P03-A5_20261106_143813456Z_ed0d8bb9/REPORT.md): no hay evidencia de exceso general de filtros; concurren disponibilidad tectónica local, BASE y visibilidad sub85. No se implementó P03-A.6. El seguimiento oficial está en `GEvolve.Engine/PANGAEA_ROADMAP.md`; P03-A sigue sin aprobación y pendiente de revisión visual, con geometría y alturas preservadas por esta auditoría.
- **Prioridad inmediata:** continuar exclusivamente con Pangea, completando sus capas con la geometría actual sin recalibrarla durante la limpieza. Tras aprobación, adaptar las capas comunes a los demás tipos.
- **H01–H04:** conservar topología flat-top, cuatro tamaños, costura E/O y bordes N/S abiertos, 16 placas, movimientos, contactos y esfuerzos tectónicos. Preservar baselines tectónicos y geográficos.
- **H05 de Continents:** experimentos de corteza A1/A2/B1/B2/B3, ensayos regionales A/B y C1 retirados del código operativo. Historial disponible y respaldo `backup/worldgenerator-pre-h05-cleanup-20261105`; no reintroducir código por compatibilidad sin autorización.
- **Tipos:** `Pangaea` seleccionable y prioridad, `Continents` seleccionable pero `SurfaceGenerationPending`, `Archipelagos` seleccionable, `SmallContinents` histórico compatible.
- **Capas:** relieve y elevación, temperatura, lluvia/humedad, pendientes, biomas, recursos y catálogos deben seguir desacoplados y conservados.
- **Validación:** usuario ejecuta la compilación y las pruebas; no exportar PNG/SVG/CSV/HTML ni utilizar laboratorios externos. Inspección visual solo dentro de Evolve.
- **Persistencia:** no asumir que los saves experimentales H05 con `SharedCortex` o superficie preliminar siguen admitidos; cualquier migración se decide por separado. Los saves históricos de los mundos conservados mantienen sus contratos.
