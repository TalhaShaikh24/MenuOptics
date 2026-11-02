// MenuEngineeringInsights.js — Menu engineering insights page scripts.

let table;
let startDate;
let endDate;
let currentEndDate = null;
let insightsQueryStartDate = null;
let insightsQueryEndDate = null;

$(document).ready(function () {
    $('#dateRange').customRangeFilter({
        onApplyFilter: function (filterType, selectedDates) {
            startDate = DateFormatter.formatDashUS(selectedDates.startDate);
            endDate = DateFormatter.formatDashUS(selectedDates.endDate);
            refreshMenuEngineeringInsightsTable(startDate, endDate);
        },
        initialFilter: 'Last 30 Days'
    });

    if (typeof $.fn.DataTable === 'undefined') {
        console.error('jQuery DataTable library is not loaded. Please check the CDN or script inclusion.');
        showNotification('DataTable library failed to load.', 'error');
        return;
    }

    const today = new Date();
    const last30Days = new Date();
    last30Days.setDate(today.getDate() - 29);

    startDate = DateFormatter.formatDashUS(last30Days);
    endDate = DateFormatter.formatDashUS(today);
    currentEndDate = endDate;

    refreshMenuEngineeringInsightsTable(startDate, endDate);
});

function refreshMenuEngineeringInsightsTable(startDate = null, endDate = null) {
    insightsQueryStartDate = startDate;
    insightsQueryEndDate = endDate;

    if (table) {
        // The old flow rebuilt the table on every refresh: page one, filters cleared.
        table.search('');
        table.column(8).search('');
        table.page.len(10);
        table.ajax.reload();
    } else {
        initMenuEngineeringInsightsTable();
    }
}

function initMenuEngineeringInsightsTable() {
    $.fn.dataTable.ext.type.order['quadrant-pre'] = function (d) {
        const text = $(d).attr('data-quadrant') || $(d).data('quadrant') || $(d).text() || d;
        switch (text) {
            case 'Star': return 1;
            case 'Plowhorse': return 2;
            case 'Puzzle': return 3;
            case 'Dog': return 4;
            default: return 5;
        }
    };

    table = initDataTable('.menu-engineering-table', {
        processing: true,
        loadingMessage: 'Loading menu insights...',
        ajax: function (dtParams, callback, settings) {
            let url = `/api/MenuEngineeringInsightsApi/GetAllMenuEngineeringInsightsByUserId?`;

            if (insightsQueryStartDate) {
                url += `startDate=${insightsQueryStartDate}&`;
                if (insightsQueryEndDate) {
                    url += `endDate=${insightsQueryEndDate}`;
                }
            }

            $.ajax({
                url: url,
                method: 'GET',
                success: function (response) {
                    callback({ data: response.data || [] });
                },
                error: function (xhr, status, error) {
                    console.error('Failed to fetch menu engineering insights:', error);
                    showNotification('Failed to fetch menu engineering insights.', 'error');
                    // The old flow left the previous table untouched on error.
                    callback({ data: table ? table.rows().data().toArray() : [] });
                }
            });
        },
        columns: [
            {
                data: 'name',
                render: function (name, type, row) {
                    return `
                        <div class="insight-info">
                            <img class="table-image" src="${imageBaseUrl + row.featuredImage}"/>
                            <p class="insight-name">${row.name || ''}</p>
                        </div>`;
                }
            },
            {
                data: 'sellingPrice',
                render: function (data) { return `$${Number(data || 0).toFixed(2)}`; }
            },
            {
                data: 'cost',
                render: function (data) { return `$${Number(data || 0).toFixed(2)}`; }
            },
            {
                data: null,
                render: function (data, type, row) {
                    const sellingPrice = Number(row.sellingPrice || 0);
                    const cost = Number(row.cost || 0);
                    const costPct = sellingPrice ? (cost / sellingPrice) * 100 : 0;
                    return `${costPct.toFixed(2)}%`;
                }
            },
            {
                data: 'unitsSold',
                render: function (data) { return `${Number(data || 0)}`; }
            },
            {
                data: 'contributionMargin',
                render: function (data) { return `$${Number(data || 0).toFixed(2)}`; }
            },
            {
                data: 'profitabilityScore',
                render: function (data) { return `$${Number(data || 0).toFixed(2)}`; }
            },
            {
                data: 'popularityShare',
                render: function (data) { return `${Number(data || 0).toFixed(2)}%`; }
            },
            {
                data: 'menuQuadrant',
                render: function (data) {
                    const quadrant = data || 'N/A';
                    return `<span class="quadrant-icon tooltip-icon" data-quadrant="${quadrant}" data-tooltip="${getQuadrantTooltipText(quadrant)}">
                                ${getQuadrantIcon(quadrant)}
                            </span>`;
                }
            },
            {
                data: 'isActive',
                render: function (data) {
                    return `<span class="status-badge ${data ? 'good' : 'critical'}">
                                ${data ? 'Active' : 'Inactive'}
                            </span>`;
                }
            }
        ],
        orderable: false,
        columnDefs: [
            { type: 'quadrant', targets: 8 },
            { searchable: false },
            { targets: [1, 2, 3, 4, 5, 6, 7], className: 'dt-body-left' },
            { targets: 9, className: 'hidden' }
        ],
        language: {
            search: '',
            searchPlaceholder: 'Search menu items...'
        },
        dom: 'Bfrtip',
        buttons: [
            {
                extend: 'csvHtml5',
                text: '<i class="fas fa-file-csv"></i> Export CSV',
                className: 'btn btn-outline',
                exportOptions: {
                    columns: [0, 1, 2, 3, 4, 5, 6, 7, 8],
                    format: {
                        body: formatExportCell
                    }
                }
            },
            {
                extend: 'pdfHtml5',
                text: '<i class="fas fa-file-pdf"></i> Export PDF',
                className: 'btn btn-primary',
                orientation: 'landscape',
                pageSize: 'A4',
                title: 'Menu Engineering Insights Report',
                exportOptions: {
                    columns: [0, 1, 2, 3, 4, 5, 6, 7, 8],
                    format: {
                        body: formatExportCell
                    }
                }
            }
        ]
    });

    if (table.buttons && $('#exportButtons').length) {
        table.buttons().container().appendTo('#exportButtons');
    }

    $('#menuItemSearch').off('keyup').on('keyup', function () {
        table.search(this.value).draw();
    });

    $('.quadrant-filter').off('click').on('click', function () {
        const quadrant = $(this).data('quadrant');
        table.column(8).search(quadrant).draw();
    });

    $('#clear-filter').off('click').on('click', function () {
        table.column(8).search('').draw();
        $('#menuItemSearch').val('').trigger('keyup');
    });
}


