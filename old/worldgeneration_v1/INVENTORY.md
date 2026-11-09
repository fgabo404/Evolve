# Alcance de la copia V1

La carpeta `snapshot/` mantiene los archivos anteriores bajo sus rutas relativas originales:
- `GEvolve.Engine/World/` — generadores oficiales y experimentales V1.
- `GEvolve.Engine/Grid/` y componentes geométricos utilizados por el mundo.
- `GEvolve.Definitions/WorldGeneration/` — configuración y perfiles.
- `GEvolve.State/World/` y persistencia relacionada.
- `GEvolve.Game/` — laboratorio, renderer y consumidor de la generación.
- `GEvolve.Tests/` — pruebas y baselines de la versión anterior, sin duplicar ValidationReports.
- Documentación técnica antigua de Engine y Pangea.

La copia histórica no sustituye todavía el código activo. Codex realizará la migración física de las clases una por una en V2.1–V2.7 con compilación y pruebas. El historial inmutable de origen es `fd6fca73dd11d6a63dfa9a4ae6df086374561688`.
