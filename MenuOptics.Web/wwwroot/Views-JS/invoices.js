/* ============================================================================
 * invoices.js — Inventory Invoices page (Views/Inventory/Invoices.cshtml)
 *
 * Project convention — every Views-JS file follows this section order:
 *   1. Constants  2. State  3. Initialization  4. Event bindings
 *   5. AJAX / CRUD  6. Helpers  7. Utilities
 * Functions stay global: they are referenced by inline onclick handlers.
 * ==========================================================================*/

/* ── 2. State ─────────────────────────────────────────────────────────── */

let table;
let allInvoices = [];
let allVendors = [];
let invoiceItems = [];
let editInvoiceItems = [];
let vendorIngredients = [];

/* ── 3. Initialization / 4. Event bindings ────────────────────────────── */

$(document).ready(function () {
    // Initialize DataTable once on page load
    initDataTable();

    // Event handler for add invoice modal
    $('#addInvoiceBtn').on('click', function () {
        openAddInvoiceModal();
    });

    // Event handlers for filters
    $('#invoiceSearch').on('input', function () {
        table.search(this.value).draw();
    });

    $('#clearFilterBtn').on('click', function () {
        $('#invoiceSearch').val('');
        table.search('').draw();
    });

    // Close modals on backdrop click
    $('#invoiceDetailModal').on('click', function (e) {
        if ($(e.target).is('#invoiceDetailModal')) {
            closeModal('invoiceDetailModal');
        }
    });

    $('#addInvoiceModal').on('click', function (e) {
        if ($(e.target).is('#addInvoiceModal')) {
            closeAddInvoiceModal();
        }
    });

    $('#editInvoiceModal').on('click', function (e) {
        if ($(e.target).is('#editInvoiceModal')) {
            closeEditInvoiceModal();
        }
    });

    // Initial load
    refreshInvoicesTable();
    loadVendors();
});

/* ── 5. AJAX / CRUD ───────────────────────────────────────────────────── */

// Function to refresh the invoices table
function refreshInvoicesTable() {
    ajaxRequest({
        url: '/api/InventoryApi/GetInventoryInvoices',
        method: 'GET',
        data: null,
        showMessage: false,
        onSuccess: function (response) {
            allInvoices = response.data || [];
            updateStats();
            renderInvoicesTable(allInvoices);
        },
        onError: function (xhr, status, error) {
            console.error('Failed to load inventory invoices:', error);
        }
    });
}

// Function to load vendors
function loadVendors() {
    ajaxRequest({
        url: '/api/PrivateVendorApi/GetAllPrivateVendors',
        method: 'GET',
        data: null,
        showMessage: false,
        onSuccess: function (response) {
            allVendors = response.data || [];

            const vendorData = [{ id: '', text: '-- Select Supplier --' }].concat(
                allVendors.map(v => ({ id: v.privateVendorId, text: v.name }))
            );

            // Init Add modal vendor select
            if ($('#newInvoiceVendor').hasClass('select2-hidden-accessible')) {
                $('#newInvoiceVendor').select2('destroy');
            }

            $('#newInvoiceVendor').select2({
                data: vendorData,
                placeholder: '-- Select Supplier --',
                width: '100%',
                dropdownParent: $('#addInvoiceModal')
            }).on('change', function () {
                const vendorId = $(this).val();
                invoiceItems = [];
                $('#invoiceItemsBody').empty();
                syncInvoiceItemsView();
                if (vendorId) {
                    loadIngredientsByVendor(vendorId);
                } else {
                    resetIngredientSelector();
                }
            });

            // Init Edit modal vendor select (disabled as supplier cannot be changed)
            if ($('#editInvoiceVendor').hasClass('select2-hidden-accessible')) {
                $('#editInvoiceVendor').select2('destroy');
            }

            $('#editInvoiceVendor').select2({
                data: vendorData,
                placeholder: '-- Select Supplier --',
                width: '100%',
                disabled: true,
                dropdownParent: $('#editInvoiceModal')
            });
        },
        onError: function (xhr, status, error) {
            console.error('Failed to load vendors:', error);
        }
    });
}

