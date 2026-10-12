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

    // CSV Import and Sample Download bindings
    $('#importInvoiceCsvBtn').on('click', function () {
        const vendorId = $('#newInvoiceVendor').val();
        if (!vendorId) {
            showNotification('Please select a supplier first.', 'warning');
            return;
        }
        $('#invoiceCsvFileInput').val('').trigger('click');
    });

    $('#invoiceCsvFileInput').on('change', function (e) {
        handleInvoiceCsvUpload(e);
    });

    $('#downloadInvoiceSampleBtn').on('click', function (e) {
        e.preventDefault();
        downloadInvoiceSampleCsv();
    });

    // Update Ingredient Prices toggle – Add modal
    $('#addUpdatePricesToggle').on('change', function () {
        const on = this.checked;
        $('#addUpdatePricesLabel').text(on ? 'Yes' : 'No').toggleClass('active', on);
        $('#addUpdatePricesContainer').toggleClass('active', on);
    });

    // Update Ingredient Prices toggle – Edit modal
    $('#editUpdatePricesToggle').on('change', function () {
        const on = this.checked;
        $('#editUpdatePricesLabel').text(on ? 'Yes' : 'No').toggleClass('active', on);
        $('#editUpdatePricesContainer').toggleClass('active', on);
    });

    // Initial load
    refreshInvoicesTable();
    loadVendors();
});

/* ── 5. AJAX / CRUD ───────────────────────────────────────────────────── */

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
                    $('#importInvoiceCsvBtn').css('display', 'inline-flex');
                    $('#downloadInvoiceSampleBtn').css('display', 'inline-flex');
                    loadIngredientsByVendor(vendorId);
                } else {
                    $('#importInvoiceCsvBtn').hide();
                    $('#downloadInvoiceSampleBtn').hide();
                    resetIngredientSelector();
                }
            });

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

function loadIngredientsByVendor(vendorId) {
    if ($('#ingredientSelector').hasClass('select2-hidden-accessible')) {
        $('#ingredientSelector').select2('destroy');
    }
    $('#ingredientSelector').empty().append('<option value="">Loading ingredients...</option>');

    ajaxRequest({
        url: `/api/IngredientApi/GetAllIngredientsByVendorId/${vendorId}/true`,
        method: 'GET',
        data: null,
        showMessage: false,
        onSuccess: function (response) {
            vendorIngredients = (response.data || []).filter(ing => ing.isActive !== false);

            $('#importInvoiceCsvBtn').css('display', 'inline-flex');
            $('#downloadInvoiceSampleBtn').css('display', 'inline-flex');

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

            editInvoiceItems.forEach(item => {
                const ing = vendorIngredients.find(i => i.ingredientId === item.ingredientId);

                if (ing) {
                    const hasPackSize = ing.packSize != null && Number(ing.packSize) > 0;
                    item.hasPackSize = hasPackSize;
                    item.packSize = hasPackSize ? (item.packSize ?? ing.packSize) : null;

                    // Keep vendorProductId from invoice item, fallback to ingredient master
                    item.vendorProductId = item.vendorProductId || ing.vendorProductId || null;
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
        updatePrices: $('#addUpdatePricesToggle').is(':checked'),
        items: invoiceItems.map(item => {
            const isPack = item.hasPackSize;
            const parsedQty = (item.quantity !== '' && item.quantity != null) ? parseFloat(item.quantity) : 1;
            const price = parseFloat(item.price) || 0;
            const isExisting = typeof item.ingredientId === 'number' && item.ingredientId > 0;
            return {
                ingredientId: isExisting ? item.ingredientId : null,
                vendorProductId: item.vendorProductId || null,
                name: item.ingredientName || null,
                unit: item.unit || null,
                size: item.size ? parseFloat(item.size) : null,
                packSize: isPack ? (item.packSize ? parseInt(item.packSize) : null) : null,
                quantity: parsedQty,
                totalPrice: price,
                weight: !isPack ? parsedQty : null
            };
        })
    };

    $('#submitInvoiceBtn')
        .prop('disabled', true)
        .html('<i class="fas fa-spinner fa-spin"></i> Saving...');

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

            $('#submitInvoiceBtn')
                .prop('disabled', false)
                .html('<i class="fas fa-save"></i> Save Invoice');
        }
    });
}

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
        updatePrices: $('#editUpdatePricesToggle').is(':checked'),
        items: editInvoiceItems.map(item => {
            const isPack = item.hasPackSize;
            const parsedQty = (item.quantity !== '' && item.quantity != null) ? parseFloat(item.quantity) : 1;
            const unitPrice = parseFloat(item.price) || 0;
            const isExisting = typeof item.ingredientId === 'number' && item.ingredientId > 0;
            return {
                ingredientId: isExisting ? item.ingredientId : null,
                vendorProductId: item.vendorProductId || null,
                name: item.ingredientName || null,
                unit: item.unit || null,
                size: item.size ? parseFloat(item.size) : null,
                packSize: isPack ? (item.packSize ? parseInt(item.packSize) : null) : null,
                quantity: parsedQty,
                unitPrice: unitPrice,
                totalPrice: parseFloat((unitPrice * parsedQty).toFixed(2)),
                price: unitPrice,
                weight: !isPack ? parsedQty : null
            };
        })
    };

    $('#updateInvoiceBtn')
        .prop('disabled', true)
        .html('<i class="fas fa-spinner fa-spin"></i> Updating...');

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

            $('#updateInvoiceBtn')
                .prop('disabled', false)
                .html('<i class="fas fa-save"></i> Update Invoice');
        }
    });
}

