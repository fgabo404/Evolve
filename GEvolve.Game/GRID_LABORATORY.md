# R02 — Laboratorio de cuadrícula

El menú ofrece **LABORATORIO DE CUADRÍCULA**, **CREAR MUNDO - NO DISPONIBLE** y salir. Crear mundo muestra una indicación explícita; no genera ni carga geografía con el motor anterior.

El laboratorio muestra solo la cuadrícula neutral de Engine/Grid, un campo int32 de semilla, semilla aleatoria y los cuatro tamaños oficiales. La semilla es metadato temporal: una cuadrícula vacía no depende de ella. No existen superficie, altura, océano, placas, montañas, clima, biomas, comparador ni etapas disponibles. No se persiste una partida parcial ni se modifica un guardado histórico.

- Rueda sobre el mapa: zoom con ancla en el cursor.
- Arrastrar con botón izquierdo sobre el mapa: desplazamiento. Se conservan wrap E/O y límites N/S de la cámara compartida.
- Semilla: edición y Ctrl+A/C/X/V mediante TextFieldEditor existente. Se admiten negativos y límites int32; entradas inválidas se rechazan y un texto parcial no produce datos geográficos.
- Cambiar tamaño reconstruye únicamente HexGrid y ajusta la cámara; cambiar semilla conserva geometría y navegación.
- Salir al menú o Escape descarta la sesión; al reentrar se usan Small/2026.

**Arquitectura:** GameSession controla navegación, GridLaboratory conserva solo GridSize/semilla/HexGrid y el renderer recibe exclusivamente HexGrid. No se define un snapshot geográfico ni el contrato de R03. El directorio Legacy queda fuera de Compile, sin flags que lo reactiven; las funciones gráficas y de edición genéricas se conservan. [Roadmap único](../GEvolve.Engine/World/WorldGeneration/ROADMAP.md).

Validación local, exclusivamente por el usuario:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\GEvolve.Tests\ValidationReports\commands.ps1 -ValidationSet GridLaboratory -Label R02 -IsolatedArtifacts
dotnet run --project GEvolve.Game
```

El script solo compila y ejecuta las suites nuevas sin ventana; el segundo comando abre el juego para revisión manual y cierre normal. Revisar menú/no disponibilidad de Crear mundo, cuatro tamaños, límites de semilla, pegado, navegación, salida/reentrada y ausencia de controles antiguos. No usar --graphics-smoke ni pruebas visuales automatizadas: esas rutas ya no existen.

Implementado y revisado estáticamente; compilación, pruebas, apariencia y fluidez no evaluadas por Codex. R02 pendiente de auditoría y aprobación.
