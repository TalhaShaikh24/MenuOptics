// wwwroot/views-js/recipes.js
let recipeTable;
let ingredientsWithPrices = [];

$(document).ready(function () {
    // Initialize Select2 for ingredients
    initializeIngredientsSelect('#ingredientsSelect', '#addRecipeModal', '#ingredientsDetails', '#recipePrice');
    initializeIngredientsSelect('#editIngredientsSelect', '#editRecipeModal', '#editIngredientsDetails', '#editRecipePrice');

    // Initialize DataTable
    initRecipeTable();

    // Event handlers
    $('#saveRecipeBtn').on('click', function () {
        const recipeData = getRecipeFormData('#addRecipeForm', '#ingredientsDetails');
        if (validateRecipeData(recipeData)) {
            addRecipe(recipeData);
        } else {
            showNotification('Please fill in all required fields.', 'error');
        }
    });

    $('#updateRecipeBtn').on('click', function () {
        const recipeData = getRecipeFormData('#editRecipeForm', '#editIngredientsDetails');
        recipeData.append('recipeId', $('#editRecipeId').val());
        if (validateRecipeData(recipeData)) {
            updateRecipe(recipeData);
        } else {
            showNotification('Please fill in all required fields.', 'error');
        }
    });

    // Search filter (DataTable built-in)
    $('#recipeSearch').on('input', function () {
        recipeTable.search(this.value).draw();
    });



    refreshRecipesTable();
});

// Initialize DataTable
function initRecipeTable() {
    recipeTable = $('#recipesTable').DataTable({
        paging: true,
        searching: true,
        ordering: true,
        order: [[0, 'asc']],
        pageLength: 10,
        lengthMenu: [10, 25, 50, 100],
        language: {
            search: '',
            searchPlaceholder: 'Search recipes...'
        }
    });
}

// Initialize Select2 for ingredients
function initializeIngredientsSelect(selectId, modalId, detailsId, priceInputId) {
    ajaxRequest({
        url: '/api/IngredientApi/GetAllIngredientsByUserId',
        method: 'GET',
        showMessage: false,
        isQuery: false,
        onSuccess: function (response) {

            const activeIngredients = response.data.filter(ing => ing.isActive != false);
            debugger
            ingredientsWithPrices = activeIngredients.map(ing => ({
                id: ing.ingredientId,
                text: ing.name,
                price: ing.price,
                unit: ing.unit, // Storage unit
                unitOfMeasure: ing.unitOfMeasure || ing.unit // Display unit
            }));

            if ($(selectId).hasClass('select2-hidden-accessible')) {
                $(selectId).select2('destroy');
            }

            $(selectId).empty(); // existing options clear karo

            $(selectId).select2({
                tags: false,
                data: ingredientsWithPrices,
                placeholder: 'Select or add ingredients',
                width: '100%',
                dropdownParent: $(modalId),

            });



            $(selectId).on('change', function () {
                const selected = $(this).val() || [];
                renderIngredientDetails(selected, detailsId, priceInputId);
            });

            $(detailsId).on('input', 'input[name^="ingredient_quantity_"]', function () {
                updateIngredientPrice($(this), detailsId, priceInputId);
            });

            $(detailsId).on('change', 'select[name^="ingredient_uom_"]', function () {
                updateIngredientPrice($(this), detailsId, priceInputId);
            });
        },
        onError: function (xhr, status, error) {
            console.error('Failed to fetch ingredients:', error);
        }
    });
}

// Render ingredient details
function renderIngredientDetails(selected, detailsId, priceInputId) {
    const existingData = {};
    $(`${detailsId} .ingredient-detail-row`).each(function () {
        const input = $(this).find('input[name^="ingredient_quantity_"]');
        const uomSelect = $(this).find('select[name^="ingredient_uom_"]');
        const ingredientId = input.attr('name').split('_')[2];
        const quantity = input.val();
        if (quantity) existingData[ingredientId] = { quantity: quantity, uom: uomSelect.val() || '' };
    });

    let html = '';
    selected.forEach(id => {
        const ingredient = ingredientsWithPrices.find(ing => ing.id == id);
        if (ingredient) {
            const name = ingredient.text;
            const existing = existingData[id];
            const savedQuantity = existing ? existing.quantity : '';
            const savedUom = (existing && existing.uom) || ingredient.unitOfMeasure || ingredient.unit;
            const uomOptions = (typeof buildUomOptionsHtml === 'function')
                ? buildUomOptionsHtml(ingredient.unit, savedUom)
                : `<option value="${savedUom}" selected>${savedUom}</option>`;
            html += `
                <div class="ingredient-detail-row" style="display:flex;align-items:center;gap:8px;margin-bottom:8px;">
                    <span class="ingredient-name" style="min-width:120px;">${name}</span>
                    <span class="unit" style="min-width:80px;">${ingredient.unit}</span>
                    <select name="ingredient_uom_${id}" required>
                        ${uomOptions}
                    </select>
                    <input type="number" step="0.01" name="ingredient_quantity_${id}" value="${savedQuantity}" placeholder="Enter Wt / Qty" style="width:150px;" min="0">
                    
                    <span class="ingredient-price" style="min-width:100px;">Price: $0.00</span>
                </div>
            `;
        }
    });
    $(detailsId).html(html);

    // Recompute prices for any restored rows and refresh the total
    $(`${detailsId} .ingredient-detail-row`).each(function () {
        const input = $(this).find('input[name^="ingredient_quantity_"]');
        if (input.val()) updateIngredientPrice(input, detailsId, priceInputId);
    });
    updateTotalPrice(detailsId, priceInputId);

    // Remove handler
    //$(detailsId).off('click', '.remove-ingredient').on('click', '.remove-ingredient', function () {
    //    const id = $(this).data('id');
    //    $(this).closest('.ingredient-detail-row').remove();
    //    const select = $(selectId);
    //    const selected = select.val() || [];
    //    select.val(selected.filter(s => s != id)).trigger('change');
    //});
}