function toggleMenuItemActive(id) {
    if (confirm('Are you sure you want to toggle the active status of this menu item?')) {
        ajaxRequest({
            url: `/api/MenuApi/ToggleMenuActive/${id}`,
            method: 'PUT',
            showMessage: false,
            onSuccess: function (response) {
                refreshMenuEngineeringInsightsTable();
                showNotification(response.responseMsg, 'success');
            },
            onError: function (xhr, status, error) {
                console.error('Failed to toggle menu item status:', error);
            }
        });
    }
}

function deleteMenuItem(id) {
    ajaxRequest({
        url: `/api/MenuApi/DeleteMenuItem/${id}`,
        method: 'DELETE',
        showMessage: false,
        onSuccess: function (response) {
            refreshMenuEngineeringInsightsTable();
            showNotification('Menu item deleted successfully!', 'success');
        },
        onError: function (xhr, status, error) {
            console.error('Failed to delete menu item:', error);
        }
    });
}

function formatExportCell(data, row, column, node) {
    if (column === 8) {
        return $(data).attr('data-quadrant') || data || 'N/A';
    }
    if (column === 0) {
        return $(data).find('.insight-name').text() || data;
    }
    return data;
}

function getQuadrantIcon(quadrant) {
    switch (quadrant) {
        case 'Star': return '⭐';
        case 'Plowhorse': return '🐴';
        case 'Puzzle': return '🧩';
        case 'Dog': return '🐶';
        default: return '❓';
    }
}

function getQuadrantTooltipText(quadrant) {
    switch (quadrant) {
        case 'Star': return 'High Popularity + High Profitability';
        case 'Plowhorse': return 'High Popularity + Low Profitability';
        case 'Puzzle': return 'Low Popularity + High Profitability';
        case 'Dog': return 'Low Popularity + Low Profitability';
        default: return '';
    }
}
