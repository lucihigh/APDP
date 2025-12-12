// Please see docs at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification

// Lightweight app-shell navigation so the navbar
// does not re-render on every page change.
(function () {
  const mainSelector = '#main';

  function updateActiveNav() {
    const nav = document.querySelector('[data-app-nav]');
    if (!nav) return;
    const links = nav.querySelectorAll('a');
    links.forEach(a => {
      if (a.pathname === window.location.pathname) {
        a.classList.add('active');
      } else {
        a.classList.remove('active');
      }
    });
  }

  async function navigate(url, pushState = true) {
    const main = document.querySelector(mainSelector);
    if (!main) {
      window.location.href = url;
      return;
    }

    const fadeDuration = 150;
    main.classList.add('page-transition-out');
    main.setAttribute('data-loading', 'true');

    await new Promise(resolve => setTimeout(resolve, fadeDuration));

    try {
      const response = await fetch(url, {
        headers: { 'X-Requested-With': 'XMLHttpRequest' }
      });
      if (!response.ok) {
        throw new Error('Navigation failed');
      }

      const html = await response.text();
      const parser = new DOMParser();
      const doc = parser.parseFromString(html, 'text/html');
      const newMain = doc.querySelector(mainSelector);
      const title = doc.querySelector('title');

      if (!newMain) {
        // Fallback to full navigation if layout is different
        window.location.href = url;
        return;
      }

      main.innerHTML = newMain.innerHTML;
      initPageScripts(main);

      main.classList.remove('page-transition-out');
      main.classList.add('page-transition-in');
      requestAnimationFrame(function () {
        main.classList.remove('page-transition-in');
      });

      if (title) {
        document.title = title.textContent || document.title;
      }

      if (pushState) {
        window.history.pushState({ url: url }, '', url);
      }

      // Re-apply validation for dynamically loaded forms
      if (window.jQuery && window.jQuery.validator && window.jQuery.validator.unobtrusive) {
        window.jQuery.validator.unobtrusive.parse(document);
      }

      updateActiveNav();
    } catch (e) {
      window.location.href = url;
    } finally {
      main.removeAttribute('data-loading');
    }
  }

  function initClassListFilter(root) {
    const input = root.querySelector('[data-class-search]');
    const rows = Array.from(root.querySelectorAll('[data-class-row]'));
    const empty = root.querySelector('[data-class-empty]');
    if (!input || rows.length === 0) return;
    if (input.dataset.bound === '1') return;
    input.dataset.bound = '1';

    const filter = () => {
      const term = (input.value || '').trim().toLowerCase();
      let visible = 0;
      rows.forEach(row => {
        const haystack = row.dataset.search || '';
        const match = term === '' || haystack.includes(term);
        row.style.display = match ? '' : 'none';
        if (match) visible++;
      });
      if (empty) empty.style.display = visible === 0 ? '' : 'none';
    };

    const form = input.closest('form');
    if (form) {
      form.addEventListener('submit', e => e.preventDefault());
    }

    input.addEventListener('input', filter);
    filter();
  }

  function initCourseFilter(root) {
    const input = root.querySelector('[data-course-search]');
    const rows = Array.from(root.querySelectorAll('[data-course-row]'));
    const empty = root.querySelector('[data-course-empty]');
    if (!input || rows.length === 0) return;
    if (input.dataset.bound === '1') return;
    input.dataset.bound = '1';

    const filter = () => {
      const term = (input.value || '').trim().toLowerCase();
      let visible = 0;
      rows.forEach(row => {
        const haystack = row.dataset.search || '';
        const match = term === '' || haystack.includes(term);
        row.style.display = match ? '' : 'none';
        if (match) visible++;
      });
      if (empty) empty.style.display = visible === 0 ? '' : 'none';
    };

    const form = input.closest('form');
    if (form) form.addEventListener('submit', e => e.preventDefault());

    input.addEventListener('input', filter);
    filter();
  }

  function initPageScripts(root = document) {
    initClassListFilter(root);
    initCourseFilter(root);
  }

  function onClick(e) {
    if (e.defaultPrevented || e.button !== 0 || e.metaKey || e.ctrlKey || e.shiftKey || e.altKey) {
      return;
    }

    const anchor = e.target.closest('a');
    if (!anchor) return;

    const url = anchor.href;
    if (!url || anchor.target && anchor.target !== '_self') return;

    // Skip if explicitly disabled for shell navigation
    if (anchor.getAttribute('data-shell') === 'off') return;

    // Only handle same-origin links
    if (!url.startsWith(window.location.origin)) return;

    // Skip in-page anchors
    var href = anchor.getAttribute('href');
    if (href && href.startsWith('#')) return;

    // Skip download links
    if (anchor.hasAttribute('download')) return;

    e.preventDefault();
    navigate(url, true);
  }

  window.addEventListener('click', onClick);
  window.addEventListener('popstate', function (e) {
    const url = (e.state && e.state.url) || window.location.href;
    navigate(url, false);
  });

  // Set correct active state on initial load
  updateActiveNav();
  initPageScripts(document);
})();
