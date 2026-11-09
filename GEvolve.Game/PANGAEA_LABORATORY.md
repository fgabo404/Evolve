# Laboratorio de Pangea — Panel lateral y flujo secuencial

El laboratorio está integrado en Game y mantiene el motor y los datos geográficos existentes. La validación automatizada es exclusivamente de código; la inspección y aprobación visual corresponden al usuario. No se ejecutan ventanas, contextos GL, capturas ni automatización gráfica desde Codex.

## Uso manual exclusivo del usuario

Desde la raíz: `dotnet run --project GEvolve.Game` → **LABORATORIO DE PANGEA**. **Crear mundo** conserva el flujo automático.

La columna izquierda contiene, en este orden: **GENERACION DE MUNDO LAB**, tipo mapa, tamaño, semilla, vista, generación manual, información de celda y **SALIR AL MENÚ**. El tipo ofrece explícitamente **PANGEA - ÚNICO DISPONIBLE**. El mapa usa todo el rectángulo restante a la derecha, sin controles superpuestos. La columna mide 280 px (240 px en ventanas de menos de 800 px de ancho). En ventanas bajas, rueda sobre los controles para desplazarlos; el inspector y la salida permanecen fijos abajo.

- La cuadrícula oficial se ejecuta automáticamente al iniciar/reiniciar. Mientras se prepara, los botones esperan. Cuando termina, solo 01 está habilitado.
- **01 Generar placas y movimientos** encadena las operaciones originales Plates→Motion. Después se habilita **02 Generar superficies**, luego **03 Generar elevaciones** (BASE), **04 Generar sistemas montañosos** y **05 Generar elevaciones finales**. Los pasos previos completados pueden repetirse. El bloqueo secuencial es una política de Game; Engine conserva las siete etapas geológicas y añade la continuación ecológica del mismo DAG.
- Después de FINAL: **06 Generar temperatura** calcula también pendientes del relieve FINAL; **07 Generar precipitación y humedad** usa esa temperatura; **08 Generar biomas** aplica el catálogo oficial. Cada botón requiere sus dependencias vigentes. Repetir FINAL elimina las tres capas ecológicas; repetir temperatura elimina lluvia/humedad/biomas; repetir precipitación elimina biomas. No se clasifican estados incompletos.
- **Tamaño** limpia toda la sesión y prepara una cuadrícula nueva. Editar **semilla** limpia inmediatamente capas, estados, trabajo y selección; pulsar **INICIAR / REINICIAR** para aplicar el texto válido. Texto incompleto o fuera de int32 no permite generar.
- El dado genera un int32 nuevo con aleatoriedad criptográfica de Game, borra automáticamente toda la sesión y arranca solo la cuadrícula. No hay confirmación ni ejecución completa implícita. El RNG determinista de Engine no cambia. Con el campo de semilla enfocado, **Ctrl+A** selecciona todo, **Ctrl+C/X** copia/corta la selección y **Ctrl+V** inserta en el cursor o sustituye la selección. Flechas y Home/End mueven el cursor; con Shift amplían la selección. Backspace/Delete eliminan hacia atrás/adelante o la selección. Los mismos atajos funcionan en creación de mundos. Clic fuera, cambio de pantalla o pérdida de foco de la ventana termina la edición; sin input enfocado no se interceptan estos atajos. El pegado solo recorta espacios exteriores; texto inválido se rechaza entero y el rango int32 se valida antes de generar, como antes.
- **Vista** ofrece únicamente capas vigentes: cuadrícula, placas, movimiento/H04, compresión, extensión, cizalla, superficie, BASE, sistemas, APORTE, FINAL, temperatura, precipitación, humedad y BIOMAS según las etapas completadas.
- Las alturas no requieren interruptor: BASE muestra BASE, APORTE muestra aporte y FINAL muestra FINAL; otras vistas utilizan FINAL vigente o BASE. Sin un campo disponible no se inventa un número. Cada número se mide con los glifos bitmap reales y solo aparece cuando cabe centrado, con margen de 2 px, dentro de su hexágono y del viewport. La letra se ajusta entre 1.25 y 1.5 px por píxel bitmap; si no alcanza el mínimo legible se oculta completamente. No hay números espaciados, muestras ni promedios a zoom lejano. El contraste blanco/negro sigue el fondo real de la capa. El inspector mantiene los valores exactos independientemente del zoom.
- En SUPERFICIE, océano azul; tierra con altura ≤90 conserva su color y tierra >90 es blanca. Se usa FINAL vigente, en su ausencia BASE, y sin alturas solo tierra/océano. La regla no cambia la paleta global del juego ni los guardados.
- Clic selecciona un hexágono; arrastrar mueve el mapa y rueda sobre él hace zoom. El inspector inferior izquierdo muestra identificador, coordenadas, BASE/APORTE/FINAL y datos de placas/esfuerzos/sistemas disponibles. Añade latitud, pendiente (niveles por paso hexagonal), temperatura (°C), lluvia (mm/año), índice de humedad 0–100, IDs de bioma/subbioma y estado de clasificación. No es humedad relativa atmosférica. Sin etapa vigente muestra pendiente/invalidado, no valores supuestos. BIOMAS usa la paleta existente por categoría; gris significa ausencia explícita de coincidencia. Rueda dentro del inspector para leer todos los detalles, con un tamaño de letra fijo; pendiente/invalidado y cero real se distinguen.
- **CANCELAR** solicita descarte cooperativo. Los bucles internos pueden terminar antes de observarlo; H04 en cola no comienza después de cancelar. **INICIAR / REINICIAR** reintenta una cuadrícula cancelada. **SALIR AL MENÚ** o Escape abandona la sesión temporal.

