## En desarrollo — Selector de tamaño dentro del mapa

- Controles Pequeño, Mediano y Grande en la vista estratégica, debajo del panel con tipo de mundo y semilla.
- Al cambiar de tamaño se genera nuevamente el mismo tipo con la misma semilla; Regenerar mundo continúa usando una semilla nueva.

## En desarrollo — Ajustes de Pangea y HUD

- Pangea con costas más accidentadas mediante cuatro escalas de ruido periódico, mayor variación de ramificaciones y golfos.
- Panel en el mapa que muestra el tipo de mundo y la semilla activa tras cada generación/regeneración.
- Versión del generador `landmass-3`. No se modifica la etiqueta histórica `v0.001`.

## En desarrollo — Pangea 2.0

- Nuevo esqueleto continental ramificado, costas periódicas y golfos; una sola masa conectada sin aguas interiores cerradas.
- Botón Regenerar mundo en el mapa: nueva semilla manteniendo tamaño y tipo de mundo.
- Identificador de generación actualizado a `landmass-2`. La etiqueta histórica `v0.001` no se modifica.

# Historial de Evolve

## v0.001 — Primera prueba de generación del mundo

- Menú principal con versión visible, creación de mundo, opciones y salida.
- Tres tamaños de mapa: 100 × 70, 150 × 100 y 250 × 200.
- Grid hexagonal flat-top `odd-q` con IDs estables y continuidad horizontal.
- Cuatro algoritmos de forma terrestre: Continentes, Pangea, Continentes pequeños y Archipiélagos.
- Semillas enteras editables, aleatorias y reproducibles.
- Estado y guardado de la clasificación tierra/océano por celda, con parámetros efectivos y versión del generador; lectura de guardados anteriores.
- Visualización estratégica 2D con rellenos de tierra y océano, zoom, arrastre y selección de celdas.
- Pruebas de generación, topología, determinismo, persistencia y renderizado OpenGL.

Todavía no hay elevaciones, clima, biomas, recursos, animales, civilizaciones, vista 3D ni controles de guardar/cargar en el menú. Los parámetros geográficos actuales son experimentales.
