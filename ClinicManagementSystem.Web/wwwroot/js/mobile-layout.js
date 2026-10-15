// Keep the same MVC forms and table semantics; add labels for the mobile card layout.
(() => {
    "use strict";
    const labelTables = () => {
        document.querySelectorAll("table[data-mobile-cards]").forEach(table => {
            const labels = [...table.querySelectorAll("thead th")].map(cell => cell.textContent.trim());
            table.querySelectorAll("tbody tr").forEach(row => {
                [...row.cells].forEach((cell, index) => {
                    if (cell.colSpan > 1) return;
                    const label = labels[index] || "Thao tác";
                    if (cell.dataset.label !== label) cell.dataset.label = label;
                });
            });
        });
    };
    labelTables();
    // Covers newly added invoice/combo rows and results replaced by live filters.
    new MutationObserver(labelTables).observe(document.body, { childList: true, subtree: true });
})();
