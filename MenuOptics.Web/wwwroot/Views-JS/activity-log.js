/* ============================================================================
 * activity-log.js — Admin Restaurant Activity Logs (Views/ActivityLog/UserActivityLog.cshtml)
 *
 * Project convention — section order:
 *   1. Constants  2. State  3. Initialization  4. Event bindings
 *   5. AJAX / CRUD  6. Helpers  7. Utilities
 * Functions stay global: referenced by inline handlers.
 * ==========================================================================*/

/* ── 2. State ─────────────────────────────────────────────────────────── */

let table = null;
let logCache = {};
let knownEndpoints = new Set();
let debounceTimer = null;

function getDefaultDateRange() {
    const end = new Date();
    end.setHours(23, 59, 59, 999);
    const start = new Date();
    start.setDate(start.getDate() - 6);
    start.setHours(0, 0, 0, 0);
    return {
        startDate: formatDateTimeForApi(start),
        endDate: formatDateTimeForApi(end)
    };
}

const defaultRange = getDefaultDateRange();
let currentFilters = {
    userId: 'all',
    logLevel: 'all',
    requestType: 'all',
    startDate: defaultRange.startDate,
    endDate: defaultRange.endDate
};

/* ── 3. Initialization / 4. Event bindings ────────────────────────────── */

$(document).ready(function () {
    // Initialize Select2 filter for Restaurants
    $('#restaurantFilter').select2({
        placeholder: 'All Users',
        allowClear: false,
        width: '100%'
    }).on('change', function () {
        currentFilters.userId = $(this).val();
        if (table) table.draw();
    });

    // Initialize Date Range filter with optional time
    $('#activityDateRange').customRangeFilter({
        enableTime: true,
        initialFilter: "Last 7 Days",
        onApplyFilter: function (filterType, selectedDates) {
            currentFilters.startDate = selectedDates.startDate ? formatDateTimeForApi(selectedDates.startDate) : null;
            currentFilters.endDate = selectedDates.endDate ? formatDateTimeForApi(selectedDates.endDate) : null;
            if (table) table.draw();
        }
    });

    loadRestaurantsDropdown();
    loadEndpointsDropdown();
    initRestaurantActivityTable();

    

    $('#restaurantLogSearch').on('input', function () {
        clearTimeout(debounceTimer);
        debounceTimer = setTimeout(() => {
            if (table) table.draw();
        }, 350);
    });

    $('#restaurantLogLevelFilter').on('change', function () {
        currentFilters.logLevel = $(this).val();
        if (table) table.draw();
    });

    // Initialize Select2 filter for Endpoints
    $('#restaurantLogRequestFilter').select2({
        placeholder: 'All Endpoints',
        allowClear: false,
        width: '100%'
    }).on('change', function () {
        currentFilters.requestType = $(this).val();
        if (table) table.draw();
    });

    $('#clearRestaurantLogFiltersBtn').on('click', function () {
        $('#restaurantLogSearch').val('');
        $('#restaurantFilter').val('all').trigger('change.select2');
        $('#restaurantLogLevelFilter').val('all');
        $('#restaurantLogRequestFilter').val('all').trigger('change.select2');
        $('#activityDateRange').trigger('resetFilter.customRangeFilter');
        const def = getDefaultDateRange();
        currentFilters = { userId: 'all', logLevel: 'all', requestType: 'all', startDate: def.startDate, endDate: def.endDate };
        if (table) table.draw();
    });
});

/* ── 5. AJAX / CRUD ───────────────────────────────────────────────────── */

function loadRestaurantsDropdown() {
    ajaxRequest({
        url: '/api/UserApi/GetAllRestaurants',
        method: 'GET',
        showMessage: false,
        onSuccess: function (response) {
            const restaurants = response.data || [];
            const $filter = $('#restaurantFilter');
            $filter.empty().append('<option value="all">All Users</option>');
            restaurants.forEach(r => {
                const name = r.restaurantName || r.username || `User #${r.userId}`;
                $filter.append(new Option(name, r.userId));
            });
            if (currentFilters.userId && currentFilters.userId !== 'all') {
                $filter.val(currentFilters.userId);
            }
            $filter.trigger('change.select2');
        },
        onError: function (xhr, status, error) {
            console.error('Failed to load users:', error);
        }
    });
}

