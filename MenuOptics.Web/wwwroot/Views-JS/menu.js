/* ============================================================================
 * menu.js — Menu management page (Views/Menu/Index.cshtml)
 * Menu grid with ingredient/recipe cost building, categories and PDF export.
 *
 * Project convention — every Views-JS file follows this section order:
 *   1. Constants  2. State  3. Initialization  4. Event bindings
 *   5. AJAX / CRUD  6. Helpers  7. Utilities
 * Functions stay global: they are referenced by inline onclick handlers.
 * ==========================================================================*/

/* ── 3. Initialization / 4. Event bindings ────────────────────────────── */

$(document).ready(function () {

    // Initialize Select2 for ingredients, recipes and categories
    initializeIngredientsSelect('#ingredientsSelect', '#addMenuItemModal', '#ingredientsDetails', '#menuCost');
    initializeIngredientsSelect('#editIngredientsSelect', '#editMenuItemModal', '#editIngredientsDetails', '#editMenuCost');
    initializeRecipesSelect('#recipesSelect', '#addMenuItemModal', '#recipesDetails', '#menuCost');
    initializeRecipesSelect('#editRecipesSelect', '#editMenuItemModal', '#editRecipesDetails', '#editMenuCost');
    initializeCategorySelect('#menuCategory', '#addMenuItemModal');
    initializeCategorySelect('#editMenuCategory', '#editMenuItemModal');

    // Event handler for add menu item
    $('#saveMenuItemBtn').on('click', function () {
        const menuData = getMenuFormData('#addMenuItemForm', '#ingredientsDetails', '#recipesDetails');
        if (validateMenuData(menuData)) {
            addMenuItem(menuData);
        } else {
            showNotification('Please fill in all required fields.', 'error');
        }
    });

    // Event handler for update menu item
    $('#updateMenuItemBtn').on('click', function () {
        const menuData = getMenuFormData('#editMenuItemForm', '#editIngredientsDetails', '#editRecipesDetails');
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
        const searchWords = searchTerm.split(/\s+/).filter(word => word !== '');

        $('.menu-item-card').each(function () {
            const name = $(this).find('.item-info h3').text().toLowerCase();
            // Every search word must exist in the item name 
            const matches = searchWords.every(word => name.includes(word));
            $(this).toggle(matches);
        });
    });

    refreshMenuGrid();
});

/* ── 2. State ─────────────────────────────────────────────────────────── */

// Store ingredients, recipes and categories data globally
let ingredientsWithPrices = [];
let recipesWithPrices = [];
let allRecipesData = [];
let categories = [];
let cachedMenuItems = [];


let isStaffFacing = false;


/* ── 7. Utilities ─────────────────────────────────────────────────────── */

async function getImageAsPngBase64(url) {
    return new Promise((resolve, reject) => {
        const img = new Image();
        img.crossOrigin = "anonymous";

        img.onload = function () {
            const targetWidth = 350;
            const aspectRatio = img.naturalHeight / img.naturalWidth;
            const targetHeight = Math.round(targetWidth * aspectRatio);

            const canvas = document.createElement('canvas');
            canvas.width = targetWidth;
            canvas.height = targetHeight;

            const ctx = canvas.getContext('2d');
            ctx.drawImage(img, 0, 0, targetWidth, targetHeight);

            const pngBase64 = canvas.toDataURL('image/png');
            resolve(pngBase64);
        };

        img.onerror = () => reject("Image load failed");
        img.src = url;
    });
}


// Initialize Select2 for ingredients
/* ── 6. Helpers — Select2 setup / cost calculations ───────────────────── */

function initializeIngredientsSelect(selectId, modalId, detailsId, costInputId) {
    ajaxRequest({
        url: '/api/IngredientApi/GetAllActiveIngredientsByUserId',
        method: 'GET',
        showMessage: false,
        isQuery: false,
        onSuccess: function (response) {
            debugger;
            ingredientsWithPrices = response.data.map(ing => ({
                id: ing.ingredientId,
                text: ing.name,
                price: ing.price,
                unit: ing.unit,
                size: ing.size,
                packSize: ing.packSize,
                unitOfMeasure: ing.unitOfMeasure || ing.unit,
                vendorProductId: ing.vendorProductId
            }));

            $(selectId).select2({
                data: ingredientsWithPrices,
                placeholder: 'Select ingredients',
                width: '100%',
                dropdownParent: $(modalId),
                matcher: function (param, data) {
                    var searchText = ($.trim(param.term) || '').toLowerCase();

                    // No search term typed yet -> show everything
                    if (searchText === '') {
                        return data;
                    }

                    var text = (data.text || '').toLowerCase();
                    var vendorProductId = (data.vendorProductId != null ? String(data.vendorProductId) : '').toLowerCase();

                    var searchWords = searchText.split(/\s+/);
                    var nameMatches = searchWords.every(function (word) {
                        return text.includes(word);
                    });

                    // VendorProductId: search using the ENTIRE search text 
                    var vendorProductIdMatches = vendorProductId.includes(searchText);
                    if (nameMatches || vendorProductIdMatches) {
                        return data;
                    }

                    return null;
                }
            });

            $(selectId).on('change', function () {
                const selected = $(this).val() || [];
                renderIngredientDetails(selected, detailsId, costInputId);
            });

            $(detailsId).on('input', 'input[name^="ingredient_quantity_"]', function () {
                updateIngredientPrice($(this).closest('.ingredient-detail-row'), detailsId, costInputId);
            });

            $(detailsId).on('change', 'select[name^="ingredient_uom_"]', function () {
                updateIngredientPrice($(this).closest('.ingredient-detail-row'), detailsId, costInputId);
            });
        },
        onError: function (xhr, status, error) {
            console.error('Failed to fetch ingredients:', error);
        }
    });
}

