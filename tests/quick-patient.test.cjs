const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');

test('quick creation preloads search and only accepts its own frame, then selects without duplicate options', () => {
    const handlers = {}; const events = [];
    const select = { options: [], addEventListener(type, fn) { handlers[type] = fn; }, add(option) { this.options.push(option); }, dispatchEvent(event) { events.push(event.type); } };
    const frame = { contentWindow: {} };
    const element = { dataset: { url: '/Patients/QuickCreate' } };
    let shown = 0; let hidden = 0; let message;
    const context = {
        document: { querySelector(selector) { assert.equal(selector, 'select[name="PatientId"]'); return select; }, getElementById(id) { return id === 'quick-patient-frame' ? frame : element; } },
        bootstrap: { Modal: { getOrCreateInstance() { return { show() { shown++; }, hide() { hidden++; } }; } } },
        location: { origin: 'http://localhost:5000' },
        window: { addEventListener(type, fn) { assert.equal(type, 'message'); message = fn; } },
        URL, Option: class { constructor(text, value) { this.text = text; this.value = value; } },
        Event: class { constructor(type) { this.type = type; } }
    };
    vm.runInNewContext(fs.readFileSync('ClinicManagementSystem.Web/wwwroot/js/quick-patient.js', 'utf8'), context);
    handlers['picker:empty-action']({ detail: { searchTerm: 'Nguyễn Mới' } });
    assert.equal(shown, 1);
    assert.equal(new URL(frame.src).searchParams.get('searchTerm'), 'Nguyễn Mới');
    const valid = { origin: context.location.origin, source: frame.contentWindow, data: { type: 'clinic:patient-created', patient: { id: 42, text: 'Nguyễn Mới - 2003 - 0900000000' } } };
    message({ ...valid, origin: 'http://other-host' });
    message({ ...valid, source: {} });
    message({ ...valid, data: { type: 'clinic:patient-created', patient: { id: -1, text: 'invalid' } } });
    assert.equal(select.options.length, 0);
    message(valid);
    assert.equal(select.value, '42');
    assert.equal(select.options[0].text, valid.data.patient.text);
    assert.deepEqual(events, ['picker:refresh', 'change']);
    assert.equal(hidden, 1);
    message(valid);
    assert.equal(select.options.length, 1);
});
