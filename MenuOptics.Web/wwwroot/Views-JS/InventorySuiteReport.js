/* ============================================================================
 * InventorySuiteReport.js — Inventory / InventorySuite report page (Views/Ingredient/Index.cshtml)
 * Date-filtered InventorySuiteReport table with editable closing counts and CSV/PDF export.
 *
 * Project convention — every Views-JS file follows this section order:
 *   1. Constants  2. State  3. Initialization  4. Event bindings
 *   5. AJAX / CRUD  6. Helpers  7. Utilities
 * ==========================================================================*/

/* ── 2. State ─────────────────────────────────────────────────────────── */

let table;
let currentEndDate = null;
let startDate = null;
let reportsList = [];
let pendingClosingCounts = {};
let currentSelectedReportId = null;
let pendingTargetReportId = null;
let isSwitchingProgrammatically = false;
let lastEndDate = null;

let currentMode = 'UPDATE'; // 'INSERT' (new report draft) or 'UPDATE' (saved report)
let loadedReportItems = [];
let newReportClosingCounts = {};

function hasUnsavedChanges() {
    if (currentMode === 'INSERT') {
        return Object.keys(newReportClosingCounts).length > 0;
    }
    return Object.keys(pendingClosingCounts).length > 0;
}

/* ── 3. Initialization / 4. Event bindings ────────────────────────────── */

$(document).ready(function () {
    // Initialize select2 on #reportSelector
    $('#reportSelector').select2({
        placeholder: '-- Select Inventory --',
        width: '100%',
        minimumResultsForSearch: 10
    });

    // Intercept report selection before change if unsaved closing counts exist
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

        // Fallback safeguard in case change event fired
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

    // Unsaved changes warning modal handlers
    $('#cancelSwitchReportBtn').on('click', function () {
        pendingTargetReportId = null;
        closeModal('unsavedChangesModal');
    });

    $('#confirmSwitchReportBtn').on('click', function () {
        closeModal('unsavedChangesModal');

        // Discard unsaved changes
        pendingClosingCounts = {};
        newReportClosingCounts = {};
        $('.cogs-report-table td.editable-closing').removeClass('modified-count');
        $('#updateInventoryBtn').prop('disabled', true);

        // If discarding from a draft, remove the draft item from reportsList
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

    // Add Report modal triggers
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
            // Apply condition: Start Date cannot be before last record's End Date
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

        $('#saveInventoryBtn').prop('disabled', false).html('Save Inventory');
        openModal('addReportModal');
    }

    // Keep End Date min synced with chosen Start Date
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

    // "Start" button inside Add Report modal
    // Does NOT call AddInventorySuiteReport directly!
    // Instead, sets up draft in dropdown, fetches ingredients, renders inputs for closing count, and shows Insert Report button.
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

        closeModal('addReportModal');

        // Switch to INSERT mode
        currentMode = 'INSERT';
        startDate = sDate;
        currentEndDate = eDate;
        pendingClosingCounts = {};
        newReportClosingCounts = {};

        // Remove any previous unconfirmed draft
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

        // Show Insert Report button, hide Update Report button
        $('#saveInventoryBtn').show().prop('disabled', false);
        $('#updateInventoryBtn').hide().prop('disabled', true);

        // Fetch ingredients for this date range with closing count inputs
        refreshInventorySuiteReportTable(startDate, currentEndDate, true);
    });

    // Insert Report button handler (calls AddInventorySuiteReport with all items)
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
        $btn.prop('disabled', true).html('<i class="fas fa-spinner fa-spin"></i> Submitting...');

        // Ensure any visible inputs currently on screen are captured
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

        // Only send ingredients whose closing count was entered / changed by user
        const itemsPayload = Object.values(newReportClosingCounts).map(item => ({
            ingredientId: parseInt(item.ingredientId, 10),
            closingCount: parseFloat(item.closingCount) || 0,
            closingValue: parseFloat(item.closingValue) || 0
        }));

        if (itemsPayload.length === 0) {
            $btn.prop('disabled', false).html('<i class="fas fa-plus-circle"></i> Insert Inventory');
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
                $btn.prop('disabled', false).html('<i class="fas fa-plus-circle"></i> Insert Inventory');
                showNotification(response.responseMsg || 'Inventory created successfully.', 'success');

                // Remove draft from list
                reportsList = reportsList.filter(r => !r.isDraft);

                // Switch back to UPDATE mode for newly created report
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
                $btn.prop('disabled', false).html('<i class="fas fa-plus-circle"></i> Insert Inventory');
                const errorMsg = (xhr.responseJSON && xhr.responseJSON.responseMsg)
                    ? xhr.responseJSON.responseMsg
                    : 'Failed to insert inventory.';
                showNotification(errorMsg, 'error');
            }
        });
    });

    // Update closing count button handler
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
        $btn.prop('disabled', true).html('<i class="fas fa-spinner fa-spin"></i> Updating...');

        ajaxRequest({
            url: '/api/InventorySuiteReportApi/UpdateInventorySuiteReport',
            method: 'POST',
            data: JSON.stringify(payload),
            contentType: 'application/json',
            showMessage: false,
            onSuccess: function (response) {
                $btn.html('<i class="fas fa-check-circle"></i> Update Inventory');

                // Update data-original-count to the new values
                $('.cogs-report-table td.editable-closing').each(function () {
                    const val = parseFloat($(this).text().trim().replace(/,/g, '')) || 0;
                    $(this).attr('data-original-count', val.toFixed(2));
                    $(this).removeClass('modified-count');
                });

                // Clear pending tracking and disable button
                pendingClosingCounts = {};
                $btn.prop('disabled', true);

                showNotification(response.responseMsg || 'Closing counts updated successfully.', 'success');

                // Refresh table with updated calculations from database
                refreshInventorySuiteReportTable(startDate, currentEndDate, false);
            },
            onError: function (xhr) {
                $btn.prop('disabled', false).html('<i class="fas fa-check-circle"></i> Update Inventory');
                console.error('Failed to update closing counts:', xhr);
                const errorMsg = (xhr.responseJSON && xhr.responseJSON.responseMsg)
                    ? xhr.responseJSON.responseMsg
                    : 'Failed to update closing counts.';
                showNotification(errorMsg, 'error');
            }
        });
    });

    // Event listener for live typing in New Report closing inputs
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

    // Ensure DataTable is loaded before proceeding
    if (typeof $.fn.DataTable === 'undefined') {
        console.error('jQuery DataTable library is not loaded. Please check the CDN or script inclusion.');
        showNotification('DataTable library failed to load.', 'error');
        return;
    }

    // Load reports from API into Select2 dropdown
    loadInventorySuiteReports();
});

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

