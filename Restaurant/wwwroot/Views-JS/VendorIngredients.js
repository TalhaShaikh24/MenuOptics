let table;

$(document).ready(function () {

    $('#addIngredientBtn').on('click', function () {
        openModal('addIngredientModal');
    });

    $('#addIngredientBtn2').on('click', function () {
        const ingredientData = {
            name:        $('#ingredientName').val(),
            description: $('#ingredientDescription').val(),
            unit:        $('#ingredientUnit').val(),
            price:       parseFloat($('#ingredientPrice').val()) || 0
        };
        if (ingredientData.name && ingredientData.unit) {
            addIngredientByUserId(ingredientData);
        } else {
            showNotification('Please fill in all required fields.', 'error');
        }
    });

    $('#updateIngredientBtn').on('click', function () {
        const ingredientData = {
            ingredientId: $('#editIngredientId').val(),
            name:         $('#editIngredientName').val(),
            description:  $('#editIngredientDescription').val(),
            unit:         $('#editIngredientUnit').val(),
            price:        parseFloat($('#editIngredientPrice').val()) || 0
        };
        if (ingredientData.name && ingredientData.unit) {
            updateIngredient(ingredientData);
        } else {
            showNotification('Please fill in all required fields.', 'error');
        }
    });

    // Bulk actions
    $('#bulkDeleteBtn').on('click', function () { bulkDeleteIngredients(); });
    $('#bulkToggleBtn').on('click',  function () { bulkToggleIngredients(); });

    // Select all checkbox
    $('#selectAllIngredients').on('change', function () {
        $('.ingredient-checkbox').prop('checked', this.checked);
    });

    initIngredientsTable();
});

