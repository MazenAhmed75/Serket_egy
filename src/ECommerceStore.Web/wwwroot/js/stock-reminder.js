// "Remind me when it's back": builds the list of sold-out colour/size combinations from the page's
// stock data, shows the bell button when there is at least one, and posts the customer's e-mail.
document.addEventListener('DOMContentLoaded', function () {
    var box = document.getElementById('remindBox');
    var dataEl = document.getElementById('stock-data');
    if (!box || !dataEl) {
        return;
    }

    var toggle = document.getElementById('remindToggle');
    var form = document.getElementById('remindForm');
    var select = document.getElementById('remindVariant');
    var message = document.getElementById('remindMessage');

    var data;
    try {
        data = JSON.parse(dataEl.textContent);
    } catch (e) {
        return;
    }

    // Every colour/size that has no stock: [{ value: 'colourId|size', label: 'Maroon - M' }, ...]
    function soldOutVariants() {
        var variants = [];

        function collect(colorId, colorName, stock) {
            Object.keys(stock).forEach(function (size) {
                if (!stock[size]) {
                    variants.push({
                        value: (colorId || 'none') + '|' + size,
                        label: colorName ? colorName + ' - ' + size : 'Size ' + size
                    });
                }
            });
        }

        if (data.hasColors) {
            Object.keys(data.colors).forEach(function (id) {
                collect(id, data.colors[id].name, data.colors[id].stock);
            });
        } else {
            collect(null, null, data.none);
        }

        return variants;
    }

    var variants = soldOutVariants();
    if (variants.length === 0) {
        return;
    }

    variants.forEach(function (variant) {
        var option = document.createElement('option');
        option.value = variant.value;
        option.textContent = variant.label;
        select.appendChild(option);
    });

    box.hidden = false;

    toggle.addEventListener('click', function () {
        var open = form.hidden;
        form.hidden = !open;
        toggle.setAttribute('aria-expanded', String(open));
    });

    function show(text, ok) {
        message.textContent = text;
        message.classList.toggle('success', ok);
        message.classList.toggle('error', !ok);
    }

    form.addEventListener('submit', function (event) {
        event.preventDefault();
        show('', true);

        var submit = form.querySelector('button[type="submit"]');
        submit.disabled = true;

        fetch(form.action, { method: 'POST', body: new FormData(form) })
            .then(function (response) {
                if (!response.ok) {
                    throw new Error('request failed');
                }
                return response.json();
            })
            .then(function (result) {
                show(result.message, result.ok);
            })
            .catch(function () {
                show('Something went wrong. Please try again.', false);
            })
            .finally(function () {
                submit.disabled = false;
            });
    });
});
