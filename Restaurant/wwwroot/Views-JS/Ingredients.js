let table;

// ─── Ingredient table (server-side DataTable) ─────────────────────────────────
function initIngredientsTable() {
    if ($.fn.DataTable.isDataTable('.ingredients-table')) {
        $('.ingredients-table').DataTable().destroy();
    }

    table = $('.ingredients-table').DataTable({
        serverSide: true,
        processing: true,
        ajax: function (dtParams, callback, settings) {
            // Map DataTables params → our API query params
            const searchValue = dtParams.search ? dtParams.search.value : '';
            const orderCol    = dtParams.order  && dtParams.order.length  ? dtParams.order[0].column : 1;
            const orderDir    = dtParams.order  && dtParams.order.length  ? dtParams.order[0].dir    : 'asc';

            $.ajax({
                url: '/api/IngredientApi/GetAllActiveInactiveIngredients',
                method: 'GET',
                data: {
                    draw:        dtParams.draw,
                    start:       dtParams.start,
                    length:      dtParams.length,
                    searchValue: searchValue,
                    orderColumn: orderCol,
                    orderDir:    orderDir
                },
                success: function (response) {
                    // Rebuild supplier dropdown from current page data
                    const supplierFilter = $('#supplierFilter');
                    const existing = supplierFilter.val();
                    supplierFilter.find('option:not(:first)').remove();
                    const suppliers = new Set();
                    (response.data || []).forEach(i => {
                        if (i.fullName) suppliers.add(i.fullName);
                    });
                    suppliers.forEach(s => supplierFilter.append(`<option value="${s}">${s}</option>`));
                    if (existing) supplierFilter.val(existing);

                    callback({
                        draw:            response.draw,
                        recordsTotal:    response.recordsTotal,
                        recordsFiltered: response.recordsFiltered,
                        data:            response.data || []
                    });
                },
                error: function (xhr) {
                    showNotification('Failed to load ingredients.', 'error');
                    callback({ draw: dtParams.draw, recordsTotal: 0, recordsFiltered: 0, data: [] });
                }
            });
        },
        columns: [
            
            {
                data: 'name',
                render: function (name, type, row) {
                    const date = DateFormatter.formatWithComma(row.createdOn);
                    return `<div class="ingredient-info">
                                <p class="ingredient-name">${name.trim().includes(' ') ? name.split(' ').slice(1).join(' ') : name}</p>
                                <p class="ingredient-meta">Brand: ${name.split(' ')[0]}</p>
                            </div>`;
                }
            },
            {
                data: 'vendorProductId',
                render: val => `<span class="${val != null ? 'category-badge' : ''}">${val || ''}</span>`
            },
            {
                // Size — not sortable, not searchable
                orderable: false,
                searchable: false,
                data: 'size',
                render: (val, type, row) => `<span class="${val != null ? 'category-badge-size' : ''}">${val ? `${val} ${row.unit || ''}` : ''}</span>`
            },
            {
                data: 'price',
                render: val => `$${val}`
            },
            {
                // Supplier
                data: 'fullName',
                render: val => val || 'N/A'
            },
            {
                data: 'isActive',
                render: function (active) {
                    const cls  = active ? 'good' : 'critical';
                    const lbl  = active ? 'Active' : 'In Active';
                    return `<span class="status-badge ${cls}">${lbl}</span>`;
                }
            },
           
           
            {
                // Actions — not sortable
                orderable: false,
                searchable: false,
                data: null,
                render: function (data, type, row) {
                    const addStock = (!row.vendorId || row.vendorId <= 0)
                        ? `<a href="#" onclick="addStock(${row.ingredientId})">
                               <i class="fas fa-box"></i> Add Stock
                           </a>`
                        : '';

                    const toggleLink = (!row.vendorId || row.vendorId <= 0)
                        ? `<a href="#" onclick="toggleIngredientActive(${row.ingredientId})">
                               <i class="fas fa-ban"></i>
                               ${row.isActive ? 'Deactivate' : 'Activate'}
                           </a>`
                        : '';

                    return `${row.vendorId && row.vendorId > 0 ? '' : `
                            <div class="dropdown">
                                <button class="dropdown-toggle">
                                    <i class="fas fa-ellipsis-h"></i>
                                </button>
                                <div class="dropdown-menu">
                                    ${addStock}
                                    <a href="#" onclick="editIngredient(${row.ingredientId})">
                                        <i class="fas fa-edit"></i> Edit Details
                                    </a>
                                    ${toggleLink}
                                </div>
                            </div>
                        `}`;
                }
            }
        ],
        pageLength: 10,
        lengthMenu: [10, 25, 50, 100],
        ordering: true,
        order: [[1, 'asc']],
        language: {
            search: '',
            searchPlaceholder: 'Search ingredients...',
            processing: '<i class="fas fa-spinner fa-spin"></i> Loading...'
        },
        drawCallback: function () {
            // Re-bind select-all checkbox after each draw
            $('#selectAllIngredients').prop('checked', false);
            $('#selectAllIngredients').off('change').on('change', function () {
                $('.ingredient-checkbox').prop('checked', this.checked);
            });
        }
    });

    // Custom search input → server
    $('#ingredientSearch').off('keyup').on('keyup', function () {
        table.search(this.value).draw();
    });

    // Supplier filter → search on supplier column (index 5)
    $('#supplierFilter').off('change').on('change', function () {
        table.column(7).search(this.value).draw();
    });
}

