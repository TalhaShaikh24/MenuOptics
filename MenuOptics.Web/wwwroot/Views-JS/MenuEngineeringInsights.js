/* ============================================================================
 * MenuEngineeringInsights.js — Menu engineering insights page
 * (Views/MenuEngineeringInsights/Index.cshtml)
 * Quadrant analysis table with CSV/PDF export and date-range filter.
 *
 * Project convention — every Views-JS file follows this section order:
 *   1. Constants  2. State  3. Initialization  4. Event bindings
 *   5. AJAX / CRUD  6. Helpers  7. Utilities
 * Functions stay global: they are referenced by inline onclick handlers.
 * ==========================================================================*/

/* ── 2. State ─────────────────────────────────────────────────────────── */

let table;
let startDate;
let endDate;

/* ── 3. Initialization / 4. Event bindings ────────────────────────────── */

$(document).ready(function () {

    $('#dateRange').customRangeFilter({
        onApplyFilter: function (filterType, selectedDates) {

            startDate = DateFormatter.formatDashUS(selectedDates.startDate);
            endDate = DateFormatter.formatDashUS(selectedDates.endDate);
            console.log('Filter 1 Applied:', filterType, selectedDates);

            refreshMenuEngineeringInsightsTable(startDate, endDate);
        },
        initialFilter: "Last 30 Days"
    });

    // Ensure DataTable is loaded before proceeding.
    if (typeof $.fn.DataTable === 'undefined') {
        console.error('jQuery DataTable library is not loaded. Please check the CDN or script inclusion.');
        showNotification('DataTable library failed to load.', 'error');
        return;
    }

    refreshMenuEngineeringInsightsTable();
});

/* ── 5. AJAX / CRUD ───────────────────────────────────────────────────── */

function refreshMenuEngineeringInsightsTable(startDate = null, endDate = null) {
    const requestData = {};

    let url = `/api/MenuEngineeringInsightsApi/GetAllMenuEngineeringInsightsByUserId?`;

    if (startDate) {
        url += `startDate=${startDate}&`;
        if (endDate) {
            url += `endDate=${endDate}`;
        }
    }

    ajaxRequest({
        url: url,
        method: 'GET',
        data: null,
        isQuery: true,
        showMessage: false,
        onSuccess: function (response) {
            const insights = response.data || [];

            // If already initialized, destroy BEFORE touching the DOM
            if ($.fn.DataTable.isDataTable('.menu-engineering-table')) {
                table.destroy();
            }

            const tbody = $('.menu-engineering-table tbody');
            tbody.empty();

            insights.forEach(insight => {
                tbody.append(buildInsightRow(insight));
            });

            // Initialize DataTable AFTER rows are present
            initDataTable();
        },
        onError: function (xhr, status, error) {
            console.error('Failed to fetch menu engineering insights:', error);
            showNotification('Failed to fetch menu engineering insights.', 'error');
        }
    });
}

// Function to toggle menu item active status
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

// Delete menu item
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

/* ── 6. Helpers ───────────────────────────────────────────────────────── */

function initDataTable() {
    // Custom order: Star < Plowhorse < Puzzle < Dog
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

    table = $('.menu-engineering-table').DataTable({
        paging: true,
        searching: true,
        ordering: false,
        orderable: false,
        columnDefs: [
            { type: 'quadrant', targets: 8 }, // Apply custom type to Quadrant column
            { searchable: false }, // Disable sorting/searching for non-data columns
            { targets: [1, 2, 3, 4, 5, 6, 7], className: 'dt-body-left' } // Right-align numeric columns
        ],
        pageLength: 10,
        lengthMenu: [10, 25, 50, 100],
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
                    columns: [0, 1, 2, 3, 4, 5, 6, 7, 8], // Export only specified columns
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
                    columns: [0, 1, 2, 3, 4, 5, 6, 7, 8], // Export only specified columns
                    format: {
                        body: formatExportCell
                    }
                }
            }
        ]
    });

    // Move export buttons to custom container
    if (table.buttons && $('#exportButtons').length) {
        table.buttons().container().appendTo('#exportButtons');
    }

    // Custom search
    $('#menuItemSearch').on('keyup', function () {
        table.search(this.value).draw();
    });

    // Quadrant filter
    $('.quadrant-filter').on('click', function () {
        const quadrant = $(this).data('quadrant');
        table.column(8).search(quadrant).draw();
    });

    // Clear filter
    $('#clear-filter').on('click', function () {
        table.column(8).search('').draw();
        $('#menuItemSearch').val('').trigger('keyup');
    });
}

