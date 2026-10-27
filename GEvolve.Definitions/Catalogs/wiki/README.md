# Evolve Catalog Wiki · Wiki de catálogos

Standalone, **read-only** catalog explorer built with HTML, CSS and vanilla JavaScript. It fetches the actual JSON files from the adjacent `Animals/`, `Plants/`, `Minerals/`, `Resources/`, `Technologies/` and `World/` directories. No build tools, packages, database, copy of catalog data or in-game runtime are needed.

## Run / Ejecutar

From `Evolve/GEvolve.Definitions/Catalogs/`:

```powershell
python -m http.server 8765
```

Then open **http://localhost:8765/wiki/**.

With VS Code's Live Server extension, you can also serve the Catalogs directory and open `wiki/index.html` over HTTP. Opening `index.html` directly via `file://` usually blocks `fetch()` due to browser local-file security rules.

## Features / Funcionalidades

- Navigation across six catalog areas
- Spanish/English labels and catalog names
- Search by name, tag and ID; item counts and detailed entry view
- Animal resource links and acquisition-method tags
- Biological reference catalogs under secondary navigation (optional, not gameplay)
- Original complete JSON entry for details not yet represented in specialized UI
- Responsive layout and no external dependencies

This is a design and documentation tool. It does not load runtime-disabled catalogs into the Engine, calculate missing yields, approve recipes, or overwrite the original JSON. Data changes made to neighboring catalog files appear when the page is reloaded.
