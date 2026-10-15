const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');

test('mobile cards retain column labels after adding rows and replacing filtered results', () => {
    const cell = (colSpan = 1) => ({ dataset: {}, colSpan });
    const row = () => ({ cells: [cell(), cell(), cell()] });
    let rows = [row(), { cells: [cell(3)] }];
    const table = {
        querySelectorAll(selector) {
            return selector === 'thead th' ? ['Sản phẩm', 'Số lượng', ''].map(textContent => ({ textContent })) : rows;
        }
    };
    let updated;
    vm.runInNewContext(fs.readFileSync('ClinicManagementSystem.Web/wwwroot/js/mobile-layout.js', 'utf8'), {
        document: { body: {}, querySelectorAll(selector) { assert.equal(selector, 'table[data-mobile-cards]'); return [table]; } },
        MutationObserver: class {
            constructor(callback) { updated = callback; }
            observe(target, options) { assert.equal(options.childList, true); assert.equal(options.subtree, true); }
        }
    });
    const check = entry => assert.deepEqual(entry.cells.map(c => c.dataset.label), ['Sản phẩm', 'Số lượng', 'Thao tác']);
    check(rows[0]);
    assert.equal(rows[1].cells[0].dataset.label, undefined);
    rows.push(row()); updated(); check(rows[2]);
    rows = [row()]; updated(); check(rows[0]);
});