/* ── 6. Helpers ───────────────────────────────────────────────────────── */

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

function updateStats() {
    const totalCount = allInvoices.length;
    let totalSpend = 0;
    const vendorSet = new Set();

    allInvoices.forEach(inv => {
        totalSpend += (Number(inv.total) || 0);

        if (inv.vendorName) {
            vendorSet.add(inv.vendorName);
        }
    });

    $('#statTotalInvoices').text(totalCount);
}

function renderInvoicesTable(invoices) {
    table.clear();

    invoices.forEach(invoice => {
        table.row.add($(buildInvoiceRow(invoice)));
    });

    table.draw();
}

function buildInvoiceRow(invoice) {
    const items = invoice.items || (invoice.itemsJson ? parseItems(invoice.itemsJson) : []);
    const itemsCount = items.length;
    const vendorName = invoice.vendorName || 'N/A';
    const invoiceNum = invoice.invoiceId || ('INV-' + invoice.inventoryInvoiceId);
    const invoiceDate = invoice.invoiceDate ? formatInvoiceDate(invoice.invoiceDate) : '-';

    const hasVendorId =
        invoice.vendorId != null &&
        invoice.vendorId !== 0 &&
        invoice.vendorId !== '';

    const editBtnHtml = !hasVendorId
        ? `
        <a href="#" onclick="editInvoice(${invoice.inventoryInvoiceId}); return false;">
            <i class="fas fa-edit"></i> Edit Details
        </a>
    `
        : '';

    const ingredientNames = items.map(i => i.ingredientName || '').join(', ');
    const vendorProductIds = items.map(i => i.vendorProductId || '').join(', ');

    return `
        <tr>
            <td>
                <div class="category-info">
                    <p class="category-name">${invoiceNum}</p>
                </div>
            </td>

            <td>
                <i class="fas fa-calendar-alt" style="color: #6b7280; margin-right: 5px;"></i>
                ${invoiceDate}
            </td>

            <td>
                <i class="fas fa-truck"></i>
                ${vendorName}
            </td>

            <td class="text-right">
                ${formatCurrency(invoice.total)}
            </td>

            <td class="text-center">
                <span class="status-badge good">${itemsCount}</span>
            </td>

            <td class="hidden">${ingredientNames}</td>
            <td class="hidden">${vendorProductIds}</td>

            <td>
                <div class="dropdown">
                    <button class="dropdown-toggle">
                        <i class="fas fa-ellipsis-h"></i>
                    </button>

                    <div class="dropdown-menu">
                        <a href="#" onclick="viewInvoiceDetails(${invoice.inventoryInvoiceId}); return false;">
                            <i class="fas fa-eye"></i> View Details
                        </a>

                        ${editBtnHtml}
                    </div>
                </div>
            </td>
        </tr>
    `;
}

