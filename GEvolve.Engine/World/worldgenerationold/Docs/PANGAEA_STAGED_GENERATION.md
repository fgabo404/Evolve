# P03-A.7.1 — Motor de Pangea por etapas

**Contexto R01:** documento histórico; sus rutas y decisiones técnicas describen el generador anterior. El [único roadmap activo](../../WorldGeneration/ROADMAP.md) rige la transición en worldgeneration.


## Auditoría inicial, anterior a la refactorización

Punto de partida verificado: `0da9da7546f811f320fde5fe8b6ba399e7a6c811`, rama `worldgeneration_pangea`. Esta entrega implementa solo motor temporal, dependencias, invalidación y equivalencia. No contiene pantalla de laboratorio, controles nuevos, comparación visual ni auditoría geomorfológica integral.

`WorldGenerationService.Generate` valida el perfil oficial, crea cuadrícula/topología, genera las 16 placas y luego H04. Para Pangea prueba los intentos 0, 1, 2 y 3 en ese orden. Cada intento llama a `PangaeaFromPlatesGenerator.Generate`, que calcula superficie y elevación juntas; **la aceptación solo consulta la superficie** mediante `LandmassAnalysis` y `LandmassProfileValidator`. Se acepta la primera superficie con fracción terrestre 0.30–0.55, una componente y cuota dominante 1. Si fallan las cuatro, se lanza la excepción con las estadísticas del último intento. Nunca se selecciona por aspecto, altura ni cordilleras.

### Operaciones y datos reales

| Operación | Entradas efectivas | Salidas / acoplamiento |
|---|---|---|
| Cuadrícula | Tamaño oficial | HexGrid y HexTopology; E/O continuo, N/S abierto |
| Placas | Topología, semilla | Array de 16 IDs; independiente del tipo de mundo |
| H04 | Topología, configuración y placas | Movimientos, interacciones, compresión, extensión, cizalla, distancias y flechas; no consume BASE ni superficie |
| Superficie Pangea | Topología, placas, semilla, intento, perfil Pangea | Superficie tras crecimiento y cierre de agua interior; campo de presión aproximada necesario para BASE |
| BASE | Superficie aceptada, presión aproximada, topología, semilla e intento ya elegidos | Potencial, cuantiles, calibración P03-A.4, alturas y fuentes diagnósticas |
| Selección y propagación | Superficie, H04, topología y semilla | Candidatos, ejes, anchuras, perfiles, propietarios y muestras verticales; independiente de BASE |
| Composición FINAL | BASE y muestras de los sistemas existentes | P03-A.6, APORTE real, FINAL, comparación aditiva y contador real de celdas afectadas |

La presión aproximada interna de Pangea usa sus propios vectores históricos; no es el campo H04. El RNG `0x4C414E44 + intento` se consume en centros/vectores, raíz y selección regional, **antes de finalizar superficie**. La parte de BASE no usa ese RNG; sus llamadas de ruido son funciones de coordenadas, semilla e intento. Se puede conservar superficie y presión inicial, y ejecutar después exactamente el resto del cálculo. Los intentos rechazados dejan de calcular una BASE que nunca se utilizaba, sin cambiar el RNG del siguiente intento ni su criterio de aceptación.

En cordilleras, la prioridad de propagación sigue siendo el aporte aditivo histórico, aunque P03-A.6 calcula además una envolvente. El propietario y la cobertura no dependen de BASE. No basta guardar una única altura objetivo por celda: dos muestras con distinto objetivo y mezcla pueden dominar para bases distintas. Se conservará la secuencia exacta de muestras válidas (celda, aporte anterior, cresta y mezcla) de la propagación existente. FINAL solo las compone con BASE; no vuelve a seleccionar ni propagar sistemas. Sus fórmulas, máximos, orden y redondeos se mantienen.

### Configuración pública frente a constantes

La configuración pública es el perfil oficial completo (`LandmassProfiles.Create`): tamaño, tipo, semilla, versión y bloque Pangea. El servicio rechaza variaciones del perfil. La implementación Pangea consume `TargetLandFraction`; los demás campos históricos del bloque no son controles nuevos del laboratorio. Son constantes internas los 16 IDs, cuatro intentos, frecuencias/coeficientes de ruido, filtros geométricos, escalas de anchura, perfiles y parámetros P03-A.6. Esta entrega no expone ni modifica ninguno como parámetro editable.

### Consumidores de Game

`GameSession.BeginGeneration/CompleteGeneration` conserva el mundo completo y los diagnósticos temporales. `EvolveWindow` (en `GEvolve.Game/Program.cs`) ejecuta la generación con `Task.Run` y entrega el resultado a la sesión. Las vistas BASE/APORTE/FINAL consumen `PangaeaElevationDiagnostics`; superficie consume superficie, FINAL y biomas si existen; placas/H04 consumen IDs, movimientos y esfuerzos; relieve consume metadatos de sistemas. Actualmente esas rutas presuponen los estados que les entrega el flujo automático. P03-A.7.1 no les pasará estados incompletos ni modificará sus controles. La adaptación explícita de vistas intermedias pertenece a P03-A.7.2.

## Diseño de la sesión temporal

La sesión vive en Engine, fuera de State y de los guardados. El servicio oficial crea la sesión y el automático ejecuta sus mismas operaciones. Las APIs previas de los generadores siguen componiendo las operaciones extraídas; no se conserva una segunda implementación geográfica.

Grafo de dependencias **directas**:

```text
01 Grid -> 02 Plates -> 03 Motion
                  \-> 04 Surface -> 05 Base
03 Motion + 04 Surface -> 06 MountainSystems
05 Base + 06 MountainSystems -> 07 Final
```