// Function to load ingredients by vendor ID for Add modal
function loadIngredientsByVendor(vendorId) {
    resetIngredientSelector();

    ajaxRequest({
        url: `/api/IngredientApi/GetAllIngredientsByVendorId/${vendorId}/true`,
        method: 'GET',
        data: null,
        showMessage: false,
        onSuccess: function (response) {
            vendorIngredients = (response.data || []).filter(ing => ing.isActive !== false);

            const ingData = [{ id: '', text: '-- Select Ingredient --' }].concat(
                vendorIngredients.map(ing => ({ id: ing.ingredientId, text: ing.name }))
            );

            $('#ingredientSelector').empty().select2({
                data: ingData,
                placeholder: '-- Select Ingredient --',
                width: '100%',
                dropdownParent: $('#addInvoiceModal')
            });
        },
        onError: function (xhr, status, error) {
            console.error('Failed to load ingredients:', error);
            $('#ingredientSelector').empty().append('<option value="">Failed to load ingredients</option>');
        }
    });
}

// Function to load ingredients by vendor ID for Edit modal
function loadEditIngredientsByVendor(vendorId) {
    resetEditIngredientSelector();

    ajaxRequest({
        url: `/api/IngredientApi/GetAllIngredientsByVendorId/${vendorId}/true`,
        method: 'GET',
        data: null,
        showMessage: false,
        onSuccess: function (response) {
            const ingredients = (response.data || []).filter(ing => ing.isActive !== false);
            ingredients.forEach(ing => {
                if (!vendorIngredients.some(vi => vi.ingredientId === ing.ingredientId)) {
                    vendorIngredients.push(ing);
                }
            });

            // Now that master ingredient data is available, resync hasPackSize
            // for items already on the invoice using each ingredient's real packSize.
            editInvoiceItems.forEach(item => {
                const ing = vendorIngredients.find(i => i.ingredientId === item.ingredientId);
                if (ing) {
                    const hasPackSize = ing.packSize != null && Number(ing.packSize) > 0;
                    item.hasPackSize = hasPackSize;
                    item.packSize = hasPackSize ? (item.packSize ?? ing.packSize) : null;
                }
            });
            renderEditInvoiceItems();

            const ingData = [{ id: '', text: '-- Select Ingredient --' }].concat(
                ingredients.map(ing => ({ id: ing.ingredientId, text: ing.name }))
            );

            $('#editIngredientSelector').empty().select2({
                data: ingData,
                placeholder: '-- Select Ingredient --',
                width: '100%',
                dropdownParent: $('#editInvoiceModal')
            });
        },
        onError: function (xhr, status, error) {
            console.error('Failed to load ingredients:', error);
            $('#editIngredientSelector').empty().append('<option value="">Failed to load ingredients</option>');
        }
    });
}

// Function to submit / create invoice
function submitAddInvoice() {
    const vendorId = parseInt($('#newInvoiceVendor').val());
    const invoiceId = $('#newInvoiceId').val().trim();
    const invoiceDate = $('#newInvoiceDate').val();

    if (!invoiceId) {
        showNotification('Please enter the invoice number.', 'error');
        return;
    }

    if (!invoiceDate) {
        showNotification('Please select the invoice date.', 'error');
        return;
    }

    if (!vendorId) {
        showNotification('Please select a supplier.', 'error');
        return;
    }

    if (invoiceItems.length === 0) {
        showNotification('Please add at least one ingredient.', 'error');
        return;
    }

    for (const item of invoiceItems) {
        if (!item.price || isNaN(parseFloat(item.price)) || parseFloat(item.price) <= 0) {
            showNotification(`Please enter a valid price for "${item.ingredientName}".`, 'error');
            return;
        }
    }

    const payload = {
        privateVendorId: vendorId,
        invoiceId: invoiceId,
        invoiceDate: invoiceDate,
        items: invoiceItems.map(item => ({
            ingredientId: item.ingredientId,
            quantity: item.quantity ? parseInt(item.quantity) : null,
            packSize: item.hasPackSize ? (item.packSize ? parseInt(item.packSize) : null) : null,
            price: parseFloat(item.price)
        }))
    };

    $('#submitInvoiceBtn').prop('disabled', true).html('<i class="fas fa-spinner fa-spin"></i> Saving...');

    ajaxRequest({
        url: '/api/InventoryApi/CreateInvoice',
        method: 'POST',
        data: payload,
        showMessage: true,
        onSuccess: function (response) {
            closeAddInvoiceModal();
            refreshInvoicesTable();
        },
        onError: function (xhr, status, error) {
            console.error('Failed to create invoice:', error);
            $('#submitInvoiceBtn').prop('disabled', false).html('<i class="fas fa-save"></i> Save Invoice');
        }
    });
}

