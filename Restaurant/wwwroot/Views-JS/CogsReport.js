let table;
let currentEndDate = null;
$(document).ready(function () {
    $('#dateRange').customRangeFilter({
        onApplyFilter: function (filterType, selectedDates) {
            startDate = DateFormatter.formatDashUS(selectedDates.startDate);
            endDate = DateFormatter.formatDashUS(selectedDates.endDate);
            currentEndDate = endDate;
            console.log('Filter Applied:', filterType, selectedDates);
            refreshCogsReportTable(startDate, endDate);
        },
        initialFilter: "Last 30 Days"
    });

    // Ensure DataTable is loaded before proceeding
    if (typeof $.fn.DataTable === 'undefined') {
        console.error('jQuery DataTable library is not loaded. Please check the CDN or script inclusion.');
        showNotification('DataTable library failed to load.', 'error');
        return;
    }

    // Initialize table with a message
    const tbody = $('.cogs-report-table tbody');
    tbody.empty();
    tbody.append('<tr><td colspan="10" class="text-center">Please apply a date filter to view the Inventory report.</td></tr>');
});


function initDataTable() {
    table = $('.cogs-report-table').DataTable({
        paging: true,
        searching: true,
        ordering: true,
        order: [[1, 'asc']],
        columnDefs: [
            { targets: [0, 3], orderable: false, searchable: false },
            { targets: [2, 4, 5, 6, 7, 8, 9, 10], className: 'dt-body-left' }
        ],
        pageLength: 10,
        lengthMenu: [10, 25, 50, 100],
        language: {
            search: '',
            searchPlaceholder: 'Search ingredients...'
        },
        dom: 'Bfrtip',
        buttons: [
            {
                extend: 'csvHtml5',
                text: '<i class="fas fa-file-csv"></i> Export CSV',
                className: 'btn btn-outline',
                footer: true,
                exportOptions: {
                    columns: [0, 1, 2, 3, 4, 5, 6, 8, 9, 10],
                    format: {
                        body: function (data, row, column, node) {
                            // Remove $ and extra spaces, keep negative sign
                            if (column === 0) {
                                return $(data).find('.ingredient-name').text() || data;
                            }
                            // For numeric columns, clean up but preserve negative values
                            let cleanData = data.replace(/\$/g, '').trim();
                            return cleanData;
                        }
                    }
                },
                customizeData: function (data) {
                    let totalCogs = $('#total-cogs-value').text() || '$0.00';
                    let totalFoodRevenue = $('#total-food-revenue-value').text() || '$0.00';
                    let actualFoodCost = $('#actual-food-cost-value').text() || '0%';
                    let theoreticalFoodCost = $('#theoretical-food-cost-value').text() || '0%';
                    let variancePercentage = $('#variance-percentage-value').text() || '0%';
                    let varianceValue = $('#variance-value').text() || '$0.00';

                    // Clean values - remove $ and spaces but keep negative sign
                    totalCogs = totalCogs.replace(/\$/g, '').trim();
                    totalFoodRevenue = totalFoodRevenue.replace(/\$/g, '').trim();
                    varianceValue = varianceValue.replace(/\$/g, '').trim();

                    // Add an empty row to separate the summary
                    data.body.push([]);


                    const emptyCols = ['', '', '', '', '', '', '', '', ''];

                    // Add summary rows
                    data.body.push(['Total Food Revenue', totalFoodRevenue, ...emptyCols]);
                    data.body.push(['Theoretical Food Cost %', theoreticalFoodCost, ...emptyCols]);
                    data.body.push(['Actual Food Cost %', actualFoodCost, ...emptyCols]);
                    data.body.push(['Variance %', variancePercentage, ...emptyCols]);
                    data.body.push(['Variance Value', varianceValue, ...emptyCols]);
                    data.body.push(['Total COGS', totalCogs, ...emptyCols]);
                }
            },

            {
                extend: 'pdfHtml5',
                text: '<i class="fas fa-file-pdf"></i> Export PDF',
                className: 'btn btn-primary',
                orientation: 'landscape',
                pageSize: 'A4',
                title: 'Inventory Report',
                footer: true,
                exportOptions: {
                    columns: [0, 1, 2, 3, 4, 5, 6, 8, 9, 10],
                    format: {
                        body: function (data, row, column, node) {
                            if (column === 0) {
                                return $(data).find('.ingredient-name').text() || data;
                            }
                            return data;
                        },
                        footer: function (data, column, node) {
                            if (column === 10) {
                                return data;
                            }
                            return '';
                        }
                    }
                },
                customize: function (doc) {
                    doc.content[1].table.footerRows = 1;

                    // Get values from footer or summary section
                    const totalCogs = $('#total-cogs-value').text() || '$0.00';
                    const totalFoodRevenue = $('#total-food-revenue-value').text() || '$0.00';
                    const actualFoodCost = $('#actual-food-cost-value').text() || '0%';
                    const theoreticalFoodCost = $('#theoretical-food-cost-value').text() || '0%';
                    const variancePercentage = $('#variance-percentage-value').text() || '0%';
                    const varianceValue = $('#variance-value').text() || '$0.00';

                    // Add a summary section below the table
                    doc.content.push({
                        margin: [0, 20, 0, 0],
                        table: {
                            widths: ['*', 'auto'],
                            body: [
                                [{ text: 'Total COGS:', bold: true, alignment: 'right' }, { text: totalCogs, alignment: 'right' }],
                                [{ text: 'Total Food Revenue:', bold: true, alignment: 'right' }, { text: totalFoodRevenue, alignment: 'right' }],
                                [{ text: 'Theoretical Food Cost %:', bold: true, alignment: 'right' }, { text: theoreticalFoodCost, alignment: 'right' }],
                                [{ text: 'Actual Food Cost %:', bold: true, alignment: 'right' }, { text: actualFoodCost, alignment: 'right' }],
                                [{ text: 'Variance %:', bold: true, alignment: 'right' }, { text: variancePercentage, alignment: 'right' }],
                                [{ text: 'Variance Value:', bold: true, alignment: 'right' }, { text: varianceValue, alignment: 'right' }]
                            ]
                        },
                        layout: 'noBorders'
                    });
                }
            }
        ]
    });

    // Move export buttons to custom container
    if (table.buttons && $('#exportButtons').length) {
        table.buttons().container().appendTo('#exportButtons');
    }

    // Make cells editable
    makeClosingCountEditable();

    // Custom search
    $('#ingredientSearch').on('keyup', function () {
        table.search(this.value).draw();
    });

    // Clear filter
    $('#clear-filter').on('click', function () {
        table.search('').draw();
        $('#ingredientSearch').val('').trigger('keyup');
    });
}

