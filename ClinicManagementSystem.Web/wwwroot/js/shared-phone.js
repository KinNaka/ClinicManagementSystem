(() => {
    const widget = document.getElementById('shared-phone-widget');
    if (!widget) return;
    const form = widget.closest('form');
    const phone = document.getElementById('PhoneNumber');
    const owner = document.getElementById('SharedPhoneWithPatientId');
    const relation = document.getElementById('SharedPhoneRelationship');
    const status = document.getElementById('shared-phone-status');
    const matchesBox = document.getElementById('shared-phone-matches');
    const relationPick = document.getElementById('shared-phone-relation');
    const modalElement = document.getElementById('shared-phone-modal');
    const modal = bootstrap.Modal.getOrCreateInstance(modalElement);
    const normalize = value => value.trim().replace(/[ \t\r\n().-]/g, '').replaceAll('+84', '0');
    let version = 0, timer, pendingSubmit = false, verifiedPhone = null, blocked = false, matches = [];
    let confirmedPhone = owner.value ? normalize(phone.value) : null;
    modalElement.addEventListener('hidden.bs.modal', () => { pendingSubmit = false; });
    function clearConfirmation() {
        owner.value = ''; relation.value = ''; confirmedPhone = null;
        status.textContent = ''; verifiedPhone = null;
    }
    function showMatches() {
        matchesBox.replaceChildren();
        matches.forEach((patient, index) => {
            const row = document.createElement('div'); row.className = 'border rounded p-2 mb-2';
            const label = document.createElement('label'); label.className = 'd-block';
            const radio = document.createElement('input'); radio.type = 'radio'; radio.name = 'shared-phone-choice';
            radio.value = String(patient.id); radio.className = 'form-check-input me-2';
            radio.checked = owner.value === String(patient.id) || (!owner.value && index === 0);
            label.append(radio, document.createTextNode(`${patient.fullName} (${patient.patientCode})`));
            const detail = document.createElement('div'); detail.className = 'small text-muted';
            detail.textContent = `${patient.phoneNumber || ''}${patient.status === 'Inactive' ? ' · Ngừng hoạt động' : ''}`;
            const link = document.createElement('a'); link.href = patient.detailsUrl; link.textContent = 'Xem hồ sơ';
            link.target = '_blank'; link.rel = 'noopener'; link.className = 'small';
            row.append(label, detail, link); matchesBox.append(row);
        });
        relationPick.value = relation.value;
        document.getElementById('shared-phone-modal-error').textContent = '';
        modal.show();
    }
    async function check(forcePopup = false) {
        const current = normalize(phone.value), request = ++version;
        if (!current) { matches = []; blocked = false; verifiedPhone = ''; status.textContent = ''; return true; }
        status.textContent = 'Đang kiểm tra số điện thoại…';
        try {
            const url = new URL(widget.dataset.url, location.origin);
            url.searchParams.set('phoneNumber', phone.value);
            if (widget.dataset.excludeId) url.searchParams.set('excludeId', widget.dataset.excludeId);
            const response = await fetch(url, { headers: { Accept: 'application/json' }, cache: 'no-store' });
            if (!response.ok) throw new Error('Phone lookup failed');
            const data = await response.json();
            if (request !== version || current !== normalize(phone.value)) return false;
            matches = data;
            const confirmed = owner.value && confirmedPhone === current && relation.value && matches.some(p => String(p.id) === owner.value);
            blocked = matches.length > 0 && !confirmed;
            verifiedPhone = current;
            if (matches.length === 0) {
                clearConfirmation(); verifiedPhone = current;
                status.textContent = 'Chưa có khách hàng dùng số điện thoại này.';
            } else if (confirmed) {
                status.textContent = `Đã xác nhận ${relation.value} của ${matches.find(p => String(p.id) === owner.value).fullName}, dùng chung số điện thoại.`;
            } else {
                status.textContent = 'Số điện thoại đã được sử dụng. Chọn hồ sơ hiện có hoặc xác nhận người thân.';
            }
            if (matches.length > 0 && (blocked || forcePopup)) showMatches();
            return !blocked;
        } catch {
            if (request === version) { verifiedPhone = null; status.textContent = 'Không kiểm tra được số điện thoại. Bấm kiểm tra lại hoặc tải lại trang.'; }
            return false;
        }
    }
    phone.addEventListener('input', () => {
        ++version; pendingSubmit = false; blocked = false; clearTimeout(timer);
        if (confirmedPhone !== normalize(phone.value)) clearConfirmation();
        verifiedPhone = null;
        if (normalize(phone.value).length >= 8) timer = setTimeout(() => check(), 500);
    });
    phone.addEventListener('blur', () => { clearTimeout(timer); if (verifiedPhone !== normalize(phone.value)) check(); });
    document.getElementById('check-shared-phone').addEventListener('click', () => { clearTimeout(timer); check(true); });
    document.getElementById('change-shared-phone').addEventListener('click', () => {
        pendingSubmit = false; clearConfirmation();
        modalElement.addEventListener('hidden.bs.modal', () => phone.focus(), { once: true });
        modal.hide();
    });
    document.getElementById('confirm-shared-phone').addEventListener('click', () => {
        const selected = matchesBox.querySelector('input:checked');
        if (!selected || !relationPick.value) { document.getElementById('shared-phone-modal-error').textContent = 'Vui lòng chọn khách hàng và quan hệ người thân.'; return; }
        owner.value = selected.value; relation.value = relationPick.value; confirmedPhone = normalize(phone.value);
        verifiedPhone = confirmedPhone; blocked = false;
        status.textContent = `Đã xác nhận ${relation.value} của ${matches.find(p => String(p.id) === owner.value).fullName}, dùng chung số điện thoại.`;
        const shouldSubmit = pendingSubmit; pendingSubmit = false;
        if (shouldSubmit) modalElement.addEventListener('hidden.bs.modal', () => form.requestSubmit(), { once: true });
        modal.hide();
    });
    form.addEventListener('submit', async event => {
        if (!phone.value.trim() || (verifiedPhone === normalize(phone.value) && !blocked)) return;
        event.preventDefault(); pendingSubmit = true;
        if (await check()) { pendingSubmit = false; form.requestSubmit(); }
    });
    if (phone.value.trim()) check();
})();
