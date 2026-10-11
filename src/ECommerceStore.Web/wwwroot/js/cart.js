// Cart page: changing a quantity saves it straight away (the "Update" button remains for browsers without scripts).
document.addEventListener('DOMContentLoaded', function () {
    document.body.classList.add('js-loaded');
    document.querySelectorAll('input[data-autosubmit]').forEach(function (input) {
        input.addEventListener('change', function () {
            if (input.form && input.checkValidity()) {
                input.form.submit();
            }
        });
    });
});
