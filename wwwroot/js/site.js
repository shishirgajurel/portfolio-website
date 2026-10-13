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
      // threshold 0 (not a percentage) so very tall sections, like a long repo list, still appear.
      var io = new IntersectionObserver(function (entries) {
        entries.forEach(function (e) {
          if (e.isIntersecting) { e.target.classList.add('in'); io.unobserve(e.target); }
        });
      }, { threshold: 0, rootMargin: '0px 0px -8% 0px' });
      els.forEach(function (el) { io.observe(el); });
    } else {
      els.forEach(function (el) { el.classList.add('in'); });
    }
  });

  // Live GitHub repositories: replaces the list baked into the page with the current one,
  // so a new repo appears without re-publishing the site. Keeps the baked list if GitHub fails.
  document.addEventListener('DOMContentLoaded', function () {
    var box = document.getElementById('gh-repos');
    if (!box || !window.fetch) return;
    var user = box.getAttribute('data-user');
    var featured = {};
    try { featured = JSON.parse(box.getAttribute('data-featured') || '{}'); } catch (e) {}

    var key = 'gh-repos:' + user, cached = null;
    try {
      var raw = sessionStorage.getItem(key);
      if (raw) { cached = JSON.parse(raw); if (Date.now() - cached.t > 10 * 60 * 1000) cached = null; }
    } catch (e) {}

    if (cached) { render(cached.repos); return; }
    fetch('https://api.github.com/users/' + encodeURIComponent(user) + '/repos?per_page=100&sort=pushed', {
      headers: { 'Accept': 'application/vnd.github+json' }
    })
      .then(function (r) { return r.ok ? r.json() : null; })
      .then(function (repos) {
        if (!Array.isArray(repos)) return;
        try { sessionStorage.setItem(key, JSON.stringify({ t: Date.now(), repos: repos })); } catch (e) {}
        render(repos);
      })
      .catch(function () {});

    // Same rules as Services/GitHubRepos.cs and IndexModel (featured first)
    function render(repos) {
      var lower = user.toLowerCase();
      var list = repos.filter(function (r) {
        var n = r.name.toLowerCase();
        return !r.fork && n !== lower && n !== lower + '.github.io';
      }).sort(function (a, b) {
        var fa = !!featured[a.html_url.replace(/\/$/, '').toLowerCase()];
        var fb = !!featured[b.html_url.replace(/\/$/, '').toLowerCase()];
        if (fa !== fb) return fa ? -1 : 1;
        if (a.archived !== b.archived) return a.archived ? 1 : -1;
        return new Date(b.pushed_at) - new Date(a.pushed_at);
      });
      if (!list.length) return;

      var frag = document.createDocumentFragment();
      list.forEach(function (r) {
        var page = featured[r.html_url.replace(/\/$/, '').toLowerCase()];
        var a = el('a', 'card repo');
        a.href = page || r.html_url;
        if (!page) { a.target = '_blank'; a.rel = 'noopener noreferrer'; }

        var top = el('div', 'repo-top');
        top.appendChild(el('h4', null, r.name));
        if (page) top.appendChild(el('span', 'tag featured', 'Featured'));
        if (r.archived) top.appendChild(el('span', 'tag', 'Archived'));
        a.appendChild(top);
        a.appendChild(el('p', null, r.description || 'No description yet.'));

        var meta = el('div', 'repo-meta');
        if (r.language) { var l = el('span'); l.appendChild(el('i', 'lang')); l.appendChild(document.createTextNode(r.language)); meta.appendChild(l); }
        meta.appendChild(el('span', null, 'Updated ' + new Date(r.pushed_at).toLocaleDateString('en', { month: 'short', year: 'numeric' })));
        a.appendChild(meta);
        frag.appendChild(a);
      });
      box.replaceChildren(frag);
    }

    function el(tag, cls, text) {
      var n = document.createElement(tag);
      if (cls) n.className = cls;
      if (text != null) n.textContent = text;
      return n;
    }
  });

  // Confirm dialogs for forms marked with data-confirm (e.g. delete buttons)
  document.addEventListener('submit', function (e) {
    var msg = e.target && e.target.dataset ? e.target.dataset.confirm : null;
    if (msg && !confirm(msg)) e.preventDefault();
  });
})();