// Update ingredient price with unit conversion
function updateIngredientPrice($el, detailsId, priceInputId) {
    const row = $el.closest('.ingredient-detail-row');
    const $quantityInput = row.find('input[name^="ingredient_quantity_"]');
    const uomSelect = row.find('select[name^="ingredient_uom_"]');
    const ingredientId = $quantityInput.attr('name').split('_')[2];
    const quantity = parseFloat($quantityInput.val()) || 0;
    const ingredientData = ingredientsWithPrices.find(ing => ing.id == ingredientId);

    if (ingredientData && quantity > 0) {
        // Convert the quantity entered in the user-selected UOM back to the
        // ingredient's stored unit (pricing is per stored unit).
        const userUnit = uomSelect.val() || ingredientData.unitOfMeasure || ingredientData.unit;
        const convertedQuantity = convertUnit(quantity, userUnit, ingredientData.unit);
        const price = (ingredientData.price * convertedQuantity).toFixed(2);
        row.find('.ingredient-price').text(`Price: $${price}`);
    } else {
        row.find('.ingredient-price').text(`Price: $0.00`);
    }

    updateTotalPrice(detailsId, priceInputId);
}

// Update total price
function updateTotalPrice(detailsId, priceInputId) {
    let total = 0;
    $(`${detailsId} .ingredient-detail-row`).each(function () {
        const priceText = $(this).find('.ingredient-price').text().replace('Price: $', '');
        total += parseFloat(priceText) || 0;
    });
    $(priceInputId).val(total.toFixed(2));
}

// Unit conversion is provided by the shared unitConversions.js
// (convertUnit / getAllowedUomForUnit / buildUomOptionsHtml). Ensure that
// script is loaded on this page BEFORE recipes.js. The previous local
// pass-through convertUnit stub has been removed so real numeric conversion
// (e.g. KG <-> GM / LB / OZ / LT / ML / FL / GA) is used.

// Get form data with proper unit conversion
function getRecipeFormData(formId, detailsId) {
    const formData = new FormData($(formId)[0]);
    const ingredients = [];

    $(`${detailsId} .ingredient-detail-row`).each(function () {
        const input = $(this).find('input[name^="ingredient_quantity_"]');
        const nameParts = input.attr('name').split('_');
        const ingredientId = nameParts[2];
        const displayQuantity = parseFloat(input.val()) || 0;
        const uomSelect = $(this).find('select[name^="ingredient_uom_"]');
        const displayUnit = uomSelect.val() || $(this).find('.unit').text().trim();

        const ingredientData = ingredientsWithPrices.find(ing => ing.id == ingredientId);

        if (ingredientData && displayQuantity > 0) {
            const storageQuantity = convertUnit(displayQuantity, displayUnit, ingredientData.unit);

            ingredients.push({
                IngredientId: parseInt(ingredientId),
                Quantity: storageQuantity
            });
        }
    });

    formData.append('ingredients', JSON.stringify(ingredients));
    return formData;
}

// Validate data
function validateRecipeData(formData) {
    const name = formData.get('name');
    const unit = formData.get('unit');
    const ingredients = JSON.parse(formData.get('ingredients') || '[]');
    return name && unit && ingredients.length > 0;
}

// Add recipe
function addRecipe(formData) {
    ajaxRequest({
        url: '/api/RecipeApi/CreateRecipe',
        method: 'POST',
        data: formData,
        contentType: false,
        processData: false,
        onSuccess: function (response) {
            refreshRecipesTable();
            closeModal('addRecipeModal');
            showNotification('Recipe added successfully!', 'success');
        },
        onError: function (xhr, status, error) {
            console.error('Failed to add recipe:', error);
            showNotification('Failed to add recipe.', 'error');
        }
    });
}

