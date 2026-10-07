// Read-only map of an order's delivery pin on the admin order page. The coordinates come from data-lat / data-lng.
document.addEventListener('DOMContentLoaded', function () {
    var mapEl = document.getElementById('order-map');
    if (!mapEl) {
        return;
    }

    var lat = parseFloat(mapEl.dataset.lat);
    var lng = parseFloat(mapEl.dataset.lng);
    if (isNaN(lat) || isNaN(lng)) {
        return;
    }

    var map = L.map('order-map', { dragging: false, scrollWheelZoom: false }).setView([lat, lng], 15);
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        maxZoom: 19,
        attribution: '&copy; OpenStreetMap contributors'
    }).addTo(map);

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
    L.marker([lat, lng], { icon: pinIcon }).addTo(map);
    setTimeout(function () { map.invalidateSize(); }, 200);
});
