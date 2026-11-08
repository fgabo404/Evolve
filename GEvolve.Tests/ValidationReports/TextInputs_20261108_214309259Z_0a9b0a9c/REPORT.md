# Evolve — Edición de campos y portapapeles

**Commit validado:** `49f24f350ce45498a0c076b1055cf7895b6036c2`, `fix(game): support clipboard and text editing shortcuts in inputs`. Rama `worldgeneration_pangea`; base `86a1ea86f0615cf85d27f29dc21fd9e90f9d2141`. Ejecución UTC 2026-11-08 21:43:09–21:43:48; .NET SDK 10.0.402. Fuente rastreada limpia al empezar. [SUMMARY.md](SUMMARY.md) conserva comandos exactos, streams completos, códigos y duraciones.

**Implementado y validado técnicamente; pendiente de verificación interactiva del usuario.** No se abrió Evolve, no se creó ventana/contexto GL, no se generaron capturas ni se usó el portapapeles nativo durante las pruebas.

## Auditoría y causa

- `EvolveWindow` en `GEvolve.Game/Program.cs` recibe teclado/texto de OpenTK 4.9.4 y dibuja controles existentes. El menú principal y Opciones no contienen campos editables; conducen a creación de mundo o laboratorio.
- Hay dos campos de texto editables: semilla en creación (`GameSession.SeedText`) y semilla del laboratorio (`PangaeaLaboratory.SeedText`), con sus propios setters/validación. El laboratorio comparte ventana mediante `PangaeaLaboratoryWindow.cs`. No había un componente común de cursor/selección.
- `OnTextInput` añadía caracteres al final. Backspace borraba únicamente el último carácter. No había selección, corte, Delete, flechas ni Home/End.
- Creación no tenía handler Ctrl+V. El laboratorio implementaba parcialmente Ctrl+V mediante sustitución completa, y Ctrl+C copiaba toda la semilla incluso sin input enfocado. Esa ruta consultaba `KeyboardState` dentro de OnKeyDown; ahora se usan los modificadores del propio evento.
- Se inspeccionó la documentación XML local de las dependencias instaladas: `KeyboardKeyEventArgs.Control/Shift/Alt/Command`, `NativeWindow.ClipboardString` y `OnFocusedChanged`. No se añadieron paquetes, acceso Win32 alternativo ni dependencias de escritorio.

## Corrección

`TextFieldEditor` concentra la edición del campo existente que tiene foco. Mantiene cursor/ancla y enlaza lectura/escritura con los propietarios actuales; no almacena otra semilla ni implementa nuevos inputs/widgets. Ambos formularios utilizan el mismo editor.

- Ctrl+A selecciona todo. Ctrl+C copia exclusivamente la selección; sin selección no altera el portapapeles. Ctrl+X copia y borra la selección.
- Ctrl+V inserta en cursor o sustituye selección. Conserva el recorte exterior de espacios del pegado previo del laboratorio; no filtra caracteres internos para fabricar otra semilla. Portapapeles vacío/nulo o solo espacios no altera el campo.
- Flechas mueven el cursor; con selección y sin Shift colapsan al extremo correspondiente. Home/End van a los extremos; Shift amplía la selección. Backspace/Delete eliminan selección o carácter anterior/siguiente. Los límites son seguros.
- Los caracteres imprimibles entran exclusivamente por `OnTextInput`; `OnKeyDown` ejecuta comandos. Los eventos de texto con modificadores de atajo activos y los caracteres de control se descartan. `OnKeyUp` actualiza esos modificadores.
- Solo un propietario está enfocado. Clic fuera, otra acción/pantalla o pérdida de foco de ventana termina la edición. Sin foco, los comandos retornan sin consumir el atajo ni consultar/escribir el portapapeles. Cambios externos del valor sincronizan cursor/selección.
- `NativeWindow.ClipboardString` sigue siendo el portapapeles real de la ventana. En las pruebas, los mismos callbacks se sustituyen por memoria para evitar abrir el juego o depender de una sesión interactiva.
- Cursor y selección se dibujan en los rectángulos de input existentes. Se corrige la pista de semilla a Ctrl+V. No se cambian posiciones, tamaños, columnas, etapas, vistas ni diseño del laboratorio.

## Validación de semillas e invalidación

`GameSession.SetSeedText`, `GameSession.BeginGeneration`, `PangaeaLaboratory.SetSeedText` y `RequestRestart` no se modifican. Conservan ASCII numérico, signo negativo inicial, longitud máxima y validación int32 antes de generar. Texto alfabético, signos interiores, caracteres no admitidos y pegados demasiado largos se rechazan de forma atómica, preservando valor y selección.

Los estados intermedios vacío o `-` siguen siendo editables; no generan un mundo. Los valores numéricos de longitud admisible pero fuera de int32 pueden permanecer visibles, como antes, y la generación/reinicio se bloquea con el mensaje existente. No se truncan, convierten a otra semilla ni provocan una excepción no controlada desde el evento de pegado.

