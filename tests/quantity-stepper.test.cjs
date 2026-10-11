const { test } = require('node:test');
const assert = require('node:assert/strict');
const { parse, next, init } = require('../ClinicManagementSystem.Web/wwwroot/js/quantity-stepper.js');

test('quantities must be whole numbers within the allowed range', () => {
    for (const value of ['', ' ', '0', '-1', '1.5', 'NaN', 'Infinity', '1000000000']) assert.equal(parse(value), null);
    assert.equal(parse('1'), 1);
    assert.equal(parse('12'), 12);
    assert.equal(parse('999999999'), 999999999);
});

test('minus and plus change exactly one unit and stay within bounds', () => {
    assert.equal(next('5', -1), 4);
    assert.equal(next('5', 1), 6);
    assert.equal(next('1', -1), 1);
    assert.equal(next('999999999', 1), 999999999);
    assert.equal(next('1.5', 1), null);
});

test('stepper updates only its own row and dispatches input for total recalculation', () => {
    const handlers = {};
    const root = { dataset: {}, addEventListener(name, fn) { handlers[name] = fn; }, contains: () => true };
    const createRow = value => {
        const input = { value, events: [], dispatchEvent(event) { this.events.push(event.type); } };
        const minus = { dataset: { quantityStep: '-1' } };
        const plus = { dataset: { quantityStep: '1' } };
        const wrapper = { querySelector(selector) { return selector === '.quantity-input' ? input : selector.includes('-1') ? minus : plus; } };
        for (const button of [minus, plus]) button.closest = () => wrapper;
        return { input, minus, plus, wrapper };
    };
    const first = createRow('1'); const second = createRow('4');
    root.querySelectorAll = () => [first.wrapper, second.wrapper];
    init(root);
    assert.equal(first.minus.disabled, true);
    handlers.click({target: {closest: () => second.plus}});
    assert.equal(second.input.value, '5');
    assert.equal(first.input.value, '1');
    assert.deepEqual(second.input.events, ['input']);
    second.input.value = '999999999';
    handlers.input({target: {matches: () => true, closest: () => second.wrapper}});
    assert.equal(second.plus.disabled, true);
});