Las referencias implícitas a cuadrícula/configuración vienen de sus ascendientes. Superficie no depende de H04; BASE no depende de cordilleras. Sistemas puede ejecutarse antes de BASE si superficie y H04 están completos.

Repetir una etapa borra su resultado y todos sus descendientes **al comenzar**, no al terminar. Repetir BASE conserva sistemas e invalida FINAL; repetir H04 conserva superficie/BASE e invalida sistemas/FINAL; repetir superficie conserva placas/H04 pero invalida BASE/sistemas/FINAL. Cambiar configuración reinicia todo, incluso si un trabajo anterior aún sigue calculándose. No se publicará un resultado cuyo identificador de operación/configuración haya caducado.

Cada etapa informa estado, disponibilidad por dependencias, revisión, hash de salida, hashes/revisiones de entradas, tiempo y error cuando exista. Las vistas de datos se entregan como copias desligadas de la sesión; no se muestran arrays de ceros como si fueran resultados aún no ejecutados. Solo una etapa FINAL vigente permite exportar un `GeneratedWorld` completo. La API asíncrona permitirá mantener la futura interfaz receptiva; la cancelación se observa antes y después de cada operación sin modificar bucles ni resultados geográficos.

## Límites de esta entrega

### API implementada

```csharp
var service = new WorldGenerationService();
var configuration = LandmassProfiles.Create(GridSize.Large, LandmassType.Pangaea, -1387338765);
var laboratory = service.CreatePangaeaSession(configuration);
await laboratory.ExecuteAsync(PangaeaGenerationStage.Grid);
await laboratory.ExecuteAsync(PangaeaGenerationStage.Plates);
await laboratory.ExecuteAsync(PangaeaGenerationStage.Motion);
await laboratory.ExecuteAsync(PangaeaGenerationStage.Surface);
await laboratory.ExecuteAsync(PangaeaGenerationStage.Base);
await laboratory.ExecuteAsync(PangaeaGenerationStage.MountainSystems);
await laboratory.ExecuteAsync(PangaeaGenerationStage.Final);
GeneratedWorld completed = laboratory.GetCompletedWorld();
```

`GetStages()` es la consulta ligera para botones/estado. `GetSnapshot()` devuelve datos desligados, con configuración/revisión y campos nulos donde todavía no hay resultado. Debe solicitarse al cambiar de revisión y almacenarse en el consumidor, no copiar mapas cada frame. El snapshot de sistemas expone geometría, cobertura y conteos de decisiones reales; no fabrica un contador `AffectedCells` antes de FINAL. Los hashes de BASE/placas usan bytes; H04 y el estado final usan JSON determinista; superficie incluye intento y hash de bytes; sistemas incluye geometría y propietarios. Las revisiones/tiempos son metadatos de ejecución y no entran en los hashes geográficos.

`Reset(configuration)` valida primero el perfil y después invalida la sesión completa. `Execute` es la variante síncrona del mismo mecanismo. Las excepciones por prerequisitos o configuración no modifican el estado; una repetición válida invalida su rama al comenzar. Un fallo de perfil conserva las estadísticas de los intentos examinados sin publicar una superficie aceptada. `GetCompletedWorld()` falla si FINAL no está vigente. Ningún snapshot temporal se convierte implícitamente en un save.

El modo automático usa la misma sesión internamente, omitiendo hashes de etapa y copias de exportación que no necesita; conserva sus diagnósticos de elevación anteriores y la traza opcional. Los demás tipos mantienen su ruta de generación original. Las copias de cuadrícula solo duplican almacenamiento, no vuelven a calcular topología. La exportación completa clona el estado y los diagnósticos para que un consumidor no pueda alterar las dependencias conservadas.

- La retención de muestras añade memoria proporcional a los pasos válidos de la propagación existente; evita repetir selección o cambiar la influencia al componer BASE nueva. Su cantidad se registrará en las pruebas.
- Cancelar no interrumpe un generador a mitad de sus bucles; impide publicar su resultado y mantiene seguro el reinicio. No se introduce un hilo gráfico en Engine.
- Los hashes y metadatos de ejecución son infraestructura de P03-A.7.1. ANTES/DESPUÉS, histogramas enriquecidos y comparación visual pertenecen a P03-A.7.3.
- P03-A.7.2 incorpora la [interfaz de Game](../../../../GEvolve.Game/PANGAEA_LABORATORY.md), con botones habilitados por el grafo, estado de trabajo, selección de vistas compatibles y datos intermedios explícitos. Reutiliza esta API sin cambiar Engine; queda pendiente de aprobación visual del usuario.
- P03-A.7.4 realizará la auditoría integral utilizando el laboratorio funcional. P03-A sigue sin aprobación y P03-B no comienza.

## Continuación vigente P09.1

Las siete operaciones anteriores conservan sus valores enum y hashes. Se añaden `08 Temperature` (calcula pendientes desde FINAL), `09 Precipitation` (lluvia/humedad) y `10 Biomes` al mismo DAG: `Final → Temperature → Precipitation → Biomes`. El laboratorio agrupa Grid/Plates/Motion como antes y muestra ocho acciones. `GetSnapshot` incluye opcionalmente Slopes/Climate/Biomes mediante copias aisladas; al invalidar una entrada desaparecen sus descendientes. `GetCompletedWorld` sigue requiriendo FINAL y exporta las capas ambientales vigentes si existen. `Generate` completa la continuación ecológica para Pangea; `GenerateGeology` recorre solo `GeologicalStages` en el mismo orquestador. Los demás tipos no cambian. Detalles y límites: [P09.1](PANGAEA_ECOLOGY_INTEGRATION.md), integración inicial pendiente de calibración y aprobación visual.