function initRestaurantActivityTable() {
    if ($.fn.DataTable.isDataTable('#restaurantActivityTable')) {
        $('#restaurantActivityTable').DataTable().destroy();
    }

    table = $('#restaurantActivityTable').DataTable({
        serverSide: true,
        processing: true,
        ordering: false,
        pageLength: 10,
        lengthMenu: [10, 25, 50, 100],
        language: {
            search: '',
            searchPlaceholder: 'Search logs...',
            processing: '<i class="fas fa-spinner fa-spin"></i> Loading...',
            info: 'Showing _START_ to _END_ of _TOTAL_ entries',
            paginate: {
                previous: '<i class="fas fa-chevron-left"></i>',
                next: '<i class="fas fa-chevron-right"></i>'
            }
        },
        ajax: function (dtParams, callback, settings) {
            const pageNumber = Math.floor(dtParams.start / dtParams.length) + 1;
            const queryData = {
                draw: dtParams.draw,
                pageNumber: pageNumber,
                pageSize: dtParams.length
            };

            const searchVal = $('#restaurantLogSearch').val().trim();
            if (searchVal) {
                queryData.searchValue = searchVal;
            }
            if (currentFilters.userId && currentFilters.userId !== 'all') {
                queryData.userId = currentFilters.userId;
            }
            if (currentFilters.logLevel && currentFilters.logLevel !== 'all') {
                queryData.logLevel = currentFilters.logLevel;
            }
            if (currentFilters.requestType && currentFilters.requestType !== 'all') {
                queryData.requestType = currentFilters.requestType;
            }
            if (currentFilters.startDate) {
                queryData.startDate = currentFilters.startDate;
            }
            if (currentFilters.endDate) {
                queryData.endDate = currentFilters.endDate;
            }

            $.ajax({
                url: '/api/ActivityLogApi/GetRestaurantActivityLogs',
                method: 'GET',
                data: queryData,
                success: function (response) {
                    const result = response.data || {};
                    const logs = Array.isArray(result.data) 
                        ? result.data 
                        : (result.data?.$values || (Array.isArray(result.items) ? result.items : []));
                    const total = result.recordsTotal ?? result.recordsFiltered ?? result.totalCount ?? 0;

                    // Cache records for the Details modal
                    logs.forEach(l => {
                        logCache[l.logId] = l;
                        if (l.requestType) knownEndpoints.add(l.requestType);
                        if (l.userId && l.restaurantName && $(`#restaurantFilter option[value="${l.userId}"]`).length === 0) {
                            $('#restaurantFilter').append(new Option(l.restaurantName, l.userId));
                        }
                    });

                    updateStatsCards(logs, total, result);

                    callback({
                        draw: result.draw || dtParams.draw,
                        recordsTotal: total,
                        recordsFiltered: total,
                        data: logs
                    });
                },
                error: function (xhr, textStatus, errorThrown) {
                    console.error('Error loading user activity logs:', errorThrown);
                    callback({
                        draw: dtParams.draw,
                        recordsTotal: 0,
                        recordsFiltered: 0,
                        data: []
                    });
                }
            });
        },
        columns: [
            {
                data: 'logId',
                render: function (data) {
                    return `<span style="font-weight: 600; color: #64748b;">#${data}</span>`;
                }
            },
            {
                data: null,
                render: function (data, type, row) {
                    const restName = escapeHtml(row.restaurantName || row.firstName);
                    const userName = row.userName ? ` (${escapeHtml(row.userName)})` : (row.userId ? ` (#${row.userId})` : '');
                    const userEmail = row.userEmail ? `<div style="font-size: 11px; color: #94a3b8;">${escapeHtml(row.userEmail)}</div>` : '';
                    return `<div>
                                <strong style="color: #0f172a;">${restName}</strong>
                                <span style="font-size: 12px; color: #64748b;">${userName}</span>
                                ${userEmail}
                            </div>`;
                }
            },
            {
                data: 'ipAddress',
                render: function (data) {
                    return `<code style="font-size: 11px; background: #f1f5f9; padding: 2px 6px; border-radius: 4px; color: #334151;">${escapeHtml(data) || 'N/A'}</code>`;
                }
            },
            {
                data: 'requestType',
                render: function (data) {
                    const badge = getMethodBadgeHtml(data);
                    return `<div style="display: flex; align-items: center; gap: 6px;">
                               
                                <span class="badge-method ${badge.cls}">${badge.method}</span>
                                <span style="font-family: monospace; font-size: 12px; color: #1e293b;">${escapeHtml(badge.action || 'N/A')}</span>
                            </div>`;
                }
            },
            {
                data: 'logLevel',
                render: function (data) {
                    return getStatusBadgeHtml(data);
                }
            },
            {
                data: 'timeStamp',
                render: function (data) {
                    const formatted = window.DateFormatter && typeof window.DateFormatter.formatWithComma === 'function'
                        ? window.DateFormatter.formatWithComma(data)
                        : formatDateTime(data);
                    return `<span style="font-size: 12px; color: #64748b;"><i class="far fa-clock" style="margin-right: 4px;"></i>${formatted}</span>`;
                }
            },
            {
                data: null,
                orderable: false,
                className: 'text-center',
                render: function (data, type, row) {
                    return `<button class="btn btn-sm btn-outline" onclick="viewRestaurantLogDetails(${row.logId})" style="height: 32px; padding: 0 10px; font-size: 12px; display: inline-flex; align-items: center; gap: 4px;">
                                <i class="fas fa-eye"></i> Details
                            </button>`;
                }
            }
        ]
    });
}

