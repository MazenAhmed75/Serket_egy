// Small behaviours for the admin pages (kept in a file so no inline scripts are needed).
document.addEventListener('DOMContentLoaded', function () {
    // <form data-confirm="Delete this?"> asks before submitting.
    document.querySelectorAll('form[data-confirm]').forEach(function (form) {
        form.addEventListener('submit', function (event) {
            if (!window.confirm(form.dataset.confirm)) {
                event.preventDefault();
            }
        });
    });

    // <select data-autosubmit> submits its form as soon as the choice changes.
    document.querySelectorAll('select[data-autosubmit]').forEach(function (select) {
        select.addEventListener('change', function () {
            select.form.submit();
        });
    });
});
