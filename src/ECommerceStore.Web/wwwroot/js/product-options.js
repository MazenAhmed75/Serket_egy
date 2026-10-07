// Product page options: size availability per colour, colour photos, size-chart switching and
// the quantity limit. Stock numbers come from the JSON block rendered by the page
// (<script type="application/json" id="stock-data">), so there is no extra request.
document.addEventListener('DOMContentLoaded', function () {
    var form = document.querySelector('.order-form');
    var dataEl = document.getElementById('stock-data');
    if (!form || !dataEl) {
        return;
    }

    var data;
    try {
        data = JSON.parse(dataEl.textContent);
    } catch (e) {
        return;
    }

    var colorInput = form.querySelector('[data-role="color-input"]');
    var swatches = form.querySelectorAll('.swatch');
    var statusEl = form.querySelector('[data-role="color-status"]');
    var sizeInputs = form.querySelectorAll('input[name="size"]');
    var genderInputs = form.querySelectorAll('input[name="gender"]');
    var qtyInput = form.querySelector('input[name="quantity"]');
    var submitButton = form.querySelector('button[type="submit"]');
    var stockNote = document.getElementById('stockNote');
    var stockNoteText = stockNote ? stockNote.querySelector('[data-role="stock-note-text"]') : null;
    var mainImage = document.getElementById('mainProductImage');
    var thumbs = document.querySelectorAll('.gallery-thumb');
    var defaultImage = mainImage ? mainImage.getAttribute('src') : null;

    function selectedColorId() {
        return colorInput ? colorInput.value : '';
    }

    function selectedSize() {
        var checked = form.querySelector('input[name="size"]:checked');
        return checked ? checked.value : null;
    }

    // Units available for a size: the chosen colour's stock, or - before a colour is chosen -
    // the best any colour has (so a size is only crossed out when no colour has it).
    function unitsFor(size) {
        if (!data.hasColors) {
            return data.none[size] || 0;
        }

        var id = selectedColorId();
        if (id) {
            var color = data.colors[id];
            return color ? (color.stock[size] || 0) : 0;
        }

        var best = 0;
        Object.keys(data.colors).forEach(function (key) {
            best = Math.max(best, data.colors[key].stock[size] || 0);
        });
        return best;
    }

    // Cross out sold-out sizes and make sure the checked size is one that can be bought.
    function refreshSizes() {
        var firstAvailable = null;

        sizeInputs.forEach(function (input) {
            var available = unitsFor(input.value) > 0;
            var pill = input.closest('.size-pill');
            pill.classList.toggle('sold-out', !available);
            pill.title = available ? '' : 'Out of stock';
            input.disabled = !available;
            if (available && !firstAvailable) {
                firstAvailable = input;
            }
        });

        var current = form.querySelector('input[name="size"]:checked');
        if (!current || current.disabled) {
            if (current) {
                current.checked = false;
            }
            if (firstAvailable) {
                firstAvailable.checked = true;
                firstAvailable.dispatchEvent(new Event('change', { bubbles: true }));
            }
        }
    }

    function updateQuantityAndButton() {
        var size = selectedSize();
        var units = size ? unitsFor(size) : 0;

        if (qtyInput) {
            qtyInput.max = Math.max(units, 1);
            var current = parseInt(qtyInput.value, 10) || 1;
            if (units > 0 && current > units) {
                qtyInput.value = units;
            } else if (current < 1) {
                qtyInput.value = 1;
            }
            qtyInput.dispatchEvent(new Event('input'));
        }

        if (stockNote && stockNoteText) {
            var showNote = size && units > 0 && units <= 5 && (!data.hasColors || selectedColorId());
            stockNote.hidden = !showNote;
            if (showNote) {
                stockNoteText.textContent = 'Only ' + units + ' left in this size';
            }
        }

        if (submitButton) {
            var colourOk = !data.hasColors || !!selectedColorId();
            submitButton.disabled = !(size && units > 0 && colourOk);
        }
    }

    // Show the photo of the set in the chosen colour (falls back to the main product photo).
    function updateImage() {
        if (!mainImage) {
            return;
        }

        var color = data.hasColors ? data.colors[selectedColorId()] : null;
        var src = color && color.image ? color.image : defaultImage;
        if (src && mainImage.getAttribute('src') !== src) {
            mainImage.setAttribute('src', src);
        }

        var showingDefault = !(color && color.image);
        thumbs.forEach(function (thumb, index) {
            thumb.classList.toggle('active', showingDefault && index === 0);
        });
    }

    function selectSwatch(swatch) {
        swatches.forEach(function (s) { s.classList.remove('selected'); });
        swatch.classList.add('selected');

        if (colorInput) {
            colorInput.value = swatch.dataset.colorId;
        }
        if (statusEl) {
            statusEl.textContent = 'Colour: ' + swatch.dataset.name;
            statusEl.classList.add('selected');
        }

        refreshSizes();
        updateQuantityAndButton();
        updateImage();
    }

    swatches.forEach(function (swatch) {
        swatch.addEventListener('click', function () {
            if (swatch.classList.contains('out-of-stock')) {
                return;
            }
            selectSwatch(swatch);
        });
    });

    sizeInputs.forEach(function (input) {
        input.addEventListener('change', updateQuantityAndButton);
    });

    // Men's / Women's fit: show the matching size chart.
    function showChartForGender() {
        var checked = form.querySelector('input[name="gender"]:checked');
        var gender = checked ? checked.value : 'Male';
        document.querySelectorAll('.size-chart-content').forEach(function (panel) {
            panel.hidden = panel.dataset.chart !== gender;
        });
    }

    genderInputs.forEach(function (input) {
        input.addEventListener('change', showChartForGender);
    });

    // Initial state.
    var preselected = colorInput && colorInput.value
        ? form.querySelector('.swatch[data-color-id="' + colorInput.value + '"]')
        : null;

    if (preselected && !preselected.classList.contains('out-of-stock')) {
        selectSwatch(preselected);
    } else {
        if (colorInput) {
            colorInput.value = '';
        }
        refreshSizes();
        updateQuantityAndButton();
    }

    showChartForGender();
});