// Function to submit / update invoice
function submitEditInvoice() {
    const inventoryInvoiceId = parseInt($('#editInventoryInvoiceId').val());
    let vendorId = parseInt($('#editInvoiceVendor').val());
    if (isNaN(vendorId) || !vendorId) {
        vendorId = parseInt($('#editInventoryInvoiceId').data('vendorId'));
    }
    const invoiceId = $('#editInvoiceId').val().trim();
    const invoiceDate = $('#editInvoiceDate').val();

    if (!invoiceId) {
        showNotification('Please enter the invoice number.', 'error');
        return;
    }

    if (!invoiceDate) {
        showNotification('Please select the invoice date.', 'error');
        return;
    }

    if (!vendorId) {
        showNotification('Please select a supplier.', 'error');
        return;
    }

    if (editInvoiceItems.length === 0) {
        showNotification('Please add at least one ingredient.', 'error');
        return;
    }

    for (const item of editInvoiceItems) {
        if (!item.price || isNaN(parseFloat(item.price)) || parseFloat(item.price) <= 0) {
            showNotification(`Please enter a valid price for "${item.ingredientName}".`, 'error');
            return;
        }
    }

    const payload = {
        inventoryInvoiceId: inventoryInvoiceId,
        privateVendorId: vendorId,
        invoiceId: invoiceId,
        invoiceDate: invoiceDate,
        items: editInvoiceItems.map(item => ({
            ingredientId: item.ingredientId,
            quantity: item.quantity ? parseInt(item.quantity) : null,
            packSize: item.hasPackSize ? (item.packSize ? parseInt(item.packSize) : null) : null,
            price: parseFloat(item.price)
        }))
    };

    $('#updateInvoiceBtn').prop('disabled', true).html('<i class="fas fa-spinner fa-spin"></i> Updating...');

    ajaxRequest({
        url: '/api/InventoryApi/UpdateInvoice',
        method: 'PUT',
        data: payload,
        showMessage: true,
        onSuccess: function (response) {
            closeEditInvoiceModal();
            refreshInvoicesTable();
        },
        onError: function (xhr, status, error) {
            console.error('Failed to update invoice:', error);
            $('#updateInvoiceBtn').prop('disabled', false).html('<i class="fas fa-save"></i> Update Invoice');
        }
    });
}

/* ── 6. Helpers ───────────────────────────────────────────────────────── */

// Function to initialize DataTable
function initDataTable() {
    table = $('#invoicesTable').DataTable({
        paging: true,
        searching: true,
        ordering: false,
        pageLength: 10,
        lengthMenu: [10, 25, 50, 100],
        language: {
            search: '',
            searchPlaceholder: 'Search invoices...'
        }
    });
}

// Function to update summary statistics
function updateStats() {
    const totalCount = allInvoices.length;
    let totalSpend = 0;
    const vendorSet = new Set();

    allInvoices.forEach(inv => {
        totalSpend += (Number(inv.total) || 0);
        if (inv.vendorName) { vendorSet.add(inv.vendorName); }
    });

    $('#statTotalInvoices').text(totalCount);
}

// Clears the table and renders the given invoices
function renderInvoicesTable(invoices) {
    // Clear existing rows in DataTable
    table.clear();

    // Add new rows
    invoices.forEach(invoice => {
        table.row.add($(buildInvoiceRow(invoice)));
    });

    // Redraw the table
    table.draw();
}

