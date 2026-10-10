// Promo code box on the checkout page. "Apply" asks the server to check the code and work out
// the price lines; the page only displays what comes back. The server checks the code again
// when the order is placed, so nothing here can be used to change the price.
document.addEventListener('DOMContentLoaded', function () {
    var card = document.getElementById('promoCard');
    var input = document.getElementById('promoInput');
    var applyButton = document.getElementById('promoApply');
    var messageEl = document.getElementById('promoMessage');
    var hiddenCode = document.getElementById('Input_PromoCode');
    var form = document.querySelector('.checkout-form');

    if (!card || !input || !applyButton || !hiddenCode || !form) {
        return;
    }

    var tokenInput = form.querySelector('input[name="__RequestVerificationToken"]');

    var applied = false;

    function setText(id, text) {
        var el = document.getElementById(id);
        if (el) {
            el.textContent = text;
        }
    }

    function showMessage(text, isError) {
        messageEl.textContent = text || '';
        messageEl.classList.toggle('error', !!isError);
        messageEl.classList.toggle('success', !isError && !!text);
    }

    function render(result) {
        setText('pb-subtotal', result.subtotalText);
        setText('pb-shipping', result.shippingText);
        setText('pb-total', result.totalText);

        var shippingEl = document.getElementById('pb-shipping');
        if (shippingEl) {
            shippingEl.classList.toggle('free', result.freeShipping || /^free/i.test(result.shippingText));
        }

        var discountRow = document.getElementById('pb-discount-row');
        if (discountRow) {
            var showDiscount = result.valid && result.discount > 0;
            discountRow.hidden = !showDiscount;
            if (showDiscount) {
                setText('pb-promo-label', result.code + ' - ' + result.percentOff + '%');
                setText('pb-discount', result.discountText);
            }
        }
    }

    function setApplied(isApplied) {
        applied = isApplied;
        input.readOnly = isApplied;
        applyButton.textContent = isApplied ? 'Remove' : 'Apply';
    }

    function send(code) {
        var body = new FormData();
        // The server works out the price from the cart it already holds; only the code is sent.
        body.append('promoCode', code);

        var headers = {};
        if (tokenInput) {
            headers['RequestVerificationToken'] = tokenInput.value;
        }

        applyButton.disabled = true;
        return fetch('?handler=ApplyPromo', { method: 'POST', body: body, headers: headers })
            .then(function (response) {
                if (!response.ok) {
                    throw new Error('request failed');
                }
                return response.json();
            })
            .finally(function () {
                applyButton.disabled = false;
            });
    }

    applyButton.addEventListener('click', function () {
        if (applied) {
            // Remove the code: put the plain prices back.
            send('').then(function (result) {
                hiddenCode.value = '';
                input.value = '';
                setApplied(false);
                showMessage('', false);
                render(result);
            }).catch(function () {
                showMessage('Something went wrong. Please try again.', true);
            });
            return;
        }

        var code = input.value.trim();
        if (!code) {
            showMessage('Enter a promo code.', true);
            return;
        }

        send(code).then(function (result) {
            if (result.valid) {
                hiddenCode.value = result.code;
                input.value = result.code;
                setApplied(true);
                showMessage(result.message, false);
            } else {
                hiddenCode.value = '';
                setApplied(false);
                showMessage(result.message, true);
            }
            render(result);
        }).catch(function () {
            showMessage('Something went wrong. Please try again.', true);
        });
    });

    input.addEventListener('keydown', function (event) {
        if (event.key === 'Enter') {
            event.preventDefault();
            applyButton.click();
        }
    });

    // If the page was re-shown after a failed submit with a code still set, show it as applied.
    if (hiddenCode.value) {
        input.value = hiddenCode.value;
        setApplied(true);
    }
});
