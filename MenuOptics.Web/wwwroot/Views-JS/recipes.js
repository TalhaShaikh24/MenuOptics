/* ============================================================================
 * recipes.js — Recipe management page (Views/Recipe/Index.cshtml)
 * Recipe table with ingredient Select2 pickers and unit-converted pricing.
 * Requires shared unitConversions.js to be loaded BEFORE this file.
 *
 * Project convention — every Views-JS file follows this section order:
 *   1. Constants  2. State  3. Initialization  4. Event bindings
 *   5. AJAX / CRUD  6. Helpers  7. Utilities
 * Functions stay global: they are referenced by inline onclick handlers.
 * ==========================================================================*/

/* ── 2. State ─────────────────────────────────────────────────────────── */

let recipeTable;
let ingredientsWithPrices = [];
let recipeDescriptions = {}

/* ── 3. Initialization / 4. Event bindings ────────────────────────────── */

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

    // Export all recipes to PDF. The Employee Facing checkbox removes pricing information.
    $('#exportRecipesPdfBtn').on('click', function () {
        ajaxRequest({
            url: '/api/RecipeApi/GetAllRecipes',
            method: 'GET',
            showMessage: false,
            isQuery: false,
            onSuccess: function (response) {
                generateRecipePDF(response.data || [], 'Full_Recipe_Report');
            },
            onError: function (xhr, status, error) {
                console.error('Failed to export recipes:', error);
                showNotification('Failed to export recipes.', 'error');
            }
        });
    });



    refreshRecipesTable();
});

/* ── 6. Helpers ───────────────────────────────────────────────────────── */

// Show full description in popup
function showRecipeDescription(recipeId) {
    const desc = recipeDescriptions[recipeId] || '';
    $('#recipeDescriptionPopupText').text(desc);
    openModal('recipeDescriptionModal');
}


// Initialize DataTable
function initRecipeTable() {
    recipeTable = $('#recipesTable').DataTable({
        paging: true,
        searching: true,
        ordering: false,
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

            ingredientsWithPrices = activeIngredients.map(ing => ({
                id: ing.ingredientId,
                text: ing.name,
                price: ing.price,
                unit: ing.unit, // Storage unit
                size: ing.size, // Size
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
        const pricePerUnit = ingredientData.price / (ingredientData.size || 1);
        const convertedQuantity = convertUnit(quantity, userUnit, ingredientData.unit);
        const price = (pricePerUnit * convertedQuantity).toFixed(2);
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
    debugger
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
                Quantity: storageQuantity,
                Uom: displayUnit
            });
        }
    });

    formData.append('ingredients', JSON.stringify(ingredients));
    return formData;
}

/* ── Validation ───────────────────────────────────────────────────────── */

// Validate data
function validateRecipeData(formData) {
    const name = formData.get('name');
    const unit = formData.get('unit');
    const ingredients = JSON.parse(formData.get('ingredients') || '[]');
    return name && unit && ingredients.length > 0;
}