// Builds a single invoice table row
function buildInvoiceRow(invoice) {
    const items = invoice.items || (invoice.itemsJson ? parseItems(invoice.itemsJson) : []);
    const itemsCount = items.length;
    const vendorName = invoice.vendorName || 'N/A';
    const invoiceNum = invoice.invoiceId || ('INV-' + invoice.inventoryInvoiceId);
    const invoiceDate = invoice.invoiceDate ? formatInvoiceDate(invoice.invoiceDate) : '-';

    const hasVendorId = invoice.vendorId != null && invoice.vendorId !== 0 && invoice.vendorId !== '';

    const editBtnHtml = !hasVendorId ? `
        <a href="#" onclick="editInvoice(${invoice.inventoryInvoiceId}); return false;"><i class="fas fa-edit"></i> Edit Details</a>
    ` : '';

    return `
                    <tr>
                        <td>
                            <div class="category-info">
                                <p class="category-name">${invoiceNum}</p>
                            </div>
                        </td>
                        <td><i class="fas fa-calendar-alt" style="color: #6b7280; margin-right: 5px;"></i> ${invoiceDate}</td>
                        <td><i class="fas fa-truck"></i> ${vendorName}</td>
                        <td class="text-right">${formatCurrency(invoice.total)}</td>
                        <td class="text-center"><span class="status-badge good">${itemsCount}</span></td>
                        <td>
                            <div class="dropdown">
                                <button class="dropdown-toggle">
                                    <i class="fas fa-ellipsis-h"></i>
                                </button>
                                <div class="dropdown-menu">
                                    <a href="#" onclick="viewInvoiceDetails(${invoice.inventoryInvoiceId}); return false;"><i class="fas fa-eye"></i> View Details</a>
                                    ${editBtnHtml}
                                </div>
                            </div>
                        </td>
                    </tr>
                `;
}

// Opens the invoice detail modal and populates it
function viewInvoiceDetails(inventoryInvoiceId) {
    const invoice = allInvoices.find(i => i.inventoryInvoiceId === inventoryInvoiceId);
    if (!invoice) { return; }

    const invoiceNum = invoice.invoiceId || ('INV-' + invoice.inventoryInvoiceId);
    const vendorName = invoice.vendorName || 'N/A';

    $('#modalInvoiceTitle').text(`Invoice #${invoiceNum}`);
    $('#modalInvoiceSubtitle').text(`Supplier: ${vendorName}`);
    $('#modalInvoiceId').text(invoiceNum);
    $('#modalInvoiceDate').text(invoice.invoiceDate ? formatInvoiceDate(invoice.invoiceDate) : '-');
    $('#modalVendorName').text(vendorName);
    $('#modalTotal').text(formatCurrency(invoice.total));

    const items = invoice.items || (invoice.itemsJson ? parseItems(invoice.itemsJson) : []);
    $('#modalItemsCount').text(items.length);

    const $modalTbody = $('#modalItemsTableBody');
    $modalTbody.empty();

    if (!items || items.length === 0) {
        $modalTbody.html(`
            <tr>
                <td colspan="4" class="text-center" style="padding: 24px; color: #6b7280;">
                    No line items available for this invoice.
                </td>
            </tr>
        `);
    } else {
        items.forEach(item => {
            const isPack = (item.packSize != null && Number(item.packSize) > 0);
            const typeHtml = isPack
                ? `<p class="ingredient-meta text-highlight"><i class="fas fa-cubes"></i> Pack Item</p>`
                : `<p class="ingredient-meta text-highlight danger"><i class="fas fa-weight-hanging"></i> Catch Weight Item</p>`;

            const vendorProductHtml = item.vendorProductId
                ? `<span class="category-badge"> ${item.vendorProductId}</span>`
                : '';

            const unit = item.unit || item.Unit;
            const unitLabel = unit ? ` ${unit}` : '';
            const qtyDisplay = isPack
                ? `${Number(item.quantity)} Case(s)`
                : `${Number(item.quantity).toFixed(2)}${unitLabel}`;

            $modalTbody.append(`
                <tr>
                    <td style="font-weight: 600; color: #1f2937;">
                        <div style="display: flex; align-items: center;">
                            <i class="fas fa-box" style="color: #f08d11; margin-right: 6px; font-size: 11px;"></i>
                            <span>${item.ingredientName}</span>
                        </div>
                        <div style="font-size: 11px; color: #6b7280; font-weight: normal; margin-top: 4px; display: flex; align-items: center; gap: 8px; flex-wrap: wrap;">
                            ${typeHtml}
                            ${vendorProductHtml}
                        </div>
                    </td>
                    <td class="text-center">${item.packSize ?? '-'}</td>
                    <td class="text-right" style="font-weight: 600;">${qtyDisplay}</td>
                    <td class="text-right" style="font-weight: 700; color: #0f172a;">${formatCurrency(item.price)}</td>
                </tr>
            `);
        });
    }

    openModal('invoiceDetailModal');
}

