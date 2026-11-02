// suppliers.js — Supplier management page scripts.

let table;

$(document).ready(function () {
    table = initDataTable('.data-table', {
        processing: true,
        language: { searchPlaceholder: 'Search suppliers...' }
    });

    $('#addSupplierBtn').on('click', function () {
        openModal('addSupplierModal');
    });

    $('#addSupplierBtn2').on('click', function () {
        const name = $('#supplierName').val();
        if (name) {
            addSupplier(name);
        } else {
            showNotification('Please fill in the Supplier Name.', 'error');
        }
    });

    $('#updateSupplierBtn').on('click', function () {
        const supplierData = {
            privateVendorId: $('#editSupplierId').val(),
            name: $('#editSupplierName').val(),
            isActive: true
        };
        if (supplierData.name) {
            updateSupplier(supplierData);
        } else {
            showNotification('Please fill in the Supplier Name.', 'error');
        }
    });

    $('#supplierSearch').on('input', function () {
        table.search(this.value).draw();
    });

    $('#activeFilter').on('click', toggleActiveFilter);

    refreshSuppliersTable();
});

function refreshSuppliersTable() {
    LoadingComponent.showForTable('.data-table', 'Loading suppliers...');
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
        },
        onComplete: function () { LoadingComponent.hideForTable('.data-table'); }
    });
}

function toggleActiveFilter() {
    LoadingComponent.showForTable('.data-table', 'Loading suppliers...');
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
        },
        onComplete: function () { LoadingComponent.hideForTable('.data-table'); }
    });
}

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

function addSupplier(name) {
    const $btn = $('#addSupplierBtn2');
    setBtnLoading($btn, true);
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
        },
        onComplete: function () { setBtnLoading($btn, false); }
    });
}

function updateSupplier(supplierData) {
    const $btn = $('#updateSupplierBtn');
    setBtnLoading($btn, true);
    ajaxRequest({
        url: '/api/PrivateVendorApi/UpdatePrivateVendor',
        method: 'PUT',
        data: {
            PrivateVendorId: supplierData.privateVendorId,
            Name: supplierData.name,
            IsActive: supplierData.isActive
        },
        onSuccess: function (response) {
            closeModal('editSupplierModal');
            refreshSuppliersTable();
        },
        onError: function (xhr, status, error) {
            console.error('Failed to update supplier:', error);
            showNotification('Failed to update supplier.', 'error');
        },
        onComplete: function () { setBtnLoading($btn, false); }
    });
}

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

function editSupplier(id) {
    getSupplierById(id);
}

function renderSuppliersTable(suppliers) {
    table.clear();
    suppliers.forEach(supplier => {
        table.row.add($(buildSupplierRow(supplier)));
    });
    table.draw();
}

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

function openModal(modalId) {
    $(`#${modalId}`).addClass('active');
}

function closeModal(modalId) {
    $(`#${modalId}`).removeClass('active');
}