// Initialize Select2 for recipes
function initializeRecipesSelect(selectId, modalId, detailsId, costInputId) {
    ajaxRequest({
        url: '/api/RecipeApi/GetAllRecipes',
        method: 'GET',
        showMessage: false,
        isQuery: false,
        onSuccess: function (response) {
            allRecipesData = response.data || [];
            recipesWithPrices = response.data.map(recipe => ({
                id: recipe.recipeId,
                text: recipe.name,
                cost: recipe.cost,
                unit: recipe.unit
            }));

            $(selectId).select2({
                data: recipesWithPrices,
                placeholder: 'Select recipes',
                width: '100%',
                dropdownParent: $(modalId)
            });

            $(selectId).on('change', function () {
                const selected = $(this).val() || [];
                renderRecipeDetails(selected, detailsId, costInputId);
            });

            $(detailsId).on('input', 'input[name^="recipe_quantity_"]', function () {
                updateRecipePrice($(this), detailsId, costInputId);
            });
        },
        onError: function (xhr, status, error) {
            console.error('Failed to fetch recipes:', error);
        }
    });
}

// Build <option> HTML for the ingredient-detail-row "Unit of Measure" dropdown.
// Restricted to the ALLOWED_UNIT_OF_MEASURE_LABELS set AND to units convertible
// with the ingredient's base unit (e.g. an ingredient stocked in KG offers the
// allowed weight/volume units GM/KG/LB/OZ/ML/LT/FL/GA, but not length or count).
function buildUomOptionsHtml(baseUnit, selectedUnit) {
    const placeholder = `<option value="" disabled${selectedUnit ? '' : ' selected'}>Select unit of measure</option>`;

    if (!baseUnit) {
        return placeholder;
    }

    const compatibleUnits = (typeof getAllowedUomForUnit === 'function')
        ? getAllowedUomForUnit(baseUnit)
        : [baseUnit];

    const normalize = (typeof normalizeUnit === 'function') ? normalizeUnit : (u => u);
    const currentSelection = normalize(selectedUnit || baseUnit);

    let optionsHtml = placeholder;
    compatibleUnits.forEach(function (unitCode) {
        const label = (typeof ALLOWED_UNIT_OF_MEASURE_LABELS !== 'undefined' && ALLOWED_UNIT_OF_MEASURE_LABELS[unitCode])
            ? ALLOWED_UNIT_OF_MEASURE_LABELS[unitCode]
            : ((typeof NEW_UNIT_LABELS !== 'undefined' && NEW_UNIT_LABELS[unitCode]) ? NEW_UNIT_LABELS[unitCode] : unitCode);
        const isSelected = normalize(unitCode) === currentSelection ? ' selected' : '';
        optionsHtml += `<option value="${unitCode}"${isSelected}>${label}</option>`;
    });

    return optionsHtml;
}

// Update ingredient price with unit conversion, using the UOM the user picked in this row
function updateIngredientPrice(row, detailsId, costInputId) {
    const quantityInput = row.find('input[name^="ingredient_quantity_"]');
    const uomSelect = row.find('select[name^="ingredient_uom_"]');
    const ingredientId = quantityInput.attr('name').split('_')[2];
    const quantity = parseFloat(quantityInput.val()) || 0;

    const ingredientData = ingredientsWithPrices.find(ing => ing.id == ingredientId);


    if (ingredientData && quantity > 0) {
        const userUnit = uomSelect.val() || ingredientData.unitOfMeasure || ingredientData.unit;
        const pricePerUnit = ingredientData.price / (ingredientData.packSize != null ? ingredientData.size : 1);

        const convertedQuantity = convertUnit(quantity, userUnit, ingredientData.unit, ingredientData.size);
        const price = (pricePerUnit * convertedQuantity).toFixed(2);
        row.find('.ingredient-price').text(`Cost: $${price}`);
    } else {
        row.find('.ingredient-price').text(`Cost: $0.00`);
    }

    updateTotalCost(detailsId, costInputId);
}

