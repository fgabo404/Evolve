using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using GEvolve.Engine;

namespace GEvolve.Simulator.Map;

/// <summary>Standalone strategic 2D hex map; no GPU or server required.</summary>
public static class StrategicMap2D
{
    public static void Export(WorldMap world, string path, long turn)
    {
        const double radius = 12;
        double dx = Math.Sqrt(3) * radius;
        double dy = 1.5 * radius;
        var width = (world.Configuracion.Ancho + .5) * dx + 30;
        var height = (world.Configuracion.Alto - 1) * dy + 2 * radius + 30;
        var svg = new StringBuilder();
        foreach (var cell in world.Celdas)
        {
            double x = 15 + dx * (cell.Columna + (cell.Fila % 2 == 1 ? .5 : 0)) + dx / 2;
            double y = 15 + radius + cell.Fila * dy;
            var points = new StringBuilder();
            for (int i = 0; i < 6; i++)
            {
                double angle = (60 * i - 30) * Math.PI / 180;
                if (i > 0) points.Append(' ');
                points.Append(F(x + radius * Math.Cos(angle))).Append(',').Append(F(y + radius * Math.Sin(angle)));
            }
            string color = cell.Superficie == TipoSuperficie.Oceano ? "#245e91" :
                cell.Altura >= 80 ? "#a7a7a5" : cell.Altura >= 60 ? "#8a8066" : "#78a86c";
            string info = $"Hex ({cell.Columna}, {cell.Fila}) | {cell.Bioma} | {cell.Relieve} | Elevation {cell.Altura}";
            svg.Append("<polygon points=\"").Append(points).Append("\" fill=\"").Append(color)
                .Append("\" stroke=\"#17293a\" stroke-width=\"0.7\" data-info=\"")
                .Append(WebUtility.HtmlEncode(info)).Append("\"/>\n");
        }
        string page = $$"""
<!doctype html>
<html lang="es"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1">
<title>Evolve — Mapa estratégico 2D</title>
<style>
body{margin:0;background:#101820;color:#e5edec;font:14px system-ui}
header{padding:12px 18px;display:flex;gap:24px;align-items:center;background:#1b2934}
#viewport{height:calc(100vh - 72px);overflow:hidden;cursor:grab}#viewport:active{cursor:grabbing}
svg{width:100%;height:100%;touch-action:none}polygon:hover{stroke:#fff;stroke-width:2}
small{color:#9fb5c4}
</style></head><body>
<header><strong>Evolve — Vista estratégica 2D</strong><span>Turno {{turn}}</span><small>Rueda: zoom · arrastrar: mover · clic: inspeccionar</small><span id="info">Selecciona un hexágono</span></header>
<div id="viewport"><svg id="map" viewBox="0 0 {{F(width)}} {{F(height)}}" xmlns="http://www.w3.org/2000/svg">
<g id="world">{{svg}}</g></svg></div>
<script>
const map=document.getElementById('map'), info=document.getElementById('info');
const bounds={x:0,y:0,w:{{F(width)}},h:{{F(height)}}}; let dragging=false, last=null;
function render(){map.setAttribute('viewBox',[bounds.x,bounds.y,bounds.w,bounds.h].join(' '))}
map.addEventListener('click',e=>{if(e.target.dataset.info) info.textContent=e.target.dataset.info});
map.addEventListener('wheel',e=>{e.preventDefault();let factor=e.deltaY>0?1.12:.89;
let rect=map.getBoundingClientRect(),px=(e.clientX-rect.left)/rect.width,py=(e.clientY-rect.top)/rect.height;
bounds.x+=bounds.w*px*(1-factor);bounds.y+=bounds.h*py*(1-factor);
bounds.w*=factor;bounds.h*=factor;render()},{passive:false});
map.addEventListener('pointerdown',e=>{dragging=true;last={x:e.clientX,y:e.clientY};map.setPointerCapture(e.pointerId)});
map.addEventListener('pointerup',()=>dragging=false);
map.addEventListener('pointermove',e=>{if(!dragging)return;let r=map.getBoundingClientRect();
bounds.x-=(e.clientX-last.x)*bounds.w/r.width;bounds.y-=(e.clientY-last.y)*bounds.h/r.height;
last={x:e.clientX,y:e.clientY};render()});
</script></body></html>
""";
        string full = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full, page, Encoding.UTF8);
    }
    static string F(double x) => x.ToString("0.###", CultureInfo.InvariantCulture);
}
