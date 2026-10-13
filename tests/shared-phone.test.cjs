const { test } = require('node:test');
const assert = require('node:assert/strict');
const vm = require('node:vm');
const fs = require('node:fs');
class Element {
    constructor() { this.value = ''; this.children = []; this.listeners = {}; this.dataset = {}; this.textContent = ''; }
    addEventListener(name, fn, options) { (this.listeners[name] ||= []).push({ fn, once: options?.once }); }
    async fire(name, event = {}) {
        const handlers = [...(this.listeners[name] || [])];
        this.listeners[name] = handlers.filter(h => !h.once);
        for (const handler of handlers) await handler.fn(event);
    }
    append(...children) { this.children.push(...children); }
    replaceChildren() { this.children = []; }
    querySelector() {
        function find(node) { if (node.checked) return node; for (const child of node.children || []) { const match = find(child); if (match) return match; } }
        return find(this);
    }
    focus() { this.focused = true; }
}
function setup(fetch) {
    const ids = {};
    for (const id of ['shared-phone-widget', 'PhoneNumber', 'SharedPhoneWithPatientId', 'SharedPhoneRelationship', 'shared-phone-status', 'shared-phone-matches', 'shared-phone-relation', 'shared-phone-modal', 'shared-phone-modal-error', 'check-shared-phone', 'change-shared-phone', 'confirm-shared-phone']) ids[id] = new Element();
    const form = new Element(); form.submits = 0; form.requestSubmit = () => form.submits++;
    ids['shared-phone-widget'].closest = () => form;
    ids['shared-phone-widget'].dataset.url = '/Patients/PhoneMatches';
    const modal = { shown: 0, show() { this.shown++; }, hide() { ids['shared-phone-modal'].fire('hidden.bs.modal'); } };
    vm.runInNewContext(fs.readFileSync('ClinicManagementSystem.Web/wwwroot/js/shared-phone.js', 'utf8'), {
        document: { getElementById: id => ids[id], createElement: () => new Element(), createTextNode: text => ({ textContent: text }) },
        bootstrap: { Modal: { getOrCreateInstance: () => modal } }, location: { origin: 'http://local' }, URL,
        fetch, setTimeout: () => 1, clearTimeout() {}
    });
    return { ids, form, modal };
}
const patient = id => ({ id, fullName: 'Owner ' + id, patientCode: 'KH' + id, phoneNumber: '0900111222', status: 'Active', detailsUrl: '/Patients/Details/' + id });
const response = rows => ({ ok: true, json: async () => rows });
const settle = () => new Promise(resolve => setImmediate(resolve));
test('duplicate popup confirms relative and invalidates confirmation when phone changes', async () => {
    const { ids, form, modal } = setup(async () => response([patient(7)]));
    ids.PhoneNumber.value = '0900111222';
    await ids['check-shared-phone'].fire('click'); await settle();
    assert.equal(modal.shown, 1);
    assert.equal(ids['shared-phone-matches'].querySelector().value, '7');
    await ids['confirm-shared-phone'].fire('click');
    assert.match(ids['shared-phone-modal-error'].textContent, /quan hệ/);
    ids['shared-phone-relation'].value = 'Con'; await ids['confirm-shared-phone'].fire('click');
    assert.equal(ids.SharedPhoneWithPatientId.value, '7'); assert.equal(ids.SharedPhoneRelationship.value, 'Con');
    assert.equal(form.submits, 0);
    ids.PhoneNumber.value = '0911222333'; await ids.PhoneNumber.fire('input');
    assert.equal(ids.SharedPhoneWithPatientId.value, ''); assert.equal(ids.SharedPhoneRelationship.value, '');
});
test('submit waits for relative confirmation and then resumes exactly once', async () => {
    const { ids, form } = setup(async () => response([patient(7)]));
    ids.PhoneNumber.value = '0900111222'; let prevented = false;
    await form.fire('submit', { preventDefault() { prevented = true; } });
    assert.equal(prevented, true); assert.equal(form.submits, 0);
    ids['shared-phone-relation'].value = 'Mẹ'; await ids['confirm-shared-phone'].fire('click'); await settle();
    assert.equal(form.submits, 1);
});
test('stale lookup cannot replace results for a newer phone', async () => {
    let release;
    const first = new Promise(resolve => release = resolve);
    let calls = 0;
    const { ids, modal } = setup(async () => ++calls === 1 ? first : response([patient(8)]));
    ids.PhoneNumber.value = '0900111222'; await ids['check-shared-phone'].fire('click');
    ids.PhoneNumber.value = '0911222333'; await ids.PhoneNumber.fire('input');
    await ids['check-shared-phone'].fire('click'); await settle();
    release(response([patient(7)])); await settle();
    assert.equal(ids['shared-phone-matches'].querySelector().value, '8'); assert.equal(modal.shown, 1);
});
test('failed phone lookup blocks unchecked submit and gives a retry message', async () => {
    const { ids, form } = setup(async () => { throw Error('Offline'); });
    ids.PhoneNumber.value = '0900111222'; let prevented = false;
    await form.fire('submit', { preventDefault() { prevented = true; } });
    assert.equal(prevented, true); assert.equal(form.submits, 0); assert.match(ids['shared-phone-status'].textContent, /Không kiểm tra/);
});
