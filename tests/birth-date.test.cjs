const { test } = require('node:test');
const assert = require('node:assert/strict');
const { format } = require('../ClinicManagementSystem.Web/wwwroot/js/birth-date.js');
test('eight digits become day month year while incomplete or formatted input stays intact', () => {
    assert.equal(format('27112003'), '27/11/2003');
    assert.equal(format('01021990'), '01/02/1990');
    assert.equal(format('27/11/2003'), '27/11/2003');
    assert.equal(format('2711200'), '2711200');
    assert.equal(format(''), '');
    assert.equal(format('31022003'), '31/02/2003'); // Server validates calendar dates.
});
