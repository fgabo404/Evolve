# Memoria operativa de Evolve

Consulta primero [arquitectura](../GEvolve.Engine/WORLD_GENERATION_ARCHITECTURE.md) y [roadmap](../GEvolve.Engine/WORLD_GENERATION_ROADMAP.md). Esta memoria es un resumen de trabajo, no una autorización para introducir funcionalidades.

## Prioridad y estado

- **Rama:** `worldgenerator`. La limpieza H05 fue solicitada por el usuario; aprobación de compilación y suite local aún pendiente.
- **Prioridad inmediata:** continuar exclusivamente con Pangea, completando sus capas con la geometría actual sin recalibrarla durante la limpieza. Tras aprobación, adaptar las capas comunes a los demás tipos.
- **H01–H04:** conservar topología flat-top, cuatro tamaños, costura E/O y bordes N/S abiertos, 16 placas, movimientos, contactos y esfuerzos tectónicos. Preservar baselines tectónicos y geográficos.
- **H05 de Continents:** experimentos de corteza A1/A2/B1/B2/B3, ensayos regionales A/B y C1 retirados del código operativo. Historial disponible y respaldo `backup/worldgenerator-pre-h05-cleanup-20261105`; no reintroducir código por compatibilidad sin autorización.
- **Tipos:** `Pangaea` seleccionable y prioridad, `Continents` seleccionable pero `SurfaceGenerationPending`, `Archipelagos` seleccionable, `SmallContinents` histórico compatible.
- **Capas:** relieve y elevación, temperatura, lluvia/humedad, pendientes, biomas, recursos y catálogos deben seguir desacoplados y conservados.
- **Validación:** usuario ejecuta la compilación y las pruebas; no exportar PNG/SVG/CSV/HTML ni utilizar laboratorios externos. Inspección visual solo dentro de Evolve.
- **Persistencia:** no asumir que los saves experimentales H05 con `SharedCortex` o superficie preliminar siguen admitidos; cualquier migración se decide por separado. Los saves históricos de los mundos conservados mantienen sus contratos.
