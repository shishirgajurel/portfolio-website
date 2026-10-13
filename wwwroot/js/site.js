(function () {
  var root = document.documentElement;
  root.classList.add('js');
  try {
    var saved = localStorage.getItem('theme');
    if (saved) root.setAttribute('data-theme', saved);
  } catch (e) {}

  document.addEventListener('DOMContentLoaded', function () {
    var btn = document.getElementById('theme');
    if (btn) {
      btn.addEventListener('click', function () {
        var current = root.getAttribute('data-theme');
        var dark = current === 'dark' || (!current && matchMedia('(prefers-color-scheme: dark)').matches);
        var next = dark ? 'light' : 'dark';
        root.setAttribute('data-theme', next);
        try { localStorage.setItem('theme', next); } catch (e) {}
      });
    }

    // Fade-in on scroll
    var els = document.querySelectorAll('.reveal');
    if ('IntersectionObserver' in window) {
      var io = new IntersectionObserver(function (entries) {
        entries.forEach(function (e) {
          if (e.isIntersecting) { e.target.classList.add('in'); io.unobserve(e.target); }
        });
      }, { threshold: 0.12 });
      els.forEach(function (el) { io.observe(el); });
    } else {
      els.forEach(function (el) { el.classList.add('in'); });
    }
  });

  // Confirm dialogs for forms marked with data-confirm (e.g. delete buttons)
  document.addEventListener('submit', function (e) {
    var msg = e.target && e.target.dataset ? e.target.dataset.confirm : null;
    if (msg && !confirm(msg)) e.preventDefault();
  });
})();
