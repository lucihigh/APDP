// Please see docs at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification

// Lightweight app-shell navigation so the navbar
// does not re-render on every page change.
(function () {
  const mainSelector = '#main';

  function closeMobileNavbar() {
    const collapseEl = document.querySelector('.navbar-collapse.show');
    if (!collapseEl) return;

    try {
      if (window.bootstrap && window.bootstrap.Collapse) {
        window.bootstrap.Collapse.getOrCreateInstance(collapseEl, { toggle: false }).hide();
        return;
      }
    } catch (e) {
      // fall back below
    }

    collapseEl.classList.remove('show');
  }

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
        headers: { 'X-Requested-With': 'XMLHttpRequest' },
        cache: 'no-store'
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
    if (typeof window.initReports === 'function') {
      window.initReports(root);
    }
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

    // Close navbar on mobile after selecting a link
    if (anchor.closest('.navbar')) {
      closeMobileNavbar();
    }
    navigate(url, true);
  }

  window.addEventListener('click', onClick);
  window.addEventListener('submit', function (e) {
    // Close navbar when submitting forms in the navbar (e.g. Logout)
    if (e.target && e.target.closest && e.target.closest('.navbar')) {
      closeMobileNavbar();
    }
  }, true);
  window.addEventListener('popstate', function (e) {
    const url = (e.state && e.state.url) || window.location.href;
    navigate(url, false);
  });

  // Set correct active state on initial load
  updateActiveNav();
  initPageScripts(document);
})();

// Reports analytics (grade distribution, submissions, programs)
(function () {
  const charts = { grade: null, submission: null, program: null };

  function ensureChartJs() {
    if (window.Chart) return Promise.resolve();
    return new Promise((resolve, reject) => {
      const existing = document.querySelector('script[data-chartjs]');
      if (existing) {
        existing.addEventListener('load', () => resolve());
        existing.addEventListener('error', reject);
        return;
      }
      const script = document.createElement('script');
      script.src = 'https://cdn.jsdelivr.net/npm/chart.js@4.4.0/dist/chart.umd.min.js';
      script.async = true;
      script.dataset.chartjs = '1';
      script.onload = () => resolve();
      script.onerror = reject;
      document.head.appendChild(script);
    });
  }

  function destroyCharts() {
    Object.keys(charts).forEach(k => {
      if (charts[k]) {
        charts[k].destroy();
        charts[k] = null;
      }
    });
  }

  function initReports(root) {
    const courseSelect = root.querySelector('[data-report-course]');
    if (!courseSelect) return;
    const metricEls = {
      total: root.querySelector('[data-metric="total"]'),
      graded: root.querySelector('[data-metric="graded"]'),
      pending: root.querySelector('[data-metric="pending"]'),
      avg: root.querySelector('[data-metric="avg"]')
    };

    function setMetric(key, value) {
      if (metricEls[key]) metricEls[key].textContent = value ?? '—';
    }

    function renderCharts(data) {
      const { distribution, graded, pending, programs } = data;
      const gradeCtx = root.querySelector('#gradeChart');
      const submissionCtx = root.querySelector('#submissionChart');
      const programCtx = root.querySelector('#programChart');
      destroyCharts();
      charts.grade = new Chart(gradeCtx, {
        type: 'bar',
        data: {
          labels: distribution.map(d => d.label),
          datasets: [{
            label: 'Students',
            data: distribution.map(d => d.count),
            backgroundColor: '#22c55e'
          }]
        },
        options: {
          scales: { y: { beginAtZero: true, ticks: { precision: 0 } } }
        }
      });
      charts.submission = new Chart(submissionCtx, {
        type: 'doughnut',
        data: {
          labels: ['Graded', 'Pending'],
          datasets: [{
            data: [graded, pending],
            backgroundColor: ['#22c55e', '#6b7280']
          }]
        },
        options: {
          plugins: { legend: { position: 'bottom' } }
        }
      });
      charts.program = new Chart(programCtx, {
        type: 'bar',
        data: {
          labels: programs.map(p => p.label),
          datasets: [{
            label: 'Students',
            data: programs.map(p => p.count),
            backgroundColor: '#38bdf8'
          }]
        },
        options: {
          scales: { y: { beginAtZero: true, ticks: { precision: 0 } } }
        }
      });
    }

    async function loadMetrics(courseId) {
      if (!courseId) {
        setMetric('total', '—');
        setMetric('graded', '—');
        setMetric('pending', '—');
        setMetric('avg', '—');
        destroyCharts();
        return;
      }
      try {
        await ensureChartJs();
        const res = await fetch(`/Reports/CourseMetrics?courseId=${courseId}`, {
          headers: { 'X-Requested-With': 'XMLHttpRequest' },
          cache: 'no-store'
        });
        if (!res.ok) throw new Error('Failed to load metrics');
        const data = await res.json();
        setMetric('total', data.total);
        setMetric('graded', data.graded);
        setMetric('pending', data.pending);
        setMetric('avg', data.averageGrade !== null ? Number(data.averageGrade).toFixed(2) : '—');
        renderCharts(data);
      } catch (err) {
        console.error(err);
      }
    }

    courseSelect.addEventListener('change', e => loadMetrics(e.target.value));
  }

  // expose to main init
  window.initReports = initReports;
})();
