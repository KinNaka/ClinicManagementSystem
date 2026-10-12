// Search-and-select controls for invoice patients and products.
(() => {
    "use strict";
    let sequence = 0;
    let active = null;
    const popup = document.createElement("div");
    popup.className = "search-picker-results";
    popup.id = "clinic-search-results";
    popup.setAttribute("role", "listbox");
    popup.hidden = true;
    document.body.appendChild(popup);
    const normalize = text => text.normalize("NFD").replace(/[\u0300-\u036f]/g, "").replace(/đ/g, "d").replace(/Đ/g, "D").toLowerCase().trim();
    const matches = (options, text) => {
        const words = normalize(text).split(/\s+/).filter(Boolean);
        return options.filter(option => words.every(word => normalize(option.text).includes(word)));
    };
    const close = () => {
        if (active) {
            clearTimeout(active.timer);
            active.input.setAttribute("aria-expanded", "false");
            active.input.removeAttribute("aria-activedescendant");
        }
        active = null;
        popup.hidden = true;
        popup.replaceChildren();
    };
    const position = () => {
        if (!active?.input.isConnected) { close(); return; }
        const rect = active.input.getBoundingClientRect();
        popup.style.left = Math.max(8, Math.min(rect.left, window.innerWidth - rect.width - 8)) + "px";
        popup.style.width = Math.min(Math.max(rect.width, 280), window.innerWidth - 16) + "px";
        const below = window.innerHeight - rect.bottom - 12;
        const above = rect.top - 12;
        const upward = below < 180 && above > below;
        popup.style.maxHeight = Math.max(80, Math.min(280, upward ? above : below)) + "px";
        popup.style.top = upward ? "auto" : (rect.bottom + 5) + "px";
        popup.style.bottom = upward ? (window.innerHeight - rect.top + 5) + "px" : "auto";
    };
    const highlight = index => {
        if (!active) return;
        const buttons = [...popup.querySelectorAll("[role=option]")];
        active.index = index;
        buttons.forEach((button, i) => {
            button.setAttribute("aria-selected", String(i === index));
        });
        if (buttons[index]) {
            active.input.setAttribute("aria-activedescendant", buttons[index].id);
            buttons[index].scrollIntoView({ block: "nearest" });
        } else active.input.removeAttribute("aria-activedescendant");
    };
    const choose = option => {
        const picker = active;
        if (!picker) return;
        picker.select.value = option.value;
        picker.input.value = option.text;
        picker.input.setCustomValidity("");
        picker.select.dispatchEvent(new Event("change", { bubbles: true }));
        close();
    };
    const render = picker => {
        if (active !== picker || !picker.input.isConnected || picker.composing) return;
        const found = matches(picker.options, picker.input.value);
        picker.found = found.slice(0, 50);
        popup.replaceChildren();
        if (!picker.found.length) {
            const message = document.createElement("div");
            message.className = "search-picker-message";
            message.setAttribute("role", "status");
            message.textContent = "Không tìm thấy kết quả phù hợp.";
            popup.appendChild(message);
        }
        picker.found.forEach((option, i) => {
            const button = document.createElement("div");
            button.id = `search-option-${picker.id}-${i}`;
            button.setAttribute("role", "option");
            button.textContent = option.text;
            button.addEventListener("pointerdown", event => {
                event.preventDefault();
                choose(option);
            });
            popup.appendChild(button);
        });
        if (found.length > 50) {
            const hint = document.createElement("div");
            hint.className = "search-picker-message";
            hint.textContent = "Nhập thêm để thu hẹp kết quả (đang hiển thị 50).";
            popup.appendChild(hint);
        }
        popup.hidden = false;
        picker.input.setAttribute("aria-expanded", "true");
        highlight(-1);
        position();
    };
    const open = picker => {
        if (active !== picker) close();
        active = picker;
        render(picker);
    };
    const init = root => {
        root.querySelectorAll("select[data-search-picker]").forEach(select => {
            if (select.dataset.pickerReady) return;
            select.dataset.pickerReady = "true";
            const input = document.createElement("input");
            input.type = "text";
            input.className = "form-control search-picker-input";
            input.placeholder = select.dataset.placeholder || (select.dataset.searchPicker === "Bệnh nhân" ? "Tìm mã hoặc tên bệnh nhân…" : "Tìm mã hoặc tên sản phẩm…");
            input.autocomplete = "off";
            input.required = select.dataset.optional !== "true"; input.disabled = select.disabled;
            input.setAttribute("role", "combobox");
            input.setAttribute("aria-autocomplete", "list");
            input.setAttribute("aria-expanded", "false");
            input.setAttribute("aria-controls", popup.id);
            input.setAttribute("aria-label", select.dataset.searchPicker);
            const picker = { id: ++sequence, input, select, options: [...select.options].filter(option => option.value && option.value !== "0" && !option.disabled).map(option => ({ value: option.value, text: option.text })), found: [], index: -1, composing: false };
            const selected = picker.options.find(option => option.value === select.value);
            input.value = selected?.text ?? "";
            input.setCustomValidity(selected || !input.required ? "" : "Vui lòng tìm kiếm và chọn một kết quả.");
            if (select.id) { input.id = select.id; select.id += "-value"; }
            select.required = false;
            select.hidden = true;
            select.before(input);
            select.addEventListener("picker:refresh", () => {
                if (active === picker) close();
                picker.options = [...select.options].filter(o => o.value && !o.disabled).map(o => ({ value: o.value, text: o.text }));
                input.value = picker.options.find(o => o.value === select.value)?.text ?? "";
                input.disabled = select.disabled;
                input.required = select.dataset.optional !== "true";
                input.setCustomValidity(input.required && !select.value ? "Vui lòng chọn một kết quả tìm kiếm." : "");
            });
            input.addEventListener("focus", () => open(picker));
            input.addEventListener("input", () => {
                // Typing invalidates the previous ID, even if its label is retyped.
                select.value = "";
                input.setCustomValidity(input.value || input.required ? "Vui lòng chọn một kết quả tìm kiếm." : "");
                select.dispatchEvent(new Event("change", { bubbles: true }));
                if (active !== picker) close();
                active = picker;
                clearTimeout(picker.timer);
                popup.hidden = true;
                input.setAttribute("aria-expanded", "false");
                input.removeAttribute("aria-activedescendant");
                picker.found = [];
                picker.index = -1;
                if (!picker.composing) picker.timer = setTimeout(() => render(picker), 200);
            });
            input.addEventListener("compositionstart", () => { picker.composing = true; clearTimeout(picker.timer); });
            input.addEventListener("compositionend", () => { picker.composing = false; picker.timer = setTimeout(() => render(picker), 200); });
            input.addEventListener("blur", () => { if (active === picker) close(); });
            input.addEventListener("keydown", event => {
                if (event.isComposing || picker.composing) return;
                if (event.key === "Escape") { close(); event.preventDefault(); }
                else if (event.key === "ArrowDown" || event.key === "ArrowUp") {
                    event.preventDefault();
                    if (active !== picker || popup.hidden) open(picker);
                    if (!picker.found.length) return;
                    highlight((picker.index + (event.key === "ArrowDown" ? 1 : -1) + picker.found.length) % picker.found.length);
                } else if (event.key === "Enter" && active === picker && !popup.hidden) {
                    event.preventDefault();
                    if (picker.index >= 0) choose(picker.found[picker.index]);
                }
            });
        });
    };
    document.addEventListener("pointerdown", event => {
        if (active && event.target !== active.input && !popup.contains(event.target)) close();
    });
    window.addEventListener("resize", close);
    document.addEventListener("scroll", event => { if (!popup.contains(event.target)) close(); }, true);
    window.ClinicSearchPicker = { init, matches };
})();
