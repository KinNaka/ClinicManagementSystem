const { test } = require('node:test');
const assert = require('node:assert/strict');
const vm = require('node:vm');
const fs = require('node:fs');
const context = { window: {} };
vm.runInNewContext(fs.readFileSync('ClinicManagementSystem.Web/wwwroot/js/sale-pricing.js', 'utf8'), context);
const calculate = context.window.ClinicSalePricing.calculate;
test('amount and percentage discounts include surcharge after discount', () => {
    assert.equal(calculate(200000, 'Amount', 30000, 50000).total, 220000);
    assert.equal(calculate(200000, 'Percent', 15, 50000).total, 220000);
    assert.equal(calculate(199.99, 'Percent', 12.5, 0).discount, 25);
    assert.equal(calculate(200000, 'Percent', 100, 50000).total, 50000);
});
test('invalid adjustments cannot produce a payable total', () => {
    for (const args of [[100,'Amount',101,0], [100,'Percent',101,0], [100,'Amount',-1,0], [100,'Amount',0,-1], [100,'Amount',NaN,0]])
        assert.ok(calculate(...args).error);
});
