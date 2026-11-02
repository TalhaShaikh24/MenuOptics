// InventorySuiteReport.js — Inventory Suite Report page scripts.

let table;
let currentEndDate = null;
let startDate = null;
let reportsList = [];
let pendingClosingCounts = {};
let currentSelectedReportId = null;
let pendingTargetReportId = null;
let isSwitchingProgrammatically = false;
let lastEndDate = null;

let currentMode = 'UPDATE'; // 'INSERT' = new report draft, 'UPDATE' = saved report
let loadedReportItems = [];
let newReportClosingCounts = {};
let currentTableRequest = null;

$(document).ready(function () {
    $('#reportSelector').select2({
        placeholder: '-- Select Inventory --',
        width: '100%',
        minimumResultsForSearch: 10
    });

    $('#reportSelector').on('select2:selecting', function (e) {
        if (isSwitchingProgrammatically) return;

        const targetId = e.params && e.params.args && e.params.args.data ? e.params.args.data.id : null;
        if (!targetId || targetId === currentSelectedReportId) return;

        if (hasUnsavedChanges()) {
            e.preventDefault();
            $('#reportSelector').select2('close');
            pendingTargetReportId = targetId;
            openModal('unsavedChangesModal');
            return false;
        }
    });

    $('#reportSelector').on('change', function () {
        if (isSwitchingProgrammatically) return;

        const selectedId = $(this).val();

        // Safety net if 'change' fires despite the selecting guard above.
        if (hasUnsavedChanges() && selectedId !== currentSelectedReportId) {
            pendingTargetReportId = selectedId;
            isSwitchingProgrammatically = true;
            $(this).val(currentSelectedReportId).trigger('change.select2');
            isSwitchingProgrammatically = false;
            openModal('unsavedChangesModal');
            return;
        }

        if (selectedId) {
            currentSelectedReportId = selectedId;
            const report = reportsList.find(r => r.id === selectedId);
            if (report) {
                startDate = report.startDate;
                currentEndDate = report.endDate;
                pendingClosingCounts = {};
                newReportClosingCounts = {};

                if (report.isDraft) {
                    currentMode = 'INSERT';
                    $('#saveInventoryBtn').show().prop('disabled', false);
                    $('#updateInventoryBtn').hide().prop('disabled', true);
                    refreshInventorySuiteReportTable(report.startDate, report.endDate, true);
                } else {
                    currentMode = 'UPDATE';
                    $('#updateInventoryBtn').show().prop('disabled', true);
                    $('#saveInventoryBtn').hide();
                    refreshInventorySuiteReportTable(report.startDate, report.endDate, false);
                }
            }
        } else {
            currentSelectedReportId = null;
            $('#updateInventoryBtn').hide().prop('disabled', true);
            $('#saveInventoryBtn').hide();
        }
    });

    $('#cancelSwitchReportBtn').on('click', function () {
        pendingTargetReportId = null;
        closeModal('unsavedChangesModal');
    });

    $('#confirmSwitchReportBtn').on('click', function () {
        closeModal('unsavedChangesModal');

        pendingClosingCounts = {};
        newReportClosingCounts = {};
        $('.cogs-report-table td.editable-closing').removeClass('modified-count');
        $('#updateInventoryBtn').prop('disabled', true);

        reportsList = reportsList.filter(r => !r.isDraft);

        if (pendingTargetReportId === 'add_report_modal') {
            pendingTargetReportId = null;
            openSaveReportModal();
            return;
        }

        if (pendingTargetReportId) {
            const nextReportId = pendingTargetReportId;
            pendingTargetReportId = null;
            currentSelectedReportId = nextReportId;

            populateReportSelector(nextReportId);

            const report = reportsList.find(r => r.id === nextReportId);
            if (report) {
                startDate = report.startDate;
                currentEndDate = report.endDate;
                currentMode = report.isDraft ? 'INSERT' : 'UPDATE';
                if (currentMode === 'INSERT') {
                    $('#saveInventoryBtn').show().prop('disabled', false);
                    $('#updateInventoryBtn').hide();
                } else {
                    $('#updateInventoryBtn').show().prop('disabled', true);
                    $('#saveInventoryBtn').hide();
                }
                refreshInventorySuiteReportTable(report.startDate, report.endDate, report.isDraft);
            }
        }
    });

    function openSaveReportModal() {
        const today = new Date();
        const last30Days = new Date();
        last30Days.setDate(today.getDate() - 29);

        const formatDateVal = (d) => {
            const y = d.getFullYear();
            const m = String(d.getMonth() + 1).padStart(2, '0');
            const day = String(d.getDate()).padStart(2, '0');
            return `${y}-${m}-${day}`;
        };

        const todayStr = formatDateVal(today);
        const minStartDateStr = formatToDateInputVal(lastEndDate);

        if (minStartDateStr) {
            $('#reportStartDate').attr('min', minStartDateStr);
            $('#reportStartDate').val(minStartDateStr);

            $('#reportEndDate').attr('min', minStartDateStr);
            if (todayStr >= minStartDateStr) {
                $('#reportEndDate').val(todayStr);
            } else {
                $('#reportEndDate').val(minStartDateStr);
            }

            $('#startDateMinHintVal').text(safeFormatUS(lastEndDate));
            $('#startDateMinHint').show();
        } else {
            $('#reportStartDate').removeAttr('min');
            $('#reportEndDate').removeAttr('min');
            $('#reportStartDate').val(formatDateVal(last30Days));
            $('#reportEndDate').val(todayStr);
            $('#startDateMinHint').hide();
        }

        setBtnLoading($('#saveInventoryBtn'), false);
        openModal('addReportModal');
    }

    $('#reportStartDate').on('change', function () {
        const chosenStart = $(this).val();
        if (chosenStart) {
            $('#reportEndDate').attr('min', chosenStart);
            if ($('#reportEndDate').val() && $('#reportEndDate').val() < chosenStart) {
                $('#reportEndDate').val(chosenStart);
            }
        }
    });

    $('#openSaveReportModalBtn').on('click', function () {
        if ($(this).is(':disabled')) return;

        if (hasUnsavedChanges()) {
            pendingTargetReportId = 'add_report_modal';
            openModal('unsavedChangesModal');
            return;
        }
        openSaveReportModal();
    });

    // 'Start' only sets up a draft; saving happens later via Insert Inventory.
    $('#saveReportBtn').on('click', function () {
        const sDate = $('#reportStartDate').val();
        const eDate = $('#reportEndDate').val();

        if (!sDate) {
            showNotification('Please select a start date.', 'error');
            return;
        }
        if (!eDate) {
            showNotification('Please select an end date.', 'error');
            return;
        }

        const minStartDateStr = formatToDateInputVal(lastEndDate);
        if (minStartDateStr && sDate < minStartDateStr) {
            showNotification(`Start date cannot be before last inventory's end date (${safeFormatUS(lastEndDate)}).`, 'error');
            return;
        }

        if (new Date(sDate) > new Date(eDate)) {
            showNotification('Start date cannot be after end date.', 'error');
            return;
        }

        const $btn = $(this);
        setBtnLoading($btn, true);

        closeModal('addReportModal');

        currentMode = 'INSERT';
        startDate = sDate;
        currentEndDate = eDate;
        pendingClosingCounts = {};
        newReportClosingCounts = {};

        reportsList = reportsList.filter(r => !r.isDraft);

        const draftId = 'draft_' + Date.now();
        const draftReport = {
            id: draftId,
            text: `New Inventory: ${safeFormatUS(sDate)} - ${safeFormatUS(eDate)} (Draft)`,
            startDate: safeFormatDash(sDate),
            endDate: safeFormatDash(eDate),
            isDraft: true
        };

        reportsList.unshift(draftReport);
        populateReportSelector(draftId);

        $('#saveInventoryBtn').show().prop('disabled', false);
        $('#updateInventoryBtn').hide().prop('disabled', true);

        refreshInventorySuiteReportTable(startDate, currentEndDate, true);
    });

    $('#saveInventoryBtn').on('click', function () {
        if ($(this).is(':disabled')) return;

        if (!startDate || !currentEndDate) {
            showNotification('Invalid date range for inventory.', 'error');
            return;
        }

        if (!loadedReportItems || loadedReportItems.length === 0) {
            showNotification('No ingredients found to insert.', 'error');
            return;
        }

        const $btn = $(this);
        setBtnLoading($btn, true);

        // Merge any on-screen inputs not yet tracked before submitting.
        $('.new-report-closing-input').each(function () {
            const $inp = $(this);
            const rawVal = $inp.val();
            const ingId = parseInt($inp.attr('data-ingredient-id'), 10);
            const unitPrice = parseFloat($inp.attr('data-unit-price')) || 0;
            if (rawVal !== '' && !isNaN(ingId) && newReportClosingCounts[ingId] === undefined) {
                const countVal = parseFloat(rawVal) || 0;
                newReportClosingCounts[ingId] = {
                    ingredientId: ingId,
                    closingCount: countVal,
                    closingValue: countVal * unitPrice
                };
            }
        });

        const itemsPayload = Object.values(newReportClosingCounts).map(item => ({
            ingredientId: parseInt(item.ingredientId, 10),
            closingCount: parseFloat(item.closingCount) || 0,
            closingValue: parseFloat(item.closingValue) || 0
        }));

        if (itemsPayload.length === 0) {
            setBtnLoading($btn, false);
            showNotification('Please enter closing count for at least one ingredient.', 'warning');
            return;
        }

        const payload = {
            startDate: startDate,
            endDate: currentEndDate,
            items: itemsPayload
        };

        ajaxRequest({
            url: '/api/InventorySuiteReportApi/AddInventorySuiteReport',
            method: 'POST',
            data: JSON.stringify(payload),
            contentType: 'application/json',
            showMessage: false,
            onSuccess: function (response) {
                showNotification(response.responseMsg || 'Inventory created successfully.', 'success');

                reportsList = reportsList.filter(r => !r.isDraft);

                currentMode = 'UPDATE';
                $('#saveInventoryBtn').hide();
                $('#updateInventoryBtn').show().prop('disabled', true);
                newReportClosingCounts = {};
                pendingClosingCounts = {};

                const newReportId = (response && response.data && response.data.inventorySuiteId)
                    ? response.data.inventorySuiteId
                    : null;

                loadInventorySuiteReports(newReportId);
            },
            onError: function (xhr) {
                const errorMsg = (xhr.responseJSON && xhr.responseJSON.responseMsg)
                    ? xhr.responseJSON.responseMsg
                    : 'Failed to insert inventory.';
                showNotification(errorMsg, 'error');
            },
            onComplete: function () { setBtnLoading($btn, false); }
        });
    });

    $('#updateInventoryBtn').on('click', function () {
        if ($(this).is(':disabled')) return;

        const suiteId = parseInt(currentSelectedReportId, 10);
        if (!suiteId || isNaN(suiteId)) {
            showNotification('Please select a valid inventory first.', 'error');
            return;
        }

        const itemsToUpdate = Object.values(pendingClosingCounts).map(item => ({
            ingredientId: parseInt(item.ingredientId, 10),
            closingCount: parseFloat(item.closingCount),
            closingValue: parseFloat(item.closingValue)
        }));

        if (itemsToUpdate.length === 0) {
            showNotification('No modified closing counts found to update.', 'info');
            return;
        }

        const payload = {
            inventorySuiteId: suiteId,
            items: itemsToUpdate
        };

        const $btn = $(this);
        setBtnLoading($btn, true);

        ajaxRequest({
            url: '/api/InventorySuiteReportApi/UpdateInventorySuiteReport',
            method: 'POST',
            data: JSON.stringify(payload),
            contentType: 'application/json',
            showMessage: false,
            onSuccess: function (response) {
                setBtnLoading($btn, false);

                $('.cogs-report-table td.editable-closing').each(function () {
                    const val = parseFloat($(this).text().trim().replace(/,/g, '')) || 0;
                    $(this).attr('data-original-count', val.toFixed(2));
                    $(this).removeClass('modified-count');
                });

                pendingClosingCounts = {};
                $btn.prop('disabled', true);

                showNotification(response.responseMsg || 'Closing counts updated successfully.', 'success');

                refreshInventorySuiteReportTable(startDate, currentEndDate, false);
            },
            onError: function (xhr) {
                setBtnLoading($btn, false);
                console.error('Failed to update closing counts:', xhr);
                const errorMsg = (xhr.responseJSON && xhr.responseJSON.responseMsg)
                    ? xhr.responseJSON.responseMsg
                    : 'Failed to update closing counts.';
                showNotification(errorMsg, 'error');
            }
        });
    });

    $(document).off('input change', '.new-report-closing-input');
    $(document).on('input change', '.new-report-closing-input', function () {
        const $input = $(this);
        const ingredientId = parseInt($input.attr('data-ingredient-id'), 10);
        const unitPrice = parseFloat($input.attr('data-unit-price')) || 0;
        const rawVal = $input.val();
        if (rawVal.trim() === '') {
            delete newReportClosingCounts[ingredientId];
        } else {
            const countVal = parseFloat(rawVal) || 0;
            const closingVal = countVal * unitPrice;
            newReportClosingCounts[ingredientId] = {
                ingredientId: ingredientId,
                closingCount: countVal,
                closingValue: closingVal
            };
        }

        const countVal = parseFloat(rawVal) || 0;
        const closingVal = countVal * unitPrice;

        const $row = $input.closest('tr');
        $row.find(`#closingValue_${ingredientId}`).text('$' + closingVal.toFixed(2));

        const openingVal = parseFloat($row.find('td:nth-child(5)').text().replace(/[$,]/g, '')) || 0;
        const purchasesVal = parseFloat($row.find('td:nth-child(7)').text().replace(/[$,]/g, '')) || 0;
        const rowCogs = (openingVal + purchasesVal) - closingVal;
        $row.find('td:nth-child(10)').text('$' + rowCogs.toFixed(2));

        recalculateNewReportSummary();
    });

    if (typeof $.fn.DataTable === 'undefined') {
        console.error('jQuery DataTable library is not loaded. Please check the CDN or script inclusion.');
        showNotification('DataTable library failed to load.', 'error');
        return;
    }

    loadInventorySuiteReports();
});