function updateFooterValues(data) {
    const totalCogs = data.totalCogs || 0;
    const totalFoodRevenue = data.totalFoodRevenue || 0;
    const actualFoodCost = data.actualFoodCostPercentage || 0;
    const theoreticalFoodCost = data.theoreticalFoodCostPercentage || 0;
    const variancePercentage = data.variancePercentage || 0;
    const varianceValue = data.varianceValue || 0;

    $('#total-cogs-value').text(`$${totalCogs.toFixed(2)}`);
    $('#total-food-revenue-value').text(`$${totalFoodRevenue.toFixed(2)}`);
    $('#actual-food-cost-value').text(`${actualFoodCost.toFixed(2)}%`);
    $('#theoretical-food-cost-value').text(`${theoreticalFoodCost.toFixed(2)}%`);
    $('#variance-percentage-value').text(`${variancePercentage.toFixed(2)}%`);
    $('#variance-value').text(`$${varianceValue.toFixed(2)}`);
}

function resetFooterValues() {
    $('#total-cogs-value').text('$0.00');
    $('#total-food-revenue-value').text('$0.00');
    $('#actual-food-cost-value').text('0%');
    $('#theoretical-food-cost-value').text('0%');
    $('#variance-percentage-value').text('0%');
    $('#variance-value').text('$0.00');
}