// Opens the Add Invoice modal and resets all its fields
function openAddInvoiceModal() {
    invoiceItems = [];
    $('#newInvoiceId').val('');

    // Default invoice date to today
    const today = new Date();
    const yyyy = today.getFullYear();
    const mm = String(today.getMonth() + 1).padStart(2, '0');
    const dd = String(today.getDate()).padStart(2, '0');
    $('#newInvoiceDate').val(`${yyyy}-${mm}-${dd}`);

    $('#invoiceItemsBody').empty();
    syncInvoiceItemsView();

    if ($('#newInvoiceVendor').hasClass('select2-hidden-accessible')) {
        $('#newInvoiceVendor').val(null).trigger('change.select2');
    }
    resetIngredientSelector();

    $('#submitInvoiceBtn').prop('disabled', false).html('<i class="fas fa-save"></i> Save Invoice');
    openModal('addInvoiceModal');
}

// Closes the Add Invoice modal
function closeAddInvoiceModal() {
    closeModal('addInvoiceModal');
}

// Resets the ingredient selector to its default state
function resetIngredientSelector() {
    if ($('#ingredientSelector').hasClass('select2-hidden-accessible')) {
        $('#ingredientSelector').select2('destroy');
    }
    $('#ingredientSelector').empty().append('<option value="">-- Select Supplier First --</option>');
}

// Resets the edit modal ingredient selector
function resetEditIngredientSelector() {
    if ($('#editIngredientSelector').hasClass('select2-hidden-accessible')) {
        $('#editIngredientSelector').select2('destroy');
    }
    $('#editIngredientSelector').empty().append('<option value="">-- Select Supplier First --</option>');
}

