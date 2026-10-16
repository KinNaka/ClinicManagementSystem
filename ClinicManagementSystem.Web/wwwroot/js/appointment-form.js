(() => {
    "use strict";
    const form = document.getElementById("appointment-form");
    if (!form) return;
    const days = form.querySelector("#MedicationDays");
    const start = form.querySelector("#MedicationStartDate");
    const appointment = form.querySelector("#AppointmentDate");
    const custom = form.querySelector("#UseCustomDate");
    const hint = document.getElementById("appointment-date-hint");
    function update() {
        appointment.readOnly = !custom.checked;
        if (custom.checked) {
            hint.textContent = "Dùng ngày tái khám bạn chọn. Ngày này phải từ ngày bắt đầu uống thuốc trở đi.";
            return;
        }
        const count = Number(days.value);
        if (!/^\d{4}-\d{2}-\d{2}$/.test(start.value) || !Number.isInteger(count) || count < 1 || count > 3650) {
            appointment.value = "";
            hint.textContent = "Nhập ngày bắt đầu và số ngày thuốc hợp lệ để tính ngày hẹn.";
            return;
        }
        // UTC arithmetic avoids browser timezone/DST changing a calendar date.
        const date = new Date(start.value + "T00:00:00Z");
        date.setUTCDate(date.getUTCDate() + count);
        const iso = date.toISOString().slice(0, 10);
        appointment.value = /^\d{4}-/.test(iso) ? iso : "";
        hint.textContent = "Ngày tái khám tự động = ngày bắt đầu uống thuốc + số ngày thuốc.";
    }
    days.addEventListener("input", update);
    start.addEventListener("change", update);
    custom.addEventListener("change", update);
    if (!form.querySelector("fieldset").disabled) update();
})();
