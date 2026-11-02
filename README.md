# Evolve

Evolve es un videojuego de simulación y generación de mundos desarrollado en C# y .NET 10.

## Estructura

- `GEvolve.Definitions`: catálogos y configuraciones.
- `GEvolve.Engine`: lógica de simulación.
- `GEvolve.Game`: interfaz y gráficos.
- `GEvolve.State`: estado y persistencia.
- `GEvolve.Tests`: pruebas automatizadas.

## Compilación

```powershell
dotnet build GEvolve.sln
dotnet run --project GEvolve.Tests
```

## Versionamiento

**Una sola versión pública del juego:** se define en `Directory.Build.props`, mediante la propiedad `<Version>`. Actualmente es `0.0.1`. Todos los proyectos .NET comparten esta versión y el juego la muestra en pantalla.

Usamos el esquema **MAJOR.MINOR.PATCH** (versionamiento semántico): `0.x.y` corresponde a desarrollo inicial; las versiones estables utilizan incrementos mayores para cambios incompatibles, menores para nuevas funciones compatibles y de parche para correcciones compatibles.

**Git** conserva cada cambio. **Los tags de Git** identifican lanzamientos concretos: `v0.1.0`, `v0.2.0`, etc. No se crea un tag por cada commit ni se incrementa la versión con cada cambio. Un lanzamiento se realiza cuando el código está probado y listo para distribuirse.

Para preparar un lanzamiento:

1. Cambiar `<Version>` en `Directory.Build.props` a la versión elegida, por ejemplo `0.1.0`.
2. Ejecutar `dotnet build GEvolve.sln` y las pruebas correspondientes.
3. Registrar y subir el cambio a `master`.
4. Crear un tag anotado sobre ese commit y subirlo:

```powershell
git tag -a v0.1.0 -m "Evolve v0.1.0"
git push origin v0.1.0
```

Los números internos del **formato de guardado** y del **algoritmo generador de mundos** siguen existiendo por compatibilidad; no son versiones públicas ni se sincronizan con la versión del juego.
