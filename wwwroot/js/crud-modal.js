/* FinTrack — auxiliar genérico de CRUD via modal (evita duplicação entre telas) */
(function () {
    'use strict';

    function setup(options) {
        const cfg = Object.assign({
            modalId: 'crudModal',
            containerId: 'crudFormContainer',
            formId: 'crudForm',
            newSelector: '[data-action="new"]',
            editSelector: '[data-action="edit"]',
            deleteSelector: '[data-action="delete"]',
            formUrl: '', saveUrl: '', deleteUrl: '',
            extraSearch: () => ({}),
            beforeOpen: () => { },
            afterLoad: () => { }
        }, options);

        const modalEl = document.getElementById(cfg.modalId);
        if (!modalEl) return;
        const modal = bootstrap.Modal.getOrCreateInstance(modalEl);
        const container = document.getElementById(cfg.containerId);

        function loading() {
            container.innerHTML = '<div class="modal-body text-center py-5"><div class="spinner mx-auto"></div><p class="text-muted mt-3">Carregando...</p></div>';
        }

        function open(id, extra) {
            cfg.beforeOpen(id);
            loading();
            modal.show();
            const params = new URLSearchParams(Object.assign({}, extra || {}, id ? { id } : {}));
            fetch(`${cfg.formUrl}?${params}`, { headers: { 'X-Requested-With': 'XMLHttpRequest' } })
                .then(r => r.ok ? r.text() : Promise.reject())
                .then(html => { container.innerHTML = html; wire(); cfg.afterLoad(container); })
                .catch(() => { container.innerHTML = '<div class="alert alert-danger m-4">Não foi possível carregar o formulário.</div>'; });
        }

        function wire() {
            const form = container.querySelector('#' + cfg.formId);
            if (!form) return;
            form.addEventListener('submit', async e => {
                e.preventDefault();
                FinTrack.loader(true);
                try {
                    const response = await fetch(cfg.saveUrl, { method: 'POST', body: new FormData(form) });
                    const result = await response.json();
                    FinTrack.loader(false);
                    if (result.success) { modal.hide(); FinTrack.showToast(result.message, 'success'); setTimeout(() => location.reload(), 600); }
                    else {
                        const box = container.querySelector('#formErrors');
                        if (box) box.innerHTML = (result.errors || [result.message]).map(m => `<div class="alert alert-danger py-2 small mb-1">${m}</div>`).join('');
                        else FinTrack.showToast(result.message, 'error');
                    }
                } catch (err) { FinTrack.loader(false); FinTrack.showToast('Erro ao salvar.', 'error'); }
            });
        }

        document.querySelectorAll(cfg.newSelector).forEach(el => el.addEventListener('click', () => open(null, cfg.extraSearch(el))));
        document.querySelectorAll(cfg.editSelector).forEach(el => el.addEventListener('click', () => open(el.dataset.id, cfg.extraSearch(el))));
        document.querySelectorAll(cfg.deleteSelector).forEach(el => el.addEventListener('click', async () => {
            const message = el.dataset.confirm || 'Esta ação não pode ser desfeita.';
            let confirmed = false;
            if (typeof Swal !== 'undefined') {
                const r = await Swal.fire({
                    title: 'Confirmar exclusão?', text: message, icon: 'warning',
                    showCancelButton: true, confirmButtonText: 'Sim, excluir', cancelButtonText: 'Cancelar',
                    confirmButtonColor: '#f43f5e', cancelButtonColor: '#64748b',
                    background: getComputedStyle(document.documentElement).getPropertyValue('--surface'),
                    color: getComputedStyle(document.documentElement).getPropertyValue('--text')
                });
                confirmed = r.isConfirmed;
            } else {
                confirmed = confirm(message);
            }
            if (!confirmed) return;
            FinTrack.loader(true);
            try {
                const res = await fetch(cfg.deleteUrl, {
                    method: 'POST',
                    headers: { 'RequestVerificationToken': FinTrack.csrfToken() },
                    body: new URLSearchParams({ id: el.dataset.id })
                });
                const result = await res.json();
                FinTrack.loader(false);
                FinTrack.showToast(result.message, result.success ? 'success' : 'error');
                if (result.success) setTimeout(() => location.reload(), 600);
            } catch (err) { FinTrack.loader(false); FinTrack.showToast('Erro ao excluir.', 'error'); }
        }));

        wire();
        return { open };
    }

    window.FinTrackCrud = { setup };
})();
