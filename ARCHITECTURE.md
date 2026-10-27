# Evolve — Arquitectura

Definitions → Engine ↔ State → Game (vistas 2D y 3D).

El juego usará un motor gráfico compartido OpenGL. GEvolve.Simulator y el prototipo gráfico 3D previo ya no forman parte del diseño.

El grid flat-top sigue definido en Definitions, Engine y State. Game inicia con una ventana blanca y no genera mundo.

Comandos:
- `dotnet build GEvolve.sln`
- `dotnet run --project GEvolve.Tests`
- `dotnet run --project GEvolve.Game`
