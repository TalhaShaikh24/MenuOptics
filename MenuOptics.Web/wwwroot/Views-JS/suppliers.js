/* ============================================================================
 * suppliers.js — Supplier management page (Views/Supplier/Index.cshtml)
 *
 * Project convention — every Views-JS file follows this section order:
 *   1. Constants  2. State  3. Initialization  4. Event bindings
 *   5. AJAX / CRUD  6. Helpers  7. Utilities
 * Functions stay global: they are referenced by inline onclick handlers.
 * ==========================================================================*/

/* ── 2. State ─────────────────────────────────────────────────────────── */

let table;

/* ── 3. Initialization / 4. Event bindings ────────────────────────────── */

$(document).ready(function () {
    // Initialize DataTable once on page load
    initDataTable();

    // Open add modal
    $('#addSupplierBtn').on('click', function () {
        openModal('addSupplierModal');
    });

    // Save new supplier
    $('#addSupplierBtn2').on('click', function () {
        const name = $('#supplierName').val();
        if (name) {
            addSupplier(name);
        } else {
            showNotification('Please fill in the Supplier Name.', 'error');
        }
    });

    // Update existing supplier
    $('#updateSupplierBtn').on('click', function () {
        const supplierData = {
            privateVendorId: $('#editSupplierId').val(),
            name:            $('#editSupplierName').val(),
            isActive:        true
        };
        if (supplierData.name) {
            updateSupplier(supplierData);
        } else {
            showNotification('Please fill in the Supplier Name.', 'error');
        }
    });

    // Search filter
    $('#supplierSearch').on('input', function () {
        table.search(this.value).draw();
    });

    // Active-only toggle
    $('#activeFilter').on('click', toggleActiveFilter);

    refreshSuppliersTable();
});

/* ── 5. AJAX / CRUD ───────────────────────────────────────────────────── */

// Add a supplier
function addSupplier(name) {
    ajaxRequest({
        url: '/api/PrivateVendorApi/CreatePrivateVendor',
        method: 'POST',
        data: { Name: name },
        onSuccess: function (response) {
            $('#addSupplierForm')[0].reset();
            closeModal('addSupplierModal');
            refreshSuppliersTable();
        },
        onError: function (xhr, status, error) {
            console.error('Failed to add supplier:', error);
            showNotification('Failed to add supplier.', 'error');
        }
    });
}

// Fetch a supplier by ID and open edit modal
function getSupplierById(id) {
    ajaxRequest({
        url: `/api/PrivateVendorApi/GetPrivateVendor/${id}`,
        method: 'GET',
        showMessage: false,
        onSuccess: function (response) {
            const s = response.data;
            $('#editSupplierId').val(s.privateVendorId);
            $('#editSupplierName').val(s.name);
            openModal('editSupplierModal');
        },
        onError: function (xhr, status, error) {
            console.error('Failed to fetch supplier:', error);
            showNotification('Failed to fetch supplier.', 'error');
        }
    });
}

// Update a supplier
function updateSupplier(supplierData) {
    ajaxRequest({
        url: '/api/PrivateVendorApi/UpdatePrivateVendor',
        method: 'PUT',
        data: {
            PrivateVendorId: supplierData.privateVendorId,
            Name:            supplierData.name,
            IsActive:        supplierData.isActive
        },
        onSuccess: function (response) {
            closeModal('editSupplierModal');
            refreshSuppliersTable();
        },
        onError: function (xhr, status, error) {
            console.error('Failed to update supplier:', error);
            showNotification('Failed to update supplier.', 'error');
        }
    });
}

// Toggle active status
function toggleSupplierActive(id) {
    if (confirm('Are you sure you want to toggle the active status of this supplier?')) {
        ajaxRequest({
            url: `/api/PrivateVendorApi/TogglePrivateVendorActive/${id}`,
            method: 'PUT',
            showMessage: false,
            onSuccess: function (response) {
                refreshSuppliersTable();
            },
            onError: function (xhr, status, error) {
                console.error('Failed to toggle supplier status:', error);
                showNotification('Failed to toggle supplier status.', 'error');
            }
        });
    }
}

// Open edit modal (called from inline onclick)
function editSupplier(id) {
    getSupplierById(id);
}

// Refresh the full suppliers table
function refreshSuppliersTable() {
    ajaxRequest({
        url: '/api/PrivateVendorApi/GetAllPrivateVendors',
        method: 'GET',
        data: null,
        showMessage: false,
        onSuccess: function (response) {
            const suppliers = response.data;
            $('#totalSuppliers').text(suppliers.filter(s => s.isActive).length);
            renderSuppliersTable(suppliers);
        },
        onError: function (xhr, status, error) {
            console.error('Failed to fetch suppliers:', error);
            showNotification('Failed to fetch suppliers.', 'error');
        }
    });
}

// Toggle active-only filter
function toggleActiveFilter() {
    const isActiveOnly = $('#activeFilter').hasClass('active');
    $('#activeFilter').toggleClass('active');

    ajaxRequest({
        url: '/api/PrivateVendorApi/GetAllPrivateVendors',
        method: 'GET',
        data: null,
        showMessage: false,
        onSuccess: function (response) {
            const suppliers = isActiveOnly ? response.data : response.data.filter(s => s.isActive);
            renderSuppliersTable(suppliers);
        },
        onError: function (xhr, status, error) {
            console.error('Failed to fetch suppliers:', error);
            showNotification('Failed to fetch suppliers.', 'error');
        }
    });
}

/* ── 6. Helpers ───────────────────────────────────────────────────────── */

// Initialize DataTable
function initDataTable() {
    table = $('.data-table').DataTable({
        paging: true,
        processing: true,
        searching: true,
        ordering: false,
        pageLength: 10,
        lengthMenu: [10, 25, 50, 100],
        language: {
            search: '',
            searchPlaceholder: 'Search suppliers...'
        }
    });
}

// Clear and re-render table rows
function renderSuppliersTable(suppliers) {
    table.clear();
    suppliers.forEach(supplier => {
        table.row.add($(buildSupplierRow(supplier)));
    });
    table.draw();
}

// Build a single supplier table row
function buildSupplierRow(supplier) {
    const createdDate = DateFormatter.formatWithComma(supplier.createdOn);
    const actionText = supplier.isActive ? 'Deactivate' : 'Activate';
    const actionIcon = supplier.isActive ? 'fas fa-ban' : 'fas fa-check';
    return `
        <tr>
            <td>
                <div class="category-info">
                    <p class="category-name">${supplier.name}</p>
                </div>
            </td>
            <td><i class="fas fa-calendar"></i> ${createdDate}</td>
            <td><span class="status-badge ${supplier.isActive ? 'good' : 'critical'}">${supplier.isActive ? 'Active' : 'Inactive'}</span></td>
            <td>
                <div class="dropdown">
                    <button class="dropdown-toggle">
                        <i class="fas fa-ellipsis-h"></i>
                    </button>
                    <div class="dropdown-menu">
                        <a href="#" onclick="editSupplier(${supplier.privateVendorId})"><i class="fas fa-edit"></i> Edit Details</a>
                        <a href="#" onclick="toggleSupplierActive(${supplier.privateVendorId})"><i class="${actionIcon}"></i> ${actionText}</a>
                    </div>
                </div>
            </td>
        </tr>
    `;
}

/* ── 7. Utilities ─────────────────────────────────────────────────────── */

function openModal(modalId) {
    $(`#${modalId}`).addClass('active');
}

function closeModal(modalId) {
    $(`#${modalId}`).removeClass('active');
}
