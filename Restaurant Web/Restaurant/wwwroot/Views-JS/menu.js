$(document).ready(function () {
    // Initialize Select2 for ingredients and categories
    initializeIngredientsSelect('#ingredientsSelect', '#addMenuItemModal', '#ingredientsDetails', '#menuCost');
    initializeIngredientsSelect('#editIngredientsSelect', '#editMenuItemModal', '#editIngredientsDetails', '#editMenuCost');
    initializeCategorySelect('#menuCategory', '#addMenuItemModal');
    initializeCategorySelect('#editMenuCategory', '#editMenuItemModal');

    // Event handler for add menu item
    $('#saveMenuItemBtn').on('click', function () {
        const menuData = getMenuFormData('#addMenuItemForm', '#ingredientsDetails');
        if (validateMenuData(menuData)) {
            addMenuItem(menuData);
        } else {
            showNotification('Please fill in all required fields.', 'error');
        }
    });

    // Event handler for update menu item
    $('#updateMenuItemBtn').on('click', function () {
        const menuData = getMenuFormData('#editMenuItemForm', '#editIngredientsDetails');
        menuData.append('menuItemId', $('#editMenuItemId').val());
        if (validateMenuData(menuData)) {
            updateMenuItem(menuData);
        } else {
            showNotification('Please fill in all required fields.', 'error');
        }
    });

    // Category tabs filter
    refreshCategoryTabs();

    // Search filter
    $('#menuSearch').on('input', function () {
        const searchTerm = $(this).val().toLowerCase();
        $('.menu-item-card').each(function () {
            const name = $(this).find('.item-info h3').text().toLowerCase();
            $(this).toggle(name.includes(searchTerm));
        });
    });

    refreshMenuGrid();
});

// Store ingredients and categories data globally
let ingredientsWithPrices = [];
let categories = [];

// Initialize Select2 for ingredients
function initializeIngredientsSelect(selectId, modalId, detailsId, costInputId) {
    ajaxRequest({
        url: '/api/IngredientApi/GetAllIngredients',
        method: 'GET',
        showMessage: false,
        onSuccess: function (response) {
            ingredientsWithPrices = response.data.map(ing => ({
                id: ing.ingredientId,
                text: ing.name,
                price: ing.price,
                unit: ing.unit
            }));

            $(selectId).select2({
                tags: true,
                data: ingredientsWithPrices,
                placeholder: 'Select or add ingredients',
                width: '100%',
                dropdownParent: $(modalId),
                createTag: function (params) {
                    return {
                        id: params.term,
                        text: params.term,
                        newTag: true
                    };
                }
            });

            $(selectId).on('select2:select', function (e) {
                const data = e.params.data;
                if (data.newTag) {
                    const ingredientName = data.text;
                    const modalHtml = `
                        <div class="modal" id="ingredientPriceModal" style="display:flex;">
                            <div class="modal-content" style="max-width:400px;">
                                <div class="modal-header">
                                    <h3>Add New Ingredient</h3>
                                    <button class="modal-close" onclick="closePriceModal('${selectId}')">&times;</button>
                                </div>
                                <div class="modal-body">
                                    <p>Do you want to add "${ingredientName}" as a new ingredient?</p>
                                    <div class="form-group">
                                        <label>Price ($)</label>
                                        <input type="number" step="0.01" id="newIngredientPrice" placeholder="Enter price" required>
                                    </div>
                                    <div class="form-group">
                                        <label>Unit</label>
                                        <select id="newIngredientUnit" required>
                                            <option value="">Select Unit</option>
                                            <option value="g">g</option>
                                            <option value="kg">kg</option>
                                            <option value="ml">ml</option>
                                            <option value="l">l</option>
                                            <option value="pcs">pcs</option>
                                        </select>
                                    </div>
                                </div>
                                <div class="modal-footer">
                                    <button class="btn btn-outline" onclick="closePriceModal('${selectId}')">Cancel</button>
                                    <button class="btn btn-primary" onclick="addNewIngredient('${ingredientName}', '${selectId}', '${detailsId}', '${costInputId}')">Add Ingredient</button>
                                </div>
                            </div>
                        </div>
                    `;
                    $('body').append(modalHtml);
                }
            });

            $(selectId).on('change', function () {
                const selected = $(this).val() || [];
                renderIngredientDetails(selected, detailsId, costInputId);
            });

            $(detailsId).on('input', 'input[name^="ingredient_quantity_"]', function () {
                const ingredientId = $(this).attr('name').split('_')[2];
                const quantity = parseFloat($(this).val()) || 0;
                const ingredientData = ingredientsWithPrices.find(ing => ing.id == ingredientId);
                const price = ingredientData ? (ingredientData.price * quantity).toFixed(2) : '0.00';
                $(this).siblings('.ingredient-price').text(`Price: $${price}`);
                updateTotalCost(detailsId, costInputId);
            });
        },
        onError: function (xhr, status, error) {
            console.error('Failed to fetch ingredients:', error);
        }
    });
}

// Initialize Select2 for categories
function initializeCategorySelect(selectId, modalId) {
    ajaxRequest({
        url: '/api/CategoryApi/GetAllCategories',
        method: 'GET',
        showMessage: false,
        onSuccess: function (response) {
            categories = response.data.map(cat => ({
                id: cat.categoryId,
                text: cat.name
            }));

            $(selectId).select2({
                tags: true,
                data: categories,
                placeholder: 'Select or add category',
                width: '100%',
                dropdownParent: $(modalId),
                createTag: function (params) {
                    return {
                        id: params.term,
                        text: params.term,
                        newTag: true
                    };
                }
            });

            $(selectId).on('select2:select', function (e) {
                const data = e.params.data;
                if (data.newTag) {
                    const categoryName = data.text;
                    const modalHtml = `
                        <div class="modal" id="categoryModal" style="display:flex;">
                            <div class="modal-content" style="max-width:400px;">
                                <div class="modal-header">
                                    <h3>Add New Category</h3>
                                    <button class="modal-close" onclick="closeCategoryModal('${selectId}')">&times;</button>
                                </div>
                                <div class="modal-body">
                                    <p>Do you want to add "${categoryName}" as a new category?</p>
                                </div>
                                <div class="modal-footer">
                                    <button class="btn btn-outline" onclick="closeCategoryModal('${selectId}')">Cancel</button>
                                    <button class="btn btn-primary" onclick="addNewCategory('${categoryName}', '${selectId}')">Add Category</button>
                                </div>
                            </div>
                        </div>
                    `;
                    $('body').append(modalHtml);
                }
            });
        },
        onError: function (xhr, status, error) {
            console.error('Failed to fetch categories:', error);
        }
    });
}

// Render ingredient details
function renderIngredientDetails(selected, detailsId, costInputId) {
    let html = '';
    selected.forEach(function (ingredientId) {
        const ingredientData = ingredientsWithPrices.find(ing => ing.id == ingredientId);
        const price = ingredientData ? ingredientData.price : 'Not set';
        const unit = ingredientData ? ingredientData.unit : 'N/A';
        html += `
            <div class="ingredient-detail-row" style="display:flex;align-items:center;gap:8px;margin-bottom:8px;">
                <span class="ing_unit" style="min-width:80px;">${ingredientData ? ingredientData.text : ingredientId}</span>
                <span class="ing_unit" style="min-width:60px;">${unit}</span>
                <input type="number" name="ingredient_quantity_${ingredientId}" placeholder="Enter Wt / Qty" style="width:200px;" min="0" step="0.01">
                <span class="ingredient-price" style="min-width:80px;">Price: $${price}</span>
            </div>
        `;
    });
    $(detailsId).html(html);
    updateTotalCost(detailsId, costInputId);
}

// Update total cost
function updateTotalCost(detailsId, costInputId) {
    let totalCost = 0;
    $(`${detailsId} .ingredient-detail-row`).each(function () {
        const ingredientId = $(this).find('input').attr('name').split('_')[2];
        const quantity = parseFloat($(this).find('input').val()) || 0;
        const ingredientData = ingredientsWithPrices.find(ing => ing.id == ingredientId);
        if (ingredientData && quantity > 0) {
            totalCost += ingredientData.price * quantity;
        }
    });
    $(costInputId).val(totalCost.toFixed(2));
}

// Close price modal
window.closePriceModal = function (selectId) {
    const $select = $(selectId);
    const selected = $select.val() || [];
    const newSelected = selected.filter(item => ingredientsWithPrices.some(ing => ing.id == item));
    $select.val(newSelected).trigger('change');
    $('#ingredientPriceModal').remove();
};

// Close category modal
window.closeCategoryModal = function (selectId) {
    selectId = selectId.replace('#', '');

    closeModal(selectId);
    const $select = $(selectId);
    const selected = $select.val() || [];
    const newSelected = selected.filter(item => categories.some(cat => cat.id == item));
    $select.val(newSelected).trigger('change');
    $('#categoryModal').remove();

    
};

// Add new ingredient
window.addNewIngredient = function (ingredientName, selectId, detailsId, costInputId) {
    const price = parseFloat($('#newIngredientPrice').val());
    const unit = $('#newIngredientUnit').val();
    if (isNaN(price) || price < 0) {
        showNotification('Please enter a valid price.', 'error');
        return;
    }
    if (!unit) {
        showNotification('Please select a unit.', 'error');
        return;
    }

    const $select = $(selectId);
    const selected = $select.val() || [];
    const filteredSelected = selected.filter(item => item !== ingredientName);

    ajaxRequest({
        url: '/api/IngredientApi/CreateIngredient',
        method: 'POST',
        data: {
            name: ingredientName,
            price: price,
            unit: unit,
            stock: 0,
            createdBy: 1 // Replace with actual user ID
        },
        onSuccess: function (response) {
            const newIngredient = {
                id: response.data.ingredientId,
                text: ingredientName,
                price: price,
                unit: unit
            };
            ingredientsWithPrices.push(newIngredient);

            $select.select2({
                tags: true,
                data: ingredientsWithPrices,
                placeholder: 'Select or add ingredients',
                width: '100%',
                dropdownParent: $(selectId === '#ingredientsSelect' ? '#addMenuItemModal' : '#editMenuItemModal'),
                createTag: function (params) {
                    return {
                        id: params.term,
                        text: params.term,
                        newTag: true
                    };
                }
            });

            filteredSelected.push(newIngredient.id);
            $select.val(filteredSelected).trigger('change');

            $('#ingredientPriceModal').remove();
            showNotification('Ingredient added successfully!', 'success');
        },
        onError: function (xhr, status, error) {
            console.error('Failed to add ingredient:', error);
            showNotification('Failed to add ingredient.', 'error');
            closePriceModal(selectId);
        }
    });
};

// Add new category
window.addNewCategory = function (categoryName, selectId) {
    const $select = $(selectId);
    // Ensure selected is always an array
    const selected = $select.val() || [];
    const selectedArray = Array.isArray(selected) ? selected : [selected];
    const filteredSelected = selectedArray.filter(item => item !== categoryName);

    ajaxRequest({
        url: '/api/CategoryApi/CreateCategory',
        method: 'POST',
        data: { name: categoryName },
        onSuccess: function (response) {
            const newCategory = {
                id: response.data.categoryId,
                text: categoryName
            };
            categories.push(newCategory);

            $select.select2({
                tags: true,
                data: categories,
                placeholder: 'Select or add category',
                width: '100%',
                dropdownParent: $(selectId === '#menuCategory' ? '#addMenuItemModal' : '#editMenuItemModal'),
                createTag: function (params) {
                    return {
                        id: params.term,
                        text: params.term,
                        newTag: true
                    };
                }
            });

            filteredSelected.push(newCategory.id);
            $select.val(filteredSelected).trigger('change');

            $('#categoryModal').remove();
            refreshCategoryTabs();
        },
        onError: function (xhr, status, error) {
            console.error('Failed to add category:', error);
            closeCategoryModal(selectId);
        }
    });
};

// Get form data
function getMenuFormData(formId, detailsId) {
    const formData = new FormData($(formId)[0]);
    const ingredients = [];
    $(`${detailsId} .ingredient-detail-row`).each(function () {
        const ingredientId = $(this).find('input').attr('name').split('_')[2];
        const quantity = parseFloat($(this).find('input').val()) || 0;
        if (quantity > 0) {
            ingredients.push({
                ingredientId: parseInt(ingredientId),
                quantity: quantity
            });
        }
    });

    formData.set('ingredients', JSON.stringify(ingredients));
    return formData;
}

// Validate menu data
function validateMenuData(formData) {
    const name = formData.get('name');
    const description = formData.get('description');
    const categoryId = formData.get('categoryId');
    const cost = parseFloat(formData.get('cost')) || 0;
    const sellingPrice = parseFloat(formData.get('sellingPrice')) || 0;
    const ingredients = JSON.parse(formData.get('ingredients') || '[]');
    return name && description && categoryId && cost > 0 && sellingPrice > 0 && ingredients.length > 0;
}

// Add menu item
function addMenuItem(formData) {
    ajaxRequest({
        url: '/api/MenuApi/CreateMenuItem',
        method: 'POST',
        data: formData,
        contentType: false,
        processData: false,
        onSuccess: function (response) {
            refreshMenuGrid();
            $('#addMenuItemForm')[0].reset();
            $('#ingredientsSelect').val(null).trigger('change');
            $('#menuCategory').val(null).trigger('change');
            closeModal('addMenuItemModal');
        },
        onError: function (xhr, status, error) {
            console.error('Failed to add menu item:', error);
        }
    });
}

// Get menu item by ID
function getMenuItemById(id) {
    ajaxRequest({
        url: `/api/MenuApi/GetMenuItem/${id}`,
        method: 'GET',
        showMessage: false,
        onSuccess: function (response) {
            const menuItem = response.data;
            $('#editMenuItemId').val(menuItem.menuItemId);
            $('#editMenuName').val(menuItem.name);
            $('#editMenuDescription').val(menuItem.description);
            $('#editMenuCategory').val(menuItem.categoryId).trigger('change');
            $('#editMenuCost').val(menuItem.cost);
            $('#editMenuSellingPrice').val(menuItem.sellingPrice);
            $('#editFeaturedImage').val(menuItem.featuredImage);
            if (menuItem.featuredImage) {
                $('#editFeaturedImagePreview').attr('src', menuItem.featuredImage).show();
                $('#editFeaturedImageLabel').text('Replace featured image');
            } else {
                $('#editFeaturedImagePreview').hide();
                $('#editFeaturedImageLabel').text('Choose new featured image');
            }
            $('#editIngredientsSelect').val(menuItem.ingredients.map(ing => ing.ingredientId)).trigger('change');
            menuItem.ingredients.forEach(ing => {
                $(`input[name="ingredient_quantity_${ing.ingredientId}"]`).val(ing.quantity);
            });
            openModal('editMenuItemModal');
        },
        onError: function (xhr, status, error) {
            console.error('Failed to fetch menu item:', error);
        }
    });
}

// Update menu item
function updateMenuItem(formData) {
    ajaxRequest({
        url: '/api/MenuApi/UpdateMenuItem',
        method: 'PUT',
        data: formData,
        contentType: false,
        processData: false,
        onSuccess: function (response) {
            refreshMenuGrid();
            closeModal('editMenuItemModal');
        },
        onError: function (xhr, status, error) {
            console.error('Failed to update menu item:', error);
        }
    });
}

// Delete menu item
function deleteMenuItem(id) {
    ajaxRequest({
        url: `/api/MenuApi/DeleteMenuItem/${id}`,
        method: 'DELETE',
        showMessage: false,
        onSuccess: function (response) {
            refreshMenuGrid();
            showNotification('Menu item deleted successfully!', 'success');
        },
        onError: function (xhr, status, error) {
            console.error('Failed to delete menu item:', error);
        }
    });
}

// Refresh category tabs
function refreshCategoryTabs() {
    ajaxRequest({
        url: '/api/CategoryApi/GetAllCategories',
        method: 'GET',
        showMessage: false,
        onSuccess: function (response) {
            const tabContainer = $('#categoryTabs');
            tabContainer.empty();
            tabContainer.append('<button class="tab-btn active" data-category="all">All</button>');
            response.data.forEach(cat => {
                tabContainer.append(`<button class="tab-btn" data-category="${cat.categoryId}">${cat.name}</button>`);
            });

            $('.tab-btn').on('click', function () {
                $('.tab-btn').removeClass('active');
                $(this).addClass('active');
                const categoryId = $(this).data('category');
                filterMenuItems(categoryId);
            });
        },
        onError: function (xhr, status, error) {
            console.error('Failed to fetch categories:', error);
        }
    });
}

// Refresh menu grid
function refreshMenuGrid() {
    ajaxRequest({
        url: '/api/MenuApi/GetAllMenuItems',
        method: 'GET',
        showMessage: false,
        onSuccess: function (response) {
            const menuItems = response.data;
            const grid = $('#menuGrid');
            grid.empty();

            if (menuItems.length > 0) {
                menuItems.forEach(item => {
                    const availability = item.isAvailable ? 'available' : 'out-of-stock';
                    const row = `
                    <div class="menu-item-card" data-category="${item.categoryId}">
                        <div class="menu-item-image">
                            <img src="${item.featuredImage || 'https://via.placeholder.com/300x200'}" alt="${item.name}">
                            <div class="item-badges">
                                <span class="availability-badge ${availability}">${item.isAvailable ? 'Available' : 'Out of Stock'}</span>
                            </div>
                        </div>
                        <div class="menu-item-content">
                            <div class="item-header">
                                <div class="item-info">
                                    <h3>${item.name}</h3>
                                    <p class="item-description">${item.description}</p>
                                    <div class="item-meta">
                                        <span class="category-tag">${item.categoryName}</span>
                                        <span class="prep-time"><i class="fas fa-clock"></i> 15 min</span>
                                    </div>
                                </div>
                                <div class="dropdown">
                                    <button class="dropdown-toggle">
                                        <i class="fas fa-ellipsis-h"></i>
                                    </button>
                                    <div class="dropdown-menu">
                                        <a href="#" onclick="editMenuItem(${item.menuItemId})"><i class="fas fa-edit"></i> Edit Item</a>
                                        <a href="#" onclick="deleteMenuItem(${item.menuItemId})"><i class="fas fa-trash"></i> Delete</a>
                                    </div>
                                </div>
                            </div>
                            <div class="item-details">
                                <div class="pricing">
                                    <span class="price">$${item.sellingPrice.toFixed(2)}</span>
                                    <span class="cost">Cost: $${item.cost.toFixed(2)}</span>
                                </div>
                            </div>
                            <div class="item-stats">
                                <div class="stat">
                                    <i class="fas fa-chart-line"></i>
                                    <span>0 orders today</span>
                                </div>
                                <div class="stat">
                                    <i class="fas fa-star"></i>
                                    <span>0 rating</span>
                                </div>
                            </div>
                        </div>
                    </div>
                `;
                    grid.append(row);
                });
            }
            else {
                grid.append(`<h2>No Menu Items Added</h2>`);
            }
        },
        onError: function (xhr, status, error) {
            console.error('Failed to fetch menu items:', error);
        }
    });
}

// Filter menu items by category
function filterMenuItems(categoryId) {
    $('.menu-item-card').each(function () {
        const itemCategory = $(this).data('category');
        $(this).toggle(categoryId === 'all' || itemCategory == categoryId);
    });
}

// Edit menu item
function editMenuItem(id) {
    getMenuItemById(id);
}

// Utility functions
function openModal(modalId) {
    $(`#${modalId}`).addClass('active');
}

function closeModal(modalId) {
    $(`#${modalId}`).removeClass('active');
}