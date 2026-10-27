/* ============================================================================
 * sales.js — Sales Records & POS Entries (Views/Sales/Index.cshtml)
 * Table form layout matching Invoice entry, with Add, Edit, View, and Import.
 *
 * Project convention — section order:
 *   1. Constants  2. State  3. Initialization  4. Event bindings
 *   5. AJAX / CRUD  6. Helpers  7. Utilities
 * Functions stay global: referenced by inline handlers.
 * ==========================================================================*/

/* ── 2. State ─────────────────────────────────────────────────────────── */

let table = null;
let allSales = [];
let menuItems = [];
let currentSaleItems = [];
let editSaleItems = [];

/* ── 3. Initialization / 4. Event bindings ────────────────────────────── */

$(document).ready(function () {
    initDataTable();

    // Event handler for Add Sale button
    $('#addSaleBtn').on('click', function () {
        openAddSaleModal();
    });

    // Search filter
    $('#salesSearch').on('input', function () {
        if (table) table.search(this.value).draw();
    });

    // Clear filter
    $('#clearFilterBtn').on('click', function () {
        $('#salesSearch').val('');
        if (table) table.search('').draw();
    });

    // CSV Import and Sample Download bindings in Add Sale modal
    $('#importSaleCsvBtn').on('click', function () {
        if (!menuItems || menuItems.length === 0) {
            showNotification('Menu items are not loaded yet. Please wait a moment and try again.', 'warning');
            return;
        }
        $('#saleCsvFileInput').val('').trigger('click');
    });

    $('#saleCsvFileInput').on('change', function (e) {
        handleSaleCsvUpload(e);
    });

    $('#downloadSaleSampleBtn').on('click', function (e) {
        e.preventDefault();
        downloadSampleSalesFile();
    });

    // Initial load
    loadMenuItems();
    refreshSalesTable();
});

/* ── 5. AJAX / CRUD ───────────────────────────────────────────────────── */

function refreshSalesTable() {
    LoadingComponent.showForTable('#salesTable', 'Loading sales...');
    ajaxRequest({
        url: '/api/SalesApi/GetAllSales',
        method: 'GET',
        showMessage: false,
        onSuccess: function (response) {
            allSales = response.data || [];
            updateStats();
            renderSalesTable(allSales);
        },
        onError: function (xhr, status, error) {
            console.error('Failed to load sales:', error);
        },
        onComplete: function () { LoadingComponent.hideForTable('#salesTable'); }
    });
}

function loadMenuItems() {
    ajaxRequest({
        url: '/api/MenuApi/GetAllMenuItems?isActive=true',
        method: 'GET',
        showMessage: false,
        onSuccess: function (response) {
            menuItems = response.data || [];
            initMenuItemSelectors();
        },
        onError: function (xhr, status, error) {
            console.error('Failed to fetch menu items:', error);
        }
    });
}

function submitAddSale() {
    const startDate = $('#newSaleStartDate').val();
    const endDate = $('#newSaleEndDate').val();

    if (!startDate) {
        showNotification('Please select a start date.', 'error');
        return;
    }
    if (!endDate) {
        showNotification('Please select an end date.', 'error');
        return;
    }
    if (new Date(startDate) > new Date(endDate)) {
        showNotification('Start Date cannot be greater than End Date.', 'error');
        return;
    }
    if (!currentSaleItems || currentSaleItems.length === 0) {
        showNotification('Please add at least one menu item.', 'error');
        return;
    }

    for (const item of currentSaleItems) {
        if (!item.quantity || isNaN(item.quantity) || item.quantity <= 0) {
            showNotification(`Please enter a valid quantity for "${item.name}".`, 'error');
            return;
        }
    }

    const subtotal = currentSaleItems.reduce((sum, i) => sum + (i.unitPrice * i.quantity), 0);
    const payload = {
        startDate: startDate,
        endDate: endDate,
        subtotal: subtotal,
        tax: 0,
        total: subtotal,
        items: currentSaleItems.map(i => ({
            menuItemId: i.menuItemId,
            quantity: i.quantity,
            revenue: i.unitPrice * i.quantity,
            cost: (i.cost || 0) * i.quantity
        }))
    };

    const $btn = $('#submitSaleBtn');
    $btn.prop('disabled', true).html('<i class="fas fa-spinner fa-spin"></i> Saving...');

    ajaxRequest({
        url: '/api/SalesApi/CreateSale',
        method: 'POST',
        data: payload,
        showMessage: true,
        onSuccess: function () {
            closeAddSaleModal();
            refreshSalesTable();
        },
        onError: function (xhr, status, error) {
            console.error('Failed to create sale:', error);
            $btn.prop('disabled', false).html('<i class="fas fa-save"></i> Save Sale');
        }
    });
}

