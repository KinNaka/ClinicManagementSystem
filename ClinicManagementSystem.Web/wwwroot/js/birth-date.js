(() => {
    const format = value => {
        const text = value.trim();
        return /^\d{8}$/.test(text) ? `${text.slice(0, 2)}/${text.slice(2, 4)}/${text.slice(4)}` : value;
    };
    if (typeof module !== 'undefined' && module.exports) module.exports = { format };
    if (typeof document === 'undefined') return;
    const input = document.getElementById('DateOfBirth');
    if (!input) return;
    function apply() {
        const original = input.value;
        const formatted = format(original);
        if (original === formatted) return;
        const digitsBeforeCaret = original.slice(0, input.selectionStart ?? original.length).replace(/\D/g, '').length;
        input.value = formatted;
        const caret = digitsBeforeCaret + (digitsBeforeCaret > 2 ? 1 : 0) + (digitsBeforeCaret > 4 ? 1 : 0);
        input.setSelectionRange(caret, caret);
    }
    input.addEventListener('input', apply);
    input.addEventListener('blur', apply);
    apply();
})();