/* Loading goes through the shared LoadingComponent (js/loadingComponent.js)
 * so this table shows the same indicator as every other DataTable.
 * hideAll is used because load/refresh calls here are not strictly paired. */
function showTableLoading(message = 'Loading inventory...') {
    LoadingComponent.showForTable('.cogs-report-table', message);
}

function hideTableLoading() {
    LoadingComponent.hideAllForTable('.cogs-report-table');
}

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

            // Get last record's endDate
            lastEndDate = (rawReports && rawReports.length > 0)
                ? (rawReports[rawReports.length - 1].endDate ?? rawReports[rawReports.length - 1].EndDate ?? null)
                : null;

            // Enable Add Report button now that API response is received
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
            // Enable button so user can still try adding a report if needed
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

/* ── 5. AJAX / CRUD ───────────────────────────────────────────────────── */

function refreshInventorySuiteReportTable(startDate = null, endDate = null, isNewReport = false) {
    if (!startDate || !endDate) {
        console.warn('Start date or end date missing.');
        showNotification('Please select a valid date range.', 'error');
        return;
    }

    showTableLoading('Loading inventory...');

    // If already initialized, destroy BEFORE touching the DOM
    if ($.fn.DataTable.isDataTable('.cogs-report-table')) {
        table.destroy();
    }

    const tbody = $('.cogs-report-table tbody');
    // Rows are cleared while loading; the LoadingComponent overlay (shown
    // above) is the only loading indicator for this table now.
    tbody.empty();

    const requestData = {
        inventorySuiteId: isNewReport ? null : currentSelectedReportId,
        startDate: startDate,
        endDate: endDate
    };

    ajaxRequest({
        url: '/api/InventorySuiteReportApi/InventorySuiteReport',
        method: 'POST',
        data: JSON.stringify(requestData),
        contentType: 'application/json',
        isQuery: false,
        showMessage: false,
        onSuccess: function (response) {
            hideTableLoading();
            pendingClosingCounts = {};
           
            const items = (response && response.data && response.data.items) ? response.data.items : [];
            debugger;
            loadedReportItems = items;

            if (isNewReport) {
                $('#saveInventoryBtn').show().prop('disabled', false);
                $('#updateInventoryBtn').hide();
            } else {
                $('#updateInventoryBtn').show().prop('disabled', true);
                $('#saveInventoryBtn').hide();
            }

            // Clean up tbody
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
                const unitPrice = item.price;

                let closingCountCell = '';
                let closingValueCell = '';

                if (isNewReport) {
                    const saved = newReportClosingCounts[item.ingredientId];
                    const initialVal = (saved !== undefined)
                        ? saved.closingCount
                        : (closingCount > 0 ? closingCount : '');
                    const displayVal = (saved !== undefined)
                        ? saved.closingValue
                        : (initialVal !== '' ? (parseFloat(initialVal) * unitPrice) : 0);

                    closingCountCell = `
                        <td data-order="${initialVal || 0}" class="dt-body-left">
                            <input type="number" step="any" min="0" 
                                   class="form-control form-control-sm new-report-closing-input" 
                                   data-ingredient-id="${item.ingredientId}" 
                                   data-unit-price="${unitPrice}" 
                                   value="${initialVal !== '' ? Number(initialVal).toFixed(2) : ''}" 
                                   placeholder="0.00"
                                   style="width: 85px; text-align: right; font-weight: 600; padding: 4px 6px; border: 1px solid #f08d11; border-radius: 6px;">
                        </td>`;
                    closingValueCell = `<td data-order="${displayVal}" id="closingValue_${item.ingredientId}">$${Number(displayVal).toFixed(2)}</td>`;
                } else {
                    closingCountCell = `
                        <td data-order="${closingCount}" class="editable-closing" data-ingredient-id="${item.ingredientId || ''}" data-original-count="${closingCount.toFixed(2)}">
                            ${closingCount.toFixed(2)}
                        </td>`;
                    closingValueCell = `<td data-order="${closingValue}" id="closingValue_${item.ingredientId}">$${closingValue.toFixed(2)}</td>`;
                }

                const row = `
        <tr>
            <td>
                <div class="ingredient-info" >
                    <p class="ingredient-name">${item.ingredientName.trim().includes(' ') ? item.ingredientName.split(' ').slice(1).join(' ') : item.ingredientName}</p>
                    <p class="ingredient-meta">Brand: ${item.ingredientName.split(' ')[0]}</p>
                   ${item.packSize == null ? `<p class="ingredient-meta text-highlight danger">Catch Weight Item</p>` : `<p class="ingredient-meta text-highlight">Pack: ${item.packSize}</p>`}
                </div >
            </td>
            <td>
              <span class="${item.unit != null ? 'category-badge-size' : ''}">
                  ${item.unit
                        ? `${Number.isInteger(parseFloat(item.size))
                            ? parseInt(item.size, 10)
                            : parseFloat(item.size)} ${item.unit}`
                        : ''
                    }
              </span>
            </td>
            <td>${item.vendorName || '(Me)'}</td>
            <td data-order="${openingCount}">${openingCount.toFixed(2)}</td>
            <td data-order="${openingValue}">$${openingValue.toFixed(2)}</td>
            
            <td data-order="${purchasesQuantity}">${purchasesQuantity.toFixed(2)}</td>
            <td data-order="${purchasesValue}">$${purchasesValue.toFixed(2)}</td>
            ${closingCountCell}
            ${closingValueCell}
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
            hideTableLoading();
            console.error('Failed to fetch Inventory:', error);
            showNotification('Failed to fetch Inventory.', 'error');
            const tbody = $('.cogs-report-table tbody');
            tbody.empty();
            tbody.append('<tr><td colspan="10" class="text-center">Failed to load inventory. Please try again.</td></tr>');
            resetFooterValues();
            initDataTable();
        }
    });
}

/* ── 6. Helpers ───────────────────────────────────────────────────────── */

// Strips HTML tags from a cell's raw markup and collapses whitespace/newlines
// Checks for input value if cell contains an input.
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

function initDataTable() {
    table = $('.cogs-report-table').DataTable({
        paging: true,
        searching: true,
        ordering: false,
        processing: true,
        order: [[1, 'asc']],
        columnDefs: [
            { targets: [3, 4, 5, 6, 7, 8, 9], className: 'dt-body-left' }
        ],
        pageLength: 10,
        lengthMenu: [10, 25, 50, 100],
        autoWidth: false,
        columnDefs: [
            { width: "150px", targets: 0 },
            { width: "80px", targets: 1 },
        ],
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
        ]
    });

    // Move export buttons to custom container
    if (table.buttons && $('#exportButtons').length) {
        table.buttons().container().appendTo('#exportButtons');
    }

    // Only make closing counts editable on click if in UPDATE mode
    if (currentMode !== 'INSERT') {
        makeClosingCountEditable();
    } else {
        // Redraw preservation for inputs across pages
        table.on('draw', function () {
            $('.new-report-closing-input').each(function () {
                const $inp = $(this);
                const ingId = parseInt($inp.attr('data-ingredient-id'), 10);
                if (newReportClosingCounts[ingId] !== undefined) {
                    $inp.val(Number(newReportClosingCounts[ingId].closingCount).toFixed(2));
                    $(`#closingValue_${ingId}`).text('$' + Number(newReportClosingCounts[ingId].closingValue).toFixed(2));
                }
            });
        });
    }

    // Custom search
    $('#ingredientSearch').off('keyup').on('keyup', function () {
        table.search(this.value).draw();
    });

    // Clear filter
    $('#clear-filter').off('click').on('click', function () {
        table.search('').draw();
        $('#ingredientSearch').val('').trigger('keyup');
    });
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
                // Mark visually as modified and queue change
                $closingCountTd.addClass('modified-count');
                pendingClosingCounts[ingredientId] = {
                    ingredientId: parseInt(ingredientId),
                    closingCount: newClosingCount,
                    closingValue: newClosingValue,
                    closingDate: currentEndDate
                };
            } else {
                // Reverted to original value
                $closingCountTd.removeClass('modified-count');
                delete pendingClosingCounts[ingredientId];
            }

            // Enable button ONLY if there are modified counts, else keep disabled
            const hasChanges = Object.keys(pendingClosingCounts).length > 0;
            $('#updateInventoryBtn').prop('disabled', !hasChanges);

            // Recalculate row COGS
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