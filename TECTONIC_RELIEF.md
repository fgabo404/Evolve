# Deformación tectónica y accidentes geológicos

**Versión:** `tectonic-relief-1` (posterior a `tectonic-motion-1`).

## Objetivo y arquitectura

El mundo ahora aplica las mismas velocidades e interacciones reales de las 16 placas que se ven en la interfaz, en vez de inventar un segundo conjunto de velocidades para la etapa final de deformación. El generador nuevo es `TectonicReliefGenerator`.

```text
Cuadrícula hexagonal con costura este-oeste
  -> Placas tectónicas (16 IDs invariables)
  -> Velocidades + interacciones + campos C/D/S
  -> Tipo de mundo: superficie y elevación base, sin cambiar costas
  -> Deformación tectónica compartida, accidentes y profundidad marina
  -> Clima y biomas (etapas aún pendientes en el pipeline activo)
```

Se aplica por igual a Pangea, Continentes, Continentes pequeños y Archipiélagos. El proceso mantiene el mismo intento determinista y exactamente las mismas placas, masas terrestres y fracción de océano que el algoritmo continental previo. **Las elevaciones finales cambian intencionadamente.**

### Proceso

Los campos `CompressionByCellId`, `ExtensionByCellId` y `ShearByCellId` son cinemática en cm/año, no presión física. Se propagan desde las fronteras por hexágonos reales con envolvente gaussiana y respetan la costura horizontal. Para una intensidad `v`, el factor de deformación es `sqrt(clamp(v/8, 0, 1))` y se anula si la celda está a más de `2.5 × InfluenceRadiusHexes` de la frontera.

**En tierra:** `elevaciónFinal = clamp(elevaciónBase + round(36C' - 25D' - 16S'), 1, 100)`.
- Convergencia: cordilleras / levantamiento.
- Divergencia: valles de rift / subsidencia.
- Cizalla: valles de falla transformante.

**Bajo el océano:** la elevación de superficie permanece **0**. Se añade una magnitud independiente de profundidad de fondo `clamp(52 + round(26C' - 23D' + 8S'), 1, 100)`, con 0 sobre tierra.
- Convergencia: fosas oceánicas (profundidad creciente).
- Divergencia: dorsales mesoceánicas (profundidad decreciente).
- Cizalla: fallas transformantes oceánicas.

El accidente se clasifica por el canal dominante en celdas próximas a una frontera con al menos **1 cm/año** de actividad. No se coloca un accidente en una región quieta o lejos de los límites. La clasificación es una **aproximación morfológica**, no un diagnóstico de subducción confirmado: todavía faltan tipos de corteza persistidos por placa, edad oceánica, densidad, espesor, deformación acumulada, tiempo, erosión e isostasia. Por eso no se crean volcanes automáticos ni se afirma que todas las convergencias produzcan exactamente la misma orogenia.

El relieve base sigue aportando grandes gradientes interiores y ruido espacial de detalle. Su distribución inicial puede conservar indicadores sintéticos antiguos para el ensamblaje continental y la forma base de Pangea; **la etapa final de deformación tectónica no utiliza esos indicadores**. En una fase posterior se podrán integrar los campos C/D/S en las etapas originales sin desestabilizar costa, semillas y perfiles.

## Estado y visualización

`LandmassState` conserva tres propiedades nuevas opcionales para compatibilidad:
- `GeologicFeaturesByCellId`: un byte por hexágono (None, MountainRange, RiftValley, FaultValley, OceanTrench, MidOceanRidge, OceanTransformFault).
- `OceanFloorDepthByCellId`: profundidad relativa de 1..100 en mar y 0 en tierra (no expresa metros).
- `GeologicModelVersion`: `tectonic-relief-1`.

Los mundos antiguos se cargan sin estas propiedades y no son regenerados ni alterados. La validación de guardado rechaza longitudes incompatibles, profundidades incoherentes y accidentes de mar sobre tierra (o viceversa). La vista **RELIEVE: GEOLOGÍA** muestra las categorías con colores separados, y la selección de hexágono muestra su accidente y profundidad relativa en el mar. Los modos de compresión, extensión y cizalla permanecen independientes.

## Pruebas y limitaciones de validación

```powershell
dotnet build GEvolve.sln --no-restore
dotnet run --project GEvolve.Tests --no-restore -- --tectonic-relief
dotnet run --project GEvolve.Tests --no-restore -- --tectonic-motion
dotnet run --project GEvolve.Tests --no-restore -- --all-suites
dotnet run --project GEvolve.Game --no-restore -- --graphics-smoke
git diff --check
```

`TectonicReliefTests` cubre estímulos controlados de C/D/S en tierra y océano, ausencia de movimiento, límite de influencia, costura horizontal y determinismo. `StagedLandmassTests` verifica recomputación determinista y persistencia de la nueva capa; `TectonicMotionTests` sigue comparando **los hashes históricos de superficie y placas**, pero deja de exigir elevaciones idénticas porque la deformación es precisamente el cambio solicitado. El archivo de huellas históricas no se modifica.

**Validación por ejecutar en un entorno con .NET 10 y OpenGL.** Los fallos ya documentados de `Continentes/Enorme/2026` y `StagedLandmass` no se silencian ni se declaran solucionados.
