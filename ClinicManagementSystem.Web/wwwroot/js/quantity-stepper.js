(() => {
    "use strict";
    const min = 1;
    const max = 999999999;
    const parse = raw => {
        if (!String(raw).trim()) return null;
        const value = Number(raw);
        return Number.isInteger(value) && value >= min && value <= max ? value : null;
    };
    const next = (raw, direction) => {
        const value = parse(raw);
        return value === null ? null : Math.max(min, Math.min(max, value + direction));
    };
    const sync = wrapper => {
        const value = parse(wrapper.querySelector(".quantity-input").value);
        wrapper.querySelector('[data-quantity-step="-1"]').disabled = value === null || value <= min;
        wrapper.querySelector('[data-quantity-step="1"]').disabled = value === null || value >= max;
    };
    const init = root => {
        root.querySelectorAll(".quantity-stepper").forEach(sync);
        if (root.dataset.quantityReady) return;
        root.dataset.quantityReady = "true";
        root.addEventListener("input", event => {
            if (event.target.matches(".quantity-input")) sync(event.target.closest(".quantity-stepper"));
        });
        root.addEventListener("click", event => {
            const button = event.target.closest("[data-quantity-step]");
            if (!button || !root.contains(button) || button.disabled) return;
            const wrapper = button.closest(".quantity-stepper");
            const input = wrapper.querySelector(".quantity-input");
            const value = next(input.value, Number(button.dataset.quantityStep));
            if (value === null) { input.reportValidity(); return; }
            input.value = String(value);
            sync(wrapper);
            input.dispatchEvent(new Event("input", { bubbles: true }));
        });
    };
    const api = { init, parse, next };
    if (typeof module !== "undefined" && module.exports) module.exports = api;
    if (typeof window !== "undefined") window.ClinicQuantity = api;
})();
