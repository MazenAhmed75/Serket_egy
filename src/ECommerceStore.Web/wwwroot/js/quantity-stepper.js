// Progressive enhancement for any ".qty-stepper" widget: a number input flanked by
// minus/plus buttons. Works on every page that includes this script — no page-specific code.
document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('.qty-stepper').forEach(function (stepper) {
        var input = stepper.querySelector('input[type="number"]');
        var minusBtn = stepper.querySelector('.qty-minus');
        var plusBtn = stepper.querySelector('.qty-plus');
        if (!input || !minusBtn || !plusBtn) {
            return;
        }

        function step(delta) {
            var min = input.min !== '' ? parseInt(input.min, 10) : 1;
            var max = input.max !== '' ? parseInt(input.max, 10) : Infinity;
            var current = parseInt(input.value, 10);
            if (isNaN(current)) {
                current = min;
            }

            var next = Math.min(max, Math.max(min, current + delta));
            input.value = next;
            input.dispatchEvent(new Event('input', { bubbles: true }));
            input.dispatchEvent(new Event('change', { bubbles: true }));

            minusBtn.disabled = next <= min;
            plusBtn.disabled = next >= max;
        }

        minusBtn.addEventListener('click', function () { step(-1); });
        plusBtn.addEventListener('click', function () { step(1); });

        input.addEventListener('input', function () {
            var min = input.min !== '' ? parseInt(input.min, 10) : 1;
            var max = input.max !== '' ? parseInt(input.max, 10) : Infinity;
            var current = parseInt(input.value, 10);
            minusBtn.disabled = !isNaN(current) && current <= min;
            plusBtn.disabled = !isNaN(current) && current >= max;
        });

        // Set the initial enabled/disabled state.
        input.dispatchEvent(new Event('input'));
    });
});
