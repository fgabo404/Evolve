"use strict";
const assert = require("node:assert/strict");
const fs = require("node:fs");
const path = require("node:path");
const vm = require("node:vm");

const wiki = path.resolve(__dirname, "..");
const catalog = JSON.parse(fs.readFileSync(path.resolve(wiki, "../Technologies/agriculture.tech.json"), "utf8"));
const context = vm.createContext({});
vm.runInContext(fs.readFileSync(path.join(wiki, "technology-roadmap.js"), "utf8"), context);
const build = context.EvolveTechnologyRoadmap;
const graph = build(catalog);
const nodes = [...graph.nodes.values()];
assert.equal(nodes.filter(node => node.kind === "technology").length, catalog.technologies.length);
assert.equal(catalog.technologies.length, 1);
assert.equal(catalog.rootTechnologyId, "language");
assert.equal(catalog.technologies[0].order, 0);
assert.equal(catalog.technologies[0].name.es, "Lenguaje");
assert.equal(nodes.filter(node => node.kind === "external").length, 0);
assert.deepEqual(catalog.branches.map(branch => branch.name.es), ["Agricultura", "Domesticación", "Otras tecnologías"]);
assert.equal(nodes.filter(node => node.kind === "branch").length, 3);
assert.equal(graph.edges.length, 3);
assert(graph.edges.every(edge => edge.kind === "branch" && edge.from === "language"));
const branchNodes = catalog.branches.map(branch => graph.nodes.get(`branch:${branch.id}`));
assert(branchNodes.every(node => node.x >= graph.nodes.get("language").x + 300));
assert(branchNodes[1].y - branchNodes[0].y >= 160);
assert(branchNodes[2].y - branchNodes[1].y >= 160);
assert(graph.nodes.get("language").y > branchNodes[0].y && graph.nodes.get("language").y < branchNodes[2].y);

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
  const widthA = a.kind === "gate" ? 74 : a.kind === "branch" ? 240 : 232;
  const widthB = b.kind === "gate" ? 74 : b.kind === "branch" ? 240 : 232;
  const heightA = a.kind === "branch" ? 66 : 48, heightB = b.kind === "branch" ? 66 : 48;
  assert(a.x + widthA <= b.x || b.x + widthB <= a.x || a.y + heightA <= b.y || b.y + heightB <= a.y,
    `overlap: ${a.id} and ${b.id}`);
}
assert.equal(graph.dependencies.get("language").size, 0);
const extended = build({ ...catalog, technologies: [...catalog.technologies, {
  id: "future_candidate", order: 1,
  prerequisites: { all: [{ type: "technology_completed", technologyId: "language" }] }
}] });
assert.equal([...extended.nodes.values()].filter(node => node.kind === "technology").length, 2);
assert(extended.dependencies.get("future_candidate").has("language"));
assert(extended.nodes.get("future_candidate").level > extended.nodes.get("language").level);
assert.throws(() => build({ ...catalog, technologies: [...catalog.technologies, {
  id: "disconnected", order: 1, prerequisites: { all: [] }
}] }), /does not descend/);

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
console.log(`Roadmap: ${catalog.technologies.length} initial technology, ${catalog.branches.length} planned branches, ${graph.edges.length} visual branch connections; root rule and nested logic verified.`);