// Opens the Edit Invoice modal and loads the invoice data
function editInvoice(inventoryInvoiceId) {
    try {
        const invoice = allInvoices.find(i => i.inventoryInvoiceId == inventoryInvoiceId);
        if (!invoice) {
            console.error('Invoice not found for ID:', inventoryInvoiceId);
            return;
        }

        const hasVendorId = invoice.vendorId != null && invoice.vendorId !== 0 && invoice.vendorId !== '';
        if (hasVendorId) {
            showNotification('Registered vendor invoices cannot be edited.', 'warning');
            return;
        }

        $('#editInventoryInvoiceId').val(invoice.inventoryInvoiceId);
        $('#editInvoiceId').val(invoice.invoiceId || '');

        if (invoice.invoiceDate) {
            const d = new Date(invoice.invoiceDate);
            if (!isNaN(d.getTime())) {
                const yyyy = d.getFullYear();
                const mm = String(d.getMonth() + 1).padStart(2, '0');
                const dd = String(d.getDate()).padStart(2, '0');
                $('#editInvoiceDate').val(`${yyyy}-${mm}-${dd}`);
            } else {
                $('#editInvoiceDate').val('');
            }
        } else {
            $('#editInvoiceDate').val('');
        }

        let vendorId = invoice.privateVendorId || invoice.vendorId || '';
        const vendorName = (invoice.vendorName || '').trim();

        // If vendorId is empty or 0, fallback to lookup by vendorName in allVendors
        if ((!vendorId || vendorId === '0' || vendorId === 0) && vendorName && allVendors && allVendors.length > 0) {
            const matched = allVendors.find(v => (v.name || '').trim().toLowerCase() === vendorName.toLowerCase());
            if (matched) {
                vendorId = matched.privateVendorId;
            }
        }

        $('#editInventoryInvoiceId').data('vendorId', vendorId);

        if (vendorId) {
            // Ensure the option exists in the select
            if ($('#editInvoiceVendor option[value="' + vendorId + '"]').length === 0) {
                const optText = vendorName || (allVendors.find(v => v.privateVendorId == vendorId)?.name) || 'Supplier';
                $('#editInvoiceVendor').append(new Option(optText, vendorId, true, true));
            }

            // Temporarily enable to let Select2 sync its visual selection text, then disable
            $('#editInvoiceVendor').prop('disabled', false);
            $('#editInvoiceVendor').val(vendorId).trigger('change');
            $('#editInvoiceId').prop('disabled', true);

            $('#editInvoiceVendor').prop('disabled', true);
        } else {
            $('#editInvoiceVendor').prop('disabled', false).val('').trigger('change').prop('disabled', true);
        }

        // Populate items. hasPackSize is provisional here (based on the invoice's own
        // saved packSize) and gets resynced against the ingredient's master packSize
        // once loadEditIngredientsByVendor() resolves below.
        const items = invoice.items || (invoice.itemsJson ? parseItems(invoice.itemsJson) : []);
        editInvoiceItems = [];

        items.forEach(item => {
            const ingId = item.ingredientId;
            const ingName = item.ingredientName || 'Unknown Ingredient';
            const qty = item.quantity ?? '';
            const pack = item.packSize ?? null;
            const price = item.price ?? '';

            const hasPackSize = pack != null && Number(pack) > 0;

            editInvoiceItems.push({
                ingredientId: ingId,
                ingredientName: ingName,
                quantity: qty,
                hasPackSize: hasPackSize,
                packSize: hasPackSize ? pack : null,
                price: price
            });
        });

        renderEditInvoiceItems();
        syncEditInvoiceItemsView();

        if (vendorId) {
            loadEditIngredientsByVendor(vendorId);
        } else {
            resetEditIngredientSelector();
        }

        $('#updateInvoiceBtn').prop('disabled', false).html('<i class="fas fa-save"></i> Update Invoice');
        openModal('editInvoiceModal');
    } catch (err) {
        console.error('Error opening edit invoice modal:', err);
    }
}

// Closes the Edit Invoice modal
function closeEditInvoiceModal() {
    closeModal('editInvoiceModal');
}

// Renders the rows inside editInvoiceItemsBody
function renderEditInvoiceItems() {
    const $body = $('#editInvoiceItemsBody');
    $body.empty();

    editInvoiceItems.forEach(item => {
        const isPack = item.hasPackSize;
        const packSizeVal = isPack ? (item.packSize ?? '') : '';

        $body.append(`
            <tr id="edit_irow_${item.ingredientId}" class="ingredient-detail-row" style="border-bottom: 1px solid #f3f4f6;">
                <td style="padding: 8px 12px; font-weight: 600; color: #1f2937;">
                    <i class="fas fa-box" style="color: #f08d11; margin-right: 6px; font-size: 11px;"></i>
                    ${item.ingredientName}
                    <div style="font-size: 11px; color: #6b7280; font-weight: normal; margin-top: 2px;">
                        ${isPack ? '<p class="ingredient-meta text-highlight"><i class="fas fa-cubes"></i> Pack Item</p>' : '<p class="ingredient-meta text-highlight danger"><i class="fas fa-weight-hanging"></i> Catch Weight Item</p>'}
                    </div>
                </td>
                <td style="padding: 6px 4px;">
                    ${isPack
                ? `<input type="number" id="edit_packSize_${item.ingredientId}" min="0" step="1" placeholder="Pack Size" value="${packSizeVal}"
                                  onchange="updateEditItemPackSize(${item.ingredientId}, this.value)">`
                : ''}
                </td>
                <td style="padding: 6px 4px;">
                    <input type="number" min="0" step="any" placeholder="${isPack ? 'Qty' : 'Weight'}" value="${item.quantity ?? ''}"
                           onchange="updateEditItemField(${item.ingredientId}, 'quantity', this.value)">
                </td>
                <td style="padding: 6px 4px;">
                    <input type="number" min="0" step="any" placeholder="0.00" value="${item.price ?? ''}"
                           onchange="updateEditItemField(${item.ingredientId}, 'price', this.value)">
                </td>
                <td style="padding: 6px 4px; text-align: center;">
                    <button type="button" onclick="removeEditIngredientRow(${item.ingredientId})"
                            style="background: none; border: none; color: #ef4444; cursor: pointer; font-size: 15px; padding: 4px;">
                        <i class="fas fa-times"></i>
                    </button>
                </td>
            </tr>
        `);
    });
}

