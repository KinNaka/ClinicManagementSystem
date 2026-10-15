const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');

class Element {
    constructor() { this.attrs = {}; this.dataset = {}; this.handlers = {}; this.children = []; this.value = ''; this.isConnected = true; this.style = {}; }
    setAttribute(k, v) { this.attrs[k] = v; }
    removeAttribute(k) { delete this.attrs[k]; }
    setCustomValidity(message) { this.validationMessage = message; }
    addEventListener(k, fn) { this.handlers[k] = fn; }
    dispatchEvent(event) { this.changes = (this.changes || 0) + 1; }
    appendChild(child) { this.children.push(child); }
    replaceChildren() { this.children = []; }
    querySelectorAll() { return this.children.filter(child => child.attrs.role === 'option'); }
    contains(child) { return this.children.includes(child); }
    before(input) { this.input = input; }
    getBoundingClientRect() { return this.rect || { left: 40, width: 300, bottom: 100, top: 60 }; }
    get offsetHeight() { return Math.min(this.children.length * 44 + 12, parseFloat(this.style.maxHeight) + 12); }
    scrollIntoView() {}
}
function setup() {
    const popup = new Element();
    const documentHandlers = {}, windowHandlers = {}, viewportHandlers = {};
    const timers = new Map();
    let id = 0;
    const context = {
        document: { createElement: type => type === 'div' ? new Element() : new Element(), body: { appendChild(element) { context.popup = element; } }, addEventListener(type, fn) { documentHandlers[type] = fn; } },
        window: { innerWidth: 1000, innerHeight: 800, addEventListener(type, fn) { windowHandlers[type] = fn; }, visualViewport: { width: 1000, height: 800, offsetLeft: 0, offsetTop: 0, addEventListener(type, fn) { viewportHandlers[type] = fn; } } },
        Event: class {},
        setTimeout(fn, ms) { const key = ++id; timers.set(key, { fn, ms }); return key; },
        clearTimeout(key) { timers.delete(key); }
    };
    vm.runInNewContext(fs.readFileSync('ClinicManagementSystem.Web/wwwroot/js/search-picker.js', 'utf8'), context);
    const make = (value = '') => {
        const select = new Element();
        select.dataset.searchPicker = 'Sản phẩm';
        select.value = value;
        select.id = 'product';
        select.options = [{value:'',text:'Chọn sản phẩm'}, {value:'1',text:'TH-01 - Thuốc Đặc biệt'}, {value:'2',text:'VT-02 - Bông y tế'}];
        context.window.ClinicSearchPicker.init({querySelectorAll: () => [select]});
        return select;
    };
    const tick = () => { const queue = [...timers.values()]; timers.clear(); queue.forEach(timer => { assert.equal(timer.ms, 200); timer.fn(); }); };
    return { context, make, tick, timers, documentHandlers, windowHandlers, viewportHandlers };
}

function pointer(overrides = {}) {
    return { pointerId: 1, pointerType: 'touch', button: 0, isPrimary: true, clientX: 30, clientY: 100, preventDefault() {}, ...overrides };
}
function tap(option, overrides = {}) {
    option.handlers.pointerdown(pointer(overrides));
    option.handlers.pointerup(pointer(overrides));
}

test('touch selects only on release and scrolling does not commit a product', () => {
    const f = setup(); const select = f.make(); const input = select.input;
    input.handlers.focus();
    let option = f.context.popup.querySelectorAll()[0];
    option.handlers.pointerdown(pointer());
    assert.equal(select.value, '');
    input.handlers.blur();
    assert.equal(f.context.popup.hidden, false);
    option.handlers.pointermove(pointer({ clientY: 45 }));
    option.handlers.pointerup(pointer({ clientY: 45 }));
    assert.equal(select.value, '');
    assert.equal(f.context.popup.hidden, false);
    // A fresh deliberate tap after the swipe still works, without refocusing the input.
    option = f.context.popup.querySelectorAll()[1];
    tap(option);
    assert.equal(select.value, '2');
    assert.equal(f.context.popup.hidden, true);
});

