"use strict";

// The diagram is a read-only projection of prerequisites, never a research evaluator.
globalThis.EvolveTechnologyRoadmap = function createTechnologyRoadmap(catalog) {
  const technologies = catalog.technologies || [];
  const byId = new Map(technologies.map(value => [value.id, value]));
  if (byId.size !== technologies.length) throw Error("Duplicate technology IDs");
  const nodes = new Map(technologies.map(value => [value.id, { id: value.id, kind: "technology", value }]));
  const edges = [];
  const dependencies = new Map(technologies.map(value => [value.id, new Set()]));
  const dependents = new Map(technologies.map(value => [value.id, new Set()]));
  const declaredExternal = new Set(catalog.externalReferences?.technologyIds || []);

  function connect(from, to, owner, scope = null) {
    if (!edges.some(edge => edge.from === from && edge.to === to && edge.owner === owner))
      edges.push({ from, to, owner, scope });
  }
  function expression(value, owner, path) {
    if (!value || typeof value !== "object") return null;
    const operator = Object.hasOwn(value, "all") ? "all" : Object.hasOwn(value, "any") ? "any" : null;
    if (operator) {
      if (!Array.isArray(value[operator]) || operator === "any" && value[operator].length === 0)
        throw Error(`Invalid ${operator} prerequisite for ${owner}`);
      const inputs = value[operator]
        .map((child, index) => expression(child, owner, `${path}.${operator}.${index}`)).filter(Boolean);
      if (!inputs.length) return null;
      if (inputs.length === 1) return inputs[0];
      const id = `gate:${owner}:${path}`;
      nodes.set(id, { id, kind: "gate", operator, owner });
      for (const input of inputs) connect(input.id, id, owner, input.scope);
      return { id };
    }
    if (value.type && value.type !== "technology_completed") {
      const id = `condition:${owner}:${path}`;
      nodes.set(id, { id, kind: "condition", value, owner });
      return { id };
    }
    if (value.type !== "technology_completed" || typeof value.technologyId !== "string") return null;
    const id = value.technologyId;
    if (!byId.has(id)) {
      if (!declaredExternal.has(id)) throw Error(`Undeclared technology prerequisite: ${id}`);
      if (!nodes.has(id)) nodes.set(id, { id, kind: "external" });
    }
    dependencies.get(owner).add(id);
    if (byId.has(id)) dependents.get(id).add(owner);
    return { id, scope: value.scope || null };
  }
  for (const technology of technologies) {
    const prerequisite = expression(technology.prerequisites, technology.id, "root");
    if (prerequisite) connect(prerequisite.id, technology.id, technology.id, prerequisite.scope);
  }

  const incoming = new Map([...nodes.keys()].map(id => [id, []]));
  const outgoing = new Map([...nodes.keys()].map(id => [id, []]));
  for (const edge of edges) { incoming.get(edge.to).push(edge); outgoing.get(edge.from).push(edge); }
  const degree = new Map([...nodes.keys()].map(id => [id, incoming.get(id).length]));
  const levels = new Map([...nodes.keys()].map(id => [id, 0]));
  const queue = [...nodes.keys()].filter(id => degree.get(id) === 0);
  let processed = 0;
  while (queue.length) {
    const id = queue.shift(); processed++;
    for (const edge of outgoing.get(id)) {
      levels.set(edge.to, Math.max(levels.get(edge.to), levels.get(id) + 1));
      degree.set(edge.to, degree.get(edge.to) - 1);
      if (degree.get(edge.to) === 0) queue.push(edge.to);
    }
  }
  if (processed !== nodes.size) throw Error("Technology prerequisite cycle");
  const columns = [];
  for (const [id, level] of levels) (columns[level] ||= []).push(id);
  const order = id => nodes.get(id).value?.order ?? Number.MAX_SAFE_INTEGER;
  for (const column of columns) column.sort((a, b) => order(a) - order(b) || a.localeCompare(b));
  // Barycentric passes reduce crossings while keeping every level in its DAG column.
  for (let pass = 0; pass < 4; pass++) {
    for (let level = 1; level < columns.length; level++) {
      const previous = new Map(columns[level - 1].map((id, index) => [id, index]));
      const score = id => {
        const parents = incoming.get(id).map(edge => previous.get(edge.from)).filter(index => index !== undefined);
        return parents.length ? parents.reduce((sum, index) => sum + index, 0) / parents.length : Number.MAX_SAFE_INTEGER;
      };
      columns[level].sort((a, b) => score(a) - score(b) || order(a) - order(b) || a.localeCompare(b));
    }
  }
  const maxRows = Math.max(...columns.map(column => column.length));
  const width = columns.length * 270 + 90;
  const height = Math.max(640, maxRows * 88 + 120);
  for (let level = 0; level < columns.length; level++) {
    const column = columns[level];
    for (let row = 0; row < column.length; row++) {
      const node = nodes.get(column[row]);
      node.x = 46 + level * 270;
      node.y = 48 + (row + (maxRows - column.length) / 2) * 88;
      node.level = level;
    }
  }
  return { nodes, edges, dependencies, dependents, incoming, outgoing, width, height, columns };
};
