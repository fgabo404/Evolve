import assert from 'node:assert/strict';
import { RADIUS, ROOT3, center, cellAt, periodWidth } from '../wwwroot/grid-geometry.js';

for (const [columns, rows] of [[100, 70], [150, 100], [250, 200]]) {
  const period = periodWidth(columns);
  for (let row = 0; row < rows; row++) {
    for (let column = 0; column < columns; column++) {
      const point = center(column, row);
      assert.deepEqual(cellAt(point.x, point.y, columns, rows), { column, row });
      if (column === 0 || column === columns - 1) {
        assert.deepEqual(cellAt(point.x + period, point.y, columns, rows), { column, row });
        assert.deepEqual(cellAt(point.x - period, point.y, columns, rows), { column, row });
      }
    }
  }
}

const even = center(0, 0), odd = center(1, 0);
assert.equal(odd.x - even.x, 1.5 * RADIUS);
assert.equal(odd.y - even.y, ROOT3 * RADIUS / 2);
assert.equal(cellAt(even.x, -20, 100, 70), null);
console.log('PASS: flat-top centers, selection and repeated horizontal view');
