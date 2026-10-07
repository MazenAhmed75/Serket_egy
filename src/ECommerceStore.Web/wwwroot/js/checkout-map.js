document.addEventListener('DOMContentLoaded', function () {
    var mapEl = document.getElementById('delivery-map');
    if (!mapEl) {
        return;
    }

    var latInput = document.getElementById('Input_Latitude');
    var lngInput = document.getElementById('Input_Longitude');
    var locationStatus = document.getElementById('location-status');

    // Default view: Cairo, Egypt. Change this if the store's service area is elsewhere.
    var defaultLat = 30.0444;
    var defaultLng = 31.2357;

    var startLat = parseFloat(latInput.value) || defaultLat;
    var startLng = parseFloat(lngInput.value) || defaultLng;

    var map = L.map('delivery-map').setView([startLat, startLng], 12);

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        maxZoom: 19,
        attribution: '&copy; OpenStreetMap contributors'
    }).addTo(map);

    // A self-contained SVG pin, drawn inline rather than Leaflet's default marker image
    // (which can silently fail to load from some CDNs, leaving no visible pin at all).
    var pinIcon = L.divIcon({
        className: 'delivery-pin',
        html:
            '<svg width="30" height="42" viewBox="0 0 28 40" xmlns="http://www.w3.org/2000/svg">' +
            '<path d="M14 0C6.3 0 0 6.3 0 14c0 10.5 14 26 14 26s14-15.5 14-26C28 6.3 21.7 0 14 0z" fill="#9C7C3F"/>' +
            '<circle cx="14" cy="14" r="5.5" fill="#F7F3EA"/>' +
            '</svg>',
        iconSize: [30, 42],
        iconAnchor: [15, 42]
    });

    var marker = null;

    function updateInputs(lat, lng) {
        latInput.value = lat.toFixed(6);
        lngInput.value = lng.toFixed(6);
    }

    function describeLocation(lat, lng) {
        if (!locationStatus) {
            return;
        }

        locationStatus.textContent = 'Looking up address…';
        locationStatus.classList.remove('selected');

        var url = 'https://nominatim.openstreetmap.org/reverse?format=jsonv2&lat=' + lat + '&lon=' + lng + '&zoom=18&addressdetails=1';

        fetch(url, { headers: { 'Accept': 'application/json' } })
            .then(function (response) { return response.ok ? response.json() : null; })
            .then(function (data) {
                var text = data && data.display_name
                    ? data.display_name
                    : ('Pinned at ' + lat.toFixed(5) + ', ' + lng.toFixed(5));
                locationStatus.textContent = 'Location selected: ' + text;
                locationStatus.classList.add('selected');
            })
            .catch(function () {
                // Nominatim is a shared public service — if it's unreachable or rate-limited,
                // fall back to plain coordinates rather than leaving the confirmation blank.
                locationStatus.textContent = 'Location selected: ' + lat.toFixed(5) + ', ' + lng.toFixed(5);
                locationStatus.classList.add('selected');
            });
    }

    function setMarker(lat, lng) {
        if (marker) {
            marker.setLatLng([lat, lng]);
        } else {
            marker = L.marker([lat, lng], { draggable: true, icon: pinIcon }).addTo(map);
            marker.on('dragend', function () {
                var pos = marker.getLatLng();
                updateInputs(pos.lat, pos.lng);
                describeLocation(pos.lat, pos.lng);
            });
        }
        updateInputs(lat, lng);
        describeLocation(lat, lng);
    }

    if (parseFloat(latInput.value) && parseFloat(lngInput.value)) {
        setMarker(startLat, startLng);
    }

    map.on('click', function (e) {
        setMarker(e.latlng.lat, e.latlng.lng);
    });

    // "Use my current location": ask the browser for the device's position and drop the pin there.
    // Needs HTTPS (or localhost) and the customer's permission; the pin can still be dragged afterwards.
    var locateButton = document.getElementById('use-my-location');
    var locateError = document.getElementById('locate-error');

    function showLocateError(text) {
        if (!locateError) {
            return;
        }
        locateError.textContent = text || '';
        locateError.hidden = !text;
    }

    if (locateButton) {
        locateButton.addEventListener('click', function () {
            showLocateError('');

            if (!navigator.geolocation) {
                showLocateError('Your browser can\'t share your location. Please tap the map instead.');
                return;
            }

            locateButton.disabled = true;
            locateButton.classList.add('loading');

            navigator.geolocation.getCurrentPosition(
                function (position) {
                    locateButton.disabled = false;
                    locateButton.classList.remove('loading');
                    var lat = position.coords.latitude;
                    var lng = position.coords.longitude;
                    map.setView([lat, lng], 17);
                    setMarker(lat, lng);
                },
                function (error) {
                    locateButton.disabled = false;
                    locateButton.classList.remove('loading');
                    showLocateError(error.code === 1
                        ? 'Location permission was denied. Allow it in your browser settings, or tap the map instead.'
                        : 'We couldn\'t find your location. Please tap the map instead.');
                },
                { enableHighAccuracy: true, timeout: 15000, maximumAge: 60000 }
            );
        });
    }

    // Payment method -> show/hide the receipt upload section and its matching instructions.
    var receiptSection = document.getElementById('receipt-section');
    var paymentRadios = document.querySelectorAll('input[name="Input.PaymentMethod"]');
    var hints = receiptSection ? receiptSection.querySelectorAll('[data-method]') : [];

    function updatePaymentUi() {
        var selected = document.querySelector('input[name="Input.PaymentMethod"]:checked');
        var value = selected ? selected.value : 'CashOnDelivery';
        var needsReceipt = value !== 'CashOnDelivery';

        if (receiptSection) {
            receiptSection.hidden = !needsReceipt;
        }

        hints.forEach(function (hint) {
            hint.style.display = hint.dataset.method === value ? '' : 'none';
        });
    }

    paymentRadios.forEach(function (radio) {
        radio.addEventListener('change', updatePaymentUi);
    });

    updatePaymentUi();

    // Give the map a nudge once it's actually visible/sized (fixes a common Leaflet
    // grey-tile issue when the container was hidden or resized after initial load).
    setTimeout(function () { map.invalidateSize(); }, 200);
});