/* ── 6. Helpers ───────────────────────────────────────────────────────── */

function updateStatsCards(logs, total, counts) {
    let successCount = 0;
    let errorCount = 0;
    let warningCount = 0;

    if (counts && counts.totalSuccessCount !== undefined) {
        successCount = counts.totalSuccessCount ?? 0;
        errorCount = counts.totalErrorCount ?? 0;
        warningCount = counts.totalWarningCount ?? 0;
    } else {
        logs.forEach(log => {
            const level = (log.logLevel || '').toLowerCase();
            if (level === 'success') {
                successCount++;
            } else if (level === 'error') {
                errorCount++;
            } else if (level === 'warning') {
                warningCount++;
            }
        });
    }

    $('#statTotalRestaurantLogs').text(total.toLocaleString());
    $('#statSuccessRestaurantLogs').text(successCount.toLocaleString());
    $('#statErrorRestaurantLogs').text(errorCount.toLocaleString());
    $('#statWarningRestaurantLogs').text(warningCount.toLocaleString());
}

function loadEndpointsDropdown() {
    ajaxRequest({
        url: '/api/ActivityLogApi/GetRestaurantEndpoints',
        method: 'GET',
        showMessage: false,
        onSuccess: function (response) {
            const endpoints = response.data || [];
            const $dropdown = $('#restaurantLogRequestFilter');
            $dropdown.empty().append('<option value="all">All Endpoints</option>');

            const uniqueOptions = new Map();

            endpoints.forEach(type => {
                if (!type) return;
                const endpoint = getMethodBadgeHtml(type);
                const filterValue = type.trim().replace(/\/\d+$/, '');
                const label = (endpoint.method && endpoint.action)
                    ? `${endpoint.action}`
                    : filterValue;

                if (!uniqueOptions.has(filterValue)) {
                    uniqueOptions.set(filterValue, label);
                }
            });

            Array.from(uniqueOptions.entries())
                .sort((a, b) => a[1].localeCompare(b[1]))
                .forEach(([val, lbl]) => {
                    $dropdown.append(new Option(lbl, val));
                });

            if (currentFilters.requestType && currentFilters.requestType !== 'all') {
                $dropdown.val(currentFilters.requestType);
            }
            $dropdown.trigger('change.select2');
        },
        onError: function (xhr, status, error) {
            console.error('Failed to load restaurant endpoints:', error);
        }
    });
}

