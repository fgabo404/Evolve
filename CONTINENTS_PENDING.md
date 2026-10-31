# Continentes: superficie pendiente

La ruta activa de **Continentes** se detiene después de generar la cuadrícula, las 16 placas originales, sus direcciones y velocidades y los campos de compresión, extensión y cizalla. No se calcula tierra, océano, costa, nivel del mar, corteza nueva, relieve, subducción ni volcanes para un mundo pendiente.

`LandmassState.SurfaceGenerationPending` identifica este estado. `SurfaceByCellId` es un arreglo **vacío**, no una máscara de océano, y `GeneratedWorld.Statistics` es nulo. `IsLand` rechaza consultas hasta que exista una superficie. La persistencia acepta este estado y valida que no incluya datos derivados de una superficie inexistente. Los guardados continentales anteriores, que sí contienen superficie y geología, siguen leyéndose sin regenerarlos.

El juego ofrece únicamente las vistas de placas y de compresión, extensión y cizalla. La selección de hexágonos muestra su placa y movimiento; las vistas de superficie, altura, clima, biomas y geología no se ofrecen en el estado pendiente. Los tres tipos de mundo restantes mantienen sus rutas de generación.

Se retiraron `ContinentsGenerator`, `ContinentalCrustGenerator`, sus capturas y los parámetros activos de porcentaje y crecimiento continental. `ContinentalCrustMap` se conserva separado como entrada del generador geológico para una futura fuente de corteza y superficie. `ContinentalReliefGenerator`, `GeologicalContactClassifier`, el estado de geología y sus validadores permanecen; el relieve rechaza ejecutarse sin una superficie clasificada. No se ha implementado un método de reemplazo.

## Comprobaciones

`ContinentalGeologyTests` compara placas y movimiento con la vista tectónica oficial en cuatro tamaños y tres semillas, verifica determinismo, persistencia del estado pendiente y lectura de un guardado geológico anterior. También comprueba que el clasificador y el generador geológico conservados funcionan con una superficie explícita de prueba. El archivo `ApprovedNonContinentalBaseline.txt` no cambia y sus 36 huellas comprueban Pangea, Continentes pequeños y Archipiélagos.

```powershell
dotnet build GEvolve.sln --no-restore
dotnet run --project GEvolve.Tests --no-build -- --continental-geology
dotnet run --project GEvolve.Tests --no-build -- --all-suites
dotnet run --project GEvolve.Game --no-build -- --graphics-smoke
git diff --check
```