function refreshCogsReportTable(startDate = null, endDate = null) {
    if (!startDate || !endDate) {
        console.warn('Start date or end date missing.');
        showNotification('Please select a valid date range.', 'error');
        return;
    }

    const requestData = {
        startDate: startDate,
        endDate: endDate
    };

    ajaxRequest({
        url: '/api/ReportApi/CogsReport',
        method: 'POST',
        data: JSON.stringify(requestData),
        contentType: 'application/json',
        isQuery: false,
        showMessage: false,
        onSuccess: function (response) {
            const items = response.data.items || [];

            // If already initialized, destroy BEFORE touching the DOM
            if ($.fn.DataTable.isDataTable('.cogs-report-table')) {
                table.destroy();
            }

            const tbody = $('.cogs-report-table tbody');
            tbody.empty();

            if (items.length === 0) {
                tbody.append('<tr><td colspan="10" class="text-center">No data available for the selected date range.</td></tr>');
                resetFooterValues();
                initDataTable();
                return;
            }

            items.forEach(item => {
                const openingCount = Number(item.openingCount || 0);
                const openingValue = Number(item.openingValue || 0);
                const closingCount = Number(item.closingCount || 0);
                const closingValue = Number(item.closingValue || 0);
                const purchasesQuantity = Number(item.purchasesQuantity || 0);
                const purchasesValue = Number(item.purchasesValue || 0);
                const cogs = Number(item.cogs || 0);

                const row = `
        <tr>
            <td>
                <div class="ingredient-info"><p class="ingredient-name">${item.ingredientName || ''}</p></div>
            </td>
            <td>${item.unit || ''}</td>
            <td>${item.unitOfMeasure || ''}</td>
            <td>${item.vendorName || ''}</td>
            <td data-order="${openingCount}">${convertUnit(openingCount, item.unit, item.unitOfMeasure || item.unit)}</td>
            <td data-order="${openingValue}">$ ${openingValue.toFixed(2)}</td>
            <td data-order="${closingCount}" class="editable-closing" data-ingredient-id="${item.ingredientId || ''}" >
                ${convertUnit(closingCount, item.unit, item.unitOfMeasure || item.unit)}
            </td>
            <td data-order="${closingValue}">${closingValue.toFixed(2)}</td>
            <td data-order="${purchasesQuantity}">${purchasesQuantity.toFixed(2)}</td>
            <td data-order="${purchasesValue}">$ ${purchasesValue.toFixed(2)}</td>
            <td data-order="${cogs}">$${cogs.toFixed(2)}</td>
        </tr>`;

                tbody.append(row);
            });

            if (response.data && response.data.summary) {
                updateFooterValues(response.data.summary);
            } else {
                resetFooterValues();
            }

            initDataTable();
        },
        onError: function (xhr, status, error) {
            console.error('Failed to fetch Inventory report:', error);
            showNotification('Failed to fetch Inventory report.', 'error');
            const tbody = $('.cogs-report-table tbody');
            tbody.empty();
            tbody.append('<tr><td colspan="10" class="text-center">Failed to load report. Please try again.</td></tr>');
            resetFooterValues();
            initDataTable();
        }
    });
}


function makeClosingCountEditable() {
    $('.cogs-report-table tbody').off('click', 'td.editable-closing');
    $('.cogs-report-table tbody').on('click', 'td.editable-closing', function (e) {
        const $td = $(this);
        if ($td.find('input').length) return;

        const oldVal = $td.text().trim().replace(/,/g, '');
        const $input = $(`<input type="text" class="form-control form-control-sm" style="width:80px;" value="${oldVal}">`);
        $td.html($input);
        $input.focus().select();

        const finish = () => {
            const newVal = parseFloat($input.val().replace(/[^0-9.-]/g, '')) || 0;
            $td.text(newVal.toFixed(2));
            $td.removeClass('editing');

            const ingredientId = $td.attr('data-ingredient-id');
            if (!ingredientId) { console.warn('ingredientId missing'); return; }

            saveClosingCount({
                ingredientId: parseInt(ingredientId),
                closingCount: newVal,
                closingDate: currentEndDate
            });
        };

        $input.on('blur', finish)
            .on('keydown', function (e) { if (e.which === 13) finish(); });
    });
}

function saveClosingCount(payload) {
    ajaxRequest({
        url: '/api/InventoryApi/UpdateClosingCount',
        method: 'POST',
        data: JSON.stringify(payload),
        contentType: 'application/json',
        showMessage: false,
        onSuccess: function () {
            showNotification('Closing count saved', 'success');
            refreshCogsReportTable(startDate, currentEndDate);
        },
        onError: function (xhr) {
            showNotification('Failed to save closing count', 'error');
            console.error(xhr);
        }
    });
}