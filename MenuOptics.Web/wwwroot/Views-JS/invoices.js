/* ============================================================================
 * invoices.js - Inventory Invoices page (Views/Inventory/Invoices.cshtml)
 * Shows supplier invoices list and itemized detail popup modal.
 *
 * Project convention - every Views-JS file follows this section order:
 *   1. Constants  2. State  3. Initialization  4. Event bindings
 *   5. AJAX / CRUD  6. Helpers  7. Utilities
 * ==========================================================================*/

/* ----------------- 1. Constants ----------------- */
const API_ENDPOINTS = {
    GET_INVOICES: '/api/InventoryApi/GetInventoryInvoices'
};

/* ----------------- 2. State --------------------- */
let invoiceTable;
let allInvoices = [];

/* ----------------- 3. Initialization ------------ */
$(document).ready(function () {
    initDataTable();
    loadInvoices();
    initEventBindings();
});

/* ----------------- 4. Event bindings ------------ */
function initEventBindings() {
    $('#invoiceSearch').on('input', function () {
        invoiceTable.search(this.value).draw();
    });

    $('#clearFilterBtn').on('click', function () {
        $('#invoiceSearch').val('');
        invoiceTable.search('').draw();
    });

    $('#refreshInvoicesBtn').on('click', function () {
        loadInvoices();
    });

    // Close modal on click outside
    $('#invoiceDetailModal').on('click', function (e) {
        if ($(e.target).is('#invoiceDetailModal')) {
            closeModal('invoiceDetailModal');
        }
    });
}

/* ----------------- 5. AJAX / CRUD --------------- */
function loadInvoices() {
    ajaxRequest({
        url: API_ENDPOINTS.GET_INVOICES,
        method: 'GET',
        showMessage: false,
        onSuccess: function (response) {
            allInvoices = response.data || [];
            updateStats();
            renderInvoicesTable();
        },
        onError: function (xhr) {
            console.error('Failed to load inventory invoices:', xhr);
        }
    });
}

/* ----------------- 6. Helpers ------------------- */
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
    $('#statTotalSpend').text(formatCurrency(totalSpend));
    $('#statTotalVendors').text(vendorSet.size);
}

// Initialize DataTable
function initDataTable() {
    invoiceTable = $('#invoicesTable').DataTable({
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

function renderInvoicesTable() {
    invoiceTable.clear();

    allInvoices.forEach(invoice => {
        invoiceTable.row.add($(buildInvoiceRow(invoice)));
    });

    invoiceTable.draw();
}

function buildInvoiceRow(invoice) {
    const items = invoice.items || (invoice.itemsJson ? parseItems(invoice.itemsJson) : []);
    const itemsCount = items.length;
    const vendorName = invoice.vendorName || 'N/A';
    const invoiceNum = invoice.invoiceId || ('INV-' + invoice.inventoryInvoiceId);

    return `
        <tr>
            <td>
                <span class="invoice-badge">
                    <i class="fas fa-hashtag" style="font-size: 10px; color: #f08d11;"></i>
                    <strong>${invoiceNum}</strong>
                </span>
            </td>
            <td>
                <span class="status-badge good">
                    <i class="fas fa-truck" style="font-size: 11px;"></i>
                    ${vendorName}
                </span>
            </td>
            <td class="text-right">${formatCurrency(invoice.subtotal)}</td>
            <td class="text-right">${formatCurrency(invoice.tax)}</td>
            <td class="text-right" style="font-weight: 700; color: #111827;">${formatCurrency(invoice.total)}</td>
            <td class="text-center">
                <span style="font-size: 13px; font-weight: 600; background: #f3f4f6; color: #374151; padding: 2px 8px; border-radius: 12px;">
                    ${itemsCount}
                </span>
            </td>
            <td class="text-center">
                <button class="btn btn-sm btn-details" onclick="viewInvoiceDetails(${invoice.inventoryInvoiceId})">
                    <i class="fas fa-eye"></i> View Details
                </button>
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
    $('#modalVendorName').text(vendorName);
    $('#modalSubtotal').text(formatCurrency(invoice.subtotal));
    $('#modalTax').text(formatCurrency(invoice.tax));
    $('#modalTotal').text(formatCurrency(invoice.total));

    const items = invoice.items || (invoice.itemsJson ? parseItems(invoice.itemsJson) : []);
    $('#modalItemsCount').text(items.length);

    const $modalTbody = $('#modalItemsTableBody');
    $modalTbody.empty();

    if (!items || items.length === 0) {
        $modalTbody.html(`
            <tr>
                <td colspan="5" class="text-center" style="padding: 24px; color: #6b7280;">
                    No line items available for this invoice.
                </td>
            </tr>
        `);
    } else {
        items.forEach(item => {
            const qty = Number(item.quantity || item.Quantity || 0);
            const price = Number(item.price || item.Price || 0);
            const packSize = item.packSize ?? item.PackSize ?? '-';
            const ingredientName = item.ingredientName || item.IngredientName || 'Unknown Ingredient';
            const itemTotal = qty * price;

            const tr = `
                <tr>
                    <td style="font-weight: 600; color: #1f2937;">
                        <i class="fas fa-box" style="color: #f08d11; margin-right: 6px; font-size: 11px;"></i>
                        ${ingredientName}
                    </td>
                    <td class="text-center">${packSize}</td>
                    <td class="text-right">${qty.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</td>
                    <td class="text-right">${formatCurrency(price)}</td>
                    <td class="text-right" style="font-weight: 700; color: #0f172a;">${formatCurrency(itemTotal)}</td>
                </tr>
            `;
            $modalTbody.append(tr);
        });
    }

    openModal('invoiceDetailModal');
}

/* ----------------- 7. Utilities ----------------- */
function parseItems(itemsJson) {
    try {
        return typeof itemsJson === 'string' ? JSON.parse(itemsJson) : itemsJson;
    } catch {
        return [];
    }
}


