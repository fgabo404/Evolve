# Continentes: selección mediante cordilleras terrestres continuas

La cuadrícula, las 16 placas, sus vectores y sus campos de esfuerzo se calculan una sola vez. `OrogenicOpportunityAnalyzer` mantiene su análisis anterior de márgenes convergentes; `PlateGroupSurfaceGenerator` mantiene la selección propuesta de dos a seis grupos conectados y la generación de aproximadamente 35 % de tierra. Ninguno de esos algoritmos cambia en esta etapa.

`WorldGenerationService` evalúa como máximo ocho candidatos deterministas. Conserva el control de calidad de superficie: cada grupo debe tener una masa principal reconocible, al menos 1 % del mapa y 50 % de la tierra del grupo en su componente mayor.

El candidato se acepta por sus elevaciones finales de versión 6. Una cordillera principal exige una cresta terrestre continua, área elevada conectada, anchura y prominencia respecto de los flancos. La longitud del límite tectónico sumergido y una colección de cumbres aisladas no sustituyen esos requisitos. Las reglas, unidades y umbrales por tamaño están documentados en [CONTINENTAL_ELEVATION.md](CONTINENTAL_ELEVATION.md).

Pequeño y Mediano aceptan el primer candidato válido. Grande y Enorme siguen buscando dos sistemas principales; si no aparecen dentro de ocho intentos, se conserva el mejor candidato que tenga uno. Si ninguno cumple, la generación falla explícitamente. No se cambia la semilla, no se mueven placas y no se agregan montañas para cumplir una cuota. No fue necesario aumentar los ocho intentos.

La elevación validada es la misma que recibe el juego. Se elimina la segunda pasada que antes evaluaba una versión antigua y luego recalculaba otra para mostrarla. `ContinentalSurfaceState.Selection` conserva los intentos, sus resultados y el intento elegido. `FinalPrincipalSystems` y `FinalSecondarySystems` coinciden con la evaluación elegida en la versión nueva; se conservan ambos campos para leer guardados anteriores.

## Superficies históricas y cambios de selección

Preservar el algoritmo de superficie no implica que una validación más exigente escoja siempre el mismo intento. Las alternativas para una misma semilla siguen produciendo exactamente las superficies anteriores; únicamente puede cambiar cuál se acepta.

Antes de modificar el generador se registraron 52 candidatos elegidos por `9fec672` en `OrogenicInputBaseline.txt`: cuatro tamaños y trece semillas. Contienen el intento y hashes SHA-256 de superficie, placas, movimiento completo, grupos, corteza e identidad continental. Las pruebas vuelven a generar esos intentos y exigen igualdad exacta. También reproducen por separado el candidato elegido por la versión nueva.

Las huellas históricas `ApprovedContinentalSurfaceBaseline.txt` y `ApprovedContinentalSelectionBaseline.txt` permanecen intactas. Las pruebas informan cuántas selecciones finales difieren y verifican que cada candidato histórico conserva su huella. `orogenic-comparison.csv` detalla intento anterior, intento nuevo y cambio de superficie para cada mundo; no se presenta un cambio de selección como una modificación del generador de costas.

Los guardados existentes no se vuelven a generar al cargar. La compatibilidad de los datos antiguos y de la geometría nueva de crestas se comprueba con guardado y lectura.

## Diagnósticos

```powershell
dotnet run --project GEvolve.Tests --no-build --no-restore -- --orogenic-morphology
dotnet run --project GEvolve.Tests --no-build --no-restore -- --orogenic-attempts
dotnet run --project GEvolve.Tests --no-build --no-restore -- --continental-selection
```

El segundo comando muestra los ocho candidatos de la semilla Enorme `-1358931540`, con longitud, anchura, área conectada, prominencia y elegibilidad de sus sistemas. Los SVG y CSV antes/después permiten revisar el relieve junto con las placas y el campo de levantamiento.

La garantía continúa condicionada al éxito de generación. No se afirma que cualquier combinación imaginable de placas y movimientos admita una cordillera terrestre válida con ocho agrupaciones. La falla explícita preserva esa distinción y evita aceptar montañas pequeñas como si fueran una gran cordillera.
