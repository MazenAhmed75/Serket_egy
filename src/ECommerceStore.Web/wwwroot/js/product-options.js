// Product page options: size availability per colour, the photos of the chosen colour (main photo + its own gallery),
// size-chart switching and the quantity limit. Stock and photo lists come from the JSON block rendered by the page
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
    var submitButtons = form.querySelectorAll('button[type="submit"]');
    var stockNote = document.getElementById('stockNote');
    var stockNoteText = stockNote ? stockNote.querySelector('[data-role="stock-note-text"]') : null;
    var mainImage = document.getElementById('mainProductImage');
    var thumbsEl = document.getElementById('galleryThumbs');
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

    function updateQuantityAndButtons() {
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

        var colourOk = !data.hasColors || !!selectedColorId();
        var canBuy = !!(size && units > 0 && colourOk);
        submitButtons.forEach(function (button) {
            button.disabled = !canBuy;
        });
    }

    // ---- photos: the chosen colour's main photo and its own gallery -----------------------------------------
    function showPhoto(src, thumb) {
        if (mainImage && src && mainImage.getAttribute('src') !== src) {
            mainImage.setAttribute('src', src);
        }
        if (thumbsEl) {
            thumbsEl.querySelectorAll('.gallery-thumb').forEach(function (t) {
                t.classList.toggle('active', t === thumb);
            });
        }
    }

    function renderPhotos(photos) {
        if (!mainImage) {
            return;
        }

        var list = photos && photos.length ? photos : (defaultImage ? [defaultImage] : []);
        showPhoto(list[0], null);

        if (!thumbsEl) {
            return;
        }

        thumbsEl.textContent = '';
        thumbsEl.hidden = list.length < 2;
        if (list.length < 2) {
            return;
        }

        list.forEach(function (src, index) {
            var button = document.createElement('button');
            button.type = 'button';
            button.className = 'gallery-thumb' + (index === 0 ? ' active' : '');
            button.setAttribute('aria-label', 'Show photo ' + (index + 1) + ' of ' + list.length);

            var img = document.createElement('img');
            img.src = src;
            img.alt = '';
            img.loading = 'lazy';
            button.appendChild(img);

            button.addEventListener('click', function () {
                showPhoto(src, button);
            });
            thumbsEl.appendChild(button);
        });
    }

    function updatePhotos() {
        var color = data.hasColors ? data.colors[selectedColorId()] : null;
        renderPhotos(color ? color.photos : null);
    }

    function selectSwatch(swatch, userClicked) {
        swatches.forEach(function (s) { s.classList.remove('selected'); });
        swatch.classList.add('selected');

        if (colorInput) {
            colorInput.value = swatch.dataset.colorId;
        }
        if (statusEl) {
            statusEl.textContent = 'Colour: ' + swatch.dataset.name;
            statusEl.classList.add('selected');
        }

        // Choosing another colour starts again from one item.
        if (userClicked && qtyInput) {
            qtyInput.value = 1;
        }

        refreshSizes();
        updateQuantityAndButtons();
        updatePhotos();
    }

    swatches.forEach(function (swatch) {
        swatch.addEventListener('click', function () {
            if (swatch.classList.contains('out-of-stock')) {
                return;
            }
            selectSwatch(swatch, true);
        });
    });

    sizeInputs.forEach(function (input) {
        input.addEventListener('change', updateQuantityAndButtons);
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
        selectSwatch(preselected, false);
    } else {
        if (colorInput) {
            colorInput.value = '';
        }
        refreshSizes();
        updateQuantityAndButtons();
        updatePhotos();
    }

    showChartForGender();
});