// ─── Server-side DataTable ─────────────────────────────────────────────────────
function initIngredientsTable() {
    if ($.fn.DataTable.isDataTable('.ingredients-table')) {
        $('.ingredients-table').DataTable().destroy();
    }

    table = $('.ingredients-table').DataTable({
        serverSide: true,
        processing: true,
        ajax: function (dtParams, callback) {
            const searchValue = dtParams.search ? dtParams.search.value : '';
            const orderCol    = dtParams.order && dtParams.order.length ? dtParams.order[0].column : 1;
            const orderDir    = dtParams.order && dtParams.order.length ? dtParams.order[0].dir    : 'asc';

            $.ajax({
                url: '/api/IngredientApi/GetAllIngredientsByVendorId',
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
                    callback({
                        draw:            response.draw,
                        recordsTotal:    response.recordsTotal,
                        recordsFiltered: response.recordsFiltered,
                        data:            response.data || []
                    });
                },
                error: function () {
                    showNotification('Failed to load ingredients.', 'error');
                    callback({ draw: dtParams.draw, recordsTotal: 0, recordsFiltered: 0, data: [] });
                }
            });
        },
        columns: [
            {
                orderable: false,
                searchable: false,
                data: 'ingredientId',
                render: id => `<input type="checkbox" class="ingredient-checkbox" data-id="${id}">`
            },
            {
                data: 'name',
                render: function (name, type, row) {
                    const date = DateFormatter.formatWithComma(row.createdOn);
                    return `<div class="ingredient-info">
                                <p class="ingredient-name">${name}</p>
                                <p class="ingredient-meta">Added on: ${date}</p>
                            </div>`;
                }
            },
            {
                data: 'unit',
                render: val => `<span class="category-badge">${val || ''}</span>`
            },
            {
                data: 'isActive',
                render: function (active) {
                    const cls = active ? 'good' : 'critical';
                    const lbl = active ? 'Active' : 'Inactive';
                    return `<span class="status-badge ${cls}">${lbl}</span>`;
                }
            },
            {
                data: 'createdOn',
                render: val => `<i class="fas fa-calendar"></i> ${DateFormatter.formatWithComma(val)}`
            },
            {
                data: 'price',
                render: val => `$${val}`
            },
            {
                orderable: false,
                searchable: false,
                data: null,
                render: function (data, type, row) {
                    return `<div class="dropdown">
                                <button class="dropdown-toggle"><i class="fas fa-ellipsis-h"></i></button>
                                <div class="dropdown-menu">
                                    <a href="#" onclick="editIngredient(${row.ingredientId})">
                                        <i class="fas fa-edit"></i> Edit Details
                                    </a>
                                    <a href="#" onclick="toggleIngredientActive(${row.ingredientId})">
                                        <i class="fas fa-ban"></i> Toggle
                                    </a>
                                    <a href="#" onclick="deleteIngredient(${row.ingredientId})">
                                        <i class="fas fa-trash"></i> Delete
                                    </a>
                                </div>
                            </div>`;
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
            $('#selectAllIngredients').prop('checked', false);
            $('#selectAllIngredients').off('change').on('change', function () {
                $('.ingredient-checkbox').prop('checked', this.checked);
            });
        }
    });

    $('#ingredientSearch').off('keyup').on('keyup', function () {
        table.search(this.value).draw();
    });
}

function refreshIngredientsTable() {
    if (table) {
        table.ajax.reload(null, false);
    } else {
        initIngredientsTable();
    }
}

// ─── CRUD Functions ────────────────────────────────────────────────────────────

function addIngredientByUserId(ingredientData) {
    ajaxRequest({
        url: '/api/IngredientApi/CreateIngredient',
        method: 'POST',
        data: {
            Name:        ingredientData.name,
            Description: ingredientData.description,
            Price:       ingredientData.price,
            Unit:        ingredientData.unit
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
    ajaxRequest({
        url: `/api/IngredientApi/GetIngredient/${id}`,
        method: 'GET',
        showMessage: false,
        onSuccess: function (response) {
            const ingredient = response.data;
            $('#editIngredientId').val(ingredient.ingredientId);
            $('#editIngredientName').val(ingredient.name);
            $('#editIngredientDescription').val(ingredient.description);
            $('#editIngredientUnit').val(ingredient.unit);
            $('#editIngredientPrice').val(ingredient.price);
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
            IngredientId: ingredientData.ingredientId,
            Name:         ingredientData.name,
            Description:  ingredientData.description,
            Price:        ingredientData.price,
            Unit:         ingredientData.unit
        },
        onSuccess: function () {
            refreshIngredientsTable();
            closeModal('editIngredientModal');
            showNotification('Ingredient updated successfully.', 'success');
        },
        onError: function () {
            showNotification('Failed to update ingredient.', 'error');
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
                showNotification('Failed to toggle ingredient status.', 'error');
            }
        });
    }
}

function deleteIngredient(id) {
    if (confirm('Are you sure you want to delete this ingredient?')) {
        ajaxRequest({
            url: `/api/IngredientApi/DeleteIngredientById/${id}`,
            method: 'DELETE',
            showMessage: false,
            onSuccess: function () {
                refreshIngredientsTable();
                showNotification('Ingredient deleted successfully.', 'success');
            },
            onError: function () {
                showNotification('Failed to delete ingredient.', 'error');
            }
        });
    }
}

function editIngredient(id) {
    getIngredientById(id);
}

function bulkDeleteIngredients() {
    const selectedIds = $('.ingredient-checkbox:checked').map(function () {
        return $(this).data('id');
    }).get();

    if (selectedIds.length === 0) {
        showNotification('Please select at least one ingredient to delete.', 'error');
        return;
    }
    if (confirm(`Are you sure you want to delete ${selectedIds.length} selected ingredient(s)?`)) {
        selectedIds.forEach(id => {
            ajaxRequest({
                url: `/api/IngredientApi/DeleteIngredientById/${id}`,
                method: 'DELETE',
                showMessage: false,
                onSuccess: function () {
                    if (id === selectedIds[selectedIds.length - 1]) {
                        refreshIngredientsTable();
                        showNotification(`${selectedIds.length} ingredient(s) deleted successfully.`, 'success');
                    }
                },
                onError: function () {
                    console.error(`Failed to delete ingredient ${id}.`);
                }
            });
        });
    }
}

function bulkToggleIngredients() {
    const selectedIds = $('.ingredient-checkbox:checked').map(function () {
        return $(this).data('id');
    }).get();

    if (selectedIds.length === 0) {
        showNotification('Please select at least one ingredient to toggle.', 'error');
        return;
    }
    if (confirm(`Are you sure you want to toggle the active status of ${selectedIds.length} selected ingredient(s)?`)) {
        selectedIds.forEach(id => {
            ajaxRequest({
                url: `/api/IngredientApi/ToggleIngredientActive/${id}`,
                method: 'PUT',
                showMessage: false,
                onSuccess: function () {
                    if (id === selectedIds[selectedIds.length - 1]) {
                        refreshIngredientsTable();
                        showNotification(`${selectedIds.length} ingredient(s) toggled successfully.`, 'success');
                    }
                },
                onError: function () {
                    console.error(`Failed to toggle ingredient ${id}.`);
                }
            });
        });
    }
}

// ─── Utility ───────────────────────────────────────────────────────────────────
function openModal(modalId) {
    $(`#${modalId}`).addClass('active');
}

function closeModal(modalId) {
    $(`#${modalId}`).removeClass('active');
    $('.modal-close').on('click', function () {
        $(this).closest('.modal').removeClass('active');
    });
}