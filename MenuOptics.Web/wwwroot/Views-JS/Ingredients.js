// Ingredients.js — Restaurant ingredients page scripts.

let table;
let activeSupplierVendorId = ''; // '' means all suppliers
let isPrivateVendor = 0;

$(document).ready(function () {
    $('#ingredientType').on('change', function () {
        toggleSizePackSizeByType($(this).val(), '#ingredientSize', '#ingredientPackSize');
    });
    $('#editIngredientType').on('change', function () {
        toggleSizePackSizeByType($(this).val(), '#editIngredientSize', '#editIngredientPackSize');
    });

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

    updateSizeUnitBadge('#ingredientUnit', '#ingredientSizeUnitBadge', '#ingredientSize');
    updateSizeUnitBadge('#editIngredientUnit', '#editIngredientSizeUnitBadge', '#editIngredientSize');

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

    loadPrivateVendors();
    refreshVendors();
    initIngredientsTable();
});

function loadPrivateVendors() {
    ajaxRequest({
        url: `/api/PrivateVendorApi/GetAllPrivateVendors/${true}`,
        method: 'GET',
        showMessage: false,
        onSuccess: function (response) {
            const vendors = response.data || [];
            const $addSelect = $('#ingredientPrivateVendorId');

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

function refreshVendors() {
    ajaxRequest({
        url: '/api/UserApi/GetAllVendors',
        method: 'GET',
        showMessage: false,
        onSuccess: function (response) {
            const vendors = response.data;

            const $supplierFilter = $('#supplierFilter');
            const currentVal = $supplierFilter.val();
            $supplierFilter.find('option:not(:first)').remove();
            vendors.forEach(vendor => {
                $supplierFilter.append(
                    `<option value="${vendor.id}" data-private="${vendor.isPrivate}">${vendor.name}</option>`
                );
            });
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

function initIngredientsTable() {
    table = initDataTable('.ingredients-table', {
        serverSide: true,
        processing: true,
        loadingMessage: 'Loading ingredients...',
        ajax: function (dtParams, callback, settings) {
            const searchValue = dtParams.search ? dtParams.search.value : '';

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
                    const priceText = row.packSize == null ? ('$' + row.price + ' / ' + row.unit) : ('$' + row.price + ' / Case');
                    const encodedName = encodeURIComponent(row.name || '');
                    return `<div style="display:inline-flex; align-items:center; gap:6px;">
                                <span>${priceText}</span>
                                <button type="button" class="history-price-icon-btn view-history-btn" 
                                    data-ingredient-id="${row.ingredientId}" 
                                    data-ingredient-name="${encodedName}" 
                                    data-ingredient-price="${row.price ?? 0}"
                                    title="View Pricing History">
                                    <i class="fas fa-chart-line"></i>
                                </button>
                            </div>`;
                }
            },
            {
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
                orderable: false,
                render: function (data, type, row) {

                    const actionLink = (row.vendorId > 0)
                        ? ''
                        : `
                        <div class="action-cell" style="display:flex; align-items:center; justify-content:flex-end; gap:6px;">
                            <div class="dropdown">
                                <button class="dropdown-toggle" title="More Options">
                                    <i class="fas fa-ellipsis-h"></i>
                                </button>
                                <div class="dropdown-menu">
                                     <a href="#" onclick="toggleIngredientActive(${row.ingredientId})">
                           <i class="fas fa-ban"></i>
                           ${row.isActive ? 'Deactivate' : 'Activate'}
                       </a>
                       <a href="#" onclick="editIngredient(${row.ingredientId})">
                           <i class="fas fa-edit"></i> Edit Details
                       </a>
                                </div>
                            </div>
                        </div>
                       `;

                    return actionLink;
                }
            }
        ],
        language: {
            searchPlaceholder: 'Search ingredients...'
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

    $('#supplierFilter').off('change').on('change', function () {
        activeSupplierVendorId = this.value;
        isPrivateVendor = Number($(this).find(":selected").data("private"));
        table.ajax.reload();
    });
}

function refreshIngredientsTable() {
    if (table) {
        table.ajax.reload(null, false); // false = keep current page
    } else {
        initIngredientsTable();
    }
}

function addIngredient(ingredientData) {
    const $btn = $('#addIngredientBtn2');
    setBtnLoading($btn, true);
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
        },
        onComplete: function () {
            setBtnLoading($btn, false);
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
            // API returns no type field, so derive it from packSize
            const ingredientType = (ingredient.packSize != null) ? 'packItem' : 'catchWeight';
            $('#editIngredientType').val(ingredientType).prop("disabled", true);
            toggleSizePackSizeByType(ingredientType, '#editIngredientSize', '#editIngredientPackSize');

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
    const $btn = $('#updateIngredientBtn');
    setBtnLoading($btn, true);
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
        },
        onComplete: function () {
            setBtnLoading($btn, false);
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

/* ==========================================================================
   Ingredient Pricing History Modal & Charts Switching
   ========================================================================== */

let currentHistoryData = [];
let currentIngredientBaselinePrice = 0;
let historyChartInstances = {
    trend: null
};

// Delegated click event to open pricing history modal
$(document).on('click', '.view-history-btn', function (e) {
    e.preventDefault();
    e.stopPropagation();
    $('.dropdown-menu').removeClass('show');

    const ingredientId = $(this).data('ingredient-id');
    const currentPrice = parseFloat($(this).data('ingredient-price')) || 0;
    const rawName = $(this).data('ingredient-name');
    let ingredientName = '';
    try {
        ingredientName = decodeURIComponent(rawName || '');
    } catch {
        ingredientName = rawName || '';
    }

    openIngredientPricingHistoryModal(ingredientId, ingredientName, currentPrice);
});

// Chart tabs switching
$(document).on('click', '.history-chart-tabs .tab-btn', function () {
    const tabKey = $(this).data('chart-tab');
    switchHistoryTab(tabKey);
});

// Destroy charts when modal is closed
$('#ingredientPricingHistoryModal').on('hidden', function () {
    destroyHistoryCharts();
});
$('#ingredientPricingHistoryModal .modal-close, #ingredientPricingHistoryModal [onclick*="closeModal"]').on('click', function () {
    destroyHistoryCharts();
});

function destroyHistoryCharts() {
    Object.keys(historyChartInstances).forEach(function (key) {
        if (historyChartInstances[key]) {
            historyChartInstances[key].destroy();
            historyChartInstances[key] = null;
        }
    });
}

function openIngredientPricingHistoryModal(ingredientId, ingredientName, currentPrice) {
    destroyHistoryCharts();
    currentHistoryData = [];
    currentIngredientBaselinePrice = (typeof currentPrice === 'number' && !isNaN(currentPrice)) ? currentPrice : 0;

    $('#historyModalIngredientName').text(ingredientName ? `${ingredientName} — Pricing History` : 'Ingredient Pricing History');
    $('#historyModalIngredientMeta').text('Historical pricing records for this ingredient');

    $('#historyLoadingState').show();
    $('#historyEmptyState').hide();
    $('#historyEmptyBanner').hide();
    $('#historyContentContainer').hide();

    // Reset tabs to default (Trend)
    $('.history-chart-tabs .tab-btn').removeClass('active');
    $('.history-chart-tabs .tab-btn[data-chart-tab="trend"]').addClass('active');
    $('.history-tab-pane').hide();
    $('#paneTrend').show();

    openModal('ingredientPricingHistoryModal');

    ajaxRequest({
        url: `/api/IngredientApi/GetIngredientsPricingHistory/${ingredientId}`,
        method: 'GET',
        showMessage: false,
        onSuccess: function (response) {
            $('#historyLoadingState').hide();
            const rawData = (response && response.data) ? response.data : [];
            if (!Array.isArray(rawData) || rawData.length === 0) {
                // If data is empty, show empty state with current baseline price
                handleEmptyHistoryData(ingredientName, currentIngredientBaselinePrice);
                return;
            }

            $('#historyEmptyBanner').hide();
            $('#historyContentContainer').show();

            // Normalize and sort chronologically (oldest to newest)
            currentHistoryData = rawData.map(item => ({
                id: item.id ?? item.Id ?? 0,
                ingredientId: item.ingredientId ?? item.IngredientId ?? ingredientId,
                restaurantId: item.restaurantId ?? item.RestaurantId ?? 0,
                price: Number(item.price ?? item.Price ?? 0),
                timestamp: item.timestamp ?? item.Timestamp ?? item.createdOn ?? item.CreatedOn ?? new Date()
            })).sort((a, b) => new Date(a.timestamp) - new Date(b.timestamp));

            populateHistoryStats(currentHistoryData);
            populateHistoryTable(currentHistoryData);
            renderHistoryChart(currentHistoryData);
        },
        onError: function (xhr) {
            $('#historyLoadingState').hide();
            // In case of error (e.g. no records or 400), gracefully show empty state with current baseline price
            handleEmptyHistoryData(ingredientName, currentIngredientBaselinePrice);
        }
    });
}

function handleEmptyHistoryData(ingredientName, baselinePrice) {
    $('#historyEmptyState').hide();
    $('#historyContentContainer').show();
    $('#historyEmptyBanner').show();

    if (baselinePrice > 0) {
        $('#historyEmptyBannerTitle').text('No Historical Price Changes Logged');
        $('#historyEmptyBannerMsg').text(`This ingredient currently has a baseline price of $${baselinePrice.toFixed(2)}, but no price change history has been logged yet. Showing the current baseline price.`);

        // Stat cards showing the baseline price
        $('#historyStatCurrent').text(`$${baselinePrice.toFixed(2)}`);
        $('#historyStatChangeBadge').removeClass('positive negative').css('color', '#6b7280').html('<i class="fas fa-info-circle"></i> Baseline price (No updates yet)');
        $('#historyStatLowest').text(`$${baselinePrice.toFixed(2)}`);
        $('#historyStatLowestDate').text('Current baseline');
        $('#historyStatHighest').text(`$${baselinePrice.toFixed(2)}`);
        $('#historyStatHighestDate').text('Current baseline');
        $('#historyStatAverage').text(`$${baselinePrice.toFixed(2)}`);
        $('#historyStatCount').text('0 updates recorded');

        currentHistoryData = [{
            id: 0,
            ingredientId: 0,
            restaurantId: 0,
            price: baselinePrice,
            timestamp: new Date().toISOString(),
            isBaselineOnly: true
        }];

        populateHistoryTable(currentHistoryData, true);
        renderHistoryChart(currentHistoryData);
    } else {
        // Completely empty (no baseline price recorded at all)
        $('#historyEmptyBannerTitle').text('No Pricing History Available');
        $('#historyEmptyBannerMsg').text('There are no recorded pricing records for this ingredient yet.');

        $('#historyStatCurrent').text('$0.00');
        $('#historyStatChangeBadge').removeClass('positive negative').css('color', '#6b7280').html('<i class="fas fa-info-circle"></i> No price data');
        $('#historyStatLowest').text('$0.00');
        $('#historyStatLowestDate').text('--');
        $('#historyStatHighest').text('$0.00');
        $('#historyStatHighestDate').text('--');
        $('#historyStatAverage').text('$0.00');
        $('#historyStatCount').text('0 updates recorded');

        currentHistoryData = [];
        populateHistoryTable([], true);
        renderHistoryChart([]);
    }
}

function switchHistoryTab(tabKey) {
    $('.history-chart-tabs .tab-btn').removeClass('active');
    $(`.history-chart-tabs .tab-btn[data-chart-tab="${tabKey}"]`).addClass('active');

    $('.history-tab-pane').hide();
    const targetPaneId = '#pane' + tabKey.charAt(0).toUpperCase() + tabKey.slice(1);
    $(targetPaneId).show();

    if (tabKey === 'trend') {
        renderHistoryChart(currentHistoryData);
    }
}

function renderHistoryChart(data) {
    // If chart already exists for trend tab, resize and update
    if (historyChartInstances.trend) {
        historyChartInstances.trend.resize();
        historyChartInstances.trend.update();
        return;
    }

    const hasData = Array.isArray(data) && data.length > 0;
    const isBaselineOnly = hasData && data.length === 1 && data[0].isBaselineOnly;

    const ctx = document.getElementById('historyChartTrend');
    const placeholder = document.getElementById('trendEmptyPlaceholder');
    if (!ctx) return;

    if (!hasData) {
        ctx.style.display = 'none';
        if (placeholder) placeholder.style.display = 'flex';
        return;
    }

    ctx.style.display = 'block';
    if (placeholder) placeholder.style.display = 'none';

    const labels = data.map((item, idx) => isBaselineOnly ? 'Current Baseline' : formatChartLabel(item, data));
    const labelsWithTime = data.map(item => isBaselineOnly ? 'Current Baseline Price' : formatChartTooltipLabel(item));
    const prices = data.map(item => item.price);

    historyChartInstances.trend = new Chart(ctx, {
        type: 'line',
        data: {
            labels: labels,
            datasets: [{
                label: 'Price ($)',
                data: prices,
                borderColor: '#f08d11',
                backgroundColor: 'rgba(240, 141, 17, 0.12)',
                borderWidth: 2.5,
                pointBackgroundColor: '#f08d11',
                pointBorderColor: '#ffffff',
                pointBorderWidth: 2,
                pointRadius: 6,
                pointHoverRadius: 8,
                tension: 0.3,
                fill: true
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: { display: false },
                tooltip: {
                    callbacks: {
                        title: function (items) {
                            const idx = items[0].dataIndex;
                            return labelsWithTime[idx] || items[0].label;
                        },
                        label: function (context) {
                            const idx = context.dataIndex;
                            const val = context.parsed.y;
                            let text = `Price: $${val.toFixed(2)}`;
                            if (isBaselineOnly) {
                                text += ' (Current Baseline)';
                            } else if (idx > 0) {
                                const prev = prices[idx - 1];
                                const diff = val - prev;
                                const pct = prev > 0 ? ((diff / prev) * 100).toFixed(1) : 0;
                                const sign = diff >= 0 ? '+' : '';
                                text += ` (${sign}$${diff.toFixed(2)}, ${sign}${pct}%)`;
                            } else {
                                text += ' (Initial Baseline)';
                            }
                            return text;
                        }
                    }
                }
            },
            scales: {
                y: {
                    beginAtZero: false,
                    ticks: {
                        callback: val => '$' + Number(val).toFixed(2)
                    },
                    grid: { color: '#f3f4f6' }
                },
                x: {
                    grid: { display: false }
                }
            }
        }
    });
}

function populateHistoryStats(data) {
    if (!data || data.length === 0) return;

    const currentItem = data[data.length - 1];
    const initialItem = data[0];
    const currentPrice = currentItem.price;
    const initialPrice = initialItem.price;

    let lowestPrice = Number.POSITIVE_INFINITY;
    let lowestItem = null;
    let highestPrice = Number.NEGATIVE_INFINITY;
    let highestItem = null;
    let sum = 0;

    data.forEach(item => {
        sum += item.price;
        if (item.price < lowestPrice) {
            lowestPrice = item.price;
            lowestItem = item;
        }
        if (item.price > highestPrice) {
            highestPrice = item.price;
            highestItem = item;
        }
    });

    const avgPrice = sum / data.length;
    const netDiff = currentPrice - initialPrice;
    const netPct = initialPrice > 0 ? ((netDiff / initialPrice) * 100) : 0;

    $('#historyStatCurrent').text(`$${currentPrice.toFixed(2)}`);

    const $badge = $('#historyStatChangeBadge');
    if (data.length === 1) {
        $badge.removeClass('positive negative').css('color', '#6b7280').html('<i class="fas fa-info-circle"></i> Initial baseline');
    } else if (netDiff > 0) {
        $badge.removeClass('positive').addClass('negative').html(`<i class="fas fa-arrow-up"></i> +$${netDiff.toFixed(2)} (+${netPct.toFixed(1)}%) from start`);
    } else if (netDiff < 0) {
        $badge.removeClass('negative').addClass('positive').html(`<i class="fas fa-arrow-down"></i> -$${Math.abs(netDiff).toFixed(2)} (${netPct.toFixed(1)}%) from start`);
    } else {
        $badge.removeClass('positive negative').css('color', '#6b7280').html('<i class="fas fa-minus"></i> No net change ($0.00)');
    }

    $('#historyStatLowest').text(`$${lowestPrice.toFixed(2)}`);
    $('#historyStatLowestDate').text(lowestItem ? formatChartTooltipLabel(lowestItem) : '--');

    $('#historyStatHighest').text(`$${highestPrice.toFixed(2)}`);
    $('#historyStatHighestDate').text(highestItem ? formatChartTooltipLabel(highestItem) : '--');

    $('#historyStatAverage').text(`$${avgPrice.toFixed(2)}`);
    $('#historyStatCount').text(`${data.length} recorded update${data.length > 1 ? 's' : ''}`);
}

function populateHistoryTable(data, isBaselineOnly) {
    const $tbody = $('#historyTableBody');
    $tbody.empty();

    if (!data || data.length === 0) {
        $tbody.html(`
            <tr>
                <td colspan="6" style="text-align:center; padding:40px 20px; color:#6b7280;">
                    <div style="width:48px; height:48px; border-radius:50%; background:#f3f4f6; display:flex; align-items:center; justify-content:center; margin:0 auto 12px;">
                        <i class="fas fa-history" style="font-size:22px; color:#9ca3af;"></i>
                    </div>
                    <strong style="color:#374151; font-size:14px; display:block; margin-bottom:4px;">No Pricing History Available</strong>
                    <span style="font-size:13px; color:#6b7280;">There are no recorded price updates for this ingredient yet.</span>
                </td>
            </tr>
        `);
        return;
    }

    if (isBaselineOnly || (data.length === 1 && data[0].isBaselineOnly)) {
        $tbody.html(`
            <tr>
                <td style="color:#6b7280; font-weight:500;">1</td>
                <td style="font-weight:500; color:#111827;">Current Baseline</td>
                <td style="font-weight:600; color:#111827;">$${data[0].price.toFixed(2)}</td>
                <td style="color:#6b7280;">--</td>
                <td style="color:#6b7280;">--</td>
                <td><span class="status-badge" style="background:#e0f2fe; color:#0284c7; border: 1px solid #bae6fd;"><i class="fas fa-tag"></i> Current Baseline</span></td>
            </tr>
            <tr>
                <td colspan="6" style="text-align:center; padding:18px; color:#6b7280; background:#f9fafb; font-size:12px;">
                    <i class="fas fa-info-circle" style="color:#9ca3af; margin-right:4px;"></i> No past price revisions logged. Changes made to this ingredient's price will automatically appear in this history log.
                </td>
            </tr>
        `);
        return;
    }

    // Show newest first in table view
    const displayList = data.slice().reverse();

    displayList.forEach((item, idx) => {
        const originalIndex = data.indexOf(item);
        const prevItem = originalIndex > 0 ? data[originalIndex - 1] : null;

        let diffText = '--';
        let pctText = '--';
        let trendBadge = '';

        if (!prevItem) {
            trendBadge = '<span class="status-badge" style="background:#e0f2fe; color:#0284c7; border: 1px solid #bae6fd;"><i class="fas fa-play"></i> Initial</span>';
        } else {
            const diff = item.price - prevItem.price;
            const pct = prevItem.price > 0 ? ((diff / prevItem.price) * 100) : 0;
            const sign = diff >= 0 ? '+' : '';

            diffText = `<span class="${diff > 0 ? 'text-danger' : (diff < 0 ? 'text-success' : '')}">${sign}$${diff.toFixed(2)}</span>`;
            pctText = `<span class="${diff > 0 ? 'text-danger' : (diff < 0 ? 'text-success' : '')}">${sign}${pct.toFixed(1)}%</span>`;

            if (diff > 0) {
                trendBadge = `<span class="status-badge critical"><i class="fas fa-arrow-up"></i> Increased</span>`;
            } else if (diff < 0) {
                trendBadge = `<span class="status-badge good"><i class="fas fa-arrow-down"></i> Decreased</span>`;
            } else {
                trendBadge = `<span class="status-badge" style="background:#f3f4f6; color:#4b5563;"><i class="fas fa-minus"></i> Unchanged</span>`;
            }
        }

        const dateStr = formatChartTooltipLabel(item);

        const row = `
            <tr>
                <td style="color:#6b7280; font-weight:500;">${originalIndex + 1}</td>
                <td style="font-weight:500; color:#111827;">${dateStr}</td>
                <td style="font-weight:600; color:#111827;">$${item.price.toFixed(2)}</td>
                <td>${diffText}</td>
                <td>${pctText}</td>
                <td>${trendBadge}</td>
            </tr>
        `;
        $tbody.append(row);
    });
}

function formatChartLabel(item, allItems) {
    if (!item || !item.timestamp) return '';
    const d = new Date(item.timestamp);
    if (isNaN(d.getTime())) return '';

    let datePart = '';
    if (window.DateFormatter && DateFormatter.formatWithComma) {
        datePart = DateFormatter.formatWithComma(item.timestamp);
    } else {
        datePart = d.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
    }

    if (allItems && allItems.length > 1) {
        const countOnSameDate = allItems.filter(x => {
            const xd = new Date(x.timestamp);
            return xd.getFullYear() === d.getFullYear() && xd.getMonth() === d.getMonth() && xd.getDate() === d.getDate();
        }).length;

        if (countOnSameDate > 1) {
            const timePart = d.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
            return `${datePart} ${timePart}`;
        }
    }
    return datePart;
}

function formatChartTooltipLabel(item) {
    if (!item || !item.timestamp) return '';
    if (window.DateFormatter && DateFormatter.formatUSWithTime) {
        return DateFormatter.formatUSWithTime(item.timestamp);
    }
    const d = new Date(item.timestamp);
    if (isNaN(d.getTime())) return '';
    return d.toLocaleString('en-US', {
        month: 'short',
        day: '2-digit',
        year: 'numeric',
        hour: '2-digit',
        minute: '2-digit',
        hour12: true
    });
}