**Transición R00–R01:** el laboratorio actual consume el generador histórico de worldgenerationold, con APIs y namespaces conservados. La rama operativa es worldgeneration; el [roadmap único](../GEvolve.Engine/World/WorldGeneration/ROADMAP.md) rige el nuevo motor. Los resultados P03/P09 de esta guía son evidencia del legado y no aprueban R00–R01 ni el motor nuevo.

## Arquitectura

`PangaeaLaboratory` controla exclusivamente la experiencia de Game y utiliza `WorldGenerationService.CreatePangaeaSession`. La secuencia se comprueba en el controlador, además de bloquear los botones; no modifica Engine. Un cambio de configuración cancela/abandona el trabajo, borra datos y selección, y evita publicar terminaciones obsoletas. Una revisión local de sesión impide reutilizar recursos de otra semilla/tamaño aunque las revisiones internas vuelvan a comenzar.

`PangaeaLaboratoryLayout` comparte rectángulos de dibujo/hit testing, controles desplazables, inspector y salida fijos, y dropdowns contenidos en la columna. `PangaeaLaboratoryWindow` administra recursos GL en el hilo gráfico. Generación y copia de snapshots siguen fuera de él; no hay snapshots ni hashes por frame. Los datos parciales nunca se convierten en estados persistibles ficticios.

La cámara admite un origen de viewport explícito para ocupar el área real del mapa. El mismo renderer añade ese origen a la proyección y overlays. El `Fit` tradicional conserva origen X=0, Y=58 y dimensiones previas; el laboratorio utiliza X=ancho del panel, Y=0. No cambian hexágonos, topología, colores geográficos ni fórmulas.

`PangaeaMapData.Height` lee campos disponibles sin alterarlos; `PangaeaHeightLabels` genera anotaciones de valores existentes, solo cuando caben con tamaño legible, sin muestreo. No calcula elevación ni crea otra geografía.

## Validación técnica

### Comparador BASE P03-A.8.2c

Tras completar 03 BASE aparece **COMPARADOR BASE - EXPERIMENTAL**, dentro de la columna desplazable: V0 — Oficial, V1 — Sin presión, V4O — Regional y V4U — Regional moderada. Seleccionar una opción lleva a ALTURAS BASE. Todas usan la misma escala absoluta 0–100, océano azul y las mismas condiciones de legibilidad de etiquetas. El inspector diferencia BASE OFICIAL MOTOR y BASE VISIBLE; las variantes no alimentan 04/05 ni las etapas ambientales.

V0 es el valor inicial. El primer cálculo de cada alternativa ocurre en un worker sobre un snapshot separado; mientras termina, se conserva el mapa anterior y se indica CALCULANDO. Los siguientes cambios usan tres arrays de bytes como máximo en caché. Elegir otra vista muestra exclusivamente datos oficiales. Cambiar semilla/tamaño, reiniciar o repetir superficie/BASE elimina las variantes y restaura V0. Salir descarta toda la sesión. Cámara y selección se conservan al alternar variantes; el renderer actualiza sus datos sin reconstruir ni ajustar el viewport.

El módulo optativo `Engine.World.Experimental.PangaeaBaseComparison` reutiliza Shape/Recover oficiales mediante acceso interno sin cambiar sus cuerpos. Solo calcula alturas desde fuentes previamente capturadas; no ejecuta el generador ni accede a persistencia. Las implementaciones anteriores de Tests permanecen como referencias independientes y se contrastan con hashes publicados en P03-A.8.2b. Game no depende de Tests ni utiliza reflexión. La captura de fuentes se activa únicamente para las sesiones del laboratorio; el automático no cambia.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\GEvolve.Tests\ValidationReports\commands.ps1 -ValidationSet BaseVisualComparator -Label P03-A82c -IsolatedArtifacts
```

Incluye comparación exacta en 16 mundos/cuatro tamaños, controles de presentación sin GL, equivalencia 28+12, laboratorio y suite general. `BaseComparatorControls` limita la ejecución a build+comparador. No ejecuta Evolve gráficamente. **Comparador implementado, evaluación visual y selección pendientes.** No adopta ninguna variante ni inicia P03-A.8.3.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\GEvolve.Tests\ValidationReports\commands.ps1 -ValidationSet PangaeaEcology -Label P09-1 -IsolatedArtifacts
```

Incluye build, `--pangaea-ecology`, `--pangaea-laboratory`, `--pangaea-staged` y `--all-suites`. Las pruebas del laboratorio verifican secuencia, reinicios inmediatos, descartes asíncronos, campos numéricos, límites del layout, coordenadas de cámara y equivalencia automática en cuatro tamaños. La referencia por etapas cubre 28 Pangea y 12 controles. Los baselines históricos no se actualizan.

`-IsolatedArtifacts` coloca salidas de build y run en una carpeta por ejecución dentro de `GEvolve.Tests/bin/ValidationArtifacts`, ya ignorada por Git. Permite validar sin reemplazar DLLs que tenga abiertas el juego del usuario. No inicia, inspecciona ni cierra ese proceso.

La apariencia, fluidez, portapapeles, dado y lectura real de etiquetas/scroll quedan pendientes de revisión manual. Los estados parciales no son partidas guardables. P03-A permanece sin aprobación; P03-A.7.3, P03-A.7.4 y P03-B no se inician con este rediseño.

P09.1 integra los componentes climáticos y el catálogo existentes sin calibrarlos ni modificar geología. La distribución de biomas queda pendiente de calibración y aprobación visual. No genera flora, fauna ni minerales. Véase [auditoría y arquitectura de integración](../GEvolve.Engine/World/worldgenerationold/Docs/PANGAEA_ECOLOGY_INTEGRATION.md).
