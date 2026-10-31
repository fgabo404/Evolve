# Continentes: elevación tectónica

`WorldGenerationService` conserva sin modificaciones la superficie aprobada de `PlateGroupSurfaceGenerator`. A continuación, `ContinentalElevationGenerator` clasifica cada interacción original con `ContinentalReliefGenerator.ClassifyContacts`, que reutiliza `GeologicalContactClassifier` y su regla determinista de subducción. No se generan nuevas direcciones, velocidades ni campos tectónicos.

Las colisiones de corteza continental alimentan el levantamiento de cordilleras; la subducción oceánica–continental actúa en la placa superior. Los contactos oceánicos–oceánicos solo pueden elevar tierra preexistente. La divergencia continental reduce la altura en rifts, la cizalla produce deformación local y la divergencia oceánica reduce la profundidad de las dorsales. `TectonicMotionGenerator.Spread` propaga estas señales con anchura limitada sobre la vecindad hexagonal, incluida la costura horizontal. Una variación periódica pequeña evita cadenas uniformes. El interior estable y la distancia a costa determinan llanuras, colinas y mesetas.

`ElevationByCellId` usa 0 en océano y 1–100 en tierra. `ContinentalElevationState` guarda los contactos clasificados, el tipo de relieve y una profundidad oceánica relativa separada. La regla global de cumbres infranqueables desde 85 permanece en `TerrainRules`. No hay normalización por cuantiles de altura: el máximo cambia con la actividad geológica de la semilla.

La interfaz ofrece **SUPERFICIE**, **VER ALTURAS**, placas y los tres esfuerzos. El clasificador geológico y el generador histórico de volcanes siguen conservados, pero la nueva ruta **no genera volcanes**, clima ni biomas y no altera la costa.

## Validación

`ApprovedContinentalSurfaceBaseline.txt` se registró **antes** de integrar las alturas. Sus 12 filas SHA-256 cubren superficie, placas, agrupación, corteza e identidad continental en cuatro tamaños y tres semillas. La prueba de elevación verifica alturas y profundidades, asociación de cordilleras con convergencia, rifts junto a extensión, continuidad horizontal, determinismo y persistencia. La suite conserva un fallo previo de `StagedLandmass` en `Small/Pangaea` sin modificar esa prueba ni Pangea.

La comparación visual de **SUPERFICIE**, **VER ALTURAS** y **COMPRESIÓN** se exporta con:

```powershell
dotnet run --project GEvolve.Tests --no-build --no-restore -- --continental-elevation-captures
```

El modelo es una aproximación estática del movimiento actual. No simula millones de años de evolución geológica. Algunas semillas tienen cordilleras menores porque sus contactos convergentes ocurren principalmente en el océano; no se fuerzan máximos artificiales.