// Convenience alias used after add/edit/delete operations
function refreshIngredientsTable() {
    if (table) {
        table.ajax.reload(null, false); // false = keep current page
    } else {
        initIngredientsTable();
    }
}

// ─── Page ready ────────────────────────────────────────────────────────────────
$(document).ready(function () {

    // ── Save new ingredient ───────────────────────────────────────────────────
    $('#addIngredientBtn2').on('click', function () {
        const ingredientData = {
            name:          $('#ingredientName').val(),
            unit:          $('#ingredientUnit').val(),
            price:         parseFloat($('#ingredientPrice').val()) || 0,
            stock:         $('#ingredientStock').val() || 0,
            size:          $('#ingredientSize').val() 
        };
        if (ingredientData.name && ingredientData.unit && ingredientData.size) {
            addIngredientByUserId(ingredientData);
        } else {
            showNotification('Please fill in all required fields.', 'error');
        }
    });

    // ── Update ingredient ─────────────────────────────────────────────────────
    $('#updateIngredientBtn').on('click', function () {
        const ingredientData = {
            ingredientId:  $('#editIngredientId').val(),
            name:          $('#editIngredientName').val(),
            unit:          $('#editIngredientUnit').val(),
            price:         parseFloat($('#editIngredientPrice').val()) || 0,
            size:         parseFloat($('#editIngredientSize').val()),
        };
        if (ingredientData.name && ingredientData.unit && ingredientData.size) {
            updateIngredient(ingredientData);
        } else {
            showNotification('Please fill in all required fields.', 'error');
        }
    });

    // ── Bulk actions ──────────────────────────────────────────────────────────
    $('#bulkDeleteBtn').on('click', function () { bulkDeleteIngredients(); });
    $('#bulkToggleBtn').on('click',  function () { bulkToggleIngredients(); });

    // ── Export CSV ────────────────────────────────────────────────────────────
    $('#exportCSVBtn').on('click', function () {
        ajaxRequest({
            url: '/api/IngredientApi/GetAllIngredientsByUserId',
            method: 'GET',
            showMessage: false,
            onSuccess: function (response) {
                const ingredients = response.data;
                let csvContent = "Name,Unit,Physical Count\n";
                ingredients.forEach(ingredient => {
                    const name = ingredient.name.replace(/,/g, ' ');
                    const unit = ingredient.unit || '';
                    csvContent += `"${name}","${unit}", ""\n`;
                });
                const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
                const link = document.createElement('a');
                const url  = URL.createObjectURL(blob);
                link.setAttribute('href', url);
                link.setAttribute('download', `Ingredients_${new Date().toISOString().slice(0, 10)}.csv`);
                link.style.visibility = 'hidden';
                document.body.appendChild(link);
                link.click();
                document.body.removeChild(link);
                showNotification('CSV exported successfully!', 'success');
            },
            onError: function () {
                showNotification('Failed to export CSV.', 'error');
            }
        });
    });

    // ── Export PDF ────────────────────────────────────────────────────────────
    $('#exportPdfBtn').on('click', function () {
        ajaxRequest({
            url: '/api/IngredientApi/GetAllIngredientsByUserId',
            method: 'GET',
            showMessage: false,
            onSuccess: function (response) {
                
                const ingredients = response.data;
                const { jsPDF } = window.jspdf;
                const doc = new jsPDF();
                doc.setFontSize(18);
                doc.text('Ingredients List', 14, 20);
                doc.setFontSize(10);
                doc.text(`Generated on: ${new Date().toLocaleDateString()}`, 14, 28);
                const headers = [['Name', 'Size', 'Physical Count']];
                const data = ingredients.map(i => [i.name, i.size == null ? '' : i.size  + ' ' + i.unit || '', '']);
                doc.autoTable({
                    head: headers,
                    body: data,
                    startY: 35,
                    theme: 'grid',
                    styles: { fontSize: 9, cellPadding: 3 },
                    headStyles: { fillColor: [41, 128, 185], textColor: 255, fontStyle: 'bold' },
                    columnStyles: { 0: { cellWidth: 70 }, 1: { cellWidth: 30 }, 2: { cellWidth: 30 }, 3: { cellWidth: 50 } }
                });
                doc.save(`Ingredients_${new Date().toISOString().slice(0, 10)}.pdf`);
                showNotification('PDF exported successfully!', 'success');
            },
            onError: function () {
                showNotification('Failed to export PDF.', 'error');
            }
        });
    });

    // ── Initialise ────────────────────────────────────────────────────────────
    initIngredientsTable();
    refreshVendors();
});

