// Theme toggle and persistence
document.addEventListener('DOMContentLoaded', function () {
    var themeToggleBtn = document.getElementById('themeToggleBtn');
    if (!themeToggleBtn) return;

    function getPreferredTheme() {
        var savedTheme = localStorage.getItem('serket-theme');
        if (savedTheme) {
            return savedTheme;
        }
        return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
    }

    function setTheme(theme) {
        document.documentElement.setAttribute('data-theme', theme);
        localStorage.setItem('serket-theme', theme);
    }

    themeToggleBtn.addEventListener('click', function () {
        var current = document.documentElement.getAttribute('data-theme') || getPreferredTheme();
        var next = current === 'dark' ? 'light' : 'dark';
        setTheme(next);
    });

    // Listen for OS system theme changes if user hasn't explicitly set a preference
    window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', function (e) {
        if (!localStorage.getItem('serket-theme')) {
            setTheme(e.matches ? 'dark' : 'light');
        }
    });

    // Expandable Size Chart Accordion logic (if present on the page)
    var sizeChartToggle = document.getElementById('sizeChartToggle');
    var sizeChartDropdown = document.getElementById('sizeChartDropdown');

    if (sizeChartToggle && sizeChartDropdown) {
        sizeChartToggle.addEventListener('click', function (e) {
            e.preventDefault();
            var isExpanded = sizeChartToggle.getAttribute('aria-expanded') === 'true';
            var nextState = !isExpanded;
            sizeChartToggle.setAttribute('aria-expanded', String(nextState));
            sizeChartDropdown.hidden = isExpanded;
            sizeChartToggle.classList.toggle('active', nextState);
        });
    }

    // Keep size pill selection highlighted and synced with size table rows if applicable
    var sizePills = document.querySelectorAll('.size-pill input[type="radio"]');
    var tableRows = document.querySelectorAll('.size-table tbody tr');

    function highlightTableRow(selectedSize) {
        if (!tableRows.length) return;
        tableRows.forEach(function (row) {
            var cell = row.querySelector('td strong');
            if (cell && cell.textContent.trim() === selectedSize) {
                row.classList.add('highlighted');
            } else {
                row.classList.remove('highlighted');
            }
        });
    }

    sizePills.forEach(function (pill) {
        pill.addEventListener('change', function () {
            if (this.checked) {
                highlightTableRow(this.value);
            }
        });
    });
});
