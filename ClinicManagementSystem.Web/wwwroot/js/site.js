// Shared live filters. GET forms still work normally when JavaScript is unavailable.
(() => {
    "use strict";
    const delay = 200;

    document.querySelectorAll("form[data-live-filter]").forEach(form => {
        const results = document.getElementById(form.dataset.resultsId);
        if (!results) return;

        const status = document.createElement("p");
        status.className = "live-filter-status text-muted";
        status.setAttribute("role", "status");
        status.setAttribute("aria-live", "polite");
        form.after(status);
        results.setAttribute("aria-live", "polite");
        results.setAttribute("aria-atomic", "true");
        let timer;
        let controller;
        let revision = 0;
        let composing = false;

        const invalidate = () => {
            clearTimeout(timer);
            controller?.abort();
            revision += 1;
            results.setAttribute("aria-busy", "false");
        };

        const search = async currentRevision => {
            const url = new URL(form.action, window.location.href);
            url.search = new URLSearchParams(new FormData(form)).toString();
            controller = new AbortController();
            status.textContent = "Đang tìm kiếm…";
            results.setAttribute("aria-busy", "true");
            try {
                const response = await fetch(url, {
                    signal: controller.signal,
                    headers: { "X-Requested-With": "XMLHttpRequest" },
                    credentials: "same-origin"
                });
                if (!response.ok || (response.redirected && new URL(response.url).pathname !== url.pathname)) {
                    throw new Error("Search failed or session expired");
                }
                const html = await response.text();
                if (currentRevision !== revision) return;
                const page = new DOMParser().parseFromString(html, "text/html");
                const nextResults = page.getElementById(form.dataset.resultsId);
                if (!nextResults) throw new Error("Missing filter results");
                results.replaceChildren(...nextResults.childNodes);
                window.history.replaceState(window.history.state, "", url.pathname + url.search + window.location.hash);
                status.textContent = "Đã cập nhật kết quả.";
            } catch (error) {
                if (error.name !== "AbortError" && currentRevision === revision) {
                    status.textContent = "Không thể cập nhật kết quả. Hãy thử lại hoặc tải lại trang để kiểm tra đăng nhập.";
                }
            } finally {
                if (currentRevision === revision) results.setAttribute("aria-busy", "false");
            }
        };

        const schedule = () => {
            invalidate();
            if (composing) return;
            status.textContent = "Đang chờ cập nhật…";
            const currentRevision = revision;
            timer = setTimeout(() => search(currentRevision), delay);
        };

        form.addEventListener("input", event => {
            if (event.target.matches("input:not([type=hidden]), textarea")) schedule();
        });
        form.addEventListener("change", event => {
            if (event.target.matches("select, input[type=checkbox], input[type=radio]")) schedule();
        });
        form.addEventListener("compositionstart", () => {
            composing = true;
            invalidate();
        });
        form.addEventListener("compositionend", () => {
            composing = false;
            schedule();
        });
        form.addEventListener("submit", event => {
            event.preventDefault();
            if (composing) return;
            invalidate();
            search(revision);
        });
        form.querySelectorAll("[data-filter-reset]").forEach(link => {
            link.addEventListener("click", event => {
                if (event.ctrlKey || event.metaKey || event.shiftKey || event.altKey || event.button !== 0) return;
                event.preventDefault();
                form.querySelectorAll("input:not([type=hidden]), select, textarea").forEach(field => {
                    if (field.type === "checkbox" || field.type === "radio") field.checked = false;
                    else field.value = "";
                });
                schedule();
            });
        });
        window.addEventListener("pagehide", invalidate);
    });
})();