// Shared CSV/PDF cell formatter (same output as before refactoring)
function formatExportCell(data, row, column, node) {
    if (column === 8) { // Quadrant column
        return $(data).attr('data-quadrant') || data || 'N/A';
    }
    if (column === 0) { // Insight name (strip HTML)
        return $(data).find('.insight-name').text() || data;
    }
    return data; // Return other columns as is
}

// Builds a single insight table row (same markup as before refactoring)
function buildInsightRow(insight) {

    // Clean numerics for sorting
    const sellingPrice = Number(insight.sellingPrice || 0);
    const cost = Number(insight.cost || 0);
    const costPct = sellingPrice ? (cost / sellingPrice) * 100 : 0;
    const unitsSold = Number(insight.unitsSold || 0);
    const cm = Number(insight.contributionMargin || 0);
    const prof = Number(insight.profitabilityScore || 0);
    const popShare = Number(insight.popularityShare || 0);
    const quadrantTooltips = insight.menuQuadrant === "";
    const quadrant = insight.menuQuadrant || 'N/A';

    return `
                    <tr>
                        <td>
                            <div class="insight-info">
                                <img class="table-image" src="${imageBaseUrl+insight.featuredImage}"/>
                                <p class="insight-name">${insight.name || ''}</p>
                            </div>
                        </td>

                        <td data-order="${sellingPrice}">$${sellingPrice.toFixed(2)}</td>
                        <td data-order="${cost}">$${cost.toFixed(2)}</td>
                        <td data-order="${costPct.toFixed(2)}">${costPct.toFixed(2)}%</td>
                        <td data-order="${unitsSold}">${unitsSold}</td>
                        <td data-order="${cm}">$${cm.toFixed(2)}</td>
                        <td data-order="${prof}">$${prof.toFixed(2)}</td>
                        <td data-order="${popShare.toFixed(2)}">${popShare.toFixed(2)}%</td>

                        <!-- Quadrant icon; the span carries data-quadrant for custom sort -->
                        <td>
                            <span class="quadrant-icon tooltip-icon" data-quadrant="${quadrant}"data-tooltip="${getQuadrantTooltipText(quadrant) }">
                                ${getQuadrantIcon(quadrant)}
                            </span>
                        </td>

                        <td class="hidden">
                            <span class="status-badge ${insight.isActive ? 'good' : 'critical'}">
                                ${insight.isActive ? 'Active' : 'Inactive'}
                            </span>
                        </td>

                        
                    </tr>
                `;
}

/* ── 7. Utilities ─────────────────────────────────────────────────────── */

function getQuadrantIcon(quadrant) {
    switch (quadrant) {
        case 'Star':
            return '⭐';
        case 'Plowhorse':
            return '🐴';
        case 'Puzzle':
            return '🧩';
        case 'Dog':
            return '🐶';
        default:
            return '❓';
    }
}
function getQuadrantTooltipText(quadrant) {
    switch (quadrant) {
        case 'Star':
            return 'High Popularity + High Profitability';
        case 'Plowhorse':
            return 'High Popularity + Low Profitability';
        case 'Puzzle':
            return 'Low Popularity + High Profitability';
        case 'Dog':
            return 'Low Popularity + Low Profitability';
        default:
            return '';
    }
}
