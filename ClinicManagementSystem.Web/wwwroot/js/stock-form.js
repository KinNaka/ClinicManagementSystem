(() => {
    const form = document.querySelector('[data-stock-form]');
    if (!form) return;
    const rows = form.querySelector('[data-stock-rows]');
    const template = rows.firstElementChild.cloneNode(true);
    const reindex = () => rows.querySelectorAll('[data-stock-row]').forEach((row, i) => {
        row.querySelectorAll('[name]').forEach(el => el.name = el.name.replace(/Details\[\d+\]/, `Details[${i}]`));
        row.querySelector('[data-remove-row]').disabled = rows.children.length === 1;
    });
    form.querySelector('[data-add-row]').addEventListener('click', () => {
        const row = template.cloneNode(true);
        row.querySelector('select').value = '';
        row.querySelectorAll('input').forEach(el => el.value = el.type === 'number' ? (el.name.endsWith('.Quantity') ? '1' : '0') : '');
        rows.appendChild(row);
        reindex();
        window.ClinicSearchPicker.init(row);
        row.querySelector('.search-picker-input')?.focus();
    });
    rows.addEventListener('click', e => {
        if (!e.target.closest('[data-remove-row]') || rows.children.length <= 1) return;
        e.target.closest('[data-stock-row]').remove(); reindex();
    });
    reindex();
    window.ClinicSearchPicker.init(form);
})();