function loadInventorySuiteReports(preselectReportId = null) {
    showTableLoading('Loading inventory...');
    ajaxRequest({
        url: '/api/InventorySuiteReportApi/GetInventorySuiteReports',
        method: 'GET',
        contentType: 'application/json',
        isQuery: false,
        showMessage: false,
        onSuccess: function (response) {
            const rawReports = (response && response.data) ? response.data : [];

            lastEndDate = (rawReports && rawReports.length > 0)
                ? (rawReports[rawReports.length - 1].endDate ?? rawReports[rawReports.length - 1].EndDate ?? null)
                : null;

            $('#openSaveReportModalBtn').prop('disabled', false);

            reportsList = rawReports.map(r => {
                const reportId = (r.inventorySuiteId ?? r.InventorySuiteId ?? '').toString();
                const rawStart = r.startDate ?? r.StartDate;
                const rawEnd = r.endDate ?? r.EndDate;

                const displayStart = safeFormatUS(rawStart);
                const displayEnd = safeFormatUS(rawEnd);

                return {
                    id: reportId,
                    text: `Inventory: ${displayStart} - ${displayEnd}`,
                    startDate: safeFormatDash(rawStart),
                    endDate: safeFormatDash(rawEnd),
                    isDraft: false
                };
            });

            if (reportsList.length > 0) {
                let targetId = reportsList[0].id;
                if (preselectReportId) {
                    const match = reportsList.find(r => r.id === preselectReportId.toString());
                    if (match) targetId = match.id;
                }

                currentMode = 'UPDATE';
                $('#saveInventoryBtn').hide();
                $('#updateInventoryBtn').show().prop('disabled', true);

                populateReportSelector(targetId);

                const activeReport = reportsList.find(r => r.id === targetId);
                if (activeReport) {
                    startDate = activeReport.startDate;
                    currentEndDate = activeReport.endDate;
                    refreshInventorySuiteReportTable(startDate, currentEndDate, false);
                } else {
                    hideTableLoading();
                }
            } else {
                hideTableLoading();
                populateReportSelector(null);
                $('#updateInventoryBtn').hide().prop('disabled', true);
                $('#saveInventoryBtn').hide();
                const tbody = $('.cogs-report-table tbody');
                tbody.empty();
                tbody.append('<tr><td colspan="10" class="text-center">No inventory found. Please click "Start Inventory" to create a inventory.</td></tr>');
            }
        },
        onError: function (xhr) {
            hideTableLoading();
            $('#openSaveReportModalBtn').prop('disabled', false);
            showNotification('Failed to load inventory.', 'error');
            const tbody = $('.cogs-report-table tbody');
            tbody.empty();
            tbody.append('<tr><td colspan="10" class="text-center">Failed to load inventory. Please refresh the page.</td></tr>');
        }
    });
}

