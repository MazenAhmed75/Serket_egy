// Thumbnail gallery on the product page: clicking a thumbnail swaps the main photo.
document.addEventListener('DOMContentLoaded', function () {
    var main = document.getElementById('mainProductImage');
    var thumbs = document.querySelectorAll('.gallery-thumb');
    if (!main || thumbs.length === 0) {
        return;
    }

    thumbs.forEach(function (thumb) {
        thumb.addEventListener('click', function () {
            main.src = thumb.dataset.full;
            thumbs.forEach(function (t) { t.classList.remove('active'); });
            thumb.classList.add('active');
        });
    });
});
