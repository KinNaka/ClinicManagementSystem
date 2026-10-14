(() => {
    window.ClinicSalePricing = { calculate(goods, type, value, surcharge) {
        const round = n => Math.round((n + Number.EPSILON) * 100) / 100;
        if (![goods, value, surcharge].every(Number.isFinite) || value < 0 || surcharge < 0 || value > 999999999999 || surcharge > 999999999999)
            return { discount: 0, total: 0, error: 'Giảm giá và thu khác phải là số không âm trong giới hạn cho phép.' };
        if (!['Percent', 'Amount'].includes(type) || (type === 'Percent' && value > 100))
            return { discount: 0, total: 0, error: 'Giảm giá phần trăm phải từ 0 đến 100.' };
        const discount = round(type === 'Percent' ? goods * value / 100 : value);
        return { discount, total: round(goods - discount + round(surcharge)), error: discount > goods ? 'Giảm giá không được vượt tiền hàng.' : '' };
    } };
})();