test('cancelled gestures, native scroll, and moving away then back never select', () => {
    for (const action of ['cancel', 'scroll', 'return', 'release-far']) {
        const f = setup(); const select = f.make(); select.input.handlers.focus();
        const option = f.context.popup.querySelectorAll()[0];
        option.handlers.pointerdown(pointer());
        if (action === 'cancel') option.handlers.pointercancel(pointer());
        if (action === 'scroll') f.context.popup.handlers.scroll();
        if (action === 'return') {
            option.handlers.pointermove(pointer({ clientY: 30 }));
            option.handlers.pointermove(pointer());
        }
        option.handlers.pointerup(pointer(action === 'release-far' ? { clientY: 30 } : {}));
        option.handlers.click({ detail: 1 });
        assert.equal(select.value, '', action);
        assert.equal(f.context.popup.hidden, false, action);
    }
});

test('mouse selection, minor touch movement and accessible activation still work', () => {
    for (const type of ['mouse', 'touch', 'accessible']) {
        const f = setup(); const select = f.make(); select.input.handlers.focus();
        const option = f.context.popup.querySelectorAll()[0];
        if (type === 'accessible') option.handlers.click({ detail: 0 });
        else {
            option.handlers.pointerdown(pointer({ pointerType: type }));
            option.handlers.pointerup(pointer({ pointerType: type, clientY: 104 }));
        }
        assert.equal(select.value, '1');
        assert.equal(select.changes, 1);
    }
});

test('outside tap closes results and the focused input can reopen them', () => {
    const f = setup(); const select = f.make(); select.input.handlers.focus();
    f.documentHandlers.pointerdown({ target: {} });
    assert.equal(f.context.popup.hidden, true);
    select.input.handlers.click();
    assert.equal(f.context.popup.hidden, false);
});

test('mobile keyboard resize and automatic scroll keep filtered results visible above the keyboard', () => {
    const f = setup(); const select = f.make(); const input = select.input;
    input.rect = { left: 20, width: 340, top: 430, bottom: 474 };
    input.handlers.focus();
    const viewport = f.context.window.visualViewport;
    Object.assign(viewport, { width: 390, height: 320, offsetTop: 200 });
    f.viewportHandlers.resize();
    f.windowHandlers.resize();
    f.documentHandlers.scroll({ target: {} });
    assert.equal(f.context.popup.hidden, false);
    assert.equal(input.attrs['aria-expanded'], 'true');
    assert.ok(parseFloat(f.context.popup.style.top) >= 208);
    assert.ok(parseFloat(f.context.popup.style.top) + f.context.popup.offsetHeight < input.rect.top);
    assert.ok(parseFloat(f.context.popup.style.left) + parseFloat(f.context.popup.style.width) <= 382);
    input.value = 'bong'; input.handlers.input();
    f.viewportHandlers.scroll(); f.tick();
    assert.equal(f.context.popup.hidden, false);
    assert.equal(f.context.popup.querySelectorAll().length, 1);
    tap(f.context.popup.querySelectorAll()[0]);
    assert.equal(select.value, '2');
});

test('desktop positioning works without the visual viewport API', () => {
    const f = setup(); delete f.context.window.visualViewport;
    f.make().input.handlers.focus();
    assert.equal(f.context.popup.style.top, '105px');
    assert.equal(f.context.popup.hidden, false);
});

test('search ignores Vietnamese accents and matches both code and name', () => {
    const f = setup();
    const matches = f.context.window.ClinicSearchPicker.matches;
    const options = [{value:'1',text:'BN-01 - Nguyễn Đặng An'}, {value:'2',text:'BN-02 - Trần Bình'}];
    assert.equal(matches(options, 'nguyen dang').length, 1);
    assert.equal(matches(options, 'BN-01 An')[0].value, '1');
    assert.equal(matches(options, 'không có').length, 0);
});

test('existing selection is restored and initialization is idempotent', () => {
    const f = setup(); const select = f.make('1'); const input = select.input;
    assert.equal(input.value, 'TH-01 - Thuốc Đặc biệt');
    assert.equal(input.validationMessage, '');
    assert.equal(select.hidden, true);
    assert.equal(input.id, 'product');
    f.context.window.ClinicSearchPicker.init({querySelectorAll: () => [select]});
    assert.equal(select.input, input);
});

