/* FinTrack — comportamento global da interface */
(function () {
    'use strict';

    const THEME_KEY = 'fintrack-theme';
    const SIDEBAR_KEY = 'fintrack-sidebar-collapsed';

    function cookie(name, value, days) {
        if (value === null) {
            document.cookie = `${name}=; Path=/; Max-Age=0; SameSite=Lax`;
            return;
        }
        const expires = new Date(Date.now() + days * 864e5).toUTCString();
        document.cookie = `${name}=${encodeURIComponent(value)}; expires=${expires}; Path=/; SameSite=Lax`;
    }

    function initialTheme() {
        return document.documentElement.getAttribute('data-theme') || 'dark';
    }

    function applyTheme(theme, persist) {
        document.documentElement.setAttribute('data-theme', theme);
        localStorage.setItem(THEME_KEY, theme);
        if (persist) cookie('fintrack-theme', theme, 90);
        document.querySelectorAll('[data-theme-icon]').forEach(icon => {
            const className = theme === 'dark' ? 'bi-moon-stars' : 'bi-sun';
            icon.className = `bi ${className}`;
        });
        window.dispatchEvent(new CustomEvent('ft:themechange', { detail: { theme } }));
    }

    function toggleTheme() {
        applyTheme(initialTheme() === 'dark' ? 'light' : 'dark', true);
    }

    function toggleSidebar() {
        const sidebar = document.getElementById('sidebar');
        if (!sidebar) return;
        if (window.innerWidth <= 991) {
            sidebar.classList.toggle('mobile-open');
            document.getElementById('sidebarOverlay')?.classList.toggle('show');
        } else {
            sidebar.classList.toggle('collapsed');
            cookie(SIDEBAR_KEY, sidebar.classList.contains('collapsed') ? '1' : '0', 90);
            window.dispatchEvent(new Event('resize'));
        }
    }

    function closeMobileSidebar() {
        document.getElementById('sidebar')?.classList.remove('mobile-open');
        document.getElementById('sidebarOverlay')?.classList.remove('show');
    }

    function showToast(message, type, title) {
        const container = document.getElementById('toastContainer');
        if (!container) return;
        const icons = { success: 'bi-check-circle-fill', error: 'bi-x-circle-fill', warning: 'bi-exclamation-triangle-fill', info: 'bi-info-circle-fill' };
        const kind = type || 'info';
        const el = document.createElement('div');
        el.className = `toast-custom ${kind}`;
        el.innerHTML = `<i class="bi ${icons[kind] || icons.info}" style="color:var(--${kind === 'error' ? 'danger' : kind})"></i>
            <div><div class="toast-title">${title || 'FinTrack'}</div><div class="toast-text">${message}</div></div>`;
        container.appendChild(el);
        setTimeout(() => {
            el.style.opacity = '0';
            el.style.transform = 'translateX(24px)';
            setTimeout(() => el.remove(), 250);
        }, 4200);
    }

    function loader(show) {
        document.getElementById('globalLoader')?.classList.toggle('show', !!show);
    }

    function csrfToken() {
        const input = document.querySelector('input[name="__RequestVerificationToken"]');
        return input ? input.value : '';
    }

    async function confirmDelete(form, message) {
        if (typeof Swal === 'undefined') return form.submit();
        const result = await Swal.fire({
            title: 'Confirmar exclusão?',
            text: message || 'Esta ação não pode ser desfeita.',
            icon: 'warning',
            showCancelButton: true,
            confirmButtonText: 'Sim, excluir',
            cancelButtonText: 'Cancelar',
            confirmButtonColor: '#f43f5e',
            cancelButtonColor: '#64748b',
            background: getComputedStyle(document.documentElement).getPropertyValue('--surface'),
            color: getComputedStyle(document.documentElement).getPropertyValue('--text'),
            customClass: { popup: 'swal-dark' }
        });
        if (result.isConfirmed) {
            loader(true);
            form.submit();
        }
    }

    async function loadNotifications() {
        const list = document.getElementById('notificationList');
        if (!list) return;
        try {
            const response = await fetch('/Notifications/Recent', { headers: { 'X-Requested-With': 'XMLHttpRequest' } });
            if (!response.ok) return;
            const items = await response.json();
            const esc = value => String(value ?? '').replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
            list.innerHTML = items.length
                ? items.map(n => `<div class="notification-item ${n.isRead ? '' : 'unread'}" data-id="${esc(n.id)}" data-url="${esc(n.actionUrl || '')}">
                    <div class="notification-icon bg-${esc(n.typeClass)}-soft"><i class="bi ${esc(n.icon)}"></i></div>
                    <div style="flex:1;min-width:0">
                        <div class="notification-title">${esc(n.title)}</div>
                        <div class="notification-message">${esc(n.message)}</div>
                        <div class="notification-time">${esc(n.timeAgo)}</div>
                    </div></div>`).join('')
                : '<div class="empty-state" style="padding:20px"><div class="empty-icon"><i class="bi bi-bell-slash"></i></div><h6>Sem notificações</h6></div>';

            list.querySelectorAll('.notification-item').forEach(item => {
                item.addEventListener('click', async () => {
                    const id = item.dataset.id;
                    await fetch('/Notifications/MarkRead', {
                        method: 'POST',
                        headers: { 'Content-Type': 'application/x-www-form-urlencoded', 'RequestVerificationToken': csrfToken() },
                        body: `id=${id}`
                    });
                    item.classList.remove('unread');
                    const badge = document.getElementById('notificationBadge');
                    if (badge) badge.textContent = Math.max(0, parseInt(badge.textContent || '0', 10) - 1);
                    const remaining = document.querySelectorAll('#notificationList .notification-item.unread').length;
                    if (remaining === 0) document.getElementById('notificationDot')?.remove();
                    if (item.dataset.url) window.location.href = item.dataset.url;
                });
            });
        } catch (e) {
            /* A central de notificações é opcional em páginas públicas. */
        }
    }

    function initPeriodTabs() {
        document.querySelectorAll('.period-tab').forEach(tab => {
            tab.addEventListener('click', () => {
                const form = document.getElementById('periodForm');
                if (!form) return;
                const input = form.querySelector('[name="preset"]');
                if (input) input.value = tab.dataset.preset;
                document.querySelectorAll('.period-tab').forEach(t => t.classList.remove('active'));
                tab.classList.add('active');
                if (tab.dataset.preset === 'Custom') {
                    document.getElementById('customDates')?.classList.remove('d-none');
                    document.getElementById('customFrom')?.focus();
                    return;
                }
                loader(true);
                form.submit();
            });
        });
    }

    function initAutoLoader() {
        document.querySelectorAll('form.auto-loader').forEach(form => {
            form.addEventListener('submit', () => {
                if (!form.dataset.confirm) loader(true);
            });
        });
        document.querySelectorAll('[data-confirm]').forEach(element => {
            element.addEventListener('click', event => {
                event.preventDefault();
                const form = element.closest('form');
                if (form) confirmDelete(form, element.dataset.confirm);
            });
        });
    }

    function boot() {
        applyTheme(initialTheme(), false);

        if (localStorage.getItem(THEME_KEY) &&
            localStorage.getItem(THEME_KEY) !== document.documentElement.getAttribute('data-theme')) {
            applyTheme(localStorage.getItem(THEME_KEY), true);
        }

        const sidebar = document.getElementById('sidebar');
        if (sidebar && (localStorage.getItem(SIDEBAR_KEY) === '1' ||
            (localStorage.getItem(SIDEBAR_KEY) === null && document.cookie.includes('fintrack-sidebar-collapsed=1')))) {
            sidebar.classList.add('collapsed');
        }

        document.querySelectorAll('[data-action="theme"]').forEach(el => el.addEventListener('click', toggleTheme));
        document.querySelectorAll('[data-action="sidebar"]').forEach(el => el.addEventListener('click', toggleSidebar));
        document.getElementById('sidebarOverlay')?.addEventListener('click', closeMobileSidebar);

        if (window.bootstrap) {
            document.querySelectorAll('[data-bs-toggle="tooltip"]').forEach(el => new bootstrap.Tooltip(el));
            document.querySelectorAll('[data-bs-toggle="popover"]').forEach(el => new bootstrap.Popover(el));
        }

        document.querySelectorAll('#toastData .toast-message').forEach(el => {
            showToast(el.dataset.message || el.textContent, el.dataset.type, el.dataset.title);
        });

        initAutoLoader();
        initPeriodTabs();
        loadNotifications();
    }

    window.FinTrack = { showToast, loader, toggleTheme, confirmDelete, csrfToken };
    document.addEventListener('DOMContentLoaded', boot);
})();
