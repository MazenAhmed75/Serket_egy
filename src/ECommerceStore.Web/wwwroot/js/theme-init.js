// Runs before the page is drawn so the saved (or system) light/dark theme is applied without a flash.
(function () {
    var saved = localStorage.getItem('serket-theme');
    if (saved === 'dark' || (!saved && window.matchMedia('(prefers-color-scheme: dark)').matches)) {
        document.documentElement.setAttribute('data-theme', 'dark');
    } else {
        document.documentElement.setAttribute('data-theme', 'light');
    }
})();