function submitEditSale() {
    const saleId = parseInt($('#editSaleId').val());
    const startDate = $('#editSaleStartDate').val();
    const endDate = $('#editSaleEndDate').val();

    if (!saleId) {
        showNotification('Invalid Sale ID.', 'error');
        return;
    }
    if (!startDate) {
        showNotification('Please select a start date.', 'error');
        return;
    }
    if (!endDate) {
        showNotification('Please select an end date.', 'error');
        return;
    }
    if (new Date(startDate) > new Date(endDate)) {
        showNotification('Start Date cannot be greater than End Date.', 'error');
        return;
    }
    if (!editSaleItems || editSaleItems.length === 0) {
        showNotification('Please add at least one menu item.', 'error');
        return;
    }

    for (const item of editSaleItems) {
        if (!item.quantity || isNaN(item.quantity) || item.quantity <= 0) {
            showNotification(`Please enter a valid quantity for "${item.name}".`, 'error');
            return;
        }
    }

    const subtotal = editSaleItems.reduce((sum, i) => sum + (i.unitPrice * i.quantity), 0);
    const payload = {
        saleId: saleId,
        startDate: startDate,
        endDate: endDate,
        subtotal: subtotal,
        tax: 0,
        total: subtotal,
        items: editSaleItems.map(i => ({
            saleId: saleId,
            menuItemId: i.menuItemId,
            quantity: i.quantity,
            revenue: i.unitPrice * i.quantity,
            cost: (i.cost || 0) * i.quantity
        }))
    };

    const $btn = $('#updateSaleBtn');
    $btn.prop('disabled', true).html('<i class="fas fa-spinner fa-spin"></i> Updating...');

    ajaxRequest({
        url: '/api/SalesApi/UpdateSale',
        method: 'PUT',
        data: payload,
        showMessage: true,
        onSuccess: function () {
            closeEditSaleModal();
            refreshSalesTable();
        },
        onError: function (xhr, status, error) {
            console.error('Failed to update sale:', error);
            $btn.prop('disabled', false).html('<i class="fas fa-save"></i> Update Sale');
        }
    });
}

function handleSaleCsvUpload(e) {
    const file = e.target.files && e.target.files[0];
    if (!file) return;

    if (!file.name.toLowerCase().endsWith('.csv')) {
        showNotification('Please upload a valid .csv file.', 'error');
        $('#saleCsvFileInput').val('');
        return;
    }

    const reader = new FileReader();
    reader.onload = function (event) {
        try {
            processSaleCsv(event.target.result);
        } catch (err) {
            console.error('Error parsing CSV:', err);
            showNotification('Error processing CSV file. Please check file structure.', 'error');
        } finally {
            $('#saleCsvFileInput').val('');
        }
    };
    reader.readAsText(file);
}

