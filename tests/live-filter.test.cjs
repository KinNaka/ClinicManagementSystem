const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
const source = fs.readFileSync('ClinicManagementSystem.Web/wwwroot/js/site.js', 'utf8');

function fixture() {
    const handlers = {};
    const resetHandlers = {};
    const pending = [];
    const timers = new Map();
    let nextTimer = 0;
    const fields = [{ name: 'searchTerm', value: '', type: 'text' }, { name: 'status', value: '', type: 'select-one' }];
    const results = { attrs: {}, nodes: [], setAttribute(k, v) { this.attrs[k] = v; }, replaceChildren(...nodes) { this.nodes = nodes; } };
    const status = { setAttribute() {} };
    const form = {
        action: 'https://clinic.test/Patients', dataset: { resultsId: 'filter-results' },
        after() {}, addEventListener(name, fn) { handlers[name] = fn; },
        querySelectorAll(selector) { return selector === '[data-filter-reset]' ? [{ addEventListener(name, fn) { resetHandlers[name] = fn; } }] : fields; }
    };
    const history = { state: null, urls: [], replaceState(_state, _title, url) { this.urls.push(url); } };
    const context = {
        document: { querySelectorAll: () => [form], getElementById: () => results, createElement: () => status },
        window: { location: { href: form.action, hash: '' }, history, addEventListener() {} },
        URL, URLSearchParams, AbortController,
        FormData: class { constructor() { return fields.map(field => [field.name, field.value]); } },
        DOMParser: class { parseFromString(html) { return { getElementById: () => html === 'missing' ? null : { childNodes: [html] } }; } },
        setTimeout(fn, ms) { const id = ++nextTimer; timers.set(id, { fn, ms }); return id; },
        clearTimeout(id) { timers.delete(id); },
        fetch(url, options) { return new Promise((resolve, reject) => pending.push({ url: String(url), options, resolve, reject })); }
    };
    vm.runInNewContext(source, context);
    const fire = (name, selector) => handlers[name]({ target: { matches: rule => rule.includes(selector) }, preventDefault() {} });
    const tick = () => { const queued = [...timers.values()]; timers.clear(); queued.forEach(timer => { assert.equal(timer.ms, 200); timer.fn(); }); };
    const settle = () => new Promise(resolve => setImmediate(resolve));
    const respond = async (index, html, extra = {}) => { pending[index].resolve({ ok: true, redirected: false, text: async () => html, ...extra }); await settle(); };
    return { fields, results, status, pending, timers, history, fire, tick, respond, settle, reset: () => resetHandlers.click({ button: 0, preventDefault() {} }) };
}

test('rapid typing waits 200ms and sends all current filter values once', async () => {
    const f = fixture();
    f.fields[0].value = 'a'; f.fire('input', 'input');
    f.fields[0].value = 'Nguyễn An'; f.fire('input', 'input');
    f.fields[1].value = 'Active'; f.fire('change', 'select');
    assert.equal(f.pending.length, 0);
    assert.equal(f.timers.size, 1);
    f.tick();
    assert.equal(f.pending.length, 1);
    const url = new URL(f.pending[0].url);
    assert.equal(url.searchParams.get('searchTerm'), 'Nguyễn An');
    assert.equal(url.searchParams.get('status'), 'Active');
    await f.respond(0, 'matching patients');
    assert.deepEqual(f.results.nodes, ['matching patients']);
    assert.equal(f.fields[0].value, 'Nguyễn An');
    assert.equal(f.results.attrs['aria-busy'], 'false');
    assert.equal(f.history.urls.length, 1);
});

test('a previous response cannot update results during debounce or after the newer response', async () => {
    const f = fixture();
    f.fire('input', 'input'); f.tick();
    f.fields[0].value = 'new'; f.fire('input', 'input');
    assert.equal(f.pending[0].options.signal.aborted, true);
    await f.respond(0, 'stale results');
    assert.deepEqual(f.results.nodes, []);
    f.tick(); await f.respond(1, 'latest results');
    assert.deepEqual(f.results.nodes, ['latest results']);
});

test('reset clears text and dropdown filters and refreshes after 200ms', async () => {
    const f = fixture();
    f.fields[0].value = 'An'; f.fields[1].value = 'Inactive';
    f.reset();
    assert.equal(f.fields[0].value, ''); assert.equal(f.fields[1].value, '');
    assert.equal(f.pending.length, 0);
    f.tick(); await f.respond(0, 'all patients');
    assert.deepEqual(f.results.nodes, ['all patients']);
});

test('Vietnamese composition does not send incomplete input', () => {
    const f = fixture();
    f.fire('compositionstart', 'input'); f.fire('input', 'input');
    assert.equal(f.timers.size, 0);
    f.fire('compositionend', 'input'); f.tick();
    assert.equal(f.pending.length, 1);
});

test('network failure retains old results and allows retry', async () => {
    const f = fixture();
    f.results.nodes = ['old results'];
    f.fire('input', 'input'); f.tick();
    f.pending[0].reject(new Error('offline')); await f.settle();
    assert.deepEqual(f.results.nodes, ['old results']);
    assert.match(f.status.textContent, /Không thể cập nhật/);
    assert.equal(f.results.attrs['aria-busy'], 'false');
    f.fire('submit', 'input'); await f.respond(1, 'retry results');
    assert.deepEqual(f.results.nodes, ['retry results']);
});

test('expired-session redirect and unexpected HTML never replace results', async () => {
    const f = fixture();
    f.fire('input', 'input'); f.tick();
    await f.respond(0, 'login', { redirected: true, url: 'https://clinic.test/Account/Login' });
    assert.deepEqual(f.results.nodes, []);
    f.fire('submit', 'input'); await f.respond(1, 'missing');
    assert.deepEqual(f.results.nodes, []);
    assert.equal(f.history.urls.length, 0);
});
