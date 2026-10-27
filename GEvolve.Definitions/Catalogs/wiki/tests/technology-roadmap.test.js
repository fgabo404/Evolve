"use strict";
const assert = require("node:assert/strict");
const fs = require("node:fs");
const path = require("node:path");
const vm = require("node:vm");

const wiki = path.resolve(__dirname, "..");
const catalog = JSON.parse(fs.readFileSync(path.resolve(wiki, "../Technologies/agriculture.technologies.json"), "utf8"));
const context = vm.createContext({});
vm.runInContext(fs.readFileSync(path.join(wiki, "technology-roadmap.js"), "utf8"), context);
const build = context.EvolveTechnologyRoadmap;
const graph = build(catalog);
const nodes = [...graph.nodes.values()];
assert.equal(nodes.filter(node => node.kind === "technology").length, catalog.technologies.length);
assert.equal(catalog.technologies.length, 23);
assert.equal(nodes.filter(node => node.kind === "external").length, 9);
assert(nodes.some(node => node.kind === "gate" && node.operator === "any" && node.owner === "gathering_containers"));
assert(nodes.some(node => node.kind === "gate" && node.operator === "all" && node.owner === "species_cultivation"));

function refs(value) {
  if (!value || typeof value !== "object") return [];
  if (Array.isArray(value)) return value.flatMap(refs);
  if (value.type === "technology_completed") return [value.technologyId];
  return Object.values(value).flatMap(refs);
}
for (const technology of catalog.technologies) {
  assert.deepEqual([...graph.dependencies.get(technology.id)].sort(), [...new Set(refs(technology.prerequisites))].sort());
  for (const prerequisite of graph.dependencies.get(technology.id)) {
    const reachable = new Set([prerequisite]);
    const queue = [prerequisite];
    while (queue.length) {
      const source = queue.shift();
      for (const edge of graph.outgoing.get(source)) {
        if (edge.owner !== technology.id || reachable.has(edge.to)) continue;
        reachable.add(edge.to); queue.push(edge.to);
      }
    }
    assert(reachable.has(technology.id), `${prerequisite} must connect to ${technology.id}`);
  }
}
for (const edge of graph.edges) assert(graph.nodes.get(edge.from).level < graph.nodes.get(edge.to).level);
for (let i = 0; i < nodes.length; i++) for (let j = i + 1; j < nodes.length; j++) {
  const a = nodes[i], b = nodes[j];
  const widthA = a.kind === "gate" ? 74 : 232, widthB = b.kind === "gate" ? 74 : 232;
  assert(a.x + widthA <= b.x || b.x + widthB <= a.x || a.y + 48 <= b.y || b.y + 48 <= a.y,
    `overlap: ${a.id} and ${b.id}`);
}
assert(!graph.dependencies.get("plant_knowledge").has("gathering_tools"), "display order must not create edges");
const extended = build({ ...catalog, technologies: [...catalog.technologies, {
  id: "future_candidate", order: 24,
  prerequisites: { all: [{ type: "technology_completed", technologyId: "grafting", scope: "current_species" }] }
}] });
assert.equal([...extended.nodes.values()].filter(node => node.kind === "technology").length, 24);
assert(extended.dependencies.get("future_candidate").has("grafting"));
assert(extended.nodes.get("future_candidate").level > extended.nodes.get("grafting").level);

const synthetic = build({ externalReferences: { technologyIds: ["external_a", "external_b"] }, technologies: [
  { id: "first", order: 2, prerequisites: { all: [] } },
  { id: "unrelated", order: 3, prerequisites: { all: [] } },
  { id: "nested", order: 1, prerequisites: { all: [
    { type: "technology_completed", technologyId: "first" },
    { any: [
      { type: "technology_completed", technologyId: "external_a" },
      { type: "technology_completed", technologyId: "external_b" },
      { type: "site_feature_available", featureId: "irrigable_site" }
    ] }
  ] } }
] });
assert.deepEqual([...synthetic.dependencies.get("nested")].sort(), ["external_a", "external_b", "first"]);
assert.equal(synthetic.dependencies.get("unrelated").size, 0);
assert.equal([...synthetic.nodes.values()].filter(node => node.kind === "technology").length, 3);
assert.equal([...synthetic.nodes.values()].filter(node => node.kind === "gate").length, 2);
assert.equal([...synthetic.nodes.values()].filter(node => node.kind === "condition").length, 1);
assert.throws(() => build({ externalReferences: { technologyIds: [] }, technologies: [
  { id: "a", prerequisites: { all: [{ type: "technology_completed", technologyId: "missing" }] } }
] }), /Undeclared/);
console.log(`Roadmap: ${catalog.technologies.length} technologies, 9 external references, ${graph.edges.length} prerequisite segments; nested logic and layout verified.`);
