# Evolve — Pipeline de generación por capas

**Reestructuración acordada: 2026-10-28**

```text
[Infraestructura] Cuadrícula hexagonal con unión este-oeste
   ↓
01. Placas tectónicas (universales, independientes del tipo de mundo)  ← ACTIVO
   ↓
02. Tipo de mapa: Pangea, continentes, continentes pequeños, archipiélagos
    ├─ Tierra y océanos
    └─ Elevaciones
   ↓
03. Temperatura
   ↓
04. Precipitaciones y humedad
   ↓
05. Pendientes y clasificación de biomas
   ↓
06. Mundo completo
```

## Qué cambia ahora

1. **Las 16 placas se generan antes de elegir la forma continental.** Con igual semilla y tamaño, los cuatro tipos de mundo comparten el mismo `PlateByCellId`.
2. **Nueva geometría:** `TectonicPlateGenerator` utiliza 16 orígenes separados por muestreo de candidatos, crecimiento simultáneo sobre vecinos hexagonales, velocidades relativas de expansión diferentes y un coste de crecimiento suavemente variable (ruido periódico). Todas las placas son regiones conectadas y las fronteras son irregulares. Es una aproximación procedural a las grandes formas tectónicas de la Tierra, **no una simulación científica de cinemática tectónica**.
3. **Solo capa 01 activa en el juego:** `WorldGenerationService.Generate` crea la cuadrícula y los IDs de placa, pero **no** genera tierra, océanos, altura, temperatura, lluvia ni biomas. El mapa se abre mostrando las placas y sus límites. El selector de tipo de mapa y los modos de terreno/clima se ocultan temporalmente.
4. **Código conservado:** `LandmassGenerationService.Generate`, `PangaeaGenerator`, los tres otros generadores, `TemperatureGenerator`, `PrecipitationGenerator`, `BiomeClassifier` y sus pruebas siguen en el repositorio. Son la implementación histórica que adaptaremos progresivamente a estas capas. **No deben mezclarse aún sus antiguas placas Voronoi con la nueva capa independiente.**
5. **Estado provisional del preview:** `LandmassState.TectonicPreviewOnly` indica que `SurfaceByCellId` es solo una matriz vacía para compatibilidad. Un valor 0 aquí **no indica océano verdadero**. El arreglo de placas contiene los únicos datos geográficos definitivos de esta etapa.

## Siguiente paso

Validar visualmente las formas de las 16 placas sobre los cuatro tamaños; revisar continuidad horizontal, variedad de áreas, conectividad y determinismo. Solo después reactivar la capa 02, haciendo que los generadores de continentes y relieve **consuman el mismo arreglo de placas** en lugar de crear otro.

## Comprobación

```powershell
git pull origin master
dotnet build
dotnet run --project GEvolve.Tests
dotnet run --project GEvolve.Game
```

La ejecución de .NET/OpenGL debe verificarse en un entorno compatible; no se ha ejecutado dentro de esta sesión.