function parseCsvText(text) {
    const lines = [];
    let row = [];
    let inQuotes = false;
    let currentCell = '';

    for (let i = 0; i < text.length; i++) {
        const char = text[i];
        const nextChar = text[i + 1];

        if (char === '"') {
            if (inQuotes && nextChar === '"') {
                currentCell += '"';
                i++;
            } else {
                inQuotes = !inQuotes;
            }
        } else if ((char === ',' || char === '\t') && !inQuotes) {
            row.push(currentCell.trim());
            currentCell = '';
        } else if ((char === '\r' || char === '\n') && !inQuotes) {
            if (char === '\r' && nextChar === '\n') {
                i++;
            }
            row.push(currentCell.trim());
            currentCell = '';
            if (row.some(c => c !== '')) {
                lines.push(row);
            }
            row = [];
        } else {
            currentCell += char;
        }
    }

    if (currentCell.length > 0 || row.length > 0) {
        row.push(currentCell.trim());
        if (row.some(c => c !== '')) {
            lines.push(row);
        }
    }

    return lines;
}

function processSaleCsv(csvText) {
    if (!csvText || !csvText.trim()) {
        showNotification('The uploaded CSV file is empty.', 'error');
        return;
    }

    if (!menuItems || menuItems.length === 0) {
        showNotification('Menu items are not loaded yet. Please wait a moment and try again.', 'warning');
        return;
    }

    const rows = parseCsvText(csvText);
    if (rows.length < 2) {
        showNotification('The CSV file does not contain any item data.', 'error');
        return;
    }

    // Map headers (case and punctuation insensitive)
    const headerRow = rows[0].map(h => (h || '').toString().toLowerCase().replace(/[^a-z0-9]/g, ''));

    const idxId = headerRow.findIndex(h => h.includes('itemid') || h.includes('productid') || h === 'id' || h === 'menuitemid');
    const idxName = headerRow.findIndex(h => h.includes('name') || h.includes('item') || h.includes('menuitem') || h.includes('description'));
    const idxQty = headerRow.findIndex(h => h.includes('qty') || h.includes('quantity') || h.includes('sold'));

    let importedCount = 0;
    let skippedZeroCount = 0;

    for (let r = 1; r < rows.length; r++) {
        const row = rows[r];
        if (!row || row.length === 0 || row.every(c => !c || !c.trim())) continue;

        let cellId = idxId !== -1 ? (row[idxId] || '').trim() : '';
        let cellName = idxName !== -1 ? (row[idxName] || '').trim() : '';
        let cellQty = idxQty !== -1 ? (row[idxQty] || '').trim() : '';

        // Fallback if headers were not recognized
        if (idxId === -1 && idxName === -1 && idxQty === -1 && row.length >= 3) {
            cellName = (row[0] || '').trim();
            cellId = (row[1] || '').trim();
            cellQty = (row[2] || '').trim();
        }

        const cleanPriceOrQty = cellQty.replace(/[^0-9.]/g, '');
        const qty = parseFloat(cleanPriceOrQty);
        if (isNaN(qty) || qty <= 0) {
            skippedZeroCount++;
            continue;
        }

        // Matching strategy:
        // 1. By menuItemId
        let matched = null;
        if (cellId) {
            const parsedId = parseInt(cellId, 10);
            if (!isNaN(parsedId)) {
                matched = menuItems.find(m => m.menuItemId === parsedId || m.id === parsedId);
            }
        }

        // 2. By Name
        if (!matched && cellName) {
            const cleanName = cellName.toLowerCase().replace(/['"]/g, '').trim();
            matched = menuItems.find(m => {
                const mName = (m.name || '').toLowerCase().replace(/['"]/g, '').trim();
                return mName === cleanName;
            });

            if (!matched) {
                matched = menuItems.find(m => {
                    const mName = (m.name || '').toLowerCase().replace(/['"]/g, '').trim();
                    return mName.includes(cleanName) || cleanName.includes(mName);
                });
            }
        }

        if (!matched) {
            continue;
        }

        // Upsert into currentSaleItems
        const existing = currentSaleItems.find(i => i.menuItemId === matched.menuItemId);
        if (existing) {
            existing.quantity = qty;
        } else {
            currentSaleItems.push({
                menuItemId: matched.menuItemId,
                name: matched.name,
                unitPrice: Number(matched.sellingPrice) || 0,
                cost: Number(matched.cost) || 0,
                quantity: qty
            });
        }

        importedCount++;
    }

    syncSaleItemsView();

    if (importedCount > 0) {
        showNotification(`Successfully imported all ${importedCount} items from CSV!`, 'success');
    } else {
        if (skippedZeroCount > 0) {
            showNotification('No items with quantity greater than 0 found in the CSV file.', 'warning');
        } else {
            showNotification('No matching menu items found in CSV file.', 'warning');
        }
    }
}

/* ── 6. Helpers ───────────────────────────────────────────────────────── */

function initDataTable() {
    table = $('#salesTable').DataTable({
        paging: true,
        searching: true,
        ordering: false,
        pageLength: 10,
        lengthMenu: [10, 25, 50, 100],
        language: {
            search: '',
            searchPlaceholder: 'Search sales...',
            info: 'Showing _START_ to _END_ of _TOTAL_ entries',
            paginate: {
                previous: '<i class="fas fa-chevron-left"></i>',
                next: '<i class="fas fa-chevron-right"></i>'
            }
        }
    });
}

function updateStats() {
    const totalCount = allSales.length;
    let totalRevenue = 0;
    let totalItemsSold = 0;

    allSales.forEach(sale => {
        
        totalRevenue += (Number(sale.total) || 0);
        const items = parseItems(sale.itemsJson);
        items.forEach(i => {
            totalItemsSold += (Number(i.Quantity || i.quantity) || 0);
        });
    });

    $('#statTotalSales').text(totalCount);
    $('#statTotalRevenue').text(formatCurrency(totalRevenue));
    $('#statTotalItemsSold').text(Math.round(totalItemsSold));
}

function renderSalesTable(sales) {
    table.clear();
    sales.forEach(sale => {
        table.row.add($(buildSaleRow(sale)));
    });
    table.draw();
}

function buildSaleRow(sale) {
    const items = parseItems(sale.itemsJson);
    const itemsCount = items.length;
    const startDateDisplay = sale.startDate ? formatDateDisplay(sale.startDate) : '-';
    const endDateDisplay = sale.endDate ? formatDateDisplay(sale.endDate) : '-';

    const itemsPreview = items.slice(0, 3).map(i => {
        const name = i.Name || i.name || 'Item';
        const qty = i.Quantity || i.quantity || 1;
        return `<span class="category-badge" style="font-size: 11px; margin-right: 4px; display: inline-block;">${escapeHtml(name)} (x${qty})</span>`;
    }).join(' ');

    const moreBadge = items.length > 3
        ? `<span class="status-badge" style="background: #f1f5f9; color: #475569; font-size: 11px;">+${items.length - 3} more</span>`
        : '';

    return `
        <tr>
            <td>
                <div class="category-info">
                    <p class="category-name" style="color: #0f172a;">${sale.saleId}</p>
                </div>
            </td>
            <td>
                <i class="fas fa-calendar-alt" style="color: #6b7280; margin-right: 6px;"></i>
                ${startDateDisplay}
            </td>
            <td>
                <i class="fas fa-calendar-check" style="color: #6b7280; margin-right: 6px;"></i>
                ${endDateDisplay}
            </td>
            <td class="text-right" style="color: #0f172a;">
                ${formatCurrency(sale.total)}
            </td>
            <td class="text-center">
                <span class="status-badge good">${itemsCount} Items</span>
            </td>
            <td class="hidden">
                <div style="display: flex; align-items: center; gap: 4px; flex-wrap: wrap;">
                    ${itemsPreview} ${moreBadge}
                </div>
            </td>
            <td class="text-center">
                <div class="dropdown">
                    <button class="dropdown-toggle" type="button">
                        <i class="fas fa-ellipsis-h"></i>
                    </button>
                    <div class="dropdown-menu">
                        <a href="javascript:void(0)" onclick="viewSaleDetails(${sale.saleId}); return false;">
                            <i class="fas fa-eye"></i> View Details
                        </a>
                        <a href="javascript:void(0)" onclick="editSale(${sale.saleId}); return false;">
                            <i class="fas fa-edit"></i> Edit Sale
                        </a>
                    </div>
                </div>
            </td>
        </tr>
    `;
}

function initMenuItemSelectors() {
    
    const options = [{ id: '', text: '-- Select Menu Item --' }].concat(
        menuItems.map(m => ({
            
            id: m.menuItemId,
            text: `${m.name}`
        }))
    );

    if ($('#menuItemSelector').hasClass('select2-hidden-accessible')) {
        $('#menuItemSelector').select2('destroy');
    }
    $('#menuItemSelector').empty().select2({
        data: options,
        placeholder: '-- Select Menu Item --',
        width: '100%',
        dropdownParent: $('#addSaleModal')
    });

    if ($('#editMenuItemSelector').hasClass('select2-hidden-accessible')) {
        $('#editMenuItemSelector').select2('destroy');
    }
    $('#editMenuItemSelector').empty().select2({
        data: options,
        placeholder: '-- Select Menu Item --',
        width: '100%',
        dropdownParent: $('#editSaleModal')
    });
}

/* ── Add Sale Modal Handlers ──────────────────────────────────────────── */
function openAddSaleModal() {
    currentSaleItems = [];
    const today = new Date().toISOString().split('T')[0];
    $('#newSaleStartDate').val(today);
    $('#newSaleEndDate').val(today);

    $('#menuItemSelector').val('').trigger('change.select2');
    $('#saleCsvFileInput').val('');
    syncSaleItemsView();

    $('#submitSaleBtn').prop('disabled', false).html('<i class="fas fa-save"></i> Save Sale');
    openModal('addSaleModal');
}

function closeAddSaleModal() {
    closeModal('addSaleModal');
}

function addSaleItemRow() {
    const selectedId = parseInt($('#menuItemSelector').val());
    if (!selectedId) {
        showNotification('Please select a menu item.', 'info');
        return;
    }

    const menuItem = menuItems.find(m => m.menuItemId === selectedId);
    if (!menuItem) return;

    const existing = currentSaleItems.find(i => i.menuItemId === selectedId);
    if (existing) {
        showNotification('This menu item is already added.', 'info');
        return;
    }

    currentSaleItems.push({
        menuItemId: menuItem.menuItemId,
        name: menuItem.name,
        unitPrice: Number(menuItem.sellingPrice) || 0,
        cost: Number(menuItem.cost) || 0,
        quantity: 1
    });

    syncSaleItemsView();
    $('#menuItemSelector').val('').trigger('change.select2');
}

function updateSaleItemQty(menuItemId, value) {
    const qty = parseFloat(value);
    const item = currentSaleItems.find(i => i.menuItemId === menuItemId);
    if (!item) return;

    item.quantity = qty;
    $(`#line_cost_${menuItemId}`).text(formatCurrency(item.cost * item.quantity));
    $(`#line_total_${menuItemId}`).text(formatCurrency(item.unitPrice * item.quantity));
    updateAddSaleTotal();
   
}

function removeSaleItem(menuItemId) {
    currentSaleItems = currentSaleItems.filter(i => i.menuItemId !== menuItemId);
    syncSaleItemsView();
}

function syncSaleItemsView() {
    const $body = $('#saleItemsBody');
    $body.empty();

    if (currentSaleItems.length === 0) {
        $('#saleItemsWrapper').hide();
        $('#saleItemsEmpty').show();
        $('#newSaleTotalDisplay').text('$0.00');
        return;
    }

    $('#saleItemsEmpty').hide();
    $('#saleItemsWrapper').show();

    currentSaleItems.forEach(item => {
        const revenue = item.unitPrice * item.quantity;
        $body.append(`
            <tr class="sale-item-row" id="sale_row_${item.menuItemId}">
                <td style="font-weight: 600; color: #1e293b;">
                    <i class="fas fa-utensils" style="color: #f08d11; margin-right: 8px; font-size: 12px;"></i>
                    ${escapeHtml(item.name)}
                </td>
                <td style="text-align: center;">
                    <input type="number" min="1" step="1" value="${item.quantity}" placeholder="0" onchange="updateSaleItemQty(${item.menuItemId}, this.value)" oninput="updateSaleItemQty(${item.menuItemId}, this.value)">
                </td>
                <td style="text-align: right; color: #64748b; font-weight: 500;" id="line_cost_${item.menuItemId}">
                    ${formatCurrency(item.cost)}
                </td>
                <td style="text-align: right; font-weight: 700; color: #0f172a;" id="line_total_${item.menuItemId}">
                    ${formatCurrency(revenue)}
                </td>
                <td style="text-align: center;">
                    <button type="button" class="sale-remove-btn" onclick="removeSaleItem(${item.menuItemId})" title="Remove item">
                        <i class="fas fa-trash"></i>
                    </button>
                </td>
            </tr>
        `);
    });

    updateAddSaleTotal();
}

function updateAddSaleTotal() {
    const total = currentSaleItems.reduce((sum, i) => sum + (i.unitPrice * i.quantity), 0);
    $('#newSaleTotalDisplay').text(formatCurrency(total));
}

/* ── Edit Sale Modal Handlers ─────────────────────────────────────────── */

function editSale(saleId) {
    const sale = allSales.find(s => s.saleId === saleId);
    ;
    if (!sale) {
        showNotification('Sale not found.', 'error');
        return;
    }

    $('#editSaleId').val(sale.saleId);
    $('#editSaleStartDate').val(formatDateForInput(sale.startDate));
    $('#editSaleEndDate').val(formatDateForInput(sale.endDate));

    const rawItems = parseItems(sale.itemsJson);
    editSaleItems = rawItems.map(raw => {
        const menuItemId = raw.MenuItemId || raw.menuItemId;
        const matchedMenu = menuItems.find(m => m.menuItemId === menuItemId);

        const qty = Number(raw.quantity ?? raw.Quantity) || 1;
        const totalLinePrice = Number(raw.Revenue ?? raw.revenue) || 0;
        const totalLineCost = Number(raw.cost ?? raw.Cost) || 0;

        const unitPrice = matchedMenu
            ? Number(matchedMenu.sellingPrice)
            : (qty > 0 ? (totalLinePrice / qty) : totalLinePrice);

        const unitCost = matchedMenu
            ? Number(matchedMenu.cost)
            : (qty > 0 ? (totalLineCost / qty) : totalLineCost);

        return {
            menuItemId: menuItemId,
            name: raw.Name || raw.name || matchedMenu?.name || `Item #${menuItemId}`,
            unitPrice: unitPrice,
            cost: unitCost,
            quantity: qty
        };
    });

    $('#editMenuItemSelector').val('').trigger('change.select2');
    syncEditSaleItemsView();

    $('#updateSaleBtn').prop('disabled', false).html('<i class="fas fa-save"></i> Update Sale');
    openModal('editSaleModal');
}

function closeEditSaleModal() {
    closeModal('editSaleModal');
}

function addEditSaleItemRow() {
    const selectedId = parseInt($('#editMenuItemSelector').val());
    if (!selectedId) {
        showNotification('Please select a menu item.', 'warning');
        return;
    }

    const menuItem = menuItems.find(m => m.menuItemId === selectedId);
    if (!menuItem) return;

    const existing = editSaleItems.find(i => i.menuItemId === selectedId);
    if (existing) {
        showNotification('This menu item is already added.', 'warning');
        return;
    }

    editSaleItems.push({
        menuItemId: menuItem.menuItemId,
        name: menuItem.name,
        unitPrice: Number(menuItem.sellingPrice) || 0,
        cost: Number(menuItem.cost) || 0,
        quantity: 1
    });

    syncEditSaleItemsView();
    $('#editMenuItemSelector').val('').trigger('change.select2');
}
function updateEditSaleItemQty(menuItemId, value) {
    const qty = parseFloat(value);
    const item = editSaleItems.find(i => i.menuItemId === menuItemId);
    if (!item) return;

    if (isNaN(qty) || qty <= 0) {
        removeEditSaleItem(menuItemId);
    } else {
        item.quantity = qty;
        $(`#edit_line_cost_${menuItemId}`).text(formatCurrency(item.cost * item.quantity));
        $(`#edit_line_total_${menuItemId}`).text(formatCurrency(item.unitPrice * item.quantity));
        updateEditSaleTotal();
    }
}
function removeEditSaleItem(menuItemId) {
    editSaleItems = editSaleItems.filter(i => i.menuItemId !== menuItemId);
    syncEditSaleItemsView();
}

function syncEditSaleItemsView() {
    const $body = $('#editSaleItemsBody');
    $body.empty();

    if (editSaleItems.length === 0) {
        $('#editSaleItemsWrapper').hide();
        $('#editSaleItemsEmpty').show();
        $('#editSaleTotalDisplay').text('$0.00');
        return;
    }

    $('#editSaleItemsEmpty').hide();
    $('#editSaleItemsWrapper').show();
    editSaleItems.forEach(item => {
        $body.append(`
            <tr class="sale-item-row" id="edit_sale_row_${item.menuItemId}">
                <td style="font-weight: 600; color: #1e293b;">
                    <i class="fas fa-utensils" style="color: #f08d11; margin-right: 8px; font-size: 12px;"></i>
                    ${escapeHtml(item.name)}
                </td>
                <td style="text-align: center;">
                    <input type="number" min="1" step="1" value="${item.quantity}" onchange="updateEditSaleItemQty(${item.menuItemId}, this.value)" oninput="updateEditSaleItemQty(${item.menuItemId}, this.value)">
                </td>
                <td style="text-align: right; color: #64748b; font-weight: 500;" id="edit_line_cost_${item.menuItemId}"">
                    ${formatCurrency(item.cost * item.quantity)}
                </td>
                <td style="text-align: right; font-weight: 700; color: #0f172a;" id="edit_line_total_${item.menuItemId}">
                    ${formatCurrency(item.unitPrice * item.quantity)}
                </td>
                <td style="text-align: center;">
                    <button type="button" class="sale-remove-btn" onclick="removeEditSaleItem(${item.menuItemId})" title="Remove item">
                        <i class="fas fa-trash"></i>
                    </button>
                </td>
            </tr>
        `);
    });

    updateEditSaleTotal();
}

function updateEditSaleTotal() {
    const total = editSaleItems.reduce((sum, i) => sum + (i.unitPrice * i.quantity), 0);
    $('#editSaleTotalDisplay').text(formatCurrency(total));
}

/* ── View Details Modal Handler ───────────────────────────────────────── */
function viewSaleDetails(saleId) {
    const sale = allSales.find(s => s.saleId === saleId);
    if (!sale) return;
    $('#modalSaleTitle').text(`Sale #${sale.saleId}`);
    $('#modalSaleSubtitle').text(
        `${formatDateDisplay(sale.startDate)} - ${formatDateDisplay(sale.endDate)}`
    );
    $('#modalSaleId').text(`${sale.saleId}`);
    $('#modalSaleStartDate').text(formatDateDisplay(sale.startDate));
    $('#modalSaleEndDate').text(formatDateDisplay(sale.endDate));
    $('#modalSaleTotal').text(formatCurrency(sale.total));

    // Parse menu items from itemsJson
    let items = [];

    try {
        if (Array.isArray(sale.items)) {
            items = sale.items;
        } else if (sale.itemsJson) {
            items = typeof sale.itemsJson === 'string'
                ? JSON.parse(sale.itemsJson)
                : sale.itemsJson;
        }
    } catch (error) {
        console.error('Failed to parse sale items:', error);
    }

    if (!Array.isArray(items)) {
        items = [];
    }

    $('#modalItemsCount').text(items.length);

    const $tbody = $('#modalItemsTableBody');
    $tbody.empty();

    if (items.length === 0) {
        $tbody.html(`
            <tr>
                <td colspan="4" class="text-center"
                    style="padding: 24px; color: #6b7280;">
                    No line items available for this sale.
                </td>
            </tr>
        `);
    } else {
        items.forEach(item => {
            const name = item.Name ?? 'Menu Item';
            const qty = Number(item.Quantity) || 0;
            const total = Number(item.Revenue) || 0;
            const cost = Number(item.Cost) || 0;

            $tbody.append(`
                <tr>
                    <td style="font-weight: 600; color: #1e293b;">
                        <i class="fas fa-utensils"
                           style="color: #f08d11; margin-right: 8px; font-size: 12px;"></i>
                        ${escapeHtml(String(name).trim())}
                    </td>
                    <td class="text-center" style="font-weight: 600;">
                        ${qty}
                    </td>
                    <td class="text-right" style="color: #64748b;">
                        ${formatCurrency(cost)}
                    </td>
                    <td class="text-right"
                        style="font-weight: 700; color: #0f172a;">
                        ${formatCurrency(total)}
                    </td>
                </tr>
            `);
        });
    }

    openModal('saleDetailModal');
}


/* ── Sample CSV Download ──────────────────────────────────────────────── */
function downloadSampleSalesFile() {
    function generateCsv(items) {
        let csvContent = "Menu Item Name,Menu Item ID,Quantity Sold\n";
        (items || []).forEach(item => {
            const name = item.name ? `"${item.name.toString().replace(/"/g, '""')}"` : '""';
            const itemId = item.menuItemId !== undefined ? item.menuItemId : (item.id || '');
            csvContent += `${name},${itemId},0\n`;
        });

        const blob = new Blob(['\uFEFF' + csvContent], { type: 'text/csv;charset=utf-8;' });
        const link = document.createElement('a');
        const url = URL.createObjectURL(blob);
        link.setAttribute('href', url);
        link.setAttribute('download', `Sales_Import_Sample_${new Date().toISOString().slice(0, 10)}.csv`);
        link.style.visibility = 'hidden';
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        URL.revokeObjectURL(url);
    }

    if (menuItems && menuItems.length > 0) {
        generateCsv(menuItems);
    } else {
        ajaxRequest({
            url: '/api/MenuApi/GetAllMenuItems?isActive=true',
            method: 'GET',
            showMessage: false,
            onSuccess: function (response) {
                if (response && response.data && response.data.length > 0) {
                    menuItems = response.data;
                    generateCsv(menuItems);
                } else {
                    showNotification('No menu items available for sample export.', 'error');
                }
            },
            onError: function () {
                showNotification('Failed to download sample file.', 'error');
            }
        });
    }
}

/* ── 7. Utilities ─────────────────────────────────────────────────────── */

function parseItems(itemsJson) {
    if (!itemsJson) return [];
    if (typeof itemsJson === 'object' && Array.isArray(itemsJson)) return itemsJson;
    try {
        return JSON.parse(itemsJson) || [];
    } catch {
        return [];
    }
}

function formatCurrency(val) {
    const num = parseFloat(val);
    if (isNaN(num)) return '$0.00';
    return '$' + num.toFixed(2);
}

function formatDateDisplay(dateStr) {
    if (!dateStr) return '-';
    if (window.DateFormatter && typeof window.DateFormatter.formatWithComma === 'function') {
        return window.DateFormatter.formatWithComma(dateStr);
    }
    try {
        const d = new Date(dateStr);
        return d.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
    } catch {
        return dateStr;
    }
}

function formatDateForInput(dateStr) {
    if (!dateStr) return '';
    try {
        const d = new Date(dateStr);
        if (isNaN(d.getTime())) return '';
        const yyyy = d.getFullYear();
        const mm = String(d.getMonth() + 1).padStart(2, '0');
        const dd = String(d.getDate()).padStart(2, '0');
        return `${yyyy}-${mm}-${dd}`;
    } catch {
        return '';
    }
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

window.downloadSampleSalesFile = downloadSampleSalesFile;
window.downloadSaleSampleCsv = downloadSampleSalesFile;


