export const RADIUS = 10;
export const ROOT3 = Math.sqrt(3);

export function center(column, row, radius = RADIUS) {
  return {
    x: radius * (1 + 1.5 * column),
    y: ROOT3 * radius * (row + 0.5 * (column & 1) + 0.5)
  };
}

export function periodWidth(columns, radius = RADIUS) {
  return 1.5 * columns * radius;
}

export function mapHeight(rows, radius = RADIUS) {
  return ROOT3 * (rows + 0.5) * radius;
}

export function cellAt(x, y, columns, rows, radius = RADIUS) {
  const q = (x / radius - 1) / 1.5;
  const r = y / (ROOT3 * radius) - 0.5 - q / 2;
  const s = -q - r;
  let cq = Math.round(q), cr = Math.round(r), cs = Math.round(s);
  const dq = Math.abs(cq - q), dr = Math.abs(cr - r), ds = Math.abs(cs - s);
  if (dq > dr && dq > ds) cq = -cr - cs;
  else if (dr > ds) cr = -cq - cs;
  const row = cr + Math.floor(cq / 2);
  if (row < 0 || row >= rows) return null;
  return { column: ((cq % columns) + columns) % columns, row };
}
