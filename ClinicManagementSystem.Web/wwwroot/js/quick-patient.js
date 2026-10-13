(() => {
    const select = document.querySelector('select[name="PatientId"]');
    const element = document.getElementById('quick-patient-modal');
    if (!select || !element) return;
    const frame = document.getElementById('quick-patient-frame');
    const modal = bootstrap.Modal.getOrCreateInstance(element);
    select.addEventListener('picker:empty-action', event => {
        const url = new URL(element.dataset.url, location.origin);
        url.searchParams.set('searchTerm', event.detail.searchTerm);
        frame.src = url.href;
        modal.show();
    });
    window.addEventListener('message', event => {
        if (event.origin !== location.origin || event.source !== frame.contentWindow || event.data?.type !== 'clinic:patient-created') return;
        const patient = event.data.patient;
        if (!patient || !Number.isSafeInteger(patient.id) || patient.id <= 0 || typeof patient.text !== 'string') return;
        const value = String(patient.id);
        let option = [...select.options].find(o => o.value === value);
        if (!option) { option = new Option(patient.text, value); select.add(option); }
        else option.text = patient.text;
        select.value = value;
        select.dispatchEvent(new Event('picker:refresh'));
        select.dispatchEvent(new Event('change', { bubbles: true }));
        modal.hide();
    });
})();