// Update recipe
function updateRecipe(formData) {
    ajaxRequest({
        url: '/api/RecipeApi/UpdateRecipe',
        method: 'PUT',
        data: formData,
        contentType: false,
        processData: false,
        onSuccess: function (response) {
            refreshRecipesTable();
            closeModal('editRecipeModal');
            showNotification('Recipe updated successfully!', 'success');
        },
        onError: function (xhr, status, error) {
            console.error('Failed to update recipe:', error);
            showNotification('Failed to update recipe.', 'error');
        }
    });
}

// Get recipe by ID
function getRecipeById(id) {
    ajaxRequest({
        url: `/api/RecipeApi/GetRecipe/${id}`,
        method: 'GET',
        showMessage: false,
        onSuccess: function (response) {
            const recipe = response.data;
            $('#editRecipeId').val(recipe.recipeId);
            $('#editRecipeName').val(recipe.name);
            $('#editRecipeUnit').val(recipe.unit);
            $('#editRecipePrice').val(recipe.price);

            $('#editIngredientsSelect').val(recipe.ingredients.map(ing => ing.ingredientId)).trigger('change');

            setTimeout(() => {
                recipe.ingredients.forEach(ing => {
                    const quantityInput = $(`input[name="ingredient_quantity_${ing.ingredientId}"]`);
                    const ingredientData = ingredientsWithPrices.find(item => item.id == ing.ingredientId);
                    if (ingredientData) {
                        const userUnit = ingredientData.unitOfMeasure || ingredientData.unit;
                        const displayQuantity = convertUnit(ing.quantity, ingredientData.unit, userUnit);
                        quantityInput.val(displayQuantity.toFixed(2));
                    } else {
                        quantityInput.val(ing.quantity);
                    }
                    quantityInput.trigger('input');
                });
                updateTotalPrice('#editIngredientsDetails', '#editRecipePrice');
            }, 300);

            openModal('editRecipeModal');
        },
        onError: function (xhr, status, error) {
            console.error('Failed to fetch recipe:', error);
        }
    });
}

// Toggle active
function toggleRecipeActive(id) {
    if (confirm('Are you sure you want to toggle the active status?')) {
        ajaxRequest({
            url: `/api/RecipeApi/ToggleRecipeActive/${id}`,
            method: 'PUT',
            showMessage: false,
            onSuccess: function (response) {
                refreshRecipesTable();
                showNotification(response.responseMsg, 'success');
            },
            onError: function (xhr, status, error) {
                console.error('Failed to toggle status:', error);
            }
        });
    }
}

// Delete recipe
function deleteRecipe(id) {
    if (confirm('Are you sure you want to delete this recipe?')) {
        ajaxRequest({
            url: `/api/RecipeApi/DeleteRecipe/${id}`,
            method: 'DELETE',
            showMessage: false,
            onSuccess: function (response) {
                refreshRecipesTable();
                showNotification('Recipe deleted successfully!', 'success');
            },
            onError: function (xhr, status, error) {
                console.error('Failed to delete recipe:', error);
            }
        });
    }
}

// Refresh table
function refreshRecipesTable() {
    ajaxRequest({
        url: '/api/RecipeApi/GetAllRecipes',
        method: 'GET',
        showMessage: false,
        isQuery: false,
        onSuccess: function (response) {
            const recipes = response.data;
            $('#totalRecipes').text(recipes.filter(r => r.isActive).length);

            recipeTable.clear();

            recipes.forEach(item => {
                const created = DateFormatter.formatWithComma(item.createdOn);

                // Get the ingredient names as a comma-separated string
                const ingredientNames = item.ingredients ? item.ingredients.map(ingredient => ingredient.ingredientName).join(', ') : '';

                const row = `
                    <tr>
                        <td>
                            <div class="recipe-info">
                                <p class="recipe-name">${item.name}</p>
                                <p class="recipe-meta">Added on: ${created}</p>
                            </div>
                        </td>
                        <td>${item.unit}</td>
                        <td>$${item.price.toFixed(2)}</td>
                        <td><span class="status-badge good">${ingredientNames}</span></td>
                        <td><i class="fas fa-calendar"></i> ${created}</td>
                     
                        <td>
                            <div class="dropdown">
                                <button class="dropdown-toggle"><i class="fas fa-ellipsis-h"></i></button>
                                <div class="dropdown-menu">
                                    <a href="#" onclick="getRecipeById(${item.recipeId})"><i class="fas fa-edit"></i> Edit</a>
                                    <a href="#" onclick="deleteRecipe(${item.recipeId})"><i class="fas fa-trash"></i> Delete</a>
                                </div>
                            </div>
                        </td>
                    </tr>`;
                recipeTable.row.add($(row));
            });

            recipeTable.draw();
        },
        onError: function (xhr, status, error) {
            console.error('Failed to fetch recipes:', error);
        }
    });
}