// common.js — Shared helpers for all Views-JS page scripts. Load before any page script.

// Create (or recreate) a DataTable with the project defaults.
// Page-specific options are merged on top; `language` is merged one level deep.
function initDataTable(selector, options) {
    options = options || {};

    if ($.fn.DataTable.isDataTable(selector)) {
        $(selector).DataTable().destroy();
    }

    const defaults = {
        paging: true,
        searching: true,
        ordering: false,
        pageLength: 10,
        lengthMenu: [10, 25, 50, 100],
        language: {
            search: '',
            searchPlaceholder: 'Search...'
        }
    };

    const config = $.extend({}, defaults, options);
    config.language = $.extend({}, defaults.language, options.language || {});

    return $(selector).DataTable(config);
}

// Escape text before injecting it into HTML.
function escapeHtml(text) {
    if (!text) return '';
    return text.toString()
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#039;');
}

// Toggle a submit button's loading state (.btn-text / .btn-loading markup).
function setBtnLoading($btn, isLoading) {
    $btn.prop('disabled', isLoading);
    $btn.find('.btn-text').toggle(!isLoading);
    $btn.find('.btn-loading').toggle(isLoading);
}

// Fill a select with options from an array of objects.
function fillSelect(selector, items, valueKey, textKey, placeholder) {
    const $select = $(selector);
    $select.empty();

    if (placeholder) {
        $select.append($('<option>', { value: '', text: placeholder }));
    }

    (items || []).forEach(function (item) {
        $select.append($('<option>', { value: item[valueKey], text: item[textKey] }));
    });
}

// Read one query-string value from the current URL.
function getQueryParam(name) {
    return new URLSearchParams(window.location.search).get(name);
}
