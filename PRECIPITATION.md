# Primera capa de precipitaciones y humedad

El modelo determinista `precipitation-1` genera precipitación anual en mm y disponibilidad relativa de humedad (0–100) después de la temperatura. Se calcula solo en mundos con elevaciones, actualmente Pangea. Las bandas de vientos, la evaporación oceánica, el ascenso orográfico y la reducción de vapor a sotavento forman un modelo provisional y simplificado, no una simulación meteorológica completa. Los valores numéricos deberán calibrarse con varias semillas.

`ClimateState` persiste los arreglos opcionales `BasePrecipitationMmByCellId` y `BaseMoistureByCellId`, preservando compatibilidad con guardados anteriores. El botón PRECIPITACIONES colorea mm/año y la selección muestra lluvia y humedad. La vista no modifica los datos del mundo.

Pangea, relieve, elevaciones, temperatura existente y clasificación de biomas no se modifican. No se han ejecutado las pruebas locales ni validación OpenGL desde esta sesión. La siguiente fase debe medir y ajustar la distribución de lluvia antes de conectarla a desiertos y demás biomas.
