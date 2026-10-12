(() => {
    const root = document.querySelector('[data-combo-form]');
    if (!root) return;
    const checkbox = root.querySelector('[name="IsCombo"]');
    const service = root.querySelector('[name="IsService"]');
    const rows = root.querySelector('[data-component-rows]');
    const panel = root.querySelector('[data-combo-components]');
    const stock = document.querySelector('[name="CurrentStock"]');
    const reindex = () => {
        [...rows.children].forEach((row, index) => {
            const select = row.querySelector('select');
            const quantity = row.querySelector('input[type="number"]');
            select.name = `Components[${index}].ComponentProductId`;
            quantity.name = `Components[${index}].Quantity`;
        });
    };
    const toggle = () => {
        panel.hidden = !checkbox.checked;
        panel.querySelectorAll('input,select,button').forEach(element => { element.disabled = !checkbox.checked; });
        stock.disabled = checkbox.checked || service.checked;
    };
    root.querySelector('[data-add-component]').addEventListener('click', () => {
        rows.append(root.querySelector('template').content.cloneNode(true));
        reindex(); window.ClinicSearchPicker.init(rows); toggle();
    });
    rows.addEventListener('click', event => {
        if (event.target.closest('[data-remove-component]')) { event.target.closest('tr').remove(); reindex(); }
    });
    checkbox.addEventListener('change', () => { if (checkbox.checked) service.checked = false; toggle(); });
    service.addEventListener('change', () => { if (service.checked) checkbox.checked = false; toggle(); });
    window.ClinicSearchPicker.init(rows); reindex(); toggle();
})();
