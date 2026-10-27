/* ============================================================================
 * Ingredients.js — Restaurant ingredients page (Views/Ingredient/Ingredients.cshtml)
 * Server-side DataTable, CRUD modals
 *
 * Project convention — every Views-JS file follows this section order:
 *   1. Constants  2. State  3. Initialization  4. Event bindings
 *   5. AJAX / CRUD  6. Helpers  7. Utilities
 * Functions stay global: they are referenced by inline onclick handlers.
 * ==========================================================================*/


// ─── Page ready ────────────────────────────────────────────────────────────────
/* ── 3. Initialization / 4. Event bindings ────────────────────────────── */

$(document).ready(function () {

    // ── Type toggle: Pack Item disables Size, Catch Weight disables Pack Size ──
    $('#ingredientType').on('change', function () {
        toggleSizePackSizeByType($(this).val(), '#ingredientSize', '#ingredientPackSize');
    });
    $('#editIngredientType').on('change', function () {
        toggleSizePackSizeByType($(this).val(), '#editIngredientSize', '#editIngredientPackSize');
    });

    // ── Unit change: dynamically show selected unit badge on Size label & Price Per Unit label ───────
    $('#ingredientUnit').on('change', function () {
        updateSizeUnitBadge('#ingredientUnit', '#ingredientSizeUnitBadge', '#ingredientSize');
        if ($('#ingredientType').val() === 'catchWeight') {
            updatePriceLabelByType('catchWeight', false);
        }
    });
    $('#editIngredientUnit').on('change', function () {
        updateSizeUnitBadge('#editIngredientUnit', '#editIngredientSizeUnitBadge', '#editIngredientSize');
        if ($('#editIngredientType').val() === 'catchWeight') {
            updatePriceLabelByType('catchWeight', true);
        }
    });
    $('#addIngredientBtn').on('click', function () {
        updateSizeUnitBadge('#ingredientUnit', '#ingredientSizeUnitBadge', '#ingredientSize');
        updatePriceLabelByType($('#ingredientType').val() || 'packItem', false);
    });

    // Initialize unit badges on ready
    updateSizeUnitBadge('#ingredientUnit', '#ingredientSizeUnitBadge', '#ingredientSize');
    updateSizeUnitBadge('#editIngredientUnit', '#editIngredientSizeUnitBadge', '#editIngredientSize');

    // ── Save new ingredient ───────────────────────────────────────────────────
    $('#addIngredientBtn2').on('click', function () {
        const ingredientData = {
            brand: $('#ingredientBrand').val(),
            name: $('#ingredientName').val(),
            unit: $('#ingredientUnit').val(),
            price: parseFloat($('#ingredientPrice').val()) || 0,
            ingredientType: $('#ingredientType').val(),
            size: $('#ingredientSize').val(),
            packSize: $('#ingredientPackSize').prop('disabled') ? null : Number($('#ingredientPackSize').val()),
            privateVendorId: parseInt($('#ingredientPrivateVendorId').val()) || null,
            vendorProductId: $('#ingredientVendorProductId').val()?.trim() || null
        };
        if (isFormValid('#addIngredientForm')) {

            if (ingredientData.packSize == 0) {
                showNotification('Pack must be greater than 0', 'error');
                return false;
            }

            addIngredient(ingredientData);
        } else {
            showNotification('Please fill in all required fields.', 'error');
        }
    });

    // ── Update ingredient ─────────────────────────────────────────────────────
    $('#updateIngredientBtn').on('click', function () {
        const ingredientData = {
            ingredientId: $('#editIngredientId').val(),
            brand: $('#editIngredientBrand').val(),
            name: $('#editIngredientName').val(),
            unit: $('#editIngredientUnit').val(),
            price: parseFloat($('#editIngredientPrice').val()) || 0,
            ingredientType: $('#editIngredientType').val(),
            size: parseFloat($('#editIngredientSize').val()),
            packSize: $('#editIngredientPackSize').prop('disabled') ? null : Number($('#editIngredientPackSize').val()),
            privateVendorId: parseInt($('#editIngredientPrivateVendorId').val()) || null,
            vendorProductId: $('#editIngredientVendorProductId').val()?.trim() || null
        };
        if (isFormValid('#editIngredientForm')) {

            if (ingredientData.packSize == 0) {

                showNotification('Pack must be greater than 0', 'error');
                return false;
            }

            updateIngredient(ingredientData);
        } else {
            showNotification('Please fill in all required fields.', 'error');
        }
    });

    // ── Export CSV ────────────────────────────────────────────────────────────
    $('#exportCSVBtn').on('click', function () {
        ajaxRequest({
            url: '/api/IngredientApi/GetAllActiveInactiveIngredients',
            method: 'GET',
            showMessage: false,
            onSuccess: function (response) {
                const ingredients = response.data;
                let csvContent = "Name,Size,Physical Count\n";
                ingredients.forEach(ingredient => {
                    const name = ingredient.name.replace(/,/g, ' ');
                    const size = ingredient.size == null ? '' : ingredient.size + ' ' + ingredient.unit || '';
                    csvContent += `"${name}","${size}", ""\n`;
                });
                const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
                const link = document.createElement('a');
                const url = URL.createObjectURL(blob);
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
            url: '/api/IngredientApi/GetAllActiveInactiveIngredients',
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
                const data = ingredients.map(i => [i.name, i.size == null ? '' : i.size + ' ' + i.unit || '', '']);
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
    loadPrivateVendors();
    refreshVendors();
    initIngredientsTable();

});


/* ── 2. State ─────────────────────────────────────────────────────────── */

let table;
let activeSupplierVendorId = ''; // vendorId of the selected supplier, or '' for all
let isPrivateVendor = 0;

/* ── 6. Helpers — server-side DataTable ───────────────────────────────── */

// Pack Item  -> Size & Pack Size both enabled and required
// Catch Weight -> Pack Size disabled (not required); Size forced to 1 and disabled
function toggleSizePackSizeByType(typeValue, sizeSelector, packSizeSelector) {
    const $size = $(sizeSelector);
    const $packSize = $(packSizeSelector);
    const $packSizeRequired = $(packSizeSelector + 'Required');
    const isEdit = sizeSelector.indexOf('edit') !== -1;

    if (typeValue === 'packItem') {
        $size.prop('disabled', false).prop('required', true);
        $packSize.prop('disabled', false).prop('required', true);
        $packSizeRequired.show();
        updatePriceLabelByType('packItem', isEdit);
    } else if (typeValue === 'catchWeight') {
        $packSize.prop('disabled', true).prop('required', false).val('');
        $size.prop('required', true);
        $packSizeRequired.hide();
        updatePriceLabelByType('catchWeight', isEdit);
    } else {
        $size.prop('disabled', false).prop('required', true);
        $packSize.prop('disabled', false).prop('required', false);
        $packSizeRequired.hide();
        updatePriceLabelByType('packItem', isEdit);
    }
}

// ── Update Price Per Case / Price Per [Unit] label and placeholder ───────────
function updatePriceLabelByType(typeValue, isEditModal) {
    const labelSelector = isEditModal ? '#editTypeLabel' : '#typeLabel';
    const inputSelector = isEditModal ? '#editIngredientPrice' : '#ingredientPrice';
    const unitSelector = isEditModal ? '#editIngredientUnit' : '#ingredientUnit';
    if (typeValue === 'catchWeight') {
        const $unit = $(unitSelector);
        const val = $unit.val();
        const selectedOption = $unit.find('option:selected');
        const optionText = selectedOption.text().trim();

        let unitCode = '';
        if (optionText && optionText.includes(' - ')) {
            unitCode = optionText.split(' - ')[0].trim().toUpperCase();
        } else if (val) {
            unitCode = String(val).toUpperCase();
        }

        const unitDisplay = unitCode ? `<span class="price-unit-highlight" title="${optionText || unitCode}">${unitCode}</span>` : 'Unit';
        const placeholderUnit = unitCode || 'Unit';

        $(labelSelector).html(`Price Per ${unitDisplay} <span style="color:red">*</span>`);
        $(inputSelector).attr('placeholder', `Enter Price Per ${placeholderUnit}`);
    } else {
        $(labelSelector).html('Price Per Case <span style="color:red">*</span>');
        $(inputSelector).attr('placeholder', 'Enter Price Per Case');
    }
}

// ── Update selected unit badge next to Size label ─────────────────────────────
function updateSizeUnitBadge(unitSelectSelector, badgeSelector, inputSelector) {
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


function initIngredientsTable() {
    if ($.fn.DataTable.isDataTable('.ingredients-table')) {
        $('.ingredients-table').DataTable().destroy();
    }

    table = $('.ingredients-table').DataTable({
        serverSide: true,
        processing: true,
        loadingMessage: 'Loading ingredients...',
        ordering: false,
        ajax: function (dtParams, callback, settings) {
            // Map DataTables params → our API query params
            const searchValue = dtParams.search ? dtParams.search.value : '';

            // Switch endpoint: vendor-specific when a supplier is selected
            const url = activeSupplierVendorId
                ? `/api/IngredientApi/GetAllIngredientsByVendorId/${activeSupplierVendorId}/${isPrivateVendor ? 'true' : 'false'}`
                : '/api/IngredientApi/GetAllActiveInactiveIngredients';

            $.ajax({
                url: url,
                method: 'GET',
                data: {
                    draw: dtParams.draw,
                    start: dtParams.start,
                    length: dtParams.length,
                    searchValue: searchValue
                },
                success: function (response) {
                    callback({
                        draw: response.draw,
                        recordsTotal: response.recordsTotal,
                        recordsFiltered: response.recordsFiltered,
                        data: response.data || []
                    });
                },
                error: function (xhr, textStatus, errorThrown) {

                    callback({ draw: dtParams.draw, recordsTotal: 0, recordsFiltered: 0, data: [] });
                    if (xhr.status == 402)
                        var $stripe = $('#subscriptioStatus');
                    $stripe.text(xhr.responseJSON.responseMsg).show();
                    return;
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
                               ${row.packSize == null ? `<p class="ingredient-meta text-highlight danger">Catch Weight Item</p>` : `<p class="ingredient-meta text-highlight">Pack: ${row.packSize}</p>`} 
                            </div>`;
                }
            },
            {
                data: 'vendorProductId',
                render: val => `<span class="${val != null ? 'category-badge' : ''}">${val || ''}</span>`
            },
            {
                data: 'size',
                render: (val, type, row) => `<span class="${val != null ? 'category-badge-size' : ''}">${val ? `${val} ${row.unit || ''}` : ''}</span>`
            },
            {
                data: 'price',
                render: function (val, type, row) {
                    return `<span">${row.packSize == null ? ('$' + row.price + ' / ' + row.unit) : ('$' + row.price + ' / Case')}</span>`;
                }
            },
            {
                // Supplier
                data: 'vendorName',
                render: val => val || 'N/A'
            },
            {
                data: 'isActive',
                render: function (active) {
                    const cls = active ? 'good' : 'critical';
                    const lbl = active ? 'Active' : 'In Active';
                    return `<span class="status-badge ${cls}">${lbl}</span>`;
                }
            },


            {

                data: null,
                render: function (data, type, row) {

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
        language: {
            search: '',
            searchPlaceholder: 'Search ingredients...'
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

    // Supplier filter → switch endpoint to GetAllIngredientsByVendorId when a vendor is chosen
    $('#supplierFilter').off('change').on('change', function () {
        activeSupplierVendorId = this.value; // vendorId or '' for all
        isPrivateVendor = Number($(this).find(":selected").data("private"));
        table.ajax.reload();
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

// ─── CRUD Functions ────────────────────────────────────────────────────────────

/* ── 5. AJAX / CRUD ───────────────────────────────────────────────────── */

function addIngredient(ingredientData) {
    ajaxRequest({
        url: '/api/IngredientApi/CreateIngredient',
        method: 'POST',
        data: {
            Name: ingredientData.brand + " " + ingredientData.name,
            Price: ingredientData.price,
            Unit: ingredientData.unit,
            Size: ingredientData.size,
            PackSize: ingredientData.packSize,
            PrivateVendorId: ingredientData.privateVendorId,
            VendorProductId: ingredientData.vendorProductId
        },
        onSuccess: function () {
            refreshIngredientsTable();
            $('#addIngredientForm')[0].reset();
            updateSizeUnitBadge('#ingredientUnit', '#ingredientSizeUnitBadge', '#ingredientSize');
            toggleSizePackSizeByType('', '#ingredientSize', '#ingredientPackSize');
            closeModal('addIngredientModal');
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
            var ingName = ingredient.name.trim().includes(' ') ? ingredient.name.split(' ').slice(1).join(' ') : ingredient.name
            var ingBrand = ingredient.name.split(' ')[0]


            $('#editIngredientId').val(ingredient.ingredientId);
            $('#editIngredientBrand').val(ingBrand);
            $('#editIngredientName').val(ingName);
            if (ingredient.unit) {
                const targetUnit = ingredient.unit.toString().trim().toLowerCase();
                let matchedVal = null;
                $('#editIngredientUnit option').each(function () {
                    if ($(this).val() && ($(this).val().toLowerCase() === targetUnit || $(this).text().toLowerCase().startsWith(targetUnit))) {
                        matchedVal = $(this).val();
                        return false;
                    }
                });
                $('#editIngredientUnit').val(matchedVal || ingredient.unit);
            } else {
                $('#editIngredientUnit').val('');
            }
            updateSizeUnitBadge('#editIngredientUnit', '#editIngredientSizeUnitBadge', '#editIngredientSize');
            $('#editIngredientPrice').val(ingredient.price);
            $('#editIngredientSize').val(ingredient.size);
            $('#editIngredientPackSize').val(ingredient.packSize);
            $('#editIngredientVendorProductId').val(ingredient.vendorProductId || '');
            // No Type column from API — derive it from packSize instead
            const ingredientType = (ingredient.packSize != null) ? 'packItem' : 'catchWeight';
            $('#editIngredientType').val(ingredientType).prop("disabled", true);
            toggleSizePackSizeByType(ingredientType, '#editIngredientSize', '#editIngredientPackSize');

            // Pre-select saved private vendor
            if (ingredient.privateVendorId) {
                $('#editIngredientPrivateVendorId').val(ingredient.privateVendorId);
            } else {
                $('#editIngredientPrivateVendorId').val('');
            }

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
        }
    });
}

function updateIngredient(ingredientData) {
    ajaxRequest({
        url: '/api/IngredientApi/UpdateIngredient',
        method: 'PUT',
        data: {
            IngredientId: ingredientData.ingredientId,
            Name: ingredientData.brand + " " + ingredientData.name,
            Price: ingredientData.price,
            Unit: ingredientData.unit,
            Size: ingredientData.size,
            PackSize: ingredientData.packSize,
            PrivateVendorId: ingredientData.privateVendorId,
            VendorProductId: ingredientData.vendorProductId
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



// ─── Private Vendor (Supplier) dropdown ───────────────────────────────────────

/**
 * Fetches the restaurant's own private vendors (Suppliers) and populates
 * the Supplier <select> in both the Add and Edit ingredient modals.
 * Only active vendors are shown.
 */
function loadPrivateVendors() {
    ajaxRequest({
        url: `/api/PrivateVendorApi/GetAllPrivateVendors/${true}`,
        method: 'GET',
        showMessage: false,
        onSuccess: function (response) {
            const vendors = response.data || [];
            const $addSelect = $('#ingredientPrivateVendorId');

            // Clear existing options (except placeholder)
            $addSelect.find('option:not(:first)').remove();

            if (vendors.length === 0) {
                const noOpt = '<option value="" disabled>No suppliers found</option>';
                $addSelect.append(noOpt);
                return;
            }

            vendors.forEach(function (v) {
                const opt = `<option value="${v.privateVendorId}">${v.name}</option>`;
                $addSelect.append(opt);
            });
        },
        onError: function () {
            console.error('Failed to load private vendors (suppliers).');
        }
    });
}

// ─── Vendor management ─────────────────────────────────────────────────────────

function refreshVendors() {
    ajaxRequest({
        url: '/api/UserApi/GetAllVendors',
        method: 'GET',
        showMessage: false,
        onSuccess: function (response) {
            const vendors = response.data;

            // ── Populate supplier filter dropdown ──────────
            const $supplierFilter = $('#supplierFilter');
            const currentVal = $supplierFilter.val();
            $supplierFilter.find('option:not(:first)').remove();
            vendors.forEach(vendor => {
                $supplierFilter.append(
                    `<option value="${vendor.id}" data-private="${vendor.isPrivate}">${vendor.name}</option>`
                );
            });
            // Restore selection if still valid, otherwise reset
            if (currentVal && $supplierFilter.find(`option[value="${currentVal}"]`).length) {
                $supplierFilter.val(currentVal);
            } else {
                $supplierFilter.val('');
            }
        },
        onError: function () {
            showNotification('Failed to fetch vendors.', 'error');
        }
    });
}