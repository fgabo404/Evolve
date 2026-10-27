# Evolve — Arquitectura

```text
Definitions → Engine ↔ State → Game
                              ├── Menú y configuración
                              ├── Vista estratégica 2D (actual)
                              └── Vista 3D (pendiente)
```

`GEvolve.Definitions` conserva los tres tamaños oficiales de la Capa 01 y sus reglas declarativas. `GEvolve.Engine.Grid` construye el grid flat-top `odd-q`, asigna IDs fila-columna y resuelve geometría, vecindad y continuidad este-oeste. `GEvolve.State` guarda la configuración efectiva en `HexGridState`; no duplica las celdas. `GEvolve.Game` consume esos modelos y los presenta con OpenTK/OpenGL, sin implementar generación espacial propia. No existen referencias circulares.

## Flujo de Game

`GameSession` empieza en el menú principal, sin grid. **Crear mundo** abre una pantalla con los perfiles Pequeño (100 × 70), Mediano (150 × 100) y Grande (250 × 200); Pequeño es el predeterminado. **Generar mundo** obtiene la definición oficial, llama a `HexGrid.Generate`, guarda `HexGridState` y abre la vista estratégica. **Opciones** informa que sus ajustes avanzados están pendientes. **Salir** cierra la ventana. Volver al menú libera el grid de la sesión y el renderizador GPU del mundo. No se ejecuta el generador anterior de continentes.

`MapCamera2D` transforma entre coordenadas de pantalla y mundo, limita el zoom y el eje norte-sur y normaliza horizontalmente la posición de la cámara. La vista inicial mantiene un tamaño de hexágono legible; el jugador puede alejar el zoom para ver una zona más amplia. La selección usa `FlatHexGeometry.CellAt` y `HexGrid.TryGet`; las repeticiones visuales apuntan al mismo ID lógico.

`HexGridRenderer` mantiene un hexágono unitario en un VBO y los centros visibles en otro. Un VAO y `glDrawArraysInstanced` dibujan el lote de contornos; el VBO de instancias solo cambia con la cámara. El HUD, los botones y una fuente bitmap local se dibujan en un lote separado mediante `UiRenderer`. El renderizado se actualiza ante entrada, cambio de pantalla, animación de hover o cambio de tamaño. Los shaders y buffers se liberan al cerrar la ventana; los buffers específicos del mapa se liberan al volver al menú.

La vista 2D muestra únicamente el grid vacío: sin tierra, relieve, biomas ni recursos. La vista 3D del diseño final sigue pendiente. `GEvolve.Simulator` y el prototipo gráfico 3D previo ya no forman parte de esta solución.

## Ejecutar

```sh
dotnet build GEvolve.sln
dotnet run --project GEvolve.Tests
dotnet run --project GEvolve.Game
dotnet run --project GEvolve.Game -- --graphics-smoke
```

En la vista 2D: rueda para zoom, arrastre para desplazar, clic para seleccionar y **Menú principal** o `Esc` para volver.
La opción `--graphics-smoke` recorre automáticamente los tres tamaños, verifica errores OpenGL y muestra el número de instancias visibles y el tiempo de cada fotograma. Es una comprobación técnica, no sustituye la interacción manual.
