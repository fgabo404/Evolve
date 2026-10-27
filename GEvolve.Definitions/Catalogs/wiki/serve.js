"use strict";
// Local documentation server using only Node.js built-ins. Run: node wiki/serve.js
const http = require("node:http");
const fs = require("node:fs");
const path = require("node:path");

const root = path.resolve(__dirname, "..");
const port = Number(process.env.PORT || 8765);
const types = { ".html": "text/html", ".css": "text/css", ".js": "text/javascript", ".json": "application/json" };
http.createServer((request, response) => {
  if (request.method !== "GET" && request.method !== "HEAD") { response.writeHead(405).end(); return; }
  let pathname;
  try { pathname = decodeURIComponent(new URL(request.url, "http://localhost").pathname); }
  catch { response.writeHead(400).end(); return; }
  const requested = path.resolve(root, "." + pathname);
  if (requested !== root && !requested.startsWith(root + path.sep)) { response.writeHead(403).end(); return; }
  const target = fs.existsSync(requested) && fs.statSync(requested).isDirectory()
    ? path.join(requested, "index.html") : requested;
  fs.readFile(target, (error, body) => {
    if (error) { response.writeHead(404).end(); return; }
    response.writeHead(200, { "Content-Type": `${types[path.extname(target)] || "text/plain"}; charset=utf-8`, "Cache-Control": "no-store" });
    response.end(request.method === "HEAD" ? undefined : body);
  });
}).listen(port, "127.0.0.1", () => console.log(`Evolve Wiki: http://127.0.0.1:${port}/wiki/`));
