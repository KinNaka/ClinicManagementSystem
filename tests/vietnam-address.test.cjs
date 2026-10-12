const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
const tick = () => new Promise(resolve => setImmediate(resolve));
function setup(saved = {}) {
    const element = () => ({ value: '', dataset: {}, handlers: {}, disabled: true,
        addEventListener(type, handler) { this.handlers[type] = handler; },
        dispatchEvent() {}, replaceChildren(...options) { this.options = options; },
    });
    const province = element(), ward = element(), status = element(), retry = element(), form = element();
    const pending = [];
    const root = { dataset: { provincesUrl: '/provinces', wardsUrl: '/wards', ...saved },
        querySelector(selector) { return selector.includes('ProvinceCode') ? province : selector.includes('WardCode') ? ward : selector.includes('status') ? status : retry; },
        closest() { return form; } };
    vm.runInNewContext(fs.readFileSync('ClinicManagementSystem.Web/wwwroot/js/vietnam-address.js', 'utf8'), {
        document: { querySelector: () => root, getElementById: () => null },
        window: { ClinicSearchPicker: { init() {} } }, Event: class {},
        Option: class { constructor(text, value) { this.text = text; this.value = value; } },
        fetch: url => new Promise((resolve, reject) => pending.push({ url, reject, complete(data) { resolve({ ok: true, headers: { get: () => 'application/json' }, json: async () => data }); } })),
    });
    return { province, ward, status, retry, form, pending };
}
test('catalog contains 34 provinces and 3321 wards with unique codes', () => {
    const data = JSON.parse(fs.readFileSync('ClinicManagementSystem.Web/Data/vietnam-addresses.json', 'utf8'));
    assert.equal(data.length, 34);
    const wards = data.flatMap(p => p.wards);
    assert.equal(wards.length, 3321);
    assert.equal(new Set(wards.map(w => w.code)).size, wards.length);
    assert.equal(data.find(p => p.code === 1).wards.find(w => w.code === 4).name, 'Phường Ba Đình');
    assert.ok(data.every(p => p.wards.length > 0));
});
test('editing restores province and ward; changing province clears ward and ignores stale responses', async () => {
    const f = setup({ province: '1', ward: '4' });
    f.pending.shift().complete([{code: 1, name: 'Hà Nội'}, {code: 79, name: 'Hồ Chí Minh'}]); await tick();
    assert.equal(f.province.value, '1');
    assert.match(f.pending[0].url, /provinceCode=1$/);
    f.pending.shift().complete([{code: 4, name: 'Ba Đình'}]); await tick();
    assert.equal(f.ward.value, '4'); assert.equal(f.ward.disabled, false);
    f.province.value = '79'; f.province.handlers.change();
    assert.equal(f.ward.value, ''); assert.equal(f.ward.disabled, true);
    const old = f.pending.shift();
    f.province.value = '1'; f.province.handlers.change();
    const latest = f.pending.shift();
    latest.complete([{code: 4, name: 'Ba Đình'}]); await tick();
    old.complete([{code: 8, name: 'Wrong province'}]); await tick();
    assert.equal(f.ward.options[1].value, '4');
    f.province.value = ''; f.province.handlers.change();
    assert.equal(f.ward.disabled, true); assert.equal(f.ward.value, '');
});
test('load failure prevents save and supports retry', async () => {
    const f = setup(); let blocked = false;
    f.pending.shift().reject(new Error('network')); await tick();
    f.form.handlers.submit({preventDefault() { blocked = true; }});
    assert.equal(blocked, true); assert.equal(f.retry.hidden, false);
    f.retry.handlers.click();
    f.pending.shift().complete([{code: 1, name: 'Hà Nội'}]); await tick();
    blocked = false; f.form.handlers.submit({preventDefault() { blocked = true; }});
    assert.equal(blocked, false);
});