function viewInvoiceDetails(inventoryInvoiceId) {
    const invoice = allInvoices.find(i => i.inventoryInvoiceId === inventoryInvoiceId);

    if (!invoice) {
        return;
    }

    const invoiceNum = invoice.invoiceId || ('INV-' + invoice.inventoryInvoiceId);
    const vendorName = invoice.vendorName || 'N/A';

    $('#modalInvoiceTitle').text(`Invoice #${invoiceNum}`);
    $('#modalInvoiceSubtitle').text(`Supplier: ${vendorName}`);
    $('#modalInvoiceId').text(invoiceNum);
    $('#modalInvoiceDate').text(
        invoice.invoiceDate ? formatInvoiceDate(invoice.invoiceDate) : '-'
    );
    $('#modalVendorName').text(vendorName);
    $('#modalTotal').text(formatCurrency(invoice.total));

    const items = invoice.items || (
        invoice.itemsJson ? parseItems(invoice.itemsJson) : []
    );

    $('#modalItemsCount').text(items.length);

    const $modalTbody = $('#modalItemsTableBody');
    $modalTbody.empty();

    if (!items || items.length === 0) {
        $modalTbody.html(`
            <tr>
                <td colspan="4"
                    class="text-center"
                    style="padding: 24px; color: #6b7280;">
                    No line items available for this invoice.
                </td>
            </tr>
        `);
    } else {
        items.forEach(item => {
            const isPack =
                item.packSize != null &&
                Number(item.packSize) > 0;

            const typeHtml = isPack
                ? `<p class="ingredient-meta text-highlight">
                        <i class="fas fa-cubes"></i> Pack Item
                   </p>`
                : `<span class="ingredient-meta text-highlight danger">
                        <i class="fas fa-weight-hanging"></i> Catch Weight Item
                   </span>`;

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
                            <i class="fas fa-box"
                               style="color: #f08d11; margin-right: 6px; font-size: 11px;">
                            </i>

                            <span>${item.ingredientName}</span>
                        </div>

                        <div style="
                            font-size: 11px;
                            color: #6b7280;
                            font-weight: normal;
                            margin-top: 4px;
                            display: flex;
                            align-items: center;
                            gap: 8px;
                            flex-wrap: wrap;">

                            ${typeHtml}
                            ${vendorProductHtml}
                        </div>
                    </td>

                    <td class="text-center">
                        ${item.packSize ?? '-'}
                    </td>

                    <td class="text-right" style="font-weight: 600;">
                        ${qtyDisplay}
                    </td>

                    <td class="text-right"
                        style="font-weight: 700; color: #0f172a;">
                        ${formatCurrency(item.price)}
                    </td>
                </tr>
            `);
        });
    }

    openModal('invoiceDetailModal');
}

function openAddInvoiceModal() {
    invoiceItems = [];

    $('#newInvoiceId').val('');

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

    $('#importInvoiceCsvBtn').hide();
    $('#downloadInvoiceSampleBtn').hide();

    resetIngredientSelector();

    $('#submitInvoiceBtn')
        .prop('disabled', false)
        .html('<i class="fas fa-save"></i> Save Invoice');

    // Reset Update Prices toggle
    $('#addUpdatePricesToggle').prop('checked', false).trigger('change');

    openModal('addInvoiceModal');
}

function closeAddInvoiceModal() {
    closeModal('addInvoiceModal');
}

function resetIngredientSelector() {
    if ($('#ingredientSelector').hasClass('select2-hidden-accessible')) {
        $('#ingredientSelector').select2('destroy');
    }

    $('#ingredientSelector')
        .empty()
        .append('<option value="">-- Select Supplier First --</option>');
}

function resetEditIngredientSelector() {
    if ($('#editIngredientSelector').hasClass('select2-hidden-accessible')) {
        $('#editIngredientSelector').select2('destroy');
    }

    $('#editIngredientSelector')
        .empty()
        .append('<option value="">-- Select Supplier First --</option>');
}

function editInvoice(inventoryInvoiceId) {
    try {
        const invoice = allInvoices.find(
            i => i.inventoryInvoiceId == inventoryInvoiceId
        );

        if (!invoice) {
            console.error('Invoice not found for ID:', inventoryInvoiceId);
            return;
        }

        const hasVendorId =
            invoice.vendorId != null &&
            invoice.vendorId !== 0 &&
            invoice.vendorId !== '';

        if (hasVendorId) {
            showNotification(
                'Registered vendor invoices cannot be edited.',
                'warning'
            );
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

        let vendorId =
            invoice.privateVendorId ||
            invoice.vendorId ||
            '';

        const vendorName =
            (invoice.vendorName || '').trim();

        if (
            (!vendorId || vendorId === '0' || vendorId === 0) &&
            vendorName &&
            allVendors &&
            allVendors.length > 0
        ) {
            const matched = allVendors.find(
                v => (v.name || '').trim().toLowerCase() ===
                    vendorName.toLowerCase()
            );

            if (matched) {
                vendorId = matched.privateVendorId;
            }
        }

        $('#editInventoryInvoiceId').data('vendorId', vendorId);

        if (vendorId) {
            if ($('#editInvoiceVendor option[value="' + vendorId + '"]').length === 0) {
                const optText =
                    vendorName ||
                    (allVendors.find(
                        v => v.privateVendorId == vendorId
                    )?.name) ||
                    'Supplier';

                $('#editInvoiceVendor').append(
                    new Option(optText, vendorId, true, true)
                );
            }

            $('#editInvoiceVendor')
                .prop('disabled', false)
                .val(vendorId)
                .trigger('change');

            $('#editInvoiceId').prop('disabled', true);

            $('#editInvoiceVendor').prop('disabled', true);
        } else {
            $('#editInvoiceVendor')
                .prop('disabled', false)
                .val('')
                .trigger('change')
                .prop('disabled', true);
        }

        const items = invoice.items || (
            invoice.itemsJson ? parseItems(invoice.itemsJson) : []
        );

        editInvoiceItems = [];

        items.forEach(item => {
            const ingId = item.ingredientId;
            const ingName = item.ingredientName || 'Unknown Ingredient';
            const qty = item.quantity ?? '';
            const pack = item.packSize ?? null;
            const price = item.price ?? '';

            const hasPackSize =
                pack != null &&
                Number(pack) > 0;

            editInvoiceItems.push({
                ingredientId: ingId,
                ingredientName: ingName,
                vendorProductId: item.vendorProductId || null,
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

        $('#updateInvoiceBtn')
            .prop('disabled', false)
            .html('<i class="fas fa-save"></i> Update Invoice');

        // Reset Update Prices toggle
        $('#editUpdatePricesToggle').prop('checked', false).trigger('change');

        openModal('editInvoiceModal');

    } catch (err) {
        console.error('Error opening edit invoice modal:', err);
    }
}

function closeEditInvoiceModal() {
    closeModal('editInvoiceModal');
}

function renderEditInvoiceItems() {
    const $body = $('#editInvoiceItemsBody');
    $body.empty();

    editInvoiceItems.forEach(item => {
        const isPack = item.hasPackSize;
        const packSizeVal = isPack ? (item.packSize ?? '') : '';

        const vendorProductHtml = item.vendorProductId
            ? `<span class="category-badge"> ${item.vendorProductId}</span>`
            : '';

        $body.append(`
            <tr id="edit_irow_${item.ingredientId}"
                class="ingredient-detail-row"
                style="border-bottom: 1px solid #f3f4f6;">

                <td style="padding: 8px 12px; font-weight: 600; color: #1f2937;">
                    <i class="fas fa-box"
                       style="color: #f08d11; margin-right: 6px; font-size: 11px;">
                    </i>

                    ${item.ingredientName}

                    <div style="
                        font-size: 11px;
                        color: #6b7280;
                        font-weight: normal;
                        margin-top: 2px;">

                        ${isPack
            ? '<span class="ingredient-meta text-highlight"><i class="fas fa-cubes"></i> Pack Item</span>'
            : '<span class="ingredient-meta text-highlight danger"><i class="fas fa-weight-hanging"></i> Catch Weight Item</span>'}

                        ${vendorProductHtml}
                    </div>
                </td>

                <td style="padding: 6px 4px;">
                    ${isPack
                ? `<input type="number"
                                  id="edit_packSize_${item.ingredientId}"
                                  min="0"
                                  step="1"
                                  placeholder="Pack Size"
                                  value="${packSizeVal}"
                                  onchange="updateEditItemPackSize(${item.ingredientId}, this.value)">`
                : ''}
                </td>

                <td style="padding: 6px 4px;">
                    <input type="number"
                           min="0"
                           step="any"
                           placeholder="${isPack ? 'Qty' : 'Weight'}"
                           value="${item.quantity ?? ''}"
                           onchange="updateEditItemField(${item.ingredientId}, 'quantity', this.value)">
                </td>

                <td style="padding: 6px 4px;">
                    <input type="number"
                           min="0"
                           step="any"
                           placeholder="0.00"
                           value="${item.price ?? ''}"
                           onchange="updateEditItemField(${item.ingredientId}, 'price', this.value)">
                </td>

                <td style="padding: 6px 4px; text-align: center;">
                    <button type="button"
                            onclick="removeEditIngredientRow(${item.ingredientId})"
                            style="
                                background: none;
                                border: none;
                                color: #ef4444;
                                cursor: pointer;
                                font-size: 15px;
                                padding: 4px;">

                        <i class="fas fa-times"></i>
                    </button>
                </td>
            </tr>
        `);
    });
}

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

    const ing = vendorIngredients.find(
        i => i.ingredientId === ingredientId
    );

    const ingPackSize = ing ? ing.packSize : null;

    const hasPackSize =
        ingPackSize != null &&
        Number(ingPackSize) > 0;

    editInvoiceItems.push({
        ingredientId: ingredientId,
        ingredientName: ingredientName,
        vendorProductId: ing?.vendorProductId || null,
        quantity: '',
        hasPackSize: hasPackSize,
        packSize: (hasPackSize && ing?.packSize != null) ? ing.packSize : '',
        size: (ing?.size != null) ? Number(ing.size) : null,
        unit: ing?.unit || null,
        price: (ing?.price != null && !isNaN(Number(ing.price))) ? Number(ing.price) : ''
    });

    renderEditInvoiceItems();

    $sel.val(null).trigger('change.select2');

    syncEditInvoiceItemsView();
}

function updateEditItemPackSize(ingredientId, value) {
    const item = editInvoiceItems.find(
        i => i.ingredientId === ingredientId
    );

    if (!item || !item.hasPackSize) {
        return;
    }

    item.packSize = value;
}

function updateEditItemField(ingredientId, field, value) {
    const item = editInvoiceItems.find(
        i => i.ingredientId === ingredientId
    );

    if (item) {
        item[field] = value;
    }
}

function removeEditIngredientRow(ingredientId) {
    editInvoiceItems = editInvoiceItems.filter(
        i => i.ingredientId !== ingredientId
    );

    $('#edit_irow_' + ingredientId).remove();

    syncEditInvoiceItemsView();
}

function syncEditInvoiceItemsView() {
    if (editInvoiceItems.length > 0) {
        $('#editInvoiceItemsWrapper').show();
        $('#editInvoiceItemsEmpty').hide();
    } else {
        $('#editInvoiceItemsWrapper').hide();
        $('#editInvoiceItemsEmpty').show();
    }
}

function renderInvoiceItems() {
    const $body = $('#invoiceItemsBody');
    $body.empty();

    invoiceItems.forEach(item => {
        const isPack = item.hasPackSize;
        const packSizeVal = isPack ? (item.packSize ?? '') : '';

        const vendorProductHtml = item.vendorProductId
            ? `<span class="category-badge"> ${item.vendorProductId}</span>`
            : '';

        $body.append(`
            <tr id="irow_${item.ingredientId}"
                class="ingredient-detail-row"
                style="border-bottom: 1px solid #f3f4f6;">

                <td style="padding: 8px 12px; font-weight: 600; color: #1f2937;">
                    <i class="fas fa-box"
                       style="color: #f08d11; margin-right: 6px; font-size: 11px;">
                    </i>

                    ${item.ingredientName}

                    <div style="
                        font-size: 11px;
                        color: #6b7280;
                        font-weight: normal;
                        margin-top: 2px;">

                        ${isPack
            ? '<span class="ingredient-meta text-highlight"><i class="fas fa-cubes"></i> Pack Item</span>'
            : '<span class="ingredient-meta text-highlight danger"><i class="fas fa-weight-hanging"></i> Catch Weight Item</span>'}

                        ${vendorProductHtml}
                    </div>
                </td>

                <td style="padding: 6px 4px;">
                    ${isPack
                ? `<input type="number"
                                  id="packSize_${item.ingredientId}"
                                  min="0"
                                  step="1"
                                  placeholder="Pack Size"
                                  value="${packSizeVal}"
                                  onchange="updateItemPackSize('${item.ingredientId}', this.value)">`
                : ''}
                </td>

                <td style="padding: 6px 4px;">
                    <input type="number"
                           min="0"
                           step="any"
                           placeholder="${isPack ? 'Qty' : 'Weight'}"
                           value="${item.quantity ?? ''}"
                           onchange="updateItemField('${item.ingredientId}', 'quantity', this.value)">
                </td>

                <td style="padding: 6px 4px;">
                    <input type="number"
                           min="0"
                           step="any"
                           placeholder="0.00"
                           value="${item.price ?? ''}"
                           onchange="updateItemField('${item.ingredientId}', 'price', this.value)">
                </td>

                <td style="padding: 6px 4px; text-align: center;">
                    <button type="button"
                            onclick="removeIngredientRow('${item.ingredientId}')"
                            style="
                                background: none;
                                border: none;
                                color: #ef4444;
                                cursor: pointer;
                                font-size: 15px;
                                padding: 4px;">

                        <i class="fas fa-times"></i>
                    </button>
                </td>
            </tr>
        `);
    });
}

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

    const ing = vendorIngredients.find(
        i => i.ingredientId === ingredientId
    );

    const ingPackSize = ing ? ing.packSize : null;

    const hasPackSize =
        ingPackSize != null &&
        Number(ingPackSize) > 0;

    invoiceItems.push({
        ingredientId: ingredientId,
        ingredientName: ingredientName,
        vendorProductId: ing?.vendorProductId || null,
        quantity: '',
        hasPackSize: hasPackSize,
        packSize: (hasPackSize && ing?.packSize != null) ? ing.packSize : '',
        size: (ing?.size != null) ? Number(ing.size) : null,
        unit: ing?.unit || null,
        price: (ing?.price != null && !isNaN(Number(ing.price))) ? Number(ing.price) : ''
    });

    renderInvoiceItems();

    $sel.val(null).trigger('change.select2');

    syncInvoiceItemsView();
}

function updateItemPackSize(ingredientId, value) {
    const item = invoiceItems.find(
        i => i.ingredientId == ingredientId
    );

    if (!item || !item.hasPackSize) {
        return;
    }

    item.packSize = value;
}

function updateItemField(ingredientId, field, value) {
    const item = invoiceItems.find(
        i => i.ingredientId == ingredientId
    );

    if (item) {
        item[field] = value;
    }
}

function removeIngredientRow(ingredientId) {
    invoiceItems = invoiceItems.filter(
        i => i.ingredientId != ingredientId
    );

    $(document.getElementById('irow_' + ingredientId)).remove();

    syncInvoiceItemsView();
}

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

function parseItems(itemsJson) {
    try {
        return typeof itemsJson === 'string'
            ? JSON.parse(itemsJson)
            : itemsJson;
    } catch {
        return [];
    }
}

function formatInvoiceDate(dateStr) {
    if (!dateStr) return '-';

    try {
        const d = new Date(dateStr);

        if (isNaN(d.getTime())) {
            return dateStr;
        }

        return d.toLocaleDateString('en-US', {
            month: 'short',
            day: '2-digit',
            year: 'numeric'
        });
    } catch (e) {
        return dateStr;
    }
}

/* ── CSV Import / Export for Add Invoice Modal ────────────────────────────── */

function escapeCsvValue(val) {
    if (val == null) return '""';
    const str = val.toString();
    if (str.includes(',') || str.includes('"') || str.includes('\n') || str.includes('\r')) {
        return `"${str.replace(/"/g, '""')}"`;
    }
    return `"${str}"`;
}

