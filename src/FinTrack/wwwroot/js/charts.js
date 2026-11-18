/* FinTrack — integração com Chart.js e adaptação ao tema */
(function () {
    'use strict';

    const registry = new Map();
    const numberFormat = new Intl.NumberFormat('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });

    function cssVar(name) {
        return getComputedStyle(document.documentElement).getPropertyValue(name).trim();
    }

    function applyDefaults() {
        if (typeof Chart === 'undefined') return;
        Chart.defaults.font.family = 'Inter, "Segoe UI", system-ui, sans-serif';
        Chart.defaults.font.size = 11;
        Chart.defaults.color = cssVar('--muted');
        Chart.defaults.borderColor = cssVar('--chart-grid');
        Chart.defaults.plugins.tooltip.backgroundColor = 'rgba(15, 23, 42, .96)';
        Chart.defaults.plugins.tooltip.titleColor = '#f8fafc';
        Chart.defaults.plugins.tooltip.bodyColor = '#cbd5e1';
        Chart.defaults.plugins.tooltip.titleFont = { size: 12, weight: '600' };
        Chart.defaults.plugins.tooltip.bodyFont = { size: 12 };
        Chart.defaults.plugins.tooltip.padding = 12;
        Chart.defaults.plugins.tooltip.cornerRadius = 10;
        Chart.defaults.plugins.tooltip.displayColors = true;
        Chart.defaults.plugins.tooltip.boxPadding = 5;

        Chart.defaults.plugins.tooltip.callbacks.label = function (context) {
            const value = typeof context.parsed === 'object'
                ? (context.parsed.y ?? context.parsed.x ?? context.parsed.r ?? 0)
                : (context.parsed || 0);
            const isPercent = context.chart.config.type === 'radar' ||
                (context.chart.options.scales && context.chart.options.scales.x &&
                 context.chart.options.scales.x.max === 100);
            const formatted = isPercent ? `${numberFormat.format(value)}%` : numberFormat.format(value);
            const label = context.dataset.label ? `${context.dataset.label}: ` : '';
            return ` ${label}${formatted}`;
        };
    }

    function decorateOptions(config) {
        const options = config.options || {};
        options.plugins = options.plugins || {};
        options.plugins.legend = options.plugins.legend || {};
        options.plugins.legend.labels = options.plugins.legend.labels || {};
        options.plugins.legend.labels.color = cssVar('--muted');
        options.plugins.legend.labels.font = { size: 11, family: 'Inter, sans-serif' };

        if (options.scales) {
            Object.keys(options.scales).forEach(key => {
                const scale = options.scales[key];
                scale.ticks = scale.ticks || {};
                scale.ticks.color = cssVar('--muted-2');
                scale.grid = scale.grid || {};
                scale.grid.color = cssVar('--chart-grid');
                if (scale.pointLabels) scale.pointLabels.color = cssVar('--muted');
            });
        }
        return options;
    }

    function render(canvasId, config) {
        const canvas = document.getElementById(canvasId);
        if (!canvas || typeof Chart === 'undefined') return null;

        let parsed = config;
        if (typeof config === 'string') {
            try { parsed = JSON.parse(config); } catch (e) { console.error('Configuração de gráfico inválida', e); return null; }
        }

        parsed.options = decorateOptions(parsed);
        const chart = new Chart(canvas.getContext('2d'), parsed);
        registry.set(canvasId, chart);
        canvas.closest('.chart-box')?.classList.add('chart-ready');
        return chart;
    }

    function refreshTheme() {
        applyDefaults();
        registry.forEach(chart => {
            decorateOptions(chart.options);
            chart.update('none');
        });
    }

    function destroyAll() {
        registry.forEach(chart => chart.destroy());
        registry.clear();
    }

    applyDefaults();
    window.addEventListener('ft:themechange', refreshTheme);

    window.FinTrackCharts = { render, refreshTheme, destroyAll };
})();