Un pegado válido diferente llama al setter real del laboratorio: invalida geología/ecología, trabajos pendientes, mapa, selección e inspección. Pegar la misma semilla o texto rechazado conserva la sesión. Iniciar de nuevo con la semilla pegada prepara solo la cuadrícula y restablece el flujo secuencial inicial.

## Resultados técnicos

| Ejecución | Estado | Código | Duración s | Log completo |
|---|---|---:|---:|---|
| Build aislado | PASS; 0 advertencias, 0 errores | 0 | 12.417 | [build.log](build.log) |
| Suite existente Game, incluido TextFieldEditingTests | PASS | 0 | 12.224 | [game-inputs.log](game-inputs.log) |
| Laboratorio existente, cuatro tamaños | PASS | 0 | 14.579 | [pangaea-laboratory.log](pangaea-laboratory.log) |

La selección `--all-suites --suite=Game` ejecuta **únicamente Game**, con 1 suite PASS / 0 FAIL. No representa una ejecución de todas las suites generales. No se infiere un número de aserciones individuales. El código agregado del script fue 0. No se repitió la suite general: el cambio está acotado a inputs y los componentes de generación permanecen idénticos. No se reinterpreta ni aprueba el estado histórico de otras suites.

`INPUT_EDIT_PASS` verifica pegado positivo y negativo, extremos int32, selección y sustitución, copiar/cortar/pegar un substring interior, navegación y colapso de selección, Backspace/Delete, texto inválido, overflow en ambos formularios, foco entre propietarios, atajos sin foco, no duplicación de KeyDown/TextInput, sincronización externa y reinicio del laboratorio.

La prueba prepara una sesión Small/42 completa hasta Biomes y selecciona una celda. Pegar la misma semilla o texto rechazado conserva ese mapa. Pegar −731 elimina todas las etapas/resultados/inspección y reiniciar produce configuración −731 con cuadrícula sin placas/clima y solo la primera acción habilitada. Las pruebas del laboratorio existentes conservan además secuencia, reinicios asíncronos, equivalencia geológica, límites numéricos del layout y etiquetas legibles en cuatro tamaños. Son funciones de código, sin validación gráfica.

## Archivos modificados

Commit de implementación:

- `GEvolve.Game/TextFieldEditor.cs` — estado común de edición y comandos OpenTK.
- `GEvolve.Game/Program.cs` — foco, callbacks teclado/texto/portapapeles y selección/cursor de creación.
- `GEvolve.Game/PangaeaLaboratoryWindow.cs` — mismo editor y representación de selección/cursor en el campo existente.
- `GEvolve.Game/PANGAEA_LABORATORY.md` — instrucciones de edición actualizadas.
- `GEvolve.Tests/TextFieldEditingTests.cs` — regresiones vinculadas a campos reales y portapapeles en memoria.
- `GEvolve.Tests/GameTests.cs` — incorpora esas comprobaciones en la suite existente.
- `GEvolve.Tests/ValidationReports/commands.ps1` — conjunto reproducible TextInputs y extracto de evidencia.

El commit posterior de evidencia añade este directorio, actualiza `GEvolve.Tests/ValidationReports/README.md` y la memoria operativa. No hay modificaciones en Engine, Definitions, State, catálogos, clima, biomas, geografía, generadores, baselines/referencias ni `PangaeaLaboratoryLayout.cs`. El swap ajeno `.BiomeTests.cs.swp` se conserva sin tocar ni versionar.

## Repetir validación técnica

Desde la raíz del repositorio:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\GEvolve.Tests\ValidationReports\commands.ps1 -ValidationSet TextInputs -Label TextInputs -IsolatedArtifacts
```

Cada ejecución conserva su directorio UTC propio. La instantánea [commands.ps1](commands.ps1) permite repetir exactamente este conjunto con los mismos argumentos. Se revisan privacidad y archivos antes de publicar; no se versionan bin/obj, ejecutables ni artefactos temporales. Aplica la [política de conservación](../README.md).

## Verificación interactiva pendiente, exclusiva del usuario

Abrir normalmente Evolve y comprobar ambos campos: Ctrl+A, pegar un entero negativo, mover cursor con flechas/Home/End, seleccionar con Shift, copiar/cortar y sustituir con pegado. Verificar que se dibuja cursor/selección en el campo correcto y que Ctrl+C/V fuera del input no se consume. Comprobar pérdida y recuperación del foco de ventana.

En el laboratorio, generar una sesión, pegar una semilla diferente y confirmar que se retiran las capas y la inspección; reiniciar y comprobar el flujo inicial. Pegar texto alfabético y overflow, confirmando que no se genera una semilla incorrecta. Estas comprobaciones cubren entrega nativa del portapapeles/eventos y apariencia real, que no se certifican con pruebas headless. No se solicita aprobar distribución geográfica ni biomas.