function downloadInvoiceSampleCsv() {
    const vendorId = $('#newInvoiceVendor').val();
    if (!vendorId) {
        showNotification('Please select a supplier first.', 'warning');
        return;
    }

    if (vendorIngredients && vendorIngredients.length > 0) {
        generateInvoiceSampleCsv(vendorIngredients);
    } else {
        ajaxRequest({
            url: `/api/IngredientApi/GetAllIngredientsByVendorId/${vendorId}/true`,
            method: 'GET',
            showMessage: false,
            onSuccess: function (response) {
                vendorIngredients = (response.data || []).filter(ing => ing.isActive !== false);
                if (vendorIngredients.length > 0) {
                    generateInvoiceSampleCsv(vendorIngredients);
                } else {
                    showNotification('No ingredients available for this supplier.', 'warning');
                }
            },
            onError: function () {
                showNotification('Failed to download sample file.', 'error');
            }
        });
    }
}

function generateInvoiceSampleCsv(ingredients) {
    const supplierName = $('#newInvoiceVendor option:selected').text() || 'Supplier';

    // Columns: Product ID, Brand, Description, Pack/Size, Weight/Qty, Catch Weight, Price
    const headers = ["Product ID", "Brand", "Description", "Pack/Size", "Weight/Qty", "Catch Weight", "Price"];
    const rows = [headers.join(",")];

    (ingredients || []).forEach(ing => {
        const prodId = escapeCsvValue(ing.vendorProductId);
        const brand = escapeCsvValue(ing.name.split(" ")[0]);
        const description = escapeCsvValue(ing.name.split(" ").slice(1).join(" "));
        const isCatchWeight = (ing.packSize == null || Number(ing.packSize) <= 0);
        const catchWeightStr = isCatchWeight ? 'Yes' : 'No';

        // Pack/Size or Weight/Qty: in dono mai se koi aik hoga, dono nahi ho sakte
        let packSize = '';
        if (!isCatchWeight) {
            const p = ing.packSize ?? ing.PackSize;
            const s = ing.size ?? ing.Size;
            const u = ing.unit ?? ing.Unit;
            if (p != null && s != null && u) {
                packSize = `${p}/${s} ${u}`;
            } else if (p != null && s != null) {
                packSize = `${p}/${s}`;
            } else if (p != null) {
                packSize = `${p}`;
            }
        }
        const weightQty = '';
        const price = ing.price != null && !isNaN(Number(ing.price)) ? Number(ing.price).toFixed(2) : '0.00';

        rows.push([
            prodId,
            brand,
            description,
            packSize,
            weightQty,
            catchWeightStr,
            price
        ].join(","));
    });

    const csvContent = rows.join("\r\n");
    const blob = new Blob(['\uFEFF' + csvContent], { type: 'text/csv;charset=utf-8;' });
    const link = document.createElement('a');
    const url = URL.createObjectURL(blob);
    const safeSupplier = supplierName.replace(/[^a-zA-Z0-9_-]/g, '_');
    link.setAttribute('href', url);
    link.setAttribute('download', `${safeSupplier}_Ingredients_${new Date().toISOString().slice(0, 10)}.csv`);
    link.style.visibility = 'hidden';
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
}