test('typing clears the old ID and keyboard selection commits the new ID', () => {
    const f = setup(); const select = f.make('1'); const input = select.input;
    input.handlers.focus();
    input.value = 'bong'; input.handlers.input();
    assert.equal(select.value, ''); assert.notEqual(input.validationMessage, '');
    assert.equal(f.context.popup.hidden, true);
    f.tick();
    assert.equal(f.context.popup.querySelectorAll().length, 1);
    const press = key => input.handlers.keydown({key, preventDefault() {}});
    press('ArrowDown'); press('Enter');
    assert.equal(select.value, '2');
    assert.equal(input.value, 'VT-02 - Bông y tế');
    assert.equal(input.validationMessage, '');
    assert.equal(f.context.popup.hidden, true);
    assert.equal(select.changes, 2);
});

test('new rows have independent selection and notes bindings exist in both forms', () => {
    const f = setup(); const first = f.make('1'); const second = f.make();
    second.input.handlers.focus(); second.input.value = 'bong'; second.input.handlers.input(); f.tick();
    tap(f.context.popup.querySelectorAll()[0]);
    assert.equal(first.value, '1'); assert.equal(second.value, '2');
    for (const name of ['Create','Edit']) {
        const view = fs.readFileSync(`ClinicManagementSystem.Web/Views/Sales/${name}.cshtml`, 'utf8');
        assert.match(view, /name="Details\[@i\]\.Notes"/);
        assert.match(view, /\.line-notes"\)\.name = `Details\[\$\{i\}\]\.Notes`/);
        assert.match(view, /ClinicSearchPicker\.init\(rows\)/);
    }
});

test('no match cannot commit arbitrary text and Escape closes results', () => {
    const f = setup(); const select = f.make(); const input = select.input;
    input.handlers.focus(); input.value = 'not-a-product'; input.handlers.input(); f.tick();
    assert.equal(f.context.popup.querySelectorAll().length, 0);
    input.handlers.keydown({key:'Enter',preventDefault() {}});
    assert.equal(select.value, ''); assert.notEqual(input.validationMessage, '');
    input.handlers.keydown({key:'Escape',preventDefault() {}});
    assert.equal(input.attrs['aria-expanded'], 'false');
});

test('patient no-match action opens quick creation by keyboard without selecting arbitrary text', () => {
    const f = setup(); const select = f.make();
    f.context.CustomEvent = class { constructor(type, init) { this.type = type; Object.assign(this, init); } };
    select.dataset.emptyAction = 'patient-create';
    select.dataset.emptyActionLabel = '+ Tạo nhanh bệnh nhân mới';
    const events = []; select.dispatchEvent = event => events.push(event);
    const input = select.input;
    input.handlers.focus(); input.value = 'Nguyễn Mới'; input.handlers.input(); f.tick();
    assert.equal(f.context.popup.querySelectorAll()[0].textContent, '+ Tạo nhanh bệnh nhân mới');
    input.handlers.keydown({key:'ArrowDown',preventDefault() {}});
    input.handlers.keydown({key:'Enter',preventDefault() {}});
    const action = events.find(event => event.type === 'picker:empty-action');
    assert.equal(action.detail.searchTerm, 'Nguyễn Mới');
    assert.equal(select.value, '');
    assert.equal(f.context.popup.hidden, true);
});

test('address picker refresh replaces options, restores selection and respects disabled/optional state', () => {
    const f = setup(); const select = f.make();
    select.dataset.optional = 'true'; select.disabled = true;
    select.handlers['picker:refresh']();
    assert.equal(select.input.disabled, true);
    assert.equal(select.input.required, false);
    assert.equal(select.input.validationMessage, '');
    select.disabled = false; select.dataset.optional = 'false';
    select.options = [{value:'',text:'Chọn'}, {value:'4',text:'Phường Ba Đình'}]; select.value = '4';
    select.handlers['picker:refresh']();
    assert.equal(select.input.value, 'Phường Ba Đình');
    assert.equal(select.input.disabled, false);
    select.input.value = 'ba dinh'; select.input.handlers.input(); f.tick();
    assert.equal(f.context.popup.querySelectorAll().length, 1);
    assert.equal(f.context.popup.querySelectorAll()[0].textContent, 'Phường Ba Đình');
});
