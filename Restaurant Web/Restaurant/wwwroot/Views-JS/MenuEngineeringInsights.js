let table;

$(document).ready(function () {
    // Ensure DataTable is loaded before proceeding
    if (typeof $.fn.DataTable === 'undefined') {
        console.error('jQuery DataTable library is not loaded. Please check the CDN or script inclusion.');
        showNotification('DataTable library failed to load.', 'error');
        return;
    }

    refreshMenuEngineeringInsightsTable();
});

function initDataTable() {
    // Define custom sorting for quadrant
    $.fn.dataTable.ext.type.order['quadrant-pre'] = function (d) {
        switch (d) {
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
        ordering: true,
        order: [[7, 'asc']], // Default sort by Quadrant column
        columnDefs: [
            { type: 'quadrant', targets: 7 } // Apply custom quadrant sorting to column 7
        ],
        pageLength: 10, // Default rows per page
        lengthMenu: [10, 25, 50, 100], // Pagination options
        language: {
            search: '', // Remove default search label
            searchPlaceholder: 'Search menu items...'
        }
    });

    // Link custom search input
    $('#menuItemSearch').on('keyup', function () {
        table.search(this.value).draw();
    });

    // Quadrant filter buttons
    $('.quadrant-filter').on('click', function () {
        var quadrant = $(this).data('quadrant');
        table.column(7).search(quadrant).draw();
    });

    // High profit filter (Stars and Puzzles)
    //$('#high-profit-filter').on('click', function () {
    //    table.column(7).search('^(Star|Puzzle)$', true, false).draw();
    //});

    // Clear filter
    $('#clear-filter').on('click', function () {
        table.column(7).search('').draw();
        $('#menuItemSearch').val('').trigger('keyup');
    });
}

function refreshMenuEngineeringInsightsTable() {
    ajaxRequest({
        url: '/api/MenuEngineeringInsightsApi/GetAllMenuEngineeringInsightsByUserId',
        method: 'GET',
        data: null,
        showMessage: false,
        onSuccess: function (response) {
            const insights = response.data;
            const tbody = $('.menu-engineering-table tbody');
            tbody.empty();

            insights.forEach(insight => {
                const createdDate = DateFormatter.formatWithComma(insight.createdOn || new Date());
                const row = `
                    <tr>
                        <td>
                            <div class="insight-info">
                                <img class="table-image" src="${insight.featuredImage}"/>
                                <p class="insight-name">${insight.name}</p>
                            </div>
                        </td>
                        <td>$${insight.sellingPrice.toFixed(2)}</td>
                        <td>$${insight.cost.toFixed(2)}</td>
                        <td>${insight.unitsSold}</td>
                        <td>$${insight.contributionMargin.toFixed(2)}</td>
                        <td>${insight.profitabilityScore.toFixed(2)}</td>
                        <td>${insight.popularityShare.toFixed(2)}%</td>
                        <td><span class="quadrant-badge">${insight.menuQuadrant || 'N/A'}</span></td>
                    </tr>
                `;
                tbody.append(row);
            });

            // (Re)initialize DataTable after population
            if (table && $.fn.DataTable.isDataTable('.menu-engineering-table')) {
                table.destroy();
            }
            initDataTable();
        },
        onError: function (xhr, status, error) {
            console.error('Failed to fetch menu engineering insights:', error);
            showNotification('Failed to fetch menu engineering insights.', 'error');
        }
    });
}