function viewRestaurantLogDetails(logId) {
    const log = logCache[logId];
    const actions = getMethodBadgeHtml(log.requestType);
    
    if (!log) return;

    $('#modalRestaurantLogSubTitle').text(`Log #${log.logId} • ${formatDateTime(log.timeStamp)}`);

    const chips = [];
    chips.push(`<span class="info-chip"><i class="fas fa-store"></i> <strong>User:</strong> ${escapeHtml(log.restaurantName)} (${escapeHtml(log.userName)})</span>`);

    if (log.userEmail) {
        chips.push(`<span class="info-chip"><i class="fas fa-envelope"></i> ${escapeHtml(log.userEmail)}</span>`);
    }
    if (log.ipAddress) {
        chips.push(`<span class="info-chip"><i class="fas fa-network-wired"></i> <strong>IP:</strong> ${escapeHtml(log.ipAddress)}</span>`);
    }
    chips.push(`<span class="info-chip"><i class="fas fa-link"></i> <strong>Action:</strong> ${escapeHtml(actions.action) || 'N/A'}</span>`);
    chips.push(`${getStatusBadgeHtml(log.logLevel)}`);

    $('#modalRestaurantMetaChips').html(chips.join(''));
    $('#modalRestaurantPayload').text(formatJsonPretty(log.payload));
    $('#modalRestaurantResponse').text(formatJsonPretty(log.response));

    openModal('restaurantLogDetailsModal');
}

/* ── 7. Utilities ─────────────────────────────────────────────────────── */

function getMethodBadgeHtml(requestType) {
    if (!requestType) return '';

    const parts = requestType.trim().split(/\s+/);
    const method = parts[0].toUpperCase();

    const segments = parts[1].split('/').filter(Boolean);
    const action = segments.findLast(segment => !/^\d+$/.test(segment));
    const cls = method.toLowerCase();


    const actions = {
        method: method,
        action: action,
        cls: cls
    }
    return actions;
}

function getCleanPath(requestType) {
    if (!requestType) return '';
    const parts = requestType.trim().split(' ');
    return parts.length > 1 ? parts.slice(1).join(' ') : parts[0];
}

function getStatusBadgeHtml(level) {
    const lvl = (level || 'Info').trim();
    const l = lvl.toLowerCase();
    let icon = 'fa-info-circle';
    let cls = 'warning';

    if (l === 'success') {
        icon = 'fa-check-circle';
        cls = 'success';
    } else if (l === 'error') {
        icon = 'fa-times-circle';
        cls = 'error';
    }

    return `<span class="badge-status ${cls}"><i class="fas ${icon}"></i> ${escapeHtml(lvl)}</span>`;
}

function formatDateTime(dateStr) {
    if (!dateStr) return 'N/A';
    try {
        const d = new Date(dateStr);
        return d.toLocaleString('en-US', {
            year: 'numeric',
            month: 'short',
            day: 'numeric',
            hour: '2-digit',
            minute: '2-digit',
            second: '2-digit'
        });
    } catch {
        return dateStr;
    }
}

function formatJsonPretty(raw) {
    if (!raw) return 'No data recorded.';
    try {
        const obj = typeof raw === 'string' ? JSON.parse(raw) : raw;
        return JSON.stringify(obj, null, 2);
    } catch {
        return raw;
    }
}

function copyJsonToClipboard(elementId) {
    const text = $(`#${elementId}`).text();
    navigator.clipboard.writeText(text).then(() => {
        showNotification('Copied to clipboard!', 'success');
    }).catch(() => {
        showNotification('Failed to copy.', 'error');
    });
}

function escapeHtml(text) {
    if (!text) return '';
    return text.toString()
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#039;');
}

function formatDateTimeForApi(d) {
    if (!d || !(d instanceof Date) || isNaN(d.getTime())) return null;
    const year = d.getFullYear();
    const month = String(d.getMonth() + 1).padStart(2, '0');
    const day = String(d.getDate()).padStart(2, '0');
    const hours = String(d.getHours()).padStart(2, '0');
    const minutes = String(d.getMinutes()).padStart(2, '0');
    const seconds = String(d.getSeconds()).padStart(2, '0');
    return `${year}-${month}-${day}T${hours}:${minutes}:${seconds}`;
}