// ─── CRUD Functions ────────────────────────────────────────────────────────────

function addIngredientByUserId(ingredientData) {
    ajaxRequest({
        url: '/api/IngredientApi/CreateIngredient',
        method: 'POST',
        data: {
            Name:          ingredientData.name,
            Price:         ingredientData.price,
            Unit:          ingredientData.unit,
            Size:         ingredientData.size,
            Stock:         ingredientData.stock
        },
        onSuccess: function () {
            refreshIngredientsTable();
            $('#addIngredientForm')[0].reset();
            closeModal('addIngredientModal');
        },
        onError: function () {
            showNotification('Failed to add ingredient.', 'error');
        }
    });
}

function getIngredientById(id) {
    $('.form-group.editIngredientStock').hide();
    ajaxRequest({
        url: `/api/IngredientApi/GetIngredient/${id}`,
        method: 'GET',
        showMessage: false,
        onSuccess: function (response) {
            const ingredient = response.data;
            $('#editIngredientId').val(ingredient.ingredientId);
            $('#editIngredientName').val(ingredient.name);
            $('#editIngredientUnit').val(ingredient.unit);
            $('#editIngredientPrice').val(ingredient.price);
            $('#editIngredientSize').val(ingredient.size);
            
            if (ingredient.vendorId > 0) {
                $('#editIngredientName').prop('disabled', true);
                $('#editIngredientUnit').prop('disabled', true);
                $('#editIngredientSize').prop('disabled', true);
                $('#editIngredientPrice').closest('.form-group').hide();
            } else {
                $('#editIngredientName').prop('disabled', false);
                $('#editIngredientUnit').prop('disabled', false);
                $('#editIngredientPrice').closest('.form-group').show();
            }
            openModal('editIngredientModal');
        },
        onError: function () {
            showNotification('Failed to fetch ingredient.', 'error');
        }
    });
}

function updateIngredient(ingredientData) {
    ajaxRequest({
        url: '/api/IngredientApi/UpdateIngredient',
        method: 'PUT',
        data: {
            IngredientId:  ingredientData.ingredientId,
            Name:          ingredientData.name,
            Price:         ingredientData.price,
            Unit:          ingredientData.unit,
            Size:          ingredientData.size
        },
        onSuccess: function () {
            refreshIngredientsTable();
            closeModal('editIngredientModal');
            showNotification('Ingredient updated successfully.', 'success');
        }
        
    });
}

