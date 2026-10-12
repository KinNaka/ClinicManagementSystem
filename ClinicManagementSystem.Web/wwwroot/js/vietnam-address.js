(() => {
    "use strict";
    const root = document.querySelector("[data-vietnam-address]");
    if (!root) return;
    const province = root.querySelector('[name="ProvinceCode"]');
    const ward = root.querySelector('[name="WardCode"]');
    const status = root.querySelector("[data-address-status]");
    const retry = root.querySelector("[data-address-retry]");
    const form = root.closest("form");
    let version = 0, failed = false, loading = false;
    const refresh = select => select.dispatchEvent(new Event("picker:refresh"));
    const fill = (select, items, value) => {
        select.replaceChildren(new Option("-- Chọn --", ""), ...items.map(item => new Option(item.name, String(item.code))));
        select.value = value || "";
        refresh(select);
    };
    const request = async url => {
        const response = await fetch(url, { headers: { Accept: "application/json" } });
        if (!response.ok || !response.headers.get("content-type")?.includes("application/json")) throw new Error();
        return response.json();
    };
    const loadWards = async saved => {
        const current = ++version;
        ward.disabled = true; ward.dataset.optional = province.value ? "false" : "true";
        fill(ward, [], "");
        failed = false; status.textContent = ""; retry.hidden = true;
        if (!province.value) { loading = false; return; }
        loading = true; status.textContent = "Đang tải xã/phường…";
        try {
            const items = await request(root.dataset.wardsUrl + "?provinceCode=" + encodeURIComponent(province.value));
            if (current !== version) return;
            ward.disabled = false; fill(ward, items, saved);
            const input = document.getElementById("WardCode");
            if (input) input.placeholder = "Tìm xã/phường…";
            status.textContent = "";
        } catch {
            if (current !== version) return;
            failed = true; status.textContent = "Không tải được xã/phường. Vui lòng thử lại."; retry.hidden = false;
        } finally { if (current === version) loading = false; }
    };
    const initialize = async () => {
        loading = true; failed = false; status.textContent = "Đang tải tỉnh/thành phố…";
        try {
            const items = await request(root.dataset.provincesUrl);
            province.disabled = false; fill(province, items, root.dataset.province);
            await loadWards(root.dataset.ward);
        } catch {
            loading = false; failed = true; status.textContent = "Không tải được danh mục địa chỉ. Vui lòng thử lại."; retry.hidden = false;
        }
    };
    window.ClinicSearchPicker.init(root);
    province.addEventListener("change", () => loadWards(""));
    retry.addEventListener("click", () => province.disabled ? initialize() : loadWards(root.dataset.ward));
    form.addEventListener("submit", event => {
        if (failed || loading) { event.preventDefault(); status.textContent = "Vui lòng tải xong danh mục địa chỉ trước khi lưu."; }
    });
    initialize();
})();