function populateReportSelector(selectedReportId = null) {
    const $selector = $('#reportSelector');
    $selector.empty();
    $selector.append(new Option('-- Select Inventory --', ''));

    reportsList.forEach(report => {
        const isSelected = report.id === selectedReportId;
        const opt = new Option(report.text, report.id, false, isSelected);
        $selector.append(opt);
    });

    currentSelectedReportId = selectedReportId;
    isSwitchingProgrammatically = true;
    $selector.val(selectedReportId).trigger('change.select2');
    isSwitchingProgrammatically = false;
}

function refreshInventorySuiteReportTable(startDate = null, endDate = null, isNewReport = false) {
    if (!startDate || !endDate) {
        console.warn('Start date or end date missing.');
        showNotification('Please select a valid date range.', 'error');
        return;
    }

    showTableLoading('Loading inventory...');

    currentTableRequest = { startDate: startDate, endDate: endDate, isNewReport: isNewReport };

    if (table) {
        // The old flow rebuilt the table on every refresh: page one, search cleared.
        table.search('');
        table.page.len(10);
        table.ajax.reload();
    } else {
        initReportTable();
    }
}

function initReportTable() {
    table = initDataTable('.cogs-report-table', {
        processing: true,
        loadingMessage: 'Loading inventory...',
        ajax: function (dtParams, callback, settings) {
            const requestData = {
                inventorySuiteId: currentTableRequest.isNewReport ? null : currentSelectedReportId,
                startDate: currentTableRequest.startDate,
                endDate: currentTableRequest.endDate
            };

            $.ajax({
                url: '/api/InventorySuiteReportApi/InventorySuiteReport',
                method: 'POST',
                data: JSON.stringify(requestData),
                contentType: 'application/json',
                success: function (response) {
                    hideTableLoading();
                    pendingClosingCounts = {};

                    const items = (response && response.data && response.data.items) ? response.data.items : [];
                    loadedReportItems = items;

                    if (currentTableRequest.isNewReport) {
                        $('#saveInventoryBtn').show().prop('disabled', false);
                        $('#updateInventoryBtn').hide();
                    } else {
                        $('#updateInventoryBtn').show().prop('disabled', true);
                        $('#saveInventoryBtn').hide();
                    }

                    if (response.data && response.data.summary) {
                        updateFooterValues(response.data.summary);
                    } else {
                        resetFooterValues();
                    }

                    if (items.length === 0) {
                        settings.oLanguage.sEmptyTable = 'No data available for the selected date range.';
                    }

                    callback({ data: items });
                },
                error: function (xhr, status, error) {
                    hideTableLoading();
                    console.error('Failed to fetch Inventory:', error);
                    showNotification('Failed to fetch Inventory.', 'error');
                    resetFooterValues();
                    settings.oLanguage.sEmptyTable = 'Failed to load inventory. Please try again.';
                    callback({ data: [] });
                },
                complete: function () { setBtnLoading($('#saveReportBtn'), false); }
            });
        },
        columns: [
            {
                data: 'ingredientName',
                render: function (name, type, row) {
                    return `<div class="ingredient-info" >
                                <p class="ingredient-name">${row.ingredientName.trim().includes(' ') ? row.ingredientName.split(' ').slice(1).join(' ') : row.ingredientName}</p>
                                <p class="ingredient-meta">Brand: ${row.ingredientName.split(' ')[0]}</p>
                               ${row.packSize == null ? `<p class="ingredient-meta text-highlight danger">Catch Weight Item</p>` : `<p class="ingredient-meta text-highlight">Pack: ${row.packSize}</p>`}
                            </div >`;
                }
            },
            {
                data: 'size',
                render: function (data, type, row) {
                    return `<span class="${row.unit != null ? 'category-badge-size' : ''}">
                                ${row.unit
                            ? `${Number.isInteger(parseFloat(row.size))
                                ? parseInt(row.size, 10)
                                : parseFloat(row.size)} ${row.unit}`
                            : ''
                        }
                            </span>`;
                }
            },
            {
                data: 'vendorName',
                render: function (data) { return data || '(Me)'; }
            },
            {
                data: 'openingCount',
                render: function (data) { return Number(data || 0).toFixed(2); }
            },
            {
                data: 'openingValue',
                render: function (data) { return `$${Number(data || 0).toFixed(2)}`; }
            },
            {
                data: 'purchasesQuantity',
                render: function (data) { return Number(data || 0).toFixed(2); }
            },
            {
                data: 'purchasesValue',
                render: function (data) { return `$${Number(data || 0).toFixed(2)}`; }
            },
            {
                data: 'closingCount',
                render: function (data, type, row) {
                    if (currentTableRequest.isNewReport) {
                        const closingCount = Number(row.closingCount || 0);
                        const saved = newReportClosingCounts[row.ingredientId];
                        const initialVal = (saved !== undefined)
                            ? saved.closingCount
                            : (closingCount > 0 ? closingCount : '');

                        return `<input type="number" step="any" min="0"
                                   class="form-control form-control-sm new-report-closing-input"
                                   data-ingredient-id="${row.ingredientId}"
                                   data-unit-price="${row.price}"
                                   value="${initialVal !== '' ? Number(initialVal).toFixed(2) : ''}"
                                   placeholder="0.00"
                                   style="width: 85px; text-align: right; font-weight: 600; padding: 4px 6px; border: 1px solid #f08d11; border-radius: 6px;">`;
                    }

                    return Number(data || 0).toFixed(2);
                },
                createdCell: function (td, cellData, row) {
                    if (currentTableRequest.isNewReport) {
                        $(td).addClass('dt-body-left');
                    } else {
                        $(td).addClass('editable-closing')
                            .attr('data-ingredient-id', row.ingredientId || '')
                            .attr('data-original-count', Number(row.closingCount || 0).toFixed(2));
                    }
                }
            },
            {
                data: 'closingValue',
                render: function (data, type, row) {
                    if (currentTableRequest.isNewReport) {
                        const closingCount = Number(row.closingCount || 0);
                        const saved = newReportClosingCounts[row.ingredientId];
                        const initialVal = (saved !== undefined)
                            ? saved.closingCount
                            : (closingCount > 0 ? closingCount : '');
                        const displayVal = (saved !== undefined)
                            ? saved.closingValue
                            : (initialVal !== '' ? (parseFloat(initialVal) * row.price) : 0);

                        return `$${Number(displayVal).toFixed(2)}`;
                    }

                    return `$${Number(data || 0).toFixed(2)}`;
                },
                createdCell: function (td, cellData, row) {
                    $(td).attr('id', `closingValue_${row.ingredientId}`);
                }
            },
            {
                data: 'cogs',
                render: function (data) { return `$${Number(data || 0).toFixed(2)}`; }
            }
        ],
        order: [[1, 'asc']],
        autoWidth: false,
        columnDefs: [
            { width: '150px', targets: 0 },
            { width: '80px', targets: 1 }
        ],
        language: {
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
                    columns: [0, 1, 2, 3, 4, 5, 6, 7, 8, 9],
                    format: {
                        body: function (data, row, column, node) {
                            if (column === 0) {
                                return $(data).find('.ingredient-name').text().trim() || cleanExportCell(data);
                            }
                            let cleanData = cleanExportCell(data).replace(/\$/g, '').trim();
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

                    totalCogs = totalCogs.replace(/\$/g, '').trim();
                    totalFoodRevenue = totalFoodRevenue.replace(/\$/g, '').trim();
                    varianceValue = varianceValue.replace(/\$/g, '').trim();

                    data.body.push([]);

                    const emptyCols = ['', '', '', '', '', '', '', ''];

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
                title: 'Inventory',
                footer: true,
                exportOptions: {
                    columns: [0, 1, 2, 3, 4, 5, 6, 7, 8, 9],
                    format: {
                        body: function (data, row, column, node) {
                            if (column === 0) {
                                return $(data).find('.ingredient-name').text().trim() || cleanExportCell(data);
                            }
                            return cleanExportCell(data);
                        },
                        footer: function (data, column, node) {
                            if (column === 9) {
                                return data;
                            }
                            return '';
                        }
                    }
                },
                customize: function (doc) {
                    doc.content[1].table.footerRows = 1;

                    const totalCogs = $('#total-cogs-value').text() || '$0.00';
                    const totalFoodRevenue = $('#total-food-revenue-value').text() || '$0.00';
                    const actualFoodCost = $('#actual-food-cost-value').text() || '0%';
                    const theoreticalFoodCost = $('#theoretical-food-cost-value').text() || '0%';
                    const variancePercentage = $('#variance-percentage-value').text() || '0%';
                    const varianceValue = $('#variance-value').text() || '$0.00';

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
        ],
        drawCallback: function () {
            if (currentMode === 'INSERT') {
                // Restore typed counts after paging redraws.
                $('.new-report-closing-input').each(function () {
                    const $inp = $(this);
                    const ingId = parseInt($inp.attr('data-ingredient-id'), 10);
                    if (newReportClosingCounts[ingId] !== undefined) {
                        $inp.val(Number(newReportClosingCounts[ingId].closingCount).toFixed(2));
                        $(`#closingValue_${ingId}`).text('$' + Number(newReportClosingCounts[ingId].closingValue).toFixed(2));
                    }
                });
            }
        }
    });

    if (table.buttons && $('#exportButtons').length) {
        table.buttons().container().appendTo('#exportButtons');
    }

    // Delegated on the tbody, so it survives redraws; inert in INSERT mode (no editable cells).
    makeClosingCountEditable();

    $('#ingredientSearch').off('keyup').on('keyup', function () {
        table.search(this.value).draw();
    });

    $('#clear-filter').off('click').on('click', function () {
        table.search('').draw();
        $('#ingredientSearch').val('').trigger('keyup');
    });
}


function makeClosingCountEditable() {
    $('.cogs-report-table tbody').off('click', 'td.editable-closing');
    $('.cogs-report-table tbody').on('click', 'td.editable-closing', function (e) {
        const $closingCountTd = $(this);
        if ($closingCountTd.find('input').length) return;

        const ingredientId = $closingCountTd.attr('data-ingredient-id');
        if (!ingredientId) { console.warn('ingredientId missing'); return; }

        const oldClosingCount = parseFloat($closingCountTd.text().trim().replace(/,/g, '')) || 0;
        const $closingCountInput = $(`<input type="number" step="any" class="form-control form-control-sm" style="width:85px; text-align:right; font-weight:600;" value="${oldClosingCount}">`);
        $closingCountTd.html($closingCountInput);
        $closingCountInput.focus().select();

        const $closingValueTd = $('#closingValue_' + ingredientId);
        const oldClosingValue = parseFloat($closingValueTd.text().replace(/[$,]/g, '')) || 0;
        const price = oldClosingCount > 0 ? (oldClosingValue / oldClosingCount) : 0;

        const finish = () => {
            const inputVal = $closingCountInput.val();
            const newClosingCount = parseFloat(inputVal.replace(/[^0-9.-]/g, '')) || 0;
            $closingCountTd.text(newClosingCount.toFixed(2));
            $closingCountTd.removeClass('editing');

            const newClosingValue = newClosingCount * price;
            $closingValueTd.text('$' + newClosingValue.toFixed(2));

            const originalCount = parseFloat($closingCountTd.attr('data-original-count')) || 0;
            const isChanged = Math.abs(newClosingCount - originalCount) > 0.001;

            if (isChanged) {
                $closingCountTd.addClass('modified-count');
                pendingClosingCounts[ingredientId] = {
                    ingredientId: parseInt(ingredientId),
                    closingCount: newClosingCount,
                    closingValue: newClosingValue,
                    closingDate: currentEndDate
                };
            } else {
                $closingCountTd.removeClass('modified-count');
                delete pendingClosingCounts[ingredientId];
            }

            const hasChanges = Object.keys(pendingClosingCounts).length > 0;
            $('#updateInventoryBtn').prop('disabled', !hasChanges);

            const $row = $closingCountTd.closest('tr');
            const openingVal = parseFloat($row.find('td:nth-child(5)').text().replace(/[$,]/g, '')) || 0;
            const purchasesVal = parseFloat($row.find('td:nth-child(7)').text().replace(/[$,]/g, '')) || 0;
            const rowCogs = (openingVal + purchasesVal) - newClosingValue;
            $row.find('td:nth-child(10)').text('$' + rowCogs.toFixed(2));
        };

        $closingCountInput.on('blur', finish)
            .on('keydown', function (e) { if (e.which === 13) finish(); });
    });
}

function hasUnsavedChanges() {
    if (currentMode === 'INSERT') {
        return Object.keys(newReportClosingCounts).length > 0;
    }
    return Object.keys(pendingClosingCounts).length > 0;
}

function safeFormatUS(dateStr) {
    if (!dateStr) return '';
    if (typeof dateStr === 'string' && dateStr.includes('-')) {
        const parts = dateStr.split('T')[0].split('-');
        if (parts.length === 3 && parts[0].length === 4) {
            const d = new Date(parseInt(parts[0], 10), parseInt(parts[1], 10) - 1, parseInt(parts[2], 10));
            return DateFormatter.formatUS(d) || dateStr;
        }
    }
    return DateFormatter.formatUS(dateStr) || dateStr;
}

function safeFormatDash(dateStr) {
    if (!dateStr) return '';
    if (typeof dateStr === 'string' && dateStr.includes('-')) {
        const parts = dateStr.split('T')[0].split('-');
        if (parts.length === 3 && parts[0].length === 4) {
            const d = new Date(parseInt(parts[0], 10), parseInt(parts[1], 10) - 1, parseInt(parts[2], 10));
            return DateFormatter.formatDashUS(d) || dateStr;
        }
    }
    return DateFormatter.formatDashUS(dateStr) || dateStr;
}

function formatToDateInputVal(dateVal) {
    if (!dateVal) return '';
    if (typeof dateVal === 'string') {
        const isoMatch = dateVal.split('T')[0];
        if (/^\d{4}-\d{2}-\d{2}$/.test(isoMatch)) {
            return isoMatch;
        }
    }
    const d = new Date(dateVal);
    if (isNaN(d.getTime())) return '';
    const y = d.getFullYear();
    const m = String(d.getMonth() + 1).padStart(2, '0');
    const day = String(d.getDate()).padStart(2, '0');
    return `${y}-${m}-${day}`;
}

// hideAll because load/refresh calls here are not strictly paired.
function showTableLoading(message = 'Loading inventory...') {
    LoadingComponent.showForTable('.cogs-report-table', message);
}

function hideTableLoading() {
    LoadingComponent.hideAllForTable('.cogs-report-table');
}

// Plain-text value of a cell for export; uses the input's value when the cell holds one.
function cleanExportCell(data) {
    if (data == null) return '';
    const $el = $('<div>').html(data);
    const $inp = $el.find('input');
    if ($inp.length) {
        return $inp.val() || '0.00';
    }
    const text = $el.text();
    return text.replace(/\s+/g, ' ').trim();
}

function recalculateNewReportSummary() {
    if (!loadedReportItems || loadedReportItems.length === 0) return;

    let totalCogs = 0;

    loadedReportItems.forEach(item => {
        const openingVal = Number(item.openingValue || 0);
        const purchasesVal = Number(item.purchasesValue || 0);
        const unitPrice = item.price;

        let closingVal = 0;
        if (newReportClosingCounts[item.ingredientId] !== undefined) {
            closingVal = newReportClosingCounts[item.ingredientId].closingValue;
        } else {
            const $inp = $(`input.new-report-closing-input[data-ingredient-id="${item.ingredientId}"]`);
            if ($inp.length) {
                closingVal = (parseFloat($inp.val()) || 0) * unitPrice;
            } else {
                closingVal = Number(item.closingValue || 0);
            }
        }

        totalCogs += (openingVal + purchasesVal - closingVal);
    });

    $('#total-cogs-value').text(`$${totalCogs.toFixed(2)}`);
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