function toggleIngredientActive(id) {
    if (confirm('Are you sure you want to toggle the active status of this Ingredient?')) {
        ajaxRequest({
            url: `/api/IngredientApi/ToggleIngredientActive/${id}`,
            method: 'PUT',
            showMessage: false,
            onSuccess: function (response) {
                refreshIngredientsTable();
                showNotification(response.responseMsg, 'success');
            },
            onError: function () {
                console.error('Failed to toggle ingredient status.');
            }
        });
    }
}

function editIngredient(id) {
    getIngredientById(id);
}

// ── Add Stock ─────────────────────────────────────────────────────────────────
function addStock(ingredientId) {
    $('#addStockIngredientId').val(ingredientId);
    $('#addStock').val('');
    openModal('addStockModal');
}

$('#saveStockBtn').on('click', function () {
    const ingredientId = $('#addStockIngredientId').val();
    const stock        = parseFloat($('#addStock').val());
    if (!stock || stock <= 0) {
        showNotification('Please enter a valid stock.', 'error');
        return;
    }
    ajaxRequest({
        url: '/api/IngredientApi/AddIngredientStock',
        method: 'POST',
        data: { ingredientId: ingredientId, stock: stock },
        onSuccess: function () {
            closeModal('addStockModal');
            refreshIngredientsTable();
        },
        onError: function () {
            console.error('Failed to add stock.');
        }
    });
});

// ── Bulk delete ───────────────────────────────────────────────────────────────
function bulkDeleteIngredients() {
    const selectedIds = $('.ingredient-checkbox:checked').map(function () {
        return $(this).data('id');
    }).get();

    if (selectedIds.length === 0) {
        showNotification('Please select at least one ingredient to delete.', 'error');
        return;
    }
    if (confirm(`Are you sure you want to delete ${selectedIds.length} selected ingredient(s)?`)) {
        let completed = 0;
        let hasError  = false;
        selectedIds.forEach(id => {
            ajaxRequest({
                url: `/api/IngredientApi/DeleteIngredientById/${id}`,
                method: 'DELETE',
                showMessage: false,
                onSuccess: function () {
                    completed++;
                    if (completed === selectedIds.length) {
                        refreshIngredientsTable();
                        showNotification(`${selectedIds.length} ingredient(s) deleted successfully.`, 'success');
                    }
                },
                onError: function () {
                    completed++;
                    hasError = true;
                    if (completed === selectedIds.length) {
                        refreshIngredientsTable();
                        showNotification(hasError ? 'Some ingredients could not be deleted.' : `${selectedIds.length} ingredient(s) deleted successfully.`, hasError ? 'error' : 'success');
                    }
                }
            });
        });
    }
}

// ── Bulk toggle ───────────────────────────────────────────────────────────────
function bulkToggleIngredients() {
    const selectedIds = $('.ingredient-checkbox:checked').map(function () {
        return $(this).data('id');
    }).get();

    if (selectedIds.length === 0) {
        showNotification('Please select at least one ingredient to toggle.', 'error');
        return;
    }
    if (confirm(`Are you sure you want to toggle the active status of ${selectedIds.length} selected ingredient(s)?`)) {
        let completed = 0;
        let hasError  = false;
        selectedIds.forEach(id => {
            ajaxRequest({
                url: `/api/IngredientApi/ToggleIngredientActive/${id}`,
                method: 'PUT',
                showMessage: false,
                onSuccess: function () {
                    completed++;
                    if (completed === selectedIds.length) {
                        refreshIngredientsTable();
                        showNotification(`${selectedIds.length} ingredient(s) toggled successfully.`, 'success');
                    }
                },
                onError: function () {
                    completed++;
                    hasError = true;
                    if (completed === selectedIds.length) {
                        refreshIngredientsTable();
                        showNotification(hasError ? 'Some ingredients could not be toggled.' : `${selectedIds.length} ingredient(s) toggled successfully.`, hasError ? 'error' : 'success');
                    }
                }
            });
        });
    }
}

// ─── Vendor management ─────────────────────────────────────────────────────────

