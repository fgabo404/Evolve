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
