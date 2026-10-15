const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');

function setup(view, initial) {
    const state = { updated: [], renamed: 0, calculated: 0, focused: 0, synced: 0 };
    const row = (id = '', quantity = '1') => {
        const product = { value: id, dispatchEvent() {} };
        const input = { value: quantity };
        return { dataset: { sourceId: '42', sourcePrice: '12000' }, product, input,
            querySelector: selector => selector === '.product-select' ? product : input };
    };
    const entries = initial.map(([id, quantity]) => row(id, quantity));
    const rows = { querySelectorAll: () => entries, appendChild(entry) { entries.push(entry); }, get lastElementChild() { return entries.at(-1); } };
    const handlers = {};
    const search = { value: '', addEventListener(type, fn) { handlers[type] = fn; }, dispatchEvent() {}, focus() { state.focused++; } };
    const source = fs.readFileSync(`ClinicManagementSystem.Web/Views/Sales/${view}.cshtml`, 'utf8');
    const start = source.indexOf('const search = document.getElementById("productSearch");');
    const end = source.indexOf('rows.addEventListener("click"', start);
    assert.ok(start >= 0 && end > start);
    vm.runInNewContext(source.slice(start, end), {
        rows, error: {}, template: { content: { cloneNode: () => row() } },
        document: { getElementById: () => search }, Event: class {},
        window: { ClinicSearchPicker: { init() {} }, ClinicQuantity: { init() { state.synced++; } } },
        renumber() { state.renamed++; }, updateRow(entry) { state.updated.push(entry); }, calculate() { state.calculated++; }
    });
    return { state, entries, choose(id) { search.value = id; handlers.change(); assert.equal(search.value, ''); } };
}

for (const view of ['Create', 'Edit']) {
    test(`${view}: selecting an existing product increments quantity and retains saved pricing`, () => {
        const f = setup(view, [['7', '1']]);
        f.choose('7');
        assert.equal(f.entries.length, 1);
        assert.equal(f.entries[0].input.value, '2');
        assert.equal(f.entries[0].dataset.sourceId, '42');
        assert.equal(f.entries[0].dataset.sourcePrice, '12000');
        assert.equal(f.state.updated.length, 0);
        assert.equal(f.state.synced, 1);
        assert.equal(f.state.calculated, 1);
        assert.equal(f.state.focused, 1);
    });
    test(`${view}: a new product fills an empty row or appends one and loads its price`, () => {
        for (const initial of [[['', '1']], [['7', '2']]]) {
            const f = setup(view, initial);
            f.choose('9');
            const added = f.entries.at(-1);
            assert.equal(added.product.value, '9');
            assert.equal(added.input.value, '1');
            assert.equal(f.entries.length, initial[0][0] ? 2 : 1);
            assert.equal(f.state.renamed, 1);
            assert.equal(f.state.updated[0], added);
            assert.equal(f.state.focused, 1);
        }
    });
}
