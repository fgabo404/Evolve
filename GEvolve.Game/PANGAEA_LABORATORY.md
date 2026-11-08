# Laboratorio de Pangea — Panel lateral y flujo secuencial

El laboratorio está integrado en Game y mantiene el motor y los datos geográficos existentes. La validación automatizada es exclusivamente de código; la inspección y aprobación visual corresponden al usuario. No se ejecutan ventanas, contextos GL, capturas ni automatización gráfica desde Codex.

## Uso manual exclusivo del usuario

Desde la raíz: `dotnet run --project GEvolve.Game` → **LABORATORIO DE PANGEA**. **Crear mundo** conserva el flujo automático.

La columna izquierda contiene, en este orden: **GENERACION DE MUNDO LAB**, tipo mapa, tamaño, semilla, vista, generación manual, información de celda y **SALIR AL MENÚ**. El tipo ofrece explícitamente **PANGEA - ÚNICO DISPONIBLE**. El mapa usa todo el rectángulo restante a la derecha, sin controles superpuestos. La columna mide 280 px (240 px en ventanas de menos de 800 px de ancho). En ventanas bajas, rueda sobre los controles para desplazarlos; el inspector y la salida permanecen fijos abajo.

- La cuadrícula oficial se ejecuta automáticamente al iniciar/reiniciar. Mientras se prepara, los botones esperan. Cuando termina, solo 01 está habilitado.
- **01 Generar placas y movimientos** encadena las operaciones originales Plates→Motion. Después se habilita **02 Generar superficies**, luego **03 Generar elevaciones** (BASE), **04 Generar sistemas montañosos** y **05 Generar elevaciones finales**. Los pasos previos completados pueden repetirse. El bloqueo secuencial es una política de Game; Engine conserva sus siete etapas y su DAG original.
- **Tamaño** limpia toda la sesión y prepara una cuadrícula nueva. Editar **semilla** limpia inmediatamente capas, estados, trabajo y selección; pulsar **INICIAR / REINICIAR** para aplicar el texto válido. Texto incompleto o fuera de int32 no permite generar.
- El dado genera un int32 nuevo con aleatoriedad criptográfica de Game, borra automáticamente toda la sesión y arranca solo la cuadrícula. No hay confirmación ni ejecución completa implícita. El RNG determinista de Engine no cambia. **Ctrl+C** copia la semilla; **Ctrl+V** con foco reemplaza el campo.
- **Vista** ofrece únicamente capas vigentes: cuadrícula, placas, movimiento/H04, compresión, extensión, cizalla, superficie, BASE, sistemas, APORTE y FINAL.
- Las alturas no requieren interruptor: BASE muestra BASE, APORTE muestra aporte y FINAL muestra FINAL; otras vistas utilizan FINAL vigente o BASE. Sin un campo disponible no se inventa un número. Cada número se mide con los glifos bitmap reales y solo aparece cuando cabe centrado, con margen de 2 px, dentro de su hexágono y del viewport. La letra se ajusta entre 1.25 y 1.5 px por píxel bitmap; si no alcanza el mínimo legible se oculta completamente. No hay números espaciados, muestras ni promedios a zoom lejano. El contraste blanco/negro sigue el fondo real de la capa. El inspector mantiene los valores exactos independientemente del zoom.
- En SUPERFICIE, océano azul; tierra con altura ≤90 conserva su color y tierra >90 es blanca. Se usa FINAL vigente, en su ausencia BASE, y sin alturas solo tierra/océano. La regla no cambia la paleta global del juego ni los guardados.
- Clic selecciona un hexágono; arrastrar mueve el mapa y rueda sobre él hace zoom. El inspector inferior izquierdo muestra identificador, coordenadas, BASE/APORTE/FINAL y datos de placas/esfuerzos/sistemas disponibles. Rueda dentro del inspector para leer todos los detalles, con un tamaño de letra fijo; pendiente/invalidado y cero real se distinguen.
- **CANCELAR** solicita descarte cooperativo. Los bucles internos pueden terminar antes de observarlo; H04 en cola no comienza después de cancelar. **INICIAR / REINICIAR** reintenta una cuadrícula cancelada. **SALIR AL MENÚ** o Escape abandona la sesión temporal.

## Arquitectura

`PangaeaLaboratory` controla exclusivamente la experiencia de Game y utiliza `WorldGenerationService.CreatePangaeaSession`. La secuencia se comprueba en el controlador, además de bloquear los botones; no modifica Engine. Un cambio de configuración cancela/abandona el trabajo, borra datos y selección, y evita publicar terminaciones obsoletas. Una revisión local de sesión impide reutilizar recursos de otra semilla/tamaño aunque las revisiones internas vuelvan a comenzar.

`PangaeaLaboratoryLayout` comparte rectángulos de dibujo/hit testing, controles desplazables, inspector y salida fijos, y dropdowns contenidos en la columna. `PangaeaLaboratoryWindow` administra recursos GL en el hilo gráfico. Generación y copia de snapshots siguen fuera de él; no hay snapshots ni hashes por frame. Los datos parciales nunca se convierten en estados persistibles ficticios.

La cámara admite un origen de viewport explícito para ocupar el área real del mapa. El mismo renderer añade ese origen a la proyección y overlays. El `Fit` tradicional conserva origen X=0, Y=58 y dimensiones previas; el laboratorio utiliza X=ancho del panel, Y=0. No cambian hexágonos, topología, colores geográficos ni fórmulas.

`PangaeaMapData.Height` lee campos disponibles sin alterarlos; `PangaeaHeightLabels` genera anotaciones de valores existentes, solo cuando caben con tamaño legible, sin muestreo. No calcula elevación ni crea otra geografía.

## Validación técnica

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\GEvolve.Tests\ValidationReports\commands.ps1 -ValidationSet LaboratoryPangaea -Label P03-A723 -IsolatedArtifacts
```

Incluye build, `--pangaea-laboratory`, `--pangaea-staged` y `--all-suites`. Las pruebas del laboratorio verifican secuencia, reinicios inmediatos, descartes asíncronos, campos numéricos, límites del layout, coordenadas de cámara y equivalencia automática en cuatro tamaños. La referencia por etapas cubre 28 Pangea y 12 controles. Los baselines históricos no se actualizan.

`-IsolatedArtifacts` coloca salidas de build y run en una carpeta por ejecución dentro de `GEvolve.Tests/bin/ValidationArtifacts`, ya ignorada por Git. Permite validar sin reemplazar DLLs que tenga abiertas el juego del usuario. No inicia, inspecciona ni cierra ese proceso.

La apariencia, fluidez, portapapeles, dado y lectura real de etiquetas/scroll quedan pendientes de revisión manual. Los estados parciales no son partidas guardables. P03-A permanece sin aprobación; P03-A.7.3, P03-A.7.4 y P03-B no se inician con este rediseño.
