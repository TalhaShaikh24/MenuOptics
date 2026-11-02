// suppliers.js — Supplier management page scripts.

let table;
let activeFilterApplied = false;

$(document).ready(function () {
    table = initDataTable('.data-table', {
        processing: true,
        loadingMessage: 'Loading suppliers...',
        ajax: function (dtParams, callback, settings) {
            $.ajax({
                url: '/api/PrivateVendorApi/GetAllPrivateVendors',
                method: 'GET',
                success: function (response) {
                    const suppliers = response.data;
                    $('#totalSuppliers').text(suppliers.filter(s => s.isActive).length);

                    const rows = activeFilterApplied
                        ? suppliers.filter(s => s.isActive)
                        : suppliers;
                    callback({ data: rows });
                },
                error: function (xhr, status, error) {
                    console.error('Failed to fetch suppliers:', error);
                    showNotification('Failed to fetch suppliers.', 'error');
                    callback({ data: [] });
                }
            });
        },
        columns: [
            {
                data: 'name',
                render: function (name) {
                    return `<div class="category-info">
                                <p class="category-name">${name}</p>
                            </div>`;
                }
            },
            {
                data: 'createdOn',
                render: function (createdOn) {
                    return `<i class="fas fa-calendar"></i> ${DateFormatter.formatWithComma(createdOn)}`;
                }
            },
            {
                data: 'isActive',
                render: function (isActive) {
                    return `<span class="status-badge ${isActive ? 'good' : 'critical'}">${isActive ? 'Active' : 'Inactive'}</span>`;
                }
            },
            {
                data: null,
                render: function (data, type, row) {
                    const actionText = row.isActive ? 'Deactivate' : 'Activate';
                    const actionIcon = row.isActive ? 'fas fa-ban' : 'fas fa-check';
                    return `<div class="dropdown">
                                <button class="dropdown-toggle">
                                    <i class="fas fa-ellipsis-h"></i>
                                </button>
                                <div class="dropdown-menu">
                                    <a href="#" onclick="editSupplier(${row.privateVendorId})"><i class="fas fa-edit"></i> Edit Details</a>
                                    <a href="#" onclick="toggleSupplierActive(${row.privateVendorId})"><i class="${actionIcon}"></i> ${actionText}</a>
                                </div>
                            </div>`;
                }
            }
        ],
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
});

function refreshSuppliersTable() {
    // Matches the old refresh: it always re-rendered the full, unfiltered list.
    activeFilterApplied = false;
    if (table) {
        table.ajax.reload(null, false);
    }
}

function toggleActiveFilter() {
    $('#activeFilter').toggleClass('active');
    activeFilterApplied = $('#activeFilter').hasClass('active');
    table.ajax.reload(null, false);
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

function openModal(modalId) {
    $(`#${modalId}`).addClass('active');
}

function closeModal(modalId) {
    $(`#${modalId}`).removeClass('active');
}