// Adds a row to the edit invoice items table for the selected ingredient
function addEditIngredientRow() {
    const $sel = $('#editIngredientSelector');
    const ingredientId = parseInt($sel.val());
    const ingredientName = $sel.find('option:selected').text();

    if (!ingredientId) {
        showNotification('Please select an ingredient first.', 'error');
        return;
    }

    if (editInvoiceItems.some(i => i.ingredientId === ingredientId)) {
        showNotification('This ingredient is already added.', 'error');
        return;
    }

    const ing = vendorIngredients.find(i => i.ingredientId === ingredientId);
    const ingPackSize = ing ? ing.packSize : null;
    const hasPackSize = ingPackSize != null && Number(ingPackSize) > 0;

    editInvoiceItems.push({
        ingredientId: ingredientId,
        ingredientName: ingredientName,
        quantity: '',
        hasPackSize: hasPackSize,
        packSize: '',
        price: ''
    });

    renderEditInvoiceItems();
    $sel.val(null).trigger('change.select2');
    syncEditInvoiceItemsView();
}

// Updates the pack size value for an edit item (no-op for catch weight items,
// since the input is disabled for them)
function updateEditItemPackSize(ingredientId, value) {
    const item = editInvoiceItems.find(i => i.ingredientId === ingredientId);
    if (!item || !item.hasPackSize) return;
    item.packSize = value;
}

// Updates a field on an edit invoice item
function updateEditItemField(ingredientId, field, value) {
    const item = editInvoiceItems.find(i => i.ingredientId === ingredientId);
    if (item) { item[field] = value; }
}

// Removes an ingredient from the edit invoice items table
function removeEditIngredientRow(ingredientId) {
    editInvoiceItems = editInvoiceItems.filter(i => i.ingredientId !== ingredientId);
    $('#edit_irow_' + ingredientId).remove();
    syncEditInvoiceItemsView();
}

// Shows or hides edit items table based on count
function syncEditInvoiceItemsView() {
    if (editInvoiceItems.length > 0) {
        $('#editInvoiceItemsWrapper').show();
        $('#editInvoiceItemsEmpty').hide();
    } else {
        $('#editInvoiceItemsWrapper').hide();
        $('#editInvoiceItemsEmpty').show();
    }
}

