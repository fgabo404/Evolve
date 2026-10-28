# Precipitación y humedad — modelo v2

## Alcance

`PrecipitationGenerator` se ejecuta sobre el terreno de Pangea después de `TemperatureGenerator` y antes de `BiomeClassifier`. No modifica superficie, placas, elevaciones, pendientes ni semillas. Otros generadores sin alturas mantienen sus valores de clima ausentes.

## Cálculo aproximado (precipitation-2)

- En lugar de avanzar por filas independientes, la humedad se transporta entre **los seis vecinos de cada hexágono**, respetando la unión horizontal del mapa. Los vientos usan un componente este-oeste y un componente norte-sur.
- Los alisios, los vientos del oeste y los vientos polares presentan transiciones latitudinales suaves. La variación regional determinista perturba ligeramente el sentido local del viento.
- El océano suministra vapor según la temperatura. La condensación y las lluvias consumen humedad del aire.
- La subida de altitud desde vecinos de barlovento incrementa la condensación. La menor humedad del aire a sotavento produce sombra de lluvia. No se modifica el relieve.
- Se ejecutan **72 iteraciones** de transporte con 82 % del vapor restante enviado a sotavento. El cálculo de lluvia promedia las últimas 12 iteraciones para reducir el transitorio de inicialización.
- La circulación ecuatorial favorece lluvia, los subtrópicos favorecen aridez y las latitudes medias permiten mayor precipitación. Los factores son curvas suaves, no bandas sin interacción.
- El total anual se escala a mm/año con un factor de calibración 6500. Es una **aproximación jugable**, no un modelo atmosférico físicamente validado.
- La humedad disponible de 0–100 relaciona la precipitación con una demanda evaporativa que aumenta cuando hace más calor. No representa humedad relativa atmosférica.

`ClimateState` guarda `BasePrecipitationMmByCellId` (`ushort[]`), `BaseMoistureByCellId` (`byte[]`) y `PrecipitationModelVersion`. La vista **PRECIPITACIONES** muestra mm/año y la selección muestra lluvia y humedad. Los guardados antiguos se mantienen compatibles.

## Verificación de distribución

`PrecipitationTests` calcula y muestra en **nueve mapas** (tres tamaños × semillas 2026, 42, −731):

- Min, max y promedio anual de precipitación.
- Porcentaje terrestre árido con menos de 250 mm/año.
- Porcentaje terrestre húmedo con 1000 mm/año o más.
- Comprobaciones de determinismo, vista, persistencia y comportamiento de lluvia de barlovento frente a sotavento sobre un relieve de prueba.

No se imponen cuotas de desierto o lluvia. La calibración debe validarse visualmente y revisarse tras ejecutar los tests locales. No se han ejecutado los tests ni la compilación en este entorno.

## Integración con biomas (biomes-4)

El orden de generación es **superficie/elevación → temperatura → lluvia/humedad → biomas**.

El clasificador **siempre filtra primero por los rangos de elevación, latitud y hemisferio** definidos en `biomes.json`. Después prioriza:

- **Desierto** si lluvia anual <250 mm y humedad ≤25, salvo frío intenso, y solo donde el catálogo lo permite.
- **Estepa y sabana seca** para lluvia baja pero no extrema, atendiendo a temperatura y latitud.
- **Pradera o sabana** con precipitación intermedia y temperaturas compatibles.
- **Bosque tropical, subtropical, templado o boreal** cuando llueve suficientemente y la temperatura es adecuada.
- **Tundra** en condiciones frías compatibles.
- **Montaña alta o abrupta, polos y océanos** conservan su prioridad previa. Cumbres infranqueables desde 85 permanecen blancas en la vista de biomas.

Los **salares** no se infieren de aridez sin geología; tampoco se infieren humedales sin hidrología superficial.

Si faltan datos climáticos completos, el clasificador conserva el criterio basado en elevación, latitud y pendiente. Los mapas guardados mantienen su clasificación; los nuevos aplican `biomes-4`. El clima base se conserva separado de los modificadores futuros de calentamiento global.

Se incluye un test específico: **26,7 °C, 81 mm/año, humedad 7/100, latitud 13,1° S, elevación 12** debe clasificarse como desierto arenoso, no sabana.
