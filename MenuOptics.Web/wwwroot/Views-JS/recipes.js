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
let cachedRecipes = [];
let pendingPreloadedIngredientData = {};

/* ── 3. Initialization / 4. Event bindings ────────────────────────────── */

$(document).ready(function () {
    // Initialize Select2 for ingredients
    initializeIngredientsSelect('#ingredientsSelect', '#addRecipeModal', '#ingredientsDetails', '#recipeCost');
    initializeIngredientsSelect('#editIngredientsSelect', '#editRecipeModal', '#editIngredientsDetails', '#editRecipeCost');

    // Initialize DataTable
    initRecipeTable();

    // ── Unit change: dynamically show selected unit badge on Recipe Size label ──
    $('#recipeUnit').on('change', function () {
        updateRecipeSizeUnitBadge('#recipeUnit', '#recipeSizeUnitBadge', '#recipeSize');
    });
    $('#editRecipeUnit').on('change', function () {
        updateRecipeSizeUnitBadge('#editRecipeUnit', '#editRecipeSizeUnitBadge', '#editRecipeSize');
    });
    $('#addRecipeBtn').on('click', function () {
        updateRecipeSizeUnitBadge('#recipeUnit', '#recipeSizeUnitBadge', '#recipeSize');
    });

    // Initialize unit badges on ready
    updateRecipeSizeUnitBadge('#recipeUnit', '#recipeSizeUnitBadge', '#recipeSize');
    updateRecipeSizeUnitBadge('#editRecipeUnit', '#editRecipeSizeUnitBadge', '#editRecipeSize');

    // Event handlers
    $('#saveRecipeBtn').on('click', function () {
        const recipeData = getRecipeFormData('#addRecipeForm', '#ingredientsDetails');
        if (validateRecipeData(recipeData) && isFormValid("#addRecipeForm")) {
            addRecipe(recipeData);
        } else {
            showNotification('Please fill in all required fields.', 'error');
        }
    });

    $('#updateRecipeBtn').on('click', function () {
        const recipeData = getRecipeFormData('#editRecipeForm', '#editIngredientsDetails');
        recipeData.append('recipeId', $('#editRecipeId').val());
        if (validateRecipeData(recipeData) && isFormValid("#editRecipeForm")) {
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
        url: '/api/IngredientApi/GetAllActiveIngredientsByUserId',
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
                packSize: ing.packSize,
                unitOfMeasure: ing.unitOfMeasure || ing.unit, // Display unit
                vendorProductId: ing.vendorProductId
            }));

            if ($(selectId).hasClass('select2-hidden-accessible')) {
                $(selectId).select2('destroy');
            }

            $(selectId).empty(); // existing options clear karo

            $(selectId).select2({
                tags: false,
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
    let existingData = {};
    if (pendingPreloadedIngredientData[detailsId]) {
        existingData = pendingPreloadedIngredientData[detailsId];
        delete pendingPreloadedIngredientData[detailsId];
    } else {
        $(`${detailsId} .ingredient-detail-row`).each(function () {
            const input = $(this).find('input[name^="ingredient_quantity_"]');
            const uomSelect = $(this).find('select[name^="ingredient_uom_"]');
            const ingredientId = input.attr('name').split('_')[2];
            const quantity = input.val();
            if (quantity) existingData[ingredientId] = { quantity: quantity, uom: uomSelect.val() || '' };
        });
    }

    let html = '';
    selected.forEach(id => {
        const ingredient = ingredientsWithPrices.find(ing => ing.id == id);
        if (ingredient) {
            const name = ingredient.text;
            const existing = existingData[id];
            const savedQuantity = existing ? existing.quantity : '';
            const savedUom = (existing && existing.uom) || ingredient.unitOfMeasure || ingredient.unit;
            let uomOptions = (typeof buildUomOptionsHtml === 'function')
                ? buildUomOptionsHtml(ingredient.unit, savedUom)
                : `<option value="${savedUom}" selected>${savedUom}</option>`;

            if (savedUom && !uomOptions.toLowerCase().includes(`value="${savedUom.toLowerCase()}"`)) {
                const uomLabel = (typeof ALLOWED_UNIT_OF_MEASURE_LABELS !== 'undefined' && ALLOWED_UNIT_OF_MEASURE_LABELS[savedUom])
                    ? ALLOWED_UNIT_OF_MEASURE_LABELS[savedUom]
                    : savedUom;
                uomOptions += `<option value="${savedUom}" selected>${uomLabel}</option>`;
            }

            html += `
                <div class="ingredient-detail-row" style="display:flex;align-items:center;gap:8px;margin-bottom:8px;">
                    <span class="ingredient-name" style="min-width:120px;">${name}</span>
                    <span class="unit" style="min-width:80px;">${ingredient.unit}</span>
                    <select name="ingredient_uom_${id}" required>
                        ${uomOptions}
                    </select>
                    <input type="number" step="0.01" name="ingredient_quantity_${id}" value="${savedQuantity}" placeholder="Enter Wt / Qty" style="width:150px;" min="0">
                    
                    <span class="ingredient-price" style="min-width:100px;">Cost: $0.00</span>
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
        const packSize = ingredientData.packSize != null ? ingredientData.packSize : 1;//Pack size will be 1 in case of catch weight item
        const size = ingredientData.packSize != null ? ingredientData.size : 1;//Size will be 1 in case of catch weight item
        const pricePerUnit = (ingredientData.price / packSize) / size;

        const convertedQuantity = (typeof convertUnit === 'function')
            ? convertUnit(quantity, userUnit, ingredientData.unit, ingredientData.size)
            : quantity;
        const price = (pricePerUnit * convertedQuantity).toFixed(2);
        row.find('.ingredient-price').text(`Cost: $${price}`);
    } else {
        row.find('.ingredient-price').text(`Cost: $0.00`);
    }

    updateTotalPrice(detailsId, priceInputId);
}

// Update total price
function updateTotalPrice(detailsId, priceInputId) {
    let total = 0;
    $(`${detailsId} .ingredient-detail-row`).each(function () {
        const priceText = $(this).find('.ingredient-price').text().replace('Cost: $', '');
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
            const storageQuantity = convertUnit(displayQuantity, displayUnit, ingredientData.unit, ingredientData.size);

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
            $('#addRecipeForm')[0].reset();
            updateRecipeSizeUnitBadge('#recipeUnit', '#recipeSizeUnitBadge', '#recipeSize');
            $('#ingredientsSelect').val(null).trigger('change');
            showNotification('Recipe added successfully!', 'success');
            closeModal('addRecipeModal');
        },
        onError: function (xhr, status, error) {
            console.error('Failed to add recipe:', error);
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
            if (recipe.unit) {
                const targetUnit = recipe.unit.toString().trim().toLowerCase();
                let matchedVal = null;
                $('#editRecipeUnit option').each(function () {
                    if ($(this).val() && ($(this).val().toLowerCase() === targetUnit || $(this).text().toLowerCase().startsWith(targetUnit))) {
                        matchedVal = $(this).val();
                        return false;
                    }
                });
                $('#editRecipeUnit').val(matchedVal || recipe.unit);
            } else {
                $('#editRecipeUnit').val('');
            }
            updateRecipeSizeUnitBadge('#editRecipeUnit', '#editRecipeSizeUnitBadge', '#editRecipeSize');

            const initialCost = recipe.cost != null ? parseFloat(recipe.cost).toFixed(2) : '0.00';
            $('#editRecipeCost').val(initialCost);
            $('#editRecipeSize').val(recipe.size);
            $('#editRecipeDescription').val(recipe.description);

            // Preload ingredient quantities & UOMs for synchronous row rendering without zero-cost flash or jerk
            const preloadedData = {};
            (recipe.ingredients || []).forEach(ing => {
                const ingredientData = ingredientsWithPrices.find(item => item.id == ing.ingredientId);
                const userUnit = ing.uom || ing.Uom || ing.unitOfMeasure || (ingredientData ? (ingredientData.unitOfMeasure || ingredientData.unit) : '');
                let displayQuantity = ing.quantity;
                if (ingredientData && typeof convertUnit === 'function') {
                    displayQuantity = convertUnit(ing.quantity, ingredientData.unit, userUnit, ingredientData.size);
                }
                preloadedData[ing.ingredientId] = {
                    quantity: displayQuantity != null ? parseFloat(displayQuantity).toFixed(2) : '',
                    uom: userUnit
                };
            });

            pendingPreloadedIngredientData['#editIngredientsDetails'] = preloadedData;

            // Trigger select2 change to render rows synchronously with quantities and computed prices
            const ingredientIds = (recipe.ingredients || []).map(ing => ing.ingredientId);
            $('#editIngredientsSelect').val(ingredientIds).trigger('change');

            // If calculated cost is 0 (e.g. no ingredients) but recipe had a saved cost, preserve it
            const currentCostVal = parseFloat($('#editRecipeCost').val()) || 0;
            if (currentCostVal === 0 && parseFloat(initialCost) > 0) {
                $('#editRecipeCost').val(initialCost);
            }

            // Open modal only after all inputs, prices, and totals are synchronously rendered
            openModal('editRecipeModal');
            updateRecipeSizeUnitBadge('#editRecipeUnit', '#editRecipeSizeUnitBadge', '#editRecipeSize');
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

// Refresh table
function refreshRecipesTable() {
    ajaxRequest({
        url: '/api/RecipeApi/GetAllRecipes',
        method: 'GET',
        showMessage: false,
        isQuery: false,
        onSuccess: function (response) {
            const recipes = response.data || [];
            cachedRecipes = recipes;
            $('#totalRecipes').text(recipes.length);

            recipeTable.clear();

            recipes.forEach(item => {
                const created = DateFormatter.formatWithComma(item.createdOn);
                recipeDescriptions[item.recipeId] = item.description || '';

                // Get the ingredient names as a comma-separated string
                const ingredientNames = item.ingredients ? item.ingredients.map(ingredient => ingredient.ingredientName).join(', ') : '';


                const cls = item.isActive ? 'good' : 'critical';
                const lbl = item.isActive ? 'Active' : 'In Active';
                const hasPriceChanged = item.cost != item.currentCost && item.ingredients?.some(i => i.oldPrice != null && i.oldPrice != i.currentPrice);
                const isCostHigh = item.currentCost > item.cost;

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
                        <td>
                          <span class="${item.size != null ? 'category-badge-size' : ''}">
                            ${item.size ? `${item.size} ${(item.unit || '')}` : ''}
                          </span>
                        </td>
                        <td>
                         ${!hasPriceChanged
                        ? `
                                  <span>
                                    $${item.cost.toFixed(2)}
                                    <span class="tooltip-icon" data-tooltip="Cost at the time this recipe was created/updated">i</span>
                                  </span>
                                `
                        : `
                                  <div>
                                      <span class="text-highlight danger" title="Cost at the time this recipe was created/updated.">
                                        $${item.cost.toFixed(2)}
                                      </span>
                                      <br>
                                      <span class="text-highlight ${isCostHigh ? `negative` : `positive`}" style="cursor:pointer;" onclick="showRecipeCostBreakdown(${item.recipeId})" title="Click to view ingredient cost breakdown">
                                        $${item.currentCost.toFixed(2)}
                                        <i class="fas fa-info-circle" style="font-size:11px; opacity:0.75; margin-left:2px;"></i>
                                      </span>
                                  </div>
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
                                        ${item.isActive ? 'Deactivate' : 'Activate'}
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
                        { text: (recipe.name || 'Recipe').toUpperCase(), style: 'itemTitle' }
                    ]
                },
                logoBase64 ? { image: logoBase64, width: 60, alignment: 'right' } : { text: '' }
            ],
            margin: [0, 0, 0, 15]
        });

        if (!isEmployeeFacing) {
            content.push({
                table: {
                    widths: ['*', 'auto'],
                    body: [
                        [
                            { text: 'Unit', style: 'tableHeader' },
                            { text: 'Cost', style: 'tableHeader' }
                        ],
                        [
                            (recipe.unit || 'N/A'),
                            formatCurrency(recipe.cost ?? 0)
                        ]
                    ]
                },
                layout: 'lightHorizontalLines',
                margin: [0, 0, 0, 20]
            });
        } else {
            content.push({
                table: {
                    widths: ['*'],
                    body: [
                        [
                            { text: 'Unit', style: 'tableHeader' }
                        ],
                        [
                            (recipe.unit || 'N/A')
                        ]
                    ]
                },
                layout: 'lightHorizontalLines',
                margin: [0, 0, 0, 20]
            });
        }


        content.push({
            table: {
                widths: ['*'],
                body: [
                    [
                        { text: 'Direction', style: 'tableHeader' }
                    ],
                    [
                        {
                            text: wrapPdfText(recipe.description || 'No Direction'),
                            fontSize: 10,
                            lineHeight: 1.2
                        }
                    ]
                ]
            },
            layout: 'lightHorizontalLines',
            margin: [0, 0, 0, 12]
        });

        if (recipe.ingredients && recipe.ingredients.length > 0) {
            content.push({ text: 'Detailed Ingredients List', style: 'sectionHeader' });

            const ingredientRows = recipe.ingredients.map(ingredient => {
                return [
                    ingredient.ingredientName || '---',
                    `${convertUnit(ingredient.quantity, ingredient.unit, ingredient.uom).toFixed(2) + " " + ingredient.uom}`
                ];
            });
            
            const headerRow = [
                { text: 'Ingredient Name', bold: true, fillColor: '#eeeeee' },
                { text: 'Quantity', bold: true, fillColor: '#eeeeee' }
            ];

            
            content.push({
                table: {
                    widths: ['*', 'auto'],
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
    return value && value !== 'null' ? $('#imageBaseUrl').val() + value : '/images/menu optics_logo.png';
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



function getRecipeIngredientExportPrice(ingredient) {
    // Prefer any price/cost returned by the API when present.
    const directPrice = parseFloat(ingredient.currentCost);
    if (Number.isFinite(directPrice) && directPrice > 0) {
        return directPrice;
    }

    // GetRecipe/GetAllRecipes usually returns only ingredientId + quantity.
    // Recalculate the line price from the globally loaded ingredient master data.
    const ingredientId = ingredient.ingredientId;
    const ingredientData = ingredientsWithPrices.find(item => item.id == ingredientId);
    if (!ingredientData) {
        return 0;
    }

    const quantity = parseFloat(ingredient.quantity) || 0;
    if (quantity <= 0) {
        return 0;
    }

    const packSize = ingredientData.packSize != null ? ingredientData.packSize : 1;//Pack size will be 1 in case of catch weight item
    const size = ingredientData.packSize != null ? ingredientData.size : 1;//Size will be 1 in case of catch weight item
    const pricePerUnit = (ingredientData.price / packSize) / size;

    // Quantity from the recipe API is normally already stored in ingredientData.unit.
    // If the API provides only the employee/display UOM, convert it before pricing.
    const apiUnit = ingredient.uom;
    const storedUnit = ingredientData.unit;
    const quantityInStoredUnit = apiUnit && storedUnit && typeof convertUnit === 'function'
        ? convertUnit(quantity, apiUnit, storedUnit, ingredientData ? ingredientData.size : 1)
        : quantity;

    return pricePerUnit * quantityInStoredUnit;
}

function sanitizePdfFileName(value) {
    return (value || 'Recipe_Report').toString().replace(/[^a-z0-9_\-]+/gi, '_').replace(/^_+|_+$/g, '');
}

function wrapPdfText(value) {
    return (value || '').toString().replace(/([^\s]{45})(?=[^\s])/g, '$1\u200B');
}

// ── Helper to open Add Recipe modal and ensure badge is displayed ───────────
function openAddRecipeModal() {
    updateRecipeSizeUnitBadge('#recipeUnit', '#recipeSizeUnitBadge', '#recipeSize');
    openModal('addRecipeModal');
    updateRecipeSizeUnitBadge('#recipeUnit', '#recipeSizeUnitBadge', '#recipeSize');
}

// ── Update selected unit badge next to Recipe Size label ─────────────────────
function updateRecipeSizeUnitBadge(unitSelectSelector, badgeSelector, inputSelector) {
    const $select = $(unitSelectSelector);
    const $badge = $(badgeSelector);
    const $input = $(inputSelector);
    const val = $select.val();

    const selectedOption = $select.find('option:selected');
    const optionText = (selectedOption.length && selectedOption.val()) ? selectedOption.text().trim() : '';

    if (!val || String(val).trim() === '' || !optionText) {
        $badge.html('<i class="fas fa-balance-scale"></i> (Unit)').attr('title', 'Select a unit').show();
        if ($input && $input.length) {
            $input.attr('placeholder', 'Enter Size');
        }
        return;
    }

    let unitCode = '';
    let unitName = '';
    if (optionText.includes(' - ')) {
        const parts = optionText.split(' - ');
        unitCode = parts[0].trim().toUpperCase();
        unitName = parts.slice(1).join(' - ').trim();
    } else {
        unitCode = String(val || optionText).toUpperCase();
        unitName = optionText || unitCode;
    }

    const badgeHtml = unitName && unitName.toUpperCase() !== unitCode
        ? `in <span class="unit-code">${unitCode}</span> - <span class="unit-name">${unitName}</span>`
        : `in <span class="unit-code">${unitCode}</span>`;

    $badge.html(badgeHtml).attr('title', `Selected Unit: ${optionText || unitCode}`).show();

    if ($input && $input.length) {
        $input.attr('placeholder', `Enter Size in ${unitCode}`);
    }
}

// Show recipe cost breakdown modal (uses data already received in GetAllRecipes)
function showRecipeCostBreakdown(recipeId) {
    const $tbody = $('#recipeCostBreakdownTableBody');
    $tbody.empty();
    $('#recipeCostBreakdownEmptyMsg').hide();

    const recipe = cachedRecipes.find(r => r.recipeId == recipeId);
    if (!recipe) {
        return;
    }

    const ingredients = (recipe.ingredients || []).slice();
    const totalCost = ingredients.reduce((sum, ing) => {
        const cp = ing.currentPrice != null ? parseFloat(ing.currentPrice) : (parseFloat(ing.oldPrice) || 0);
        return sum + (isNaN(cp) ? 0 : cp);
    }, 0) || (parseFloat(recipe.currentCost) || parseFloat(recipe.cost) || 0);

    $('#recipeCostBreakdownName').text(recipe.name || 'Recipe');
    $('#recipeCostBreakdownUnit').text(
        `${recipe.unit ? `Unit: ${recipe.unit}` : ''}${recipe.unit && totalCost > 0 ? ' • ' : ''}${totalCost > 0 ? `Total Cost: $${totalCost.toFixed(2)}` : ''}`
    );

    const rows = [];

    if (ingredients.length > 0) {
        const list = ingredients.map(ing => {
            const oldPrice = ing.oldPrice != null ? parseFloat(ing.oldPrice) : (parseFloat(ing.currentPrice) || 0);
            const currentPrice = ing.currentPrice != null ? parseFloat(ing.currentPrice) : oldPrice;
            const costPercent = totalCost > 0 ? (currentPrice / totalCost) * 100 : 0;
            return {
                ...ing,
                oldPriceNum: oldPrice,
                currentPriceNum: currentPrice,
                costPercent: costPercent
            };
        });

        // Sort descending by highest percentage at top
        list.sort((a, b) => b.costPercent - a.costPercent);

        list.forEach(ing => {
            const ingName = ing.ingredientName || (ing.name || `Ingredient #${ing.ingredientId}`);

            const oldPrice = ing.oldPriceNum;
            const currentPrice = ing.currentPriceNum;
            const costPercent = ing.costPercent;

            const diff = currentPrice - oldPrice;
            let trendHtml = '';
            if (diff > 0.005) {
                trendHtml = `<span class="trend-badge up" style="color: #dc2626; background: #fee2e2; font-weight: 600; padding: 3px 8px; border-radius: 6px; display: inline-flex; align-items: center; gap: 4px;"><i class="fas fa-arrow-up"></i> +$${diff.toFixed(2)}</span>`;
            } else if (diff < -0.005) {
                trendHtml = `<span class="trend-badge down" style="color: #16a34a; background: #dcfce7; font-weight: 600; padding: 3px 8px; border-radius: 6px; display: inline-flex; align-items: center; gap: 4px;"><i class="fas fa-arrow-down"></i> -$${Math.abs(diff).toFixed(2)}</span>`;
            } else {
                trendHtml = `<span class="trend-badge neutral" style="color: #6b7280; background: #f3f4f6; font-weight: 500; padding: 3px 8px; border-radius: 6px; display: inline-flex; align-items: center; gap: 4px;"><i class="fas fa-minus"></i> $0.00</span>`;
            }

            const costPercentBadge = `<span style="background: #f0fdf4; color: #166534; border: 1px solid #bbf7d0; font-weight: 600; font-size: 12px; padding: 2px 8px; border-radius: 12px; display: inline-flex; align-items: center;">${costPercent.toFixed(2)}%</span>`;

            rows.push(`
                <tr style="border-bottom: 1px solid #f1f5f9;">
                    <td style="padding: 10px 14px; font-weight: 600; color: #1e293b;">
                        ${ingName}
                    </td>
                    <td style="padding: 10px 14px; color: #475569; font-weight: 500;">
                        $${oldPrice.toFixed(2)}
                    </td>
                    <td style="padding: 10px 14px; color: #1e293b; font-weight: 600;">
                        $${currentPrice.toFixed(2)}
                    </td>
                    <td style="padding: 10px 14px; text-align: center;">
                        ${costPercentBadge}
                    </td>
                    <td style="padding: 10px 14px;">
                        ${trendHtml}
                    </td>
                </tr>
            `);
        });
    }

    if (rows.length === 0) {
        $('#recipeCostBreakdownEmptyMsg').show();
    } else {
        $tbody.html(rows.join(''));
        $('#recipeCostBreakdownEmptyMsg').hide();
    }

    openModal('recipeCostBreakdownModal');
}

window.showRecipeCostBreakdown = showRecipeCostBreakdown;

