/* ============================================================================
 * loadingComponent.js — Menu Optics loading component (shared / constant)
 *
 * One loading indicator for the whole project, styled from the project
 * theme (see wwwroot/css/loadingComponent.css: orange #f08d11 -> #ff9f2e,
 * white card, #e5e7eb border, 12px radius, Inter).
 *
 * Usage — manual (client-side tables whose data is fetched outside DataTables,
 * e.g. refreshXTable() functions in wwwroot/Views-JS):
 *
 *     LoadingComponent.show('#salesTable');                 // default text
 *     LoadingComponent.show('.data-table', 'Loading sales...');
 *     LoadingComponent.hide('#salesTable');                 // call on complete
 *     LoadingComponent.hideAll('#salesTable');              // force hide
 *
 * The target does not have to be a table: any container element works the
 * same way and the overlay is placed inside that element itself — e.g. the
 * Menu page, which has no DataTable and renders a card grid:
 *
 *     LoadingComponent.show('#menuGrid', 'Loading menu items...');
 *     LoadingComponent.hide('#menuGrid');
 *
 * `target` may be a selector, DOM element, jQuery object or a DataTables
 * table node. Manual shows/hides are counted per table, so overlapping
 * requests (e.g. Inventory: load reports -> load report rows) keep the
 * overlay up until the last one finishes. hideAll() resets the counter —
 * use it from code paths whose show/hide calls are not strictly paired.
 * DataTables' own state is tracked separately (see processing.dt below),
 * because DataTables 1.10 can fire processing=true more than once for a
 * single load — the overlay is visible while either source is active.
 *
 * Usage — automatic (server-side / ajax DataTables):
 *   Nothing to call. When this file is loaded after DataTables (it is, from
 *   Views/Shared/_Layout.cshtml, before the page's Scripts section):
 *     1. `processing: true` becomes the default for every DataTable, and
 *        DataTables' processing markup becomes this component's card, so
 *        per-file `language.processing` strings are no longer needed.
 *     2. The `processing.dt` event shows/hides this component's overlay on
 *        the table that is loading (paging, searching, sorting, filtering).
 *   The library's own processing box is hidden in loadingComponent.css so
 *   the two indicators never appear together.
 * ==========================================================================*/

(function (window, $) {
    'use strict';

    if (!$) {
        return;
    }

    var DEFAULT_MESSAGE = 'Loading...';

    /* Card markup — the same HTML DataTables shows for `processing`. */
    function cardHtml(message) {
        return '<span class="mo-loading-card" role="status" aria-live="polite">'
            + '<span class="mo-loading-spinner" aria-hidden="true"></span>'
            + '<span class="mo-loading-text">' + message + '</span>'
            + '</span>';
    }

    /* ── Target / host resolution ─────────────────────────────────────── */

    function resolveTables(target) {
        if (!target) {
            return $();
        }
        if (target.nodeType === 1 && target.tagName === 'TABLE') {
            return $(target);
        }
        if (target.nTable) { // DataTables settings object
            return $(target.nTable);
        }
        var $tables = $(target).filter('table').add($(target).find('table'));
        // Non-table containers (e.g. #menuGrid on the Menu page) are tracked
        // as themselves when the target contains no table.
        return $tables.length ? $tables : $(target);
    }

    /* For tables, the overlay must survive DataTable destroy/re-init, so it
     * lives on .table-container (present around every DataTable view) rather
     * than on the DataTables wrapper, which destroy() removes.
     * For any other container, the overlay lives inside the container itself. */
    function resolveHost($table) {
        if ($table.length && $table[0].tagName !== 'TABLE') {
            return $table;
        }
        var $host = $table.closest('.table-container');
        if (!$host.length) {
            $host = $table.closest('.dataTables_wrapper');
        }
        if (!$host.length) {
            $host = $table.parent();
        }
        return $host;
    }

    function getOverlay($table) {
        var $host = resolveHost($table);
        var $overlay = $host.children('.mo-loading-overlay');

        if (!$overlay.length) {
            $overlay = $('<div class="mo-loading-overlay" aria-hidden="true"></div>')
                .html(cardHtml(DEFAULT_MESSAGE))
                .appendTo($host);
        }

        $host.addClass('mo-loading-host');
        return $overlay;
    }

    /* ── Visibility state ─────────────────────────────────────────────── */

    function updateVisibility($table) {
        var manualCount = $table.data('mo-loading-count') || 0;
        var isProcessing = !!$table.data('mo-loading-processing');
        var $overlay = resolveHost($table).children('.mo-loading-overlay');

        if (manualCount > 0 || isProcessing) {
            if (!$overlay.length) {
                $overlay = getOverlay($table);
            }
            $overlay.addClass('mo-loading-visible').attr('aria-hidden', 'false');
        } else {
            $overlay.removeClass('mo-loading-visible').attr('aria-hidden', 'true');
        }
    }

    /* ── Public API ───────────────────────────────────────────────────── */

    function show(target, message) {
        resolveTables(target).each(function () {
            var $table = $(this);
            $table.data('mo-loading-count', ($table.data('mo-loading-count') || 0) + 1);

            if (message) {
                getOverlay($table).find('.mo-loading-text').text(message);
            }
            updateVisibility($table);
        });
    }

    function hide(target) {
        resolveTables(target).each(function () {
            var $table = $(this);
            $table.data('mo-loading-count', Math.max(($table.data('mo-loading-count') || 0) - 1, 0));
            updateVisibility($table);
        });
    }

    function hideAll(target) {
        resolveTables(target).each(function () {
            var $table = $(this);
            $table.data('mo-loading-count', 0);
            $table.data('mo-loading-processing', false);
            updateVisibility($table);
        });
    }

    window.LoadingComponent = {
        show: show,
        hide: hide,
        hideAll: hideAll,
        // Aliases that read naturally at DataTable call sites
        showForTable: show,
        hideForTable: hide,
        hideAllForTable: hideAll,
        markup: cardHtml,
        defaultMessage: DEFAULT_MESSAGE
    };

    /* ── DataTables integration ───────────────────────────────────────── */

    if ($.fn && $.fn.dataTable) {
        // 1. Every DataTable gets the processing indicator + this component's
        //    markup by default — no per-file copy of the spinner HTML.
        $.extend(true, $.fn.dataTable.defaults, {
            processing: true,
            language: {
                processing: cardHtml(DEFAULT_MESSAGE)
            }
        });

        // 2. Show/hide the overlay while a table is processing (server-side
        //    paging / searching / sorting, and client-side draws). This is a
        //    flag, not the manual counter: DataTables 1.10 may fire
        //    processing=true twice for one load and false only once.
        $(document).on('processing.dt', function (event, settings, processing) {
            if (!settings || !settings.nTable) {
                return;
            }
            var $table = $(settings.nTable);
            $table.data('mo-loading-processing', !!processing);
            updateVisibility($table);
        });
    }
})(window, window.jQuery);
