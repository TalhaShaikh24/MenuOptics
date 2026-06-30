$(document).ready(function () {
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

    refreshIngredientsTable();
    refreshVendors(); // Load vendors on page load
});

// Existing addIngredientByUserId function remains the same
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
            $('#ingredientForm')[0].reset();
            closeModal('addIngredientModal');
        },
        onError: function (xhr, status, error) {
            console.error('Failed to add ingredient:', error);
            showNotification('Failed to add ingredient.', 'error');
        }
    });
}

// Function to get ingredient by ID
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

// Function to update ingredient
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

// Function to delete ingredient
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

// Modified editIngredient function
function editIngredient(id) {
    getIngredientById(id);
}

// Modified refreshIngredientsTable
function refreshIngredientsTable() {
    ajaxRequest({
        url: '/api/IngredientApi/GetAllIngredientsByUserId',
        method: 'GET',
        data: null,
        showMessage: false,
        onSuccess: function (response) {
            const ingredients = response.data;
            const tbody = $('.data-table tbody');
            tbody.empty();

            ingredients.forEach(ingredient => {
                const minStock = 20;
                const createdDate = DateFormatter.formatWithComma(ingredient.createdOn);
                const supplier = ingredient.fullName != null ? ingredient.fullName : 'N/A';

                const row = `
                    <tr>
                        <td>
                            <div class="ingredient-info">
                                <p class="ingredient-name">${ingredient.name}</p>
                                <p class="ingredient-meta">Added on: ${createdDate}</p>
                            </div>
                        </td>
                        <td><span class="category-badge">${ingredient.unit}</span></td>
                        <td><span class="status-badge ${ingredient.isActive == 1 ? "good" : "critical"}">${ingredient.isActive == 1 ? "Active" : "In Active"}</span></td>
                        <td>${supplier}</td>
                        <td><i class="fas fa-calendar"></i> ${createdDate}</td>
                        <td>$${ingredient.price}</td>
                        <td>
                            <div class="dropdown">
                                <button class="dropdown-toggle">
                                    <i class="fas fa-ellipsis-h"></i>
                                </button>
                                <div class="dropdown-menu">
                                    <a href="#" onclick="editIngredient(${ingredient.ingredientId})"><i class="fas fa-edit"></i> Edit Details</a>
                                    <a href="#" onclick="deleteIngredient(${ingredient.ingredientId})"><i class="fas fa-trash"></i> Delete</a>
                                </div>
                            </div>
                        </td>
                    </tr>
                `;
                tbody.append(row);
            });
        },
        onError: function (xhr, status, error) {
            console.error('Failed to fetch ingredients:', error);
        }
    });
}

// New function to refresh vendors
function refreshVendors() {
    ajaxRequest({
        url: '/api/IngredientApi/GetAllVendors',
        method: 'GET',
        showMessage: false,
        onSuccess: function (response) {
            const vendors = response.data;
            const vendorGrid = $('.vendor-grid');
            const linkedVendorsList = $('.linked-vendors-list');

            // Empty state containers
            const addVendorEmpty = $('.add-vendor-empty');
            const linkedVendorEmpty = $('.linked-vendor-empty');

            // Clear previous content
            vendorGrid.empty();
            linkedVendorsList.empty();
            addVendorEmpty.hide();
            linkedVendorEmpty.hide();

            let hasUnlinked = false;
            let hasLinked = false;

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
                    </div>
                `;

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
                    </div>
                `;

                if (vendor.isLinked) {
                    hasLinked = true;
                    linkedVendorsList.append(linkedVendorItem);
                } else {
                    hasUnlinked = true;
                    vendorGrid.append(vendorCard);
                }
            });

            // Show empty states if needed
            if (!hasUnlinked) addVendorEmpty.show();
            if (!hasLinked) linkedVendorEmpty.show();
        },
        onError: function (xhr, status, error) {
            console.error('Failed to fetch vendors:', error);
            showNotification('Failed to fetch vendors.', 'error');
        }
    });
}


// Link Vendor Modal functionality
$("#linkVendorBtn").on("click", () => {
    openModal("linkVendorModal");
    refreshVendors();
});

// Tab switching functionality
$(".tab-btn").on("click", function () {
    const tabName = $(this).data("tab");
    $(".tab-btn").removeClass("active");
    $(this).addClass("active");
    $(".tab-content").removeClass("active");
    $(`#${tabName}Tab`).addClass("active");
});

// Link vendor functionality
$(document).on("click", ".link-vendor-btn", function () {
    const vendorName = $(this).data("vendor");
    const vendorId = $(this).data("vendor-id");

    $(this).addClass("loading").text("Linking...");

    ajaxRequest({
        url: '/api/IngredientApi/LinkVendor',
        method: 'POST',
        data: { UserId: vendorId },
        onSuccess: function (response) {
            $(this).removeClass("loading").html('<i class="fas fa-check"></i> Linked')
                .prop("disabled", true)
                .removeClass("btn-primary")
                .addClass("btn-success");
            showNotification(`${vendorName} linked successfully!`, "success");
            refreshVendors();
        },
        onError: function (xhr, status, error) {
            $(this).removeClass("loading").html('<i class="fas fa-link"></i> Link Vendor');
            console.error('Failed to link vendor:', error);
            showNotification(`Failed to link ${vendorName}.`, 'error');
        }
    });
});

// Unlink vendor functionality
$(document).on("click", ".unlink-vendor-btn", function () {
    const vendorName = $(this).data("vendor");
    const vendorId = $(this).data("vendor-id");

    if (confirm(`Are you sure you want to unlink ${vendorName}?`)) {
        $(this).addClass("loading").text("Unlinking...");

        ajaxRequest({
            url: `/api/IngredientApi/UnlinkVendor/${vendorId}`,
            method: 'DELETE',
            showMessage: false,
            onSuccess: function (response) {
                $(this).closest(".linked-vendor-item").fadeOut(300, function () {
                    $(this).remove();
                });
                showNotification(`${vendorName} unlinked successfully!`, "success");
                refreshVendors();
            },
            onError: function (xhr, status, error) {
                $(this).removeClass("loading").html('<i class="fas fa-unlink"></i> Unlink');
                console.error('Failed to unlink vendor:', error);
                showNotification(`Failed to unlink ${vendorName}.`, 'error');
            }
        });
    }
});

// Utility functions
function openModal(modalId) {
    $(`#${modalId}`).addClass('active');
}

function closeModal(modalId) {
    $(`#${modalId}`).removeClass('active');
}