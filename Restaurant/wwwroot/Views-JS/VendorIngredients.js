let table;

$(document).ready(function () {
    // Corrected the button ID to match the CSHTML file
    $('#addIngredientBtn').on('click', function () {
        openModal('addIngredientModal');
    });

    $('#addIngredientBtn2').on('click', function () {
        const ingredientData = {
            name: $('#ingredientName').val(),
            description: $('#ingredientDescription').val(),
            unit: $('#ingredientUnit').val(),
            price: parseFloat($('#ingredientPrice').val()) || 0
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
            name: $('#editIngredientName').val(),
            description: $('#editIngredientDescription').val(),
            unit: $('#editIngredientUnit').val(),
            price: parseFloat($('#editIngredientPrice').val()) || 0
        };

        if (ingredientData.name && ingredientData.unit) {
            updateIngredient(ingredientData);
        } else {
            showNotification('Please fill in all required fields.', 'error');
        }
    });

    // Bulk action buttons
    $('#bulkDeleteBtn').on('click', function () {
        bulkDeleteIngredients();
    });

    $('#bulkToggleBtn').on('click', function () {
        bulkToggleIngredients();
    });

    // Select all checkbox
    $('#selectAllIngredients').on('change', function () {
        $('.ingredient-checkbox').prop('checked', this.checked);
    });

    refreshIngredientsTable();
});

function addIngredientByUserId(ingredientData) {
    ajaxRequest({
        url: '/api/IngredientApi/CreateIngredient',
        method: 'POST',
        data: {
            Name: ingredientData.name,
            Description: ingredientData.description,
            Price: ingredientData.price,
            Unit: ingredientData.unit
        },
        onSuccess: function (response) {
            refreshIngredientsTable();
            $('#addIngredientForm')[0].reset();
            closeModal('addIngredientModal');
        },
        onError: function (xhr, status, error) {
            console.error('Failed to add ingredient:', error);
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
        onError: function (xhr, status, error) {
            console.error('Failed to fetch ingredient:', error);
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
            Name: ingredientData.name,
            Description: ingredientData.description,
            Price: ingredientData.price,
            Unit: ingredientData.unit
        },
        onSuccess: function (response) {
            refreshIngredientsTable();
            closeModal('editIngredientModal');
            showNotification('Ingredient updated successfully.', 'success');
        },
        onError: function (xhr, status, error) {
            console.error('Failed to update ingredient:', error);
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
            onError: function (xhr, status, error) {
                console.error('Failed to toggle ingredient status:', error);
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
            onSuccess: function (response) {
                refreshIngredientsTable();
                showNotification('Ingredient deleted successfully.', 'success');
            },
            onError: function (xhr, status, error) {
                console.error('Failed to delete ingredient:', error);
                showNotification('Failed to delete ingredient.', 'error');
            }
        });
    }
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
                    // Refresh table after the last request
                    if (id === selectedIds[selectedIds.length - 1]) {
                        refreshIngredientsTable();
                        showNotification(`${selectedIds.length} ingredient(s) deleted successfully.`, 'success');
                    }
                },
                onError: function (xhr, status, error) {
                    console.error(`Failed to delete ingredient ${id}:`, error);
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
                onSuccess: function (response) {
                    // Refresh table after the last request
                    if (id === selectedIds[selectedIds.length - 1]) {
                        refreshIngredientsTable();
                        showNotification(`${selectedIds.length} ingredient(s) toggled successfully.`, 'success');
                    }
                },
                onError: function (xhr, status, error) {
                    console.error(`Failed to toggle ingredient ${id}:`, error);
                }
            });
        });
    }
}

function editIngredient(id) {
    getIngredientById(id);
}

function refreshIngredientsTable() {
    debugger;
    ajaxRequest({
        url: '/api/IngredientApi/GetAllIngredientsByVendorId',
        method: 'GET',
        showMessage: false,
        onSuccess: function (response) {
            const ingredients = response.data;
            const tbody = $('.data-table tbody');
            tbody.empty();

            ingredients.forEach(ingredient => {
                const createdDate = DateFormatter.formatWithComma(ingredient.createdOn);
                const row = `
                    <tr>
                        <td><input type="checkbox" class="ingredient-checkbox" data-id="${ingredient.ingredientId}"></td>
                        <td>
                            <div class="ingredient-info">
                                <p class="ingredient-name">${ingredient.name}</p>
                                <p class="ingredient-meta">Added on: ${createdDate}</p>
                            </div>
                        </td>
                        <td><span class="category-badge">${ingredient.unit}</span></td>
                        <td><span class="status-badge ${ingredient.isActive ? "good" : "critical"}">${ingredient.isActive ? "Active" : "Inactive"}</span></td>
                        <td><i class="fas fa-calendar"></i> ${createdDate}</td>
                        <td>$${ingredient.price}</td>
                        <td>
                            <div class="dropdown">
                                <button class="dropdown-toggle"><i class="fas fa-ellipsis-h"></i></button>
                                <div class="dropdown-menu">
                                    <a href="#" onclick="editIngredient(${ingredient.ingredientId})"><i class="fas fa-edit"></i> Edit Details</a>
                                    <a href="#" onclick="toggleIngredientActive(${ingredient.ingredientId})"><i class="fas fa-ban"></i> Toggle</a>
                                    <a href="#" onclick="deleteIngredient(${ingredient.ingredientId})"><i class="fas fa-trash"></i> Delete</a>
                                </div>
                            </div>
                        </td>
                    </tr>
                `;
                tbody.append(row);
            });

            if (table && $.fn.DataTable.isDataTable('.ingredients-table')) {
                table.destroy();
            }
            initDataTable();
        },
        onError: function (xhr, status, error) {
            console.error('Failed to fetch ingredients:', error);
        }
    });
}

function initDataTable() {
    table = $('.ingredients-table').DataTable({
        paging: true,
        searching: true,
        ordering: true,
        order: [[1, 'asc']], // Sort by Ingredient name column
        pageLength: 10,
        lengthMenu: [10, 25, 50, 100],
        language: {
            search: '',
            searchPlaceholder: 'Search ingredients...'
        }
    });

    $('#ingredientSearch').on('keyup', function () {
        table.search(this.value).draw();
    });
}

// Utility functions
function openModal(modalId) {
    $(`#${modalId}`).addClass('active');
}

function closeModal(modalId) {
    $(`#${modalId}`).removeClass('active');
    // Also handle the specific close buttons inside modals
    $('.modal-close').on('click', function () {
        $(this).closest('.modal').removeClass('active');
    });
}