function refreshVendors() {
    ajaxRequest({
        url: '/api/IngredientApi/GetAllVendors',
        method: 'GET',
        showMessage: false,
        onSuccess: function (response) {
            const vendors         = response.data;
            const vendorGrid      = $('.vendor-grid');
            const linkedVendorsList = $('.linked-vendors-list');
            const addVendorEmpty   = $('.add-vendor-empty');
            const linkedVendorEmpty = $('.linked-vendor-empty');

            vendorGrid.empty();
            linkedVendorsList.empty();
            addVendorEmpty.hide();
            linkedVendorEmpty.hide();

            let hasUnlinked = false;
            let hasLinked   = false;

            vendors.forEach(vendor => {
                const name = vendor.restaurantName || vendor.firstName + ' ' + vendor.lastName;

                const vendorCard = `
                    <div class="vendor-card">
                        <div class="vendor-info">
                            <h4>${name}</h4>
                            <p class="vendor-contact"><i class="fas fa-phone"></i> ${vendor.phone || 'N/A'}</p>
                            <p class="vendor-email"><i class="fas fa-envelope"></i> ${vendor.email}</p>
                        </div>
                        <div class="vendor-actions">
                            <button class="btn btn-primary btn-sm link-vendor-btn"
                                    data-vendor="${name}"
                                    data-vendor-id="${vendor.userId}">
                                <i class="fas fa-link"></i> Link Vendor
                            </button>
                        </div>
                    </div>`;

                const linkedVendorItem = `
                    <div class="linked-vendor-item">
                        <div class="vendor-info">
                            <h4>${name}</h4>
                            <p class="vendor-contact"><i class="fas fa-phone"></i> ${vendor.phone || 'N/A'}</p>
                            <p class="vendor-email"><i class="fas fa-envelope"></i> ${vendor.email}</p>
                            <p class="link-date">Linked on: ${DateFormatter.formatWithComma(vendor.modifiedOn || vendor.createdOn)}</p>
                        </div>
                        <div class="vendor-actions">
                            <button class="btn btn-danger btn-sm unlink-vendor-btn"
                                    data-vendor="${name}"
                                    data-vendor-id="${vendor.userId}">
                                <i class="fas fa-unlink"></i> Unlink
                            </button>
                        </div>
                    </div>`;

                if (vendor.isLinked) {
                    hasLinked = true;
                    linkedVendorsList.append(linkedVendorItem);
                } else {
                    hasUnlinked = true;
                    vendorGrid.append(vendorCard);
                }
            });

            if (!hasUnlinked) addVendorEmpty.show();
            if (!hasLinked)   linkedVendorEmpty.show();
        },
        onError: function () {
            showNotification('Failed to fetch vendors.', 'error');
        }
    });
}

// Open vendor modal
$("#linkVendorBtn").on("click", () => {
    openModal("linkVendorModal");
    refreshVendors();
});

// Tab switching
$(".tab-btn").on("click", function () {
    const tabName = $(this).data("tab");
    $(".tab-btn").removeClass("active");
    $(this).addClass("active");
    $(".tab-content").removeClass("active");
    $(`#${tabName}Tab`).addClass("active");
});

// Link vendor
$(document).on("click", ".link-vendor-btn", function () {
    const vendorName = $(this).data("vendor");
    const vendorId   = $(this).data("vendor-id");
    $(this).addClass("loading").text("Linking...");

    ajaxRequest({
        url: '/api/IngredientApi/LinkVendor',
        method: 'POST',
        data: { vendorId: vendorId },
        onSuccess: function () {
            showNotification(`${vendorName} linked successfully!`, "success");
            refreshVendors();
            refreshIngredientsTable();
        },
        onError: function () {
            showNotification(`Failed to link ${vendorName}.`, 'error');
        }
    });
});

// Unlink vendor
$(document).on("click", ".unlink-vendor-btn", function () {
    const vendorName = $(this).data("vendor");
    const vendorId   = $(this).data("vendor-id");

    if (confirm(`Are you sure you want to unlink ${vendorName}?`)) {
        $(this).addClass("loading").text("Unlinking...");
        ajaxRequest({
            url: `/api/IngredientApi/UnlinkVendor/${vendorId}`,
            method: 'DELETE',
            showMessage: false,
            onSuccess: function () {
                showNotification(`${vendorName} unlinked successfully!`, "success");
                refreshVendors();
                refreshIngredientsTable();
            },
            onError: function () {
                showNotification(`Failed to unlink ${vendorName}.`, 'error');
            }
        });
    }
});