// Function to render rows inside invoiceItemsBody (Add modal)
function renderInvoiceItems() {
    const $body = $('#invoiceItemsBody');
    $body.empty();

    invoiceItems.forEach(item => {
        const isPack = item.hasPackSize;
        const packSizeVal = isPack ? (item.packSize ?? '') : '';

        $body.append(`
            <tr id="irow_${item.ingredientId}" class="ingredient-detail-row" style="border-bottom: 1px solid #f3f4f6;">
                <td style="padding: 8px 12px; font-weight: 600; color: #1f2937;">
                    <i class="fas fa-box" style="color: #f08d11; margin-right: 6px; font-size: 11px;"></i>
                    ${item.ingredientName}
                    <div style="font-size: 11px; color: #6b7280; font-weight: normal; margin-top: 2px;">
                        ${isPack ? '<p class="ingredient-meta text-highlight"><i class="fas fa-cubes"></i> Pack Item</p>' : '<p class="ingredient-meta text-highlight danger"><i class="fas fa-weight-hanging"></i> Catch Weight Item</p>'}
                    </div>
                </td>
                <td style="padding: 6px 4px;">
                    ${isPack
                ? `<input type="number" id="packSize_${item.ingredientId}" min="0" step="1" placeholder="Pack Size" value="${packSizeVal}"
                                  onchange="updateItemPackSize(${item.ingredientId}, this.value)">`
                : ''}
                </td>
                <td style="padding: 6px 4px;">
                    <input type="number" min="0" step="any" placeholder="${isPack ? 'Qty' : 'Weight'}" value="${item.quantity ?? ''}"
                           onchange="updateItemField(${item.ingredientId}, 'quantity', this.value)">
                </td>
                <td style="padding: 6px 4px;">
                    <input type="number" min="0" step="any" placeholder="0.00" value="${item.price ?? ''}"
                           onchange="updateItemField(${item.ingredientId}, 'price', this.value)">
                </td>
                <td style="padding: 6px 4px; text-align: center;">
                    <button type="button" onclick="removeIngredientRow(${item.ingredientId})"
                            style="background: none; border: none; color: #ef4444; cursor: pointer; font-size: 15px; padding: 4px;">
                        <i class="fas fa-times"></i>
                    </button>
                </td>
            </tr>
        `);
    });
}

// Adds a row to the invoice items table for the selected ingredient (Add modal)
function addIngredientRow() {
    const $sel = $('#ingredientSelector');
    const ingredientId = parseInt($sel.val());
    const ingredientName = $sel.find('option:selected').text();

    if (!ingredientId) {
        showNotification('Please select an ingredient first.', 'error');
        return;
    }

    if (invoiceItems.some(i => i.ingredientId === ingredientId)) {
        showNotification('This ingredient is already added.', 'error');
        return;
    }

    const ing = vendorIngredients.find(i => i.ingredientId === ingredientId);
    const ingPackSize = ing ? ing.packSize : null;
    const hasPackSize = ingPackSize != null && Number(ingPackSize) > 0;

    invoiceItems.push({
        ingredientId: ingredientId,
        ingredientName: ingredientName,
        quantity: '',
        hasPackSize: hasPackSize,
        packSize: '',
        price: ''
    });

    renderInvoiceItems();
    $sel.val(null).trigger('change.select2');
    syncInvoiceItemsView();
}

// Updates the pack size value for an item (no-op for catch weight items,
// since the input is disabled for them)
function updateItemPackSize(ingredientId, value) {
    const item = invoiceItems.find(i => i.ingredientId === ingredientId);
    if (!item || !item.hasPackSize) return;
    item.packSize = value;
}

// Updates a single field on an already-added invoice item
function updateItemField(ingredientId, field, value) {
    const item = invoiceItems.find(i => i.ingredientId === ingredientId);
    if (item) { item[field] = value; }
}

// Removes an ingredient row from the invoice items table
function removeIngredientRow(ingredientId) {
    invoiceItems = invoiceItems.filter(i => i.ingredientId !== ingredientId);
    $('#irow_' + ingredientId).remove();
    syncInvoiceItemsView();
}

// Shows the items table or the empty-state placeholder based on item count
function syncInvoiceItemsView() {
    if (invoiceItems.length > 0) {
        $('#invoiceItemsWrapper').show();
        $('#invoiceItemsEmpty').hide();
    } else {
        $('#invoiceItemsWrapper').hide();
        $('#invoiceItemsEmpty').show();
    }
}

/* ── 7. Utilities ─────────────────────────────────────────────────────── */

// Safely parses an items JSON string into an array
function parseItems(itemsJson) {
    try {
        return typeof itemsJson === 'string' ? JSON.parse(itemsJson) : itemsJson;
    } catch {
        return [];
    }
}

// Formats invoice date into readable format (e.g. Oct 24, 2024)
function formatInvoiceDate(dateStr) {
    if (!dateStr) return '-';
    try {
        const d = new Date(dateStr);
        if (isNaN(d.getTime())) return dateStr;
        return d.toLocaleDateString('en-US', { month: 'short', day: '2-digit', year: 'numeric' });
    } catch (e) {
        return dateStr;
    }
}