/* ── 5. AJAX / CRUD ───────────────────────────────────────────────────── */

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
            $('#editRecipeDescription').val(recipe.description);

            $('#editIngredientsSelect').val(recipe.ingredients.map(ing => ing.ingredientId)).trigger('change');

            setTimeout(() => {
                recipe.ingredients.forEach(ing => {
                    const quantityInput = $(`input[name="ingredient_quantity_${ing.ingredientId}"]`);
                    const uomSelect = $(`select[name="ingredient_uom_${ing.ingredientId}"]`);
                    const ingredientData = ingredientsWithPrices.find(item => item.id == ing.ingredientId);
                    if (ingredientData) {
                        const userUnit = ing.uom || ing.Uom || ing.unitOfMeasure || ingredientData.unitOfMeasure || ingredientData.unit;
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
                recipeDescriptions[item.recipeId] = item.description || '';

                // Get the ingredient names as a comma-separated string
                const ingredientNames = item.ingredients ? item.ingredients.map(ingredient => ingredient.ingredientName).join(', ') : '';


                const cls = item.isActive ? 'good' : 'critical';
                const lbl = item.isActive ? 'Active' : 'In Active';
                const price = item.price;
                const currentPrice = item.currentPrice;

                const row = `
                    <tr>
                        <td>
                            <div class="recipe-info">
                                <p class="recipe-name">${item.name}</p>
                            </div>
                        </td>
                        <td>
                            <span class="recipeDescription" style="cursor:pointer;" onclick="showRecipeDescription(${item.recipeId})">${item.description}</span>
                        </td>
                        <td>${item.unit}</td>
                        <td>
                         ${
                                currentPrice == null || price === currentPrice
                                    ? `
                                  <span>
                                    ${price}
                                    <span class="tooltip-icon" data-tooltip="Price at the time this recipe was created/updated">i</span>
                                  </span>
                                `
                                                    : `
                                  <span class="text-highlight danger">
                                    ${price}
                                    <span class="tooltip-icon danger" data-tooltip="Price at the time this recipe was created/updated">i</span>
                                  </span>
                                    <br>
                                  <span class="text-highlight success">
                                    ${currentPrice}
                                    <span class="tooltip-icon success" data-tooltip="Current price">i</span>
                                  </span>
                                `
                            }
                        </td>
                        <td><span class="status-badge good">${ingredientNames}</span></td>
                        <td><span class="status-badge ${cls}" > ${lbl}</span ></td>
                        <td>
                            <div class="dropdown">
                                <button class="dropdown-toggle"><i class="fas fa-ellipsis-h"></i></button>
                                <div class="dropdown-menu">
                                    <a href="#" onclick="getRecipeById(${item.recipeId})"><i class="fas fa-edit"></i> Edit</a>
                                    <a href = "#" onclick = "toggleRecipeActive(${item.recipeId})" >
                                        <i class="fas fa-ban"></i>
                                        ${item.isActive ? 'Deactivate' : 'Activate' }
                                   </a>
                                    <a href="#" onclick="exportSingleRecipePDF(${item.recipeId})"><i class="fas fa-file-pdf"></i> Export PDF</a>
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

// Export one recipe to PDF.
function exportSingleRecipePDF(id) {
    ajaxRequest({
        url: `/api/RecipeApi/GetRecipe/${id}`,
        method: 'GET',
        showMessage: false,
        onSuccess: function (response) {
            const recipe = response.data;
            generateRecipePDF([recipe], `Recipe_${sanitizePdfFileName(recipe.name || id)}`);
        },
        onError: function (xhr, status, error) {
            console.error('Failed to export recipe:', error);
            showNotification('Failed to export recipe.', 'error');
        }
    });
}

// Generate recipe PDF. When Employee Facing is checked, pricing is omitted.
async function generateRecipePDF(recipeData, filename) {
    const isEmployeeFacing = document.getElementById('employeeFacingRecipe')?.checked || false;
    const logoPath = getRecipeExportLogoPath();
    let logoBase64 = null;
    debugger
    try {
        logoBase64 = await getImageAsPngBase64(logoPath);
    } catch (e) {
        console.warn('Logo load failed', e);
    }

    const content = [];

    if (!recipeData || recipeData.length === 0) {
        content.push({ text: 'No recipes available to export.', style: 'sectionHeader' });
    }

    for (const [index, recipe] of (recipeData || []).entries()) {
        if (index > 0) content.push({ text: '', pageBreak: 'before' });

        content.push({
            columns: [
                {
                    stack: [
                        { text: (recipe.name || 'Recipe').toUpperCase(), style: 'itemTitle' },
                        { text: `Unit: ${recipe.unit || 'N/A'}`, margin: [0, 2, 0, 0] }
                    ]
                },
                logoBase64 ? { image: logoBase64, width: 60, alignment: 'right' } : { text: '' }
            ],
            margin: [0, 0, 0, 15]
        });

        content.push({ text: 'Direction', style: 'sectionHeader' });
        content.push({
            text: wrapPdfText(recipe.description || 'No Direction'),
            fontSize: 10,
            lineHeight: 1.2,
            margin: [0, 5, 0, isEmployeeFacing ? 20 : 12]
        });

        if (!isEmployeeFacing) {
            content.push({
                table: {
                    widths: ['*', 'auto'],
                    body: [
                        [
                            { text: 'Unit', style: 'tableHeader' },
                            { text: 'Price', style: 'tableHeader' }
                        ],
                        [
                            recipe.unit || 'N/A',
                            formatCurrency(recipe.price)
                        ]
                    ]
                },
                layout: 'lightHorizontalLines',
                margin: [0, 0, 0, 20]
            });
        }

        if (recipe.ingredients && recipe.ingredients.length > 0) {
            content.push({ text: 'Detailed Ingredients List', style: 'sectionHeader' });

            const ingredientRows = recipe.ingredients.map(ingredient => {
                const row = [
                    ingredient.ingredientName || ingredient.name || '---',
                    `${formatQuantity(ingredient.quantity)} ${ingredient.uom || ingredient.Uom || ingredient.unit || ''}`
                ];

                if (!isEmployeeFacing) {
                    row.push(formatCurrency(getRecipeIngredientExportPrice(ingredient)));
                }

                return row;
            });

            const headerRow = [
                { text: 'Ingredient Name', bold: true, fillColor: '#eeeeee' },
                { text: 'Quantity', bold: true, fillColor: '#eeeeee' }
            ];

            if (!isEmployeeFacing) {
                headerRow.push({ text: 'Price', bold: true, fillColor: '#eeeeee' });
            }

            content.push({
                table: {
                    widths: isEmployeeFacing ? ['*', 'auto'] : ['*', 'auto', 'auto'],
                    body: [headerRow, ...ingredientRows]
                },
                margin: [0, 5, 0, 15]
            });
        } else {
            content.push({ text: 'No ingredients added.', italics: true, color: '#6b7280', margin: [0, 5, 0, 15] });
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

    pdfMake.createPdf(docDefinition).download(`${sanitizePdfFileName(filename)}.pdf`);
}

function getRecipeExportLogoPath() {
    const value = localStorage.getItem('profilePic');
    return value && value !== 'null' ? value : '/images/menu optics_logo.png';
}

async function getImageAsPngBase64(url) {
    return new Promise((resolve, reject) => {
        const img = new Image();
        img.crossOrigin = 'anonymous';

        img.onload = function () {
            const targetWidth = 350;
            const aspectRatio = img.naturalHeight / img.naturalWidth;
            const targetHeight = Math.round(targetWidth * aspectRatio);

            const canvas = document.createElement('canvas');
            canvas.width = targetWidth;
            canvas.height = targetHeight;

            const ctx = canvas.getContext('2d');
            ctx.drawImage(img, 0, 0, targetWidth, targetHeight);

            resolve(canvas.toDataURL('image/png'));
        };

        img.onerror = () => reject('Image load failed');
        img.src = url;
    });
}

function formatCurrency(value) {
    const number = parseFloat(value);
    return Number.isFinite(number) ? `$${number.toFixed(2)}` : '$0.00';
}

function getRecipeIngredientExportPrice(ingredient) {
    // Prefer any price/cost returned by the API when present.
    const directPrice = parseFloat(ingredient.price ?? ingredient.cost ?? ingredient.totalPrice ?? ingredient.totalCost);
    if (Number.isFinite(directPrice) && directPrice > 0) {
        return directPrice;
    }

    // GetRecipe/GetAllRecipes usually returns only ingredientId + quantity.
    // Recalculate the line price from the globally loaded ingredient master data.
    const ingredientId = ingredient.ingredientId ?? ingredient.IngredientId ?? ingredient.id;
    const ingredientData = ingredientsWithPrices.find(item => item.id == ingredientId);
    if (!ingredientData) {
        return 0;
    }

    const quantity = parseFloat(ingredient.quantity ?? ingredient.Quantity) || 0;
    if (quantity <= 0) {
        return 0;
    }

    const pricePerStoredUnit = (parseFloat(ingredientData.price) || 0) / (parseFloat(ingredientData.size) || 1);

    // Quantity from the recipe API is normally already stored in ingredientData.unit.
    // If the API provides only the employee/display UOM, convert it before pricing.
    const apiUnit = ingredient.uom || ingredient.Uom || ingredient.unitOfMeasure || ingredient.unit;
    const storedUnit = ingredientData.unit;
    const quantityInStoredUnit = apiUnit && storedUnit && typeof convertUnit === 'function'
        ? convertUnit(quantity, apiUnit, storedUnit)
        : quantity;

    return pricePerStoredUnit * quantityInStoredUnit;
}

function formatQuantity(value) {
    const number = parseFloat(value);
    return Number.isFinite(number) ? number.toFixed(2) : '0.00';
}

function sanitizePdfFileName(value) {
    return (value || 'Recipe_Report').toString().replace(/[^a-z0-9_\-]+/gi, '_').replace(/^_+|_+$/g, '');
}

function wrapPdfText(value) {
    return (value || '').toString().replace(/([^\s]{45})(?=[^\s])/g, '$1\u200B');
}