function handleInvoiceCsvUpload(e) {
    const file = e.target.files && e.target.files[0];
    if (!file) return;

    if (!file.name.toLowerCase().endsWith('.csv')) {
        showNotification('Please upload a valid .csv file.', 'error');
        $('#invoiceCsvFileInput').val('');
        return;
    }

    const reader = new FileReader();
    reader.onload = function (event) {
        try {
            processInvoiceCsv(event.target.result);
        } catch (err) {
            console.error('Error parsing CSV:', err);
            showNotification('Error processing CSV file. Please check file structure.', 'error');
        } finally {
            $('#invoiceCsvFileInput').val('');
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

function parsePackSizeValue(raw) {
    if (!raw) return { packSize: null, size: null, unit: null };
    const str = raw.toString().trim();
    if (!str) return { packSize: null, size: null, unit: null };

    // Format: "2/10 KG", "6/5 LB", "2/10KG", "2/10"
    const slashMatch = str.match(/^(\d+(?:\.\d+)?)\s*\/\s*(\d+(?:\.\d+)?)(?:\s*([a-zA-Z]+))?/);
    if (slashMatch) {
        const pSize = parseInt(slashMatch[1], 10);
        const sz = parseFloat(slashMatch[2]);
        const u = slashMatch[3] ? slashMatch[3].toUpperCase().trim() : null;
        return {
            packSize: !isNaN(pSize) ? pSize : null,
            size: !isNaN(sz) ? sz : null,
            unit: u || null
        };
    }

    // Format: single number with optional unit, e.g. "10 KG", "6"
    const singleMatch = str.match(/^(\d+(?:\.\d+)?)(?:\s*([a-zA-Z]+))?/);
    if (singleMatch) {
        const val = parseFloat(singleMatch[1]);
        const u = singleMatch[2] ? singleMatch[2].toUpperCase().trim() : null;
        return {
            packSize: !isNaN(val) ? parseInt(val, 10) : null,
            size: !isNaN(val) ? val : null,
            unit: u || null
        };
    }

    return { packSize: null, size: null, unit: null };
}

function parseWeightQtyValue(raw) {
    if (!raw) return { qty: null, unit: null };
    const str = raw.toString().trim();
    if (!str) return { qty: null, unit: null };

    const match = str.match(/^(\d+(?:\.\d+)?)(?:\s*([a-zA-Z]+))?/);
    if (match) {
        const q = parseFloat(match[1]);
        const u = match[2] ? match[2].toUpperCase().trim() : null;
        return {
            qty: !isNaN(q) ? q : null,
            unit: u || null
        };
    }
    return { qty: null, unit: null };
}

function processInvoiceCsv(csvText) {
    if (!csvText || !csvText.trim()) {
        showNotification('The uploaded CSV file is empty.', 'error');
        return;
    }

    if (!vendorIngredients || vendorIngredients.length === 0) {
        showNotification('Supplier ingredients are not loaded yet. Please wait a moment and try again.', 'warning');
        return;
    }

    const rows = parseCsvText(csvText);
    if (rows.length < 2) {
        showNotification('The CSV file does not contain any item data.', 'error');
        return;
    }

    // Map headers (case and punctuation insensitive)
    const headerRow = rows[0].map(h => (h || '').toString().toLowerCase().replace(/[^a-z0-9]/g, ''));
    
    const idxProductId = headerRow.findIndex(h => h.includes('productid') || h === 'product' || h === 'id' || h === 'itemid');
    const idxBrand = headerRow.findIndex(h => h.includes('brand'));
    const idxDescription = headerRow.findIndex(h => h.includes('desc') || h.includes('name') || h.includes('ingredient'));
    const idxPackSize = headerRow.findIndex(h => h.includes('pack'));
    const idxWeightQty = headerRow.findIndex(h => h.includes('qty') || h.includes('weight') || h.includes('quantity'));
    const idxCatchWeight = headerRow.findIndex(h => h.includes('catch'));
    const idxPrice = headerRow.findIndex(h => h.includes('price') || h.includes('cost'));

    let importedCount = 0;
    let unmatchedRows = [];

    for (let r = 1; r < rows.length; r++) {
        const row = rows[r];
        if (!row || row.length === 0 || row.every(c => !c || !c.trim())) continue;

        const cellProductId = idxProductId !== -1 ? (row[idxProductId] || '').trim() : '';
        const cellBrand = idxBrand !== -1 ? (row[idxBrand] || '').trim() : '';
        const cellDescription = idxDescription !== -1 ? (row[idxDescription] || '').trim() : '';
        const cellPackSize = idxPackSize !== -1 ? (row[idxPackSize] || '').trim() : '';
        const cellWeightQty = idxWeightQty !== -1 ? (row[idxWeightQty] || '').trim() : '';
        const cellPrice = idxPrice !== -1 ? (row[idxPrice] || '').trim() : '';
        const cellCatchWeight = idxCatchWeight !== -1 ? (row[idxCatchWeight] || '').trim().toLowerCase() : '';

        debugger

        // Skip rows where Weight/Qty and Price are not present in CSV
        const parsedWeight = parseWeightQtyValue(cellWeightQty);
        const hasWeight = parsedWeight && parsedWeight.qty !== null && !isNaN(parsedWeight.qty) && parsedWeight.qty > 0;
        const cleanPrice = cellPrice.replace(/[^0-9.]/g, '');
        const priceVal = parseFloat(cleanPrice);
        const hasPrice = cleanPrice !== '' && !isNaN(priceVal) && priceVal > 0;

        if (!hasPrice || !hasWeight || !cellProductId || !cellWeightQty || !cellPackSize || !cellCatchWeight) {
            continue;
        }

        // Matching strategy:
        // 1. By Product ID (vendorProductId)
        let matched = null;
        if (cellProductId) {
            matched = vendorIngredients.find(v => {
                const vPid = (v.vendorProductId || v.VendorProductId || '').toString().trim().toLowerCase();
                return vPid === cellProductId.toLowerCase();
            });
        }

        // 2. By Ingredient Name / Description / Brand + Description
        if (!matched) {
            const combinedName = (cellBrand ? cellBrand + ' ' : '') + cellDescription;
            matched = vendorIngredients.find(v => {
                const vName = (v.name || v.Name || '').toString().trim().toLowerCase();
                const vBrand = (v.brand || v.Brand || '').toString().trim().toLowerCase();
                const vCombined = (vBrand ? vBrand + ' ' : '') + vName;

                return (cellDescription && vName === cellDescription.toLowerCase())
                    || (combinedName && vName === combinedName.toLowerCase())
                    || (combinedName && vCombined === combinedName.toLowerCase())
                    || (cellDescription && vName.includes(cellDescription.toLowerCase()))
                    || (cellDescription && cellDescription.toLowerCase().includes(vName));
            });
        }

        
        const parsedPack = parsePackSizeValue(cellPackSize);
        const hasPack = parsedPack.packSize !== null || parsedPack.size !== null;

        let ingredientId;
        let ingredientName;
        let isCatchWeight;
        let vProdId = cellProductId || null;
        let unit = parsedPack.unit || parsedWeight.unit || null;
        let size = parsedPack.size ?? null;
        let packSize = parsedPack.packSize ?? null;

        



        if (matched) {
            ingredientId = matched.ingredientId;
            ingredientName = matched.name;
            vProdId = matched.vendorProductId || matched.VendorProductId || cellProductId || null;
            isCatchWeight = (matched.packSize == null || Number(matched.packSize) <= 0);
            if (!unit) unit = matched.unit || matched.Unit || null;
            if (size == null && matched.size != null) size = Number(matched.size);
            if (packSize == null && matched.packSize != null) packSize = Number(matched.packSize);
        } else {
            // New ingredient row added in CSV!
            ingredientId = 'new_' + (cellProductId ? cellProductId.replace(/[^a-zA-Z0-9_-]/g, '') : ('row' + r)) + '_' + Date.now();

            if (cellBrand && cellDescription) {
                ingredientName = cellDescription.toLowerCase().includes(cellBrand.toLowerCase())
                    ? cellDescription
                    : `${cellBrand} ${cellDescription}`;
            } else {
                ingredientName = cellDescription || cellBrand || cellProductId || `New Item ${r}`;
            }

            // Determine catch weight
            if (cellCatchWeight === 'yes' || cellCatchWeight === 'true' || cellCatchWeight === '1' || cellCatchWeight === 'y') {
                isCatchWeight = true;
            } else if (cellCatchWeight === 'no' || cellCatchWeight === 'false' || cellCatchWeight === '0' || cellCatchWeight === 'n') {
                isCatchWeight = false;
            } else if (hasWeight && !hasPack) {
                isCatchWeight = true;
            } else {
                isCatchWeight = false;
            }
        }

        const hasPackSize = !isCatchWeight;

        if (!hasPackSize) {
            packSize = null;
        } else if (packSize == null && hasPack) {
            packSize = parsedPack.packSize || 1;
        }

        // Quantity & Price strictly from CSV (no auto-population from DB)
        const qty = parsedWeight.qty;
        const price = priceVal;

        // Upsert in invoiceItems
        const existingIdx = invoiceItems.findIndex(i =>
            (matched && i.ingredientId === matched.ingredientId) ||
            (i.ingredientName && ingredientName && i.ingredientName.toLowerCase() === ingredientName.toLowerCase())
        );

        if (existingIdx !== -1) {
            if (qty !== '') invoiceItems[existingIdx].quantity = qty;
            invoiceItems[existingIdx].price = price;
            invoiceItems[existingIdx].hasPackSize = hasPackSize;
            invoiceItems[existingIdx].packSize = packSize;
            if (size !== null) invoiceItems[existingIdx].size = size;
            if (unit) invoiceItems[existingIdx].unit = unit;
            if (vProdId) invoiceItems[existingIdx].vendorProductId = vProdId;
        } else {
            invoiceItems.push({
                ingredientId: ingredientId,
                ingredientName: ingredientName,
                vendorProductId: vProdId,
                quantity: qty,
                hasPackSize: hasPackSize,
                packSize: packSize,
                size: size,
                unit: unit,
                price: price
            });
        }

        importedCount++;
    }
    debugger
    renderInvoiceItems();
    syncInvoiceItemsView();

    if (importedCount > 0) {
        showNotification(`Successfully imported all ${importedCount} items from CSV!`, 'success');
    } else {
        showNotification('No valid items found in CSV file.', 'warning');
    }
}