// Update recipe price
function updateRecipePrice($quantityInput, detailsId, costInputId) {
    const recipeId = $quantityInput.attr('name').split('_')[2];
    const quantity = parseFloat($quantityInput.val()) || 0;
    const row = $quantityInput.closest('.recipe-detail-row');

    const recipeData = recipesWithPrices.find(rec => rec.id == recipeId);

    if (recipeData && quantity > 0) {
        const cost = (recipeData.cost * quantity).toFixed(2);
        row.find('.recipe-price').text(`Cost: $${cost}`);
    } else {
        row.find('.recipe-price').text(`Cost: $0.00`);
    }

    updateTotalCost(detailsId, costInputId);
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

// Render ingredient details with unit conversion dropdown
function renderIngredientDetails(selected, detailsId, costInputId) {
    const existingData = {};
    $(`${detailsId} .ingredient-detail-row`).each(function () {
        const input = $(this).find('input[name^="ingredient_quantity_"]');
        const uomSelect = $(this).find('select[name^="ingredient_uom_"]');
        const ingredientId = input.attr('name').split('_')[2];
        const quantity = input.val();
        if (quantity) {
            existingData[ingredientId] = {
                quantity: quantity,
                uom: uomSelect.val() || ''
            };
        }
    });

    let html = '';
    selected.forEach(function (ingredientId) {
        const ingredientData = ingredientsWithPrices.find(ing => ing.id == ingredientId);
        const existing = existingData[ingredientId];

        if (!ingredientData) {
            html += `
                <div class="ingredient-detail-row" style="display:flex;align-items:center;gap:8px;margin-bottom:8px;">
                    <span class="ing_name" style="min-width:120px;">${ingredientId}</span>
                    <span class="ing_unit" style="min-width:80px;">N/A</span>
                    <select name="ingredient_uom_${ingredientId}" required>
                        ${buildUomOptionsHtml(null, existing ? existing.uom : '')}
                    </select>
                    <input type="number" name="ingredient_quantity_${ingredientId}" placeholder="Enter Wt / Qty" style="width:150px;" min="0" step="1">
                    <span class="ingredient-price" style="min-width:100px;">Cost: $0.00</span>
                </div>
            `;
            return;
        }

        const savedQuantity = existing ? existing.quantity : '';
        const savedUom = (existing && existing.uom) || ingredientData.unitOfMeasure || ingredientData.unit;

        let displayPrice = '0.00';
        if (savedQuantity) {
            const convertedQuantity = convertUnit(parseFloat(savedQuantity), savedUom, ingredientData.unit, ingredientData.size);
            const pricePerUnit = ingredientData.price / (ingredientData.packSize != null ? ingredientData.size : 1);
            displayPrice = (pricePerUnit * convertedQuantity).toFixed(2);
        }

        html += `
            <div class="ingredient-detail-row" style="display:flex;align-items:center;gap:8px;margin-bottom:8px;">
                <span class="ing_name" style="min-width:120px;" title="${ingredientData.text}">${ingredientData.text}</span>
                <span class="ing_unit" style="min-width:80px;">${ingredientData.unit}</span>
                <select name="ingredient_uom_${ingredientId}" required>
                    ${buildUomOptionsHtml(ingredientData.unit, savedUom)}
                </select>
                <input type="number" name="ingredient_quantity_${ingredientId}" placeholder="Enter Wt / Qty" style="width:150px;" min="0" step="1" value="${savedQuantity}">
                <span class="ingredient-price" style="min-width:100px;">Cost: $${displayPrice}</span>
            </div>
        `;
    });
    $(detailsId).html(html);
    updateTotalCost(detailsId, costInputId);
}

// Render recipe details
function renderRecipeDetails(selected, detailsId, costInputId) {
    const existingData = {};
    $(`${detailsId} .recipe-detail-row`).each(function () {
        const input = $(this).find('input[name^="recipe_quantity_"]');
        const recipeId = input.attr('name').split('_')[2];
        const quantity = input.val();
        if (quantity) {
            existingData[recipeId] = quantity;
        }
    });

    let html = '';
    selected.forEach(function (recipeId) {
        const recipeData = recipesWithPrices.find(rec => rec.id == recipeId);

        if (!recipeData) {
            html += `
                <div class="recipe-detail-row" style="display:flex;align-items:center;gap:8px;margin-bottom:8px;">
                    <span class="recipe_name" style="min-width:120px;">${recipeId}</span>
                    <span class="recipe_unit" style="min-width:80px;">N/A</span>
                    <input type="number" name="recipe_quantity_${recipeId}" placeholder="Enter Qty" style="width:150px;" min="0" step="1">
                    <span class="recipe-price" style="min-width:100px;">Cost: $0.00</span>
                </div>
            `;
            return;
        }

        const savedQuantity = existingData[recipeId] || '';

        let displayPrice = '0.00';
        if (savedQuantity) {
            displayPrice = (recipeData.cost * parseFloat(savedQuantity)).toFixed(2);
        }

        html += `
            <div class="recipe-detail-row" style="display:flex;align-items:center;gap:8px;margin-bottom:8px;">
                <span class="recipe_name" style="min-width:120px;" title="${recipeData.text}">${recipeData.text}</span>
                <span class="recipe_unit" style="min-width:80px;">${recipeData.unit}</span>
                <input type="number" name="recipe_quantity_${recipeId}" placeholder="Enter Qty" style="width:150px;" min="0" step="1" value="${savedQuantity}">
                <span class="recipe-price" style="min-width:100px;">Cost: $${displayPrice}</span>
            </div>
        `;
    });
    $(detailsId).html(html);
    updateTotalCost(detailsId, costInputId);
}

// Update total cost with unit conversion for ingredients and recipes
function updateTotalCost(detailsId, costInputId) {
    let totalCost = 0;

    // Calculate ingredient costs
    const ingredientsDetailsId = detailsId.includes('ingredient') ? detailsId : detailsId.replace('recipes', 'ingredients').replace('Recipes', 'Ingredients');
    const recipesDetailsId = detailsId.includes('recipe') ? detailsId : detailsId.replace('ingredients', 'recipes').replace('Ingredients', 'Recipes');

    // Get both containers
    const $ingredientsContainer = $(ingredientsDetailsId);
    const $recipesContainer = $(recipesDetailsId);

    // Calculate ingredient costs
    $ingredientsContainer.find('.ingredient-detail-row').each(function () {
        const ingredientId = $(this).find('input[name^="ingredient_quantity_"]').attr('name').split('_')[2];
        const quantity = parseFloat($(this).find('input[name^="ingredient_quantity_"]').val()) || 0;
        const uomSelect = $(this).find('select[name^="ingredient_uom_"]');
        const ingredientData = ingredientsWithPrices.find(ing => ing.id == ingredientId);

        if (ingredientData && quantity > 0) {
            const userUnit = uomSelect.val() || ingredientData.unitOfMeasure || ingredientData.unit;
            const convertedQuantity = convertUnit(quantity, userUnit, ingredientData.unit, ingredientData.size);
            const pricePerUnit = ingredientData.price / (ingredientData.packSize != null ? ingredientData.size : 1);
            totalCost += pricePerUnit * convertedQuantity;
        }
    });

    // Calculate recipe costs
    $recipesContainer.find('.recipe-detail-row').each(function () {
        const recipeId = $(this).find('input[name^="recipe_quantity_"]').attr('name').split('_')[2];
        const quantity = parseFloat($(this).find('input[name^="recipe_quantity_"]').val()) || 0;
        const recipeData = recipesWithPrices.find(rec => rec.id == recipeId);

        if (recipeData && quantity > 0) {
            totalCost += recipeData.cost * quantity;
        }
    });

    $(costInputId).val(totalCost.toFixed(2));
}

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

// Add new category
window.addNewCategory = function (categoryName, selectId) {
    const $select = $(selectId);
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

// Get form data with unit conversion for ingredients and recipes
function getMenuFormData(formId, ingredientsDetailsId, recipesDetailsId) {
    const formData = new FormData($(formId)[0]);
    const ingredients = [];
    const recipes = [];

    // Process ingredients
    $(`${ingredientsDetailsId} .ingredient-detail-row`).each(function () {
        const ingredientId = $(this).find('input[name^="ingredient_quantity_"]').attr('name').split('_')[2];
        const quantity = parseFloat($(this).find('input[name^="ingredient_quantity_"]').val()) || 0;
        const uomSelect = $(this).find('select[name^="ingredient_uom_"]');
        const ingredientData = ingredientsWithPrices.find(ing => ing.id == ingredientId);

        if (quantity > 0 && ingredientData) {
            const userUnit = uomSelect.val() || ingredientData.unitOfMeasure || ingredientData.unit;
            const convertedQuantity = convertUnit(quantity, userUnit, ingredientData.unit, ingredientData.size);

            ingredients.push({
                ingredientId: parseInt(ingredientId),
                quantity: convertedQuantity,
                unit: ingredientData.unit,
                uom: userUnit
            });
        }
    });

    // Process recipes
    $(`${recipesDetailsId} .recipe-detail-row`).each(function () {
        const recipeId = $(this).find('input[name^="recipe_quantity_"]').attr('name').split('_')[2];
        const quantity = parseFloat($(this).find('input[name^="recipe_quantity_"]').val()) || 0;
        const recipeData = recipesWithPrices.find(rec => rec.id == recipeId);

        if (quantity > 0 && recipeData) {
            recipes.push({
                recipeId: parseInt(recipeId),
                quantity: quantity
            });
        }
    });

    formData.set('ingredients', JSON.stringify(ingredients));
    formData.set('recipes', JSON.stringify(recipes));
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
    const recipes = JSON.parse(formData.get('recipes') || '[]');

    // At least one ingredient or recipe should be present
    return name && description && categoryId && cost > 0 && sellingPrice > 0 && (ingredients.length > 0 || recipes.length > 0);
}

// Add menu item
/* ── 5. AJAX / CRUD ───────────────────────────────────────────────────── */

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
            $('#recipesSelect').val(null).trigger('change');
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
        isQuery: false,
        onSuccess: function (response) {
            const menuItem = response.data;
            $('#editMenuItemId').val(menuItem.menuItemId);
            $('#editMenuName').val(menuItem.name);
            $('#editMenuDescription').val(menuItem.description);
            $('#editMenuCategory').val(menuItem.categoryId).trigger('change');
            $('#editMenuCost').val(menuItem.cost);
            $('#editMenuSellingPrice').val(menuItem.sellingPrice);
            $('#editFeaturedImage').val(imageBaseUrl + menuItem.featuredImage);

            if (menuItem.featuredImage) {
                $('#editFeaturedImagePreview').attr('src', imageBaseUrl + menuItem.featuredImage).show();
                $('#editFeaturedImageLabel').text('Replace featured image');
            } else {
                $('#editFeaturedImagePreview').hide();
                $('#editFeaturedImageLabel').text('Choose new featured image');
            }

            // Set ingredients
            $('#editIngredientsSelect').val(menuItem.ingredients.map(ing => ing.ingredientId)).trigger('change');

            // Set recipes
            $('#editRecipesSelect').val(menuItem.recipes.map(rec => rec.recipeId)).trigger('change');

            // Set quantities and trigger cost update
            setTimeout(() => {
                // Set ingredient quantities
                menuItem.ingredients.forEach(ing => {
                    const quantityInput = $(`input[name="ingredient_quantity_${ing.ingredientId}"]`);
                    const uomSelect = $(`select[name="ingredient_uom_${ing.ingredientId}"]`);
                    const ingredientData = ingredientsWithPrices.find(item => item.id == ing.ingredientId);

                    if (ingredientData) {
                        const userUnit = ing.uom || ing.unitOfMeasure || ingredientData.unitOfMeasure || ingredientData.unit;
                        if (uomSelect.length) {
                            const normalizeFn = (typeof normalizeUnit === 'function')
                                ? normalizeUnit
                                : (u => (u || '').toString().trim().toUpperCase());
                            const targetUom = normalizeFn(userUnit);

                            // Find the option whose value matches the saved uom (case/format insensitive)
                            let matchedValue = null;
                            uomSelect.find('option').each(function () {
                                if (this.value && normalizeFn(this.value) === targetUom) {
                                    matchedValue = this.value;
                                    return false;
                                }
                            });

                            // If the saved uom isn't among the compatible options, add it so it can be selected
                            if (!matchedValue) {
                                const uomLabel = (typeof ALLOWED_UNIT_OF_MEASURE_LABELS !== 'undefined' && ALLOWED_UNIT_OF_MEASURE_LABELS[userUnit])
                                    ? ALLOWED_UNIT_OF_MEASURE_LABELS[userUnit]
                                    : userUnit;
                                uomSelect.append(`<option value="${userUnit}">${uomLabel}</option>`);
                                matchedValue = userUnit;
                            }

                            uomSelect.val(matchedValue).trigger('change');
                        }
                        const displayQuantity = convertUnit(ing.quantity, ingredientData.unit, userUnit, ingredientData.size);
                        quantityInput.val(displayQuantity.toFixed(2));
                    } else {
                        quantityInput.val(ing.quantity);
                    }

                    quantityInput.trigger('input');
                });

                // Set recipe quantities
                menuItem.recipes.forEach(rec => {
                    const quantityInput = $(`input[name="recipe_quantity_${rec.recipeId}"]`);
                    quantityInput.val(rec.quantity);
                    quantityInput.trigger('input');
                });

                // Explicitly update total cost after setting quantities
                updateTotalCost('#editIngredientsDetails', '#editMenuCost');
            }, 300);

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

// Function to toggle menu item active status
function toggleMenuItemActive(id) {
    if (confirm('Are you sure you want to toggle the active status of this menu item?')) {
        ajaxRequest({
            url: `/api/MenuApi/ToggleMenuActive/${id}`,
            method: 'PUT',
            showMessage: false,
            onSuccess: function (response) {
                refreshMenuGrid();
                showNotification(response.responseMsg, 'success');
            },
            onError: function (xhr, status, error) {
                console.error('Failed to toggle menu item status:', error);
            }
        });
    }
}

// Refresh menu grid
function refreshMenuGrid() {
    ajaxRequest({
        url: '/api/MenuApi/GetAllMenuItems',
        method: 'GET',
        showMessage: false,
        isQuery: false,
        onSuccess: function (response) {
            const menuItems = response.data || [];
            cachedMenuItems = menuItems;
            const grid = $('#menuGrid');
            grid.empty();

            let totalCostPercentage = 0;
            let totalCurrentCostPercentage = 0;
            let activeItemCount = 0;
            if (menuItems.length > 0) {
                menuItems.forEach(item => {
                    if (item.isActive && item.sellingPrice > 0) {
                        const itemCostPercent = (item.cost / item.sellingPrice) * 100;
                        const itemCurrentCostPercent = (item.currentCost / item.sellingPrice) * 100;
                        totalCostPercentage += itemCostPercent;
                        totalCurrentCostPercentage += itemCurrentCostPercent;
                        activeItemCount++;
                    }

                    const availability = item.isAvailable ? 'available' : 'out-of-stock';
                    const actionText = item.isActive ? 'Deactivate' : 'Activate';
                    const actionIcon = item.isActive ? 'fas fa-ban' : 'fas fa-check';
                    const costPercentage = ((item.cost / item.sellingPrice) * 100).toFixed(2);
                    const currentCostPercentage = ((item.currentCost / item.sellingPrice) * 100).toFixed(2);

                    const row = `
                    <div class="menu-item-card" data-category="${item.categoryId}">
                        <div class="menu-item-image">
                            <span class="status-badge ${item.isActive ? 'good' : 'critical'}" style="position:absolute; top:10px; left:10px;">
                                ${item.isActive ? 'Active' : 'Inactive'}
                            </span>
                            <img src="${imageBaseUrl + item.featuredImage || 'https://via.placeholder.com/300x200'}" alt="${item.name}">
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
                                        <span class="prep-time">
                                        <strong>Food Cost Percentage: </strong>
                                        ${item.cost == item.currentCost
                            ? `<span>${currentCostPercentage}%<span class="tooltip-icon" data-tooltip="Food Cost % at the time this item was created/updated">i</span></span>`
                            : `
                                                <span>
                                                    <span class="text-highlight danger" title="Food Cost % at the time this item was created/updated">${costPercentage}%</span>
                                                    <span class="text-highlight" style="cursor:pointer;" onclick="showMenuCostBreakdown(${item.menuItemId})" title="Click to view ingredient cost breakdown">${currentCostPercentage}% <i class="fas fa-info-circle" style="font-size:11px; opacity:0.75; margin-left:2px;"></i></span>
                                                </span>
                                              `
                        }
                                    </span>

                                    </div>
                                </div>
                                <div class="dropdown">
                                    <button class="dropdown-toggle">
                                        <i class="fas fa-ellipsis-h"></i>
                                    </button>
                                    <div class="dropdown-menu">
                                        <a href="#" onclick="editMenuItem(${item.menuItemId})"><i class="fas fa-edit"></i> Edit Item</a>
                                        <a href="#" onclick="toggleMenuItemActive(${item.menuItemId})"><i class="${actionIcon}"></i> ${actionText}</a>
                                        <a href="#" onclick="exportSingleMenuPDF(${item.menuItemId})"><i class="fas fa-file-pdf"></i> Export PDF</a>
                                    </div>
                                </div>
                            </div>
                            <div class="item-details">
                                <div class="pricing">
                                    <span class="price">$${item.sellingPrice.toFixed(2)}</span>
                                    ${item.cost == item.currentCost 
                                        ? `<span class="cost">Cost: $${item.currentCost.toFixed(2)}<span class="tooltip-icon" data-tooltip="Cost at the time this item was created/updated.">i</span></span>`
                                            : `<div class="clickable-cost-breakdown" style="display:inline-flex; align-items:center; gap:4px;">
                                            Cost: <span class="cost text-highlight danger" title="Cost at the time this item was created/updated.">$${item.cost.toFixed(2)}</span> 
                                            <span class="cost text-highlight success" style="cursor:pointer;" onclick="showMenuCostBreakdown(${item.menuItemId})" title="Click to view ingredient cost breakdown">$${item.currentCost.toFixed(2)} <i class="fas fa-info-circle" style="font-size:12px; opacity:0.7; margin-left:2px;"></i></span>
                                           </div>`
                                    }
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
                debugger
                // Compute the average once, after all items have been summed
                const avgCostPercentage = activeItemCount > 0 ? (totalCostPercentage / activeItemCount).toFixed(2) : "0.00";
                const avgCurrentCostPercentage = activeItemCount > 0 ? (totalCurrentCostPercentage / activeItemCount).toFixed(2) : "0.00";

                if (avgCostPercentage === avgCurrentCostPercentage) {
                    $('#avgFoodCost').html(`${avgCurrentCostPercentage}%<span class="tooltip-icon" data-tooltip="Average Food Cost % at the time menu items were created/updated.">i</span>`);
                } else {
                    $('#avgFoodCost').html(`
                        <span class="text-highlight danger">${avgCostPercentage}%<span class="tooltip-icon danger" data-tooltip="Average Food Cost % at the time menu items were created/updated.">i</span></span>
                        <span class="text-highlight">${avgCurrentCostPercentage}%<span class="tooltip-icon success" data-tooltip="Average Food Cost % according to the current prices.">i</span></span>
                    `);
                }
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
    let visibleItems = 0;
    $('.menu-item-card').each(function () {
        const itemCategory = $(this).data('category');
        const isVisible = categoryId === 'all' || itemCategory == categoryId;
        $(this).toggle(isVisible);
        if (isVisible) visibleItems++;
    });

    const grid = $('#menuGrid');
    grid.find('.no-items-message').remove();

    if (visibleItems === 0) {
        const categoryName = categoryId === 'all' ? 'All' : categories.find(cat => cat.id == categoryId)?.text || 'This Category';
        const message = `
            <div class="no-items-message" style="text-align: center; padding: 40px; color: #6b7280; font-size: 1.2rem;">
                <i class="fas fa-utensils" style="font-size: 2rem; color: #9ca3af; margin-bottom: 10px;"></i>
                <h2 style="font-size: 1.5rem; font-weight: 500; margin-bottom: 10px;">No Menu Items in ${categoryName}</h2>
                <p style="color: #9ca3af;">It looks like there are no menu items available for this category. Try adding a new item or selecting a different category.</p>
            </div>
        `;
        grid.append(message);
    }
}

// Edit menu item
function editMenuItem(id) {
    getMenuItemById(id);
}



// 1. Single Menu Item Export with Ingredients
function exportSingleMenuPDF(id) {
    ajaxRequest({
        url: `/api/MenuApi/GetMenuItem/${id}`,
        method: 'GET',
        onSuccess: function (response) {
            generateMenuPDF([response.data], `Menu_Item_${response.data.name}`);
        }
    });
}

// 2. Event Listener for All Menu Items
$('#exportMenuPdfBtn').on('click', function () {
    ajaxRequest({
        url: '/api/MenuApi/GetAllMenuItems',
        method: 'GET',
        onSuccess: function (response) {
            generateMenuPDF(response.data, 'Full_Menu_Report');
        }
    });
});


async function generateMenuPDF(menuData, filename) {
    const value = localStorage.getItem("profilePic");
    const profilePic = value && value !== null ? $('#imageBaseUrl').val() + value : '/images/menu optics_logo.png';
    debugger
    isStaffFacing = document.getElementById('staffFacing').checked;
    let logoBase64 = null;
    try {
        logoBase64 = await getImageAsPngBase64(profilePic);
    } catch (e) { console.warn("Logo load failed", e); }

    const content = [];

    // Use for...of loop to handle the async image fetching for each item
    for (const [index, item] of menuData.entries()) {
        if (index > 0) content.push({ text: '', pageBreak: 'before' });

        // --- NEW: Fetch Item Image ---
        let itemImageBase64 = null;
        if (item.featuredImage) {
            try {
                itemImageBase64 = await getImageAsPngBase64(imageBaseUrl + item.featuredImage);
            } catch (e) {
                console.warn(`Could not load image for ${item.name}`, e);
            }
        }

        const foodCostPercent = (item.cost / item.sellingPrice) * 100;
        const marginPercent = ((item.sellingPrice - item.cost) / item.sellingPrice) * 100;

        // Header with Branding
        content.push({
            columns: [
                {
                    stack: [
                        { text: item.name.toUpperCase(), style: 'itemTitle' },
                        { text: `Category: ${item.categoryName || 'General'}`, margin: [0, 2, 0, 0] }
                    ]
                },
                logoBase64 ? { image: logoBase64, width: 60, alignment: 'right' } : { text: '' }
            ],
            margin: [0, 0, 0, 15]
        });

        // --- NEW: Insert Item Image Section ---
        if (itemImageBase64) {
            content.push({
                image: itemImageBase64,
                width: 200, // Adjust size as needed
                alignment: 'center',
                margin: [0, 0, 0, 15]
            });
        }

        if (isStaffFacing) {

            // Financials Table
            content.push({
                table: {
                    widths: ['*'],
                    body: [
                        [
                            { text: 'Direction', style: 'tableHeader' }
                        ],
                        [
                            item.description || 'No Direction'
                        ]
                    ]
                },
                layout: 'lightHorizontalLines',
                margin: [0, 0, 0, 20]
            });

        } else {

            // Financials Table
            content.push({
                table: {
                    widths: ['*', 'auto', 'auto', 'auto', 'auto'],
                    body: [
                        [
                            { text: 'Direction', style: 'tableHeader' },
                            { text: 'Cost', style: 'tableHeader' },
                            { text: 'Price', style: 'tableHeader' },
                            { text: 'Food Cost %', style: 'tableHeader' },
                            { text: 'Margin %', style: 'tableHeader' }
                        ],
                        [
                            item.description || 'No Direction',
                            `$${item.cost.toFixed(2)}`,
                            `$${item.sellingPrice.toFixed(2)}`,
                            { text: `${foodCostPercent.toFixed(2)}%`, color: foodCostPercent > 35 ? 'red' : 'green', bold: true },
                            { text: `${marginPercent.toFixed(2)}%`, bold: true, color: '#2c3e50' }
                        ]
                    ]
                },
                layout: 'lightHorizontalLines',
                margin: [0, 0, 0, 20]
            });

        }
        // Ingredients Breakdown
        if (item.ingredients && item.ingredients.length > 0) {
            content.push({ text: 'Detailed Ingredients List', style: 'sectionHeader' });
            const ingRows = item.ingredients.map(ing => [
                ing.ingredientName || `---`,
                `${ing.quantity.toFixed(2)} ${ing.unit || ''}`
            ]);

            content.push({
                table: {
                    widths: ['*', 'auto'],
                    body: [
                        [{ text: 'Ingredient Name', bold: true, fillColor: '#eeeeee' }, { text: 'Quantity', bold: true, fillColor: '#eeeeee' }],
                        ...ingRows
                    ]
                },
                margin: [0, 5, 0, 15]
            });
        }


        // Recipes Breakdown
        if (item.recipes && item.recipes.length > 0) {
            content.push({ text: 'Detailed Recipes List', style: 'sectionHeader' });
            const ingRows = item.recipes.map(ing => [
                ing.recipeName || `---`,
                `${ing.quantity.toFixed(2)} ${ing.unit || ''}`
            ]);

            content.push({
                table: {
                    widths: ['*', 'auto'],
                    body: [
                        [{ text: 'Recipe Name', bold: true, fillColor: '#eeeeee' }, { text: 'Quantity', bold: true, fillColor: '#eeeeee' }],
                        ...ingRows
                    ]
                },
                margin: [0, 5, 0, 15]
            });
        }
    }

    const docDefinition = {
        content: content,
        styles: {
            itemTitle: { fontSize: 20, bold: true, color: '#f08d11' },
            sectionHeader: { fontSize: 13, bold: true, margin: [0, 10, 0, 5], color: '#333', decoration: 'underline' },
            tableHeader: { bold: true, fontSize: 10, fillColor: '#f8f9fa' }
        },
        pageSize: 'A4',
        pageMargins: [40, 40, 40, 40]
    };

    pdfMake.createPdf(docDefinition).download(`${filename}.pdf`);
}

// Show menu item cost breakdown modal (uses data already received in GetAllMenuItems)
function showMenuCostBreakdown(menuItemId) {
    const $tbody = $('#costBreakdownTableBody');
    $tbody.empty();
    $('#costBreakdownEmptyMsg').hide();

    const menuItem = cachedMenuItems.find(m => m.menuItemId == menuItemId);
    if (!menuItem) {
        return;
    }

    $('#costBreakdownMenuName').text(menuItem.name || 'Menu Item');
    $('#costBreakdownCategory').text(menuItem.categoryName ? `Category: ${menuItem.categoryName}` : '');

    const rows = [];
    const ingredients = menuItem.ingredients || [];

    if (ingredients.length > 0) {
        ingredients.forEach(ing => {
            const ingName = ing.ingredientName || (ing.name || `Ingredient #${ing.ingredientId}`);
            
            const sourceLabel = ing.source && ing.source.trim() !== '' ? ing.source.trim() : null;

            const oldPrice = ing.oldPrice != null ? parseFloat(ing.oldPrice) : (parseFloat(ing.currentPrice) || 0);
            const currentPrice = ing.currentPrice != null ? parseFloat(ing.currentPrice) : oldPrice;

            const diff = currentPrice - oldPrice;
            let trendHtml = '';
            if (diff > 0.005) {
                trendHtml = `<span class="trend-badge up" style="color: #dc2626; background: #fee2e2; font-weight: 600; padding: 3px 8px; border-radius: 6px; display: inline-flex; align-items: center; gap: 4px;"><i class="fas fa-arrow-up"></i> +$${diff.toFixed(2)}</span>`;
            } else if (diff < -0.005) {
                trendHtml = `<span class="trend-badge down" style="color: #16a34a; background: #dcfce7; font-weight: 600; padding: 3px 8px; border-radius: 6px; display: inline-flex; align-items: center; gap: 4px;"><i class="fas fa-arrow-down"></i> -$${Math.abs(diff).toFixed(2)}</span>`;
            } else {
                trendHtml = `<span class="trend-badge neutral" style="color: #6b7280; background: #f3f4f6; font-weight: 500; padding: 3px 8px; border-radius: 6px; display: inline-flex; align-items: center; gap: 4px;"><i class="fas fa-minus"></i> $0.00</span>`;
            }

            const sourceBadge = sourceLabel && sourceLabel !== "Ingredient"
                ? `<span style="background: #fef3c7; color: #92400e; font-weight: 600; font-size: 11px; padding: 3px 8px; border-radius: 4px; display: inline-flex; align-items: center; gap: 4px;"><i class="fas fa-utensils"></i> ${sourceLabel}</span>`
                : `<span style="background: #e0f2fe; color: #0369a1; font-weight: 600; font-size: 11px; padding: 3px 8px; border-radius: 4px; display: inline-flex; align-items: center; gap: 4px;"><i class="fas fa-cube"></i> ${sourceLabel}</span>`;

            rows.push(`
                <tr style="border-bottom: 1px solid #f1f5f9;">
                    <td style="padding: 10px 14px; font-weight: 600; color: #1e293b;">
                        ${ingName}
                    </td>
                    <td style="padding: 10px 14px;">
                        ${sourceBadge}
                    </td>
                    <td style="padding: 10px 14px; color: #475569; font-weight: 500;">
                        $${oldPrice.toFixed(2)}
                    </td>
                    <td style="padding: 10px 14px; color: #1e293b; font-weight: 600;">
                        $${currentPrice.toFixed(2)}
                    </td>
                    <td style="padding: 10px 14px;">
                        ${trendHtml}
                    </td>
                </tr>
            `);
        });
    }

    if (rows.length === 0) {
        $('#costBreakdownEmptyMsg').show();
    } else {
        $tbody.html(rows.join(''));
        $('#costBreakdownEmptyMsg').hide();
    }

    openModal('menuCostBreakdownModal');
}

window.showMenuCostBreakdown = showMenuCostBreakdown;
