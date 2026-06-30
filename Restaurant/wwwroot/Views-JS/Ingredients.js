let table;

$(document).ready(function () {
    $('#addIngredientBtn2').on('click', function () {
        const ingredientData = {
            name: $('#ingredientName').val(),
            description: $('#ingredientDescription').val(),
            unit: $('#ingredientUnit').val(),
            unitOfMeasure: $('#ingredientUnit').val(),
            price: parseFloat($('#ingredientPrice').val()) || 0,
            stock: $('#ingredientStock').val() || 0
        };
        debugger
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
            unitOfMeasure: $('#editIngredientUnitOfMeasure').val(),
            price: parseFloat($('#editIngredientPrice').val()) || 0,
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



    // Export CSV Function
    $('#exportCSVBtn').on('click', function () {
        ajaxRequest({
            url: '/api/IngredientApi/GetAllIngredientsByUserId',
            method: 'GET',
            showMessage: false,
            onSuccess: function (response) {
                const ingredients = response.data;

                // CSV Headers
                let csvContent = "Name,Unit,UOM,Physical Count\n";

                // Add data rows
                ingredients.forEach(ingredient => {
                    const name = ingredient.name.replace(/,/g, ' '); // Remove commas
                    const unit = ingredient.unit || '';
                    const uom = ingredient.unitOfMeasure || '';
                    const physicalCount = ''; // Blank column

                    csvContent += `"${name}","${unit}","${uom}","${physicalCount}"\n`;
                });

                // Create blob and download
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
            onError: function (xhr, status, error) {
                console.error('Failed to export CSV:', error);
                showNotification('Failed to export CSV.', 'error');
            }
        });
    });

    // Export PDF Function
    $('#exportPdfBtn').on('click', function () {
        ajaxRequest({
            url: '/api/IngredientApi/GetAllIngredientsByUserId',
            method: 'GET',
            showMessage: false,
            onSuccess: function (response) {
                const ingredients = response.data;

                // Create PDF using jsPDF
                const { jsPDF } = window.jspdf;
                const doc = new jsPDF();

                // Title
                doc.setFontSize(18);
                doc.text('Ingredients List', 14, 20);

                // Date
                doc.setFontSize(10);
                doc.text(`Generated on: ${new Date().toLocaleDateString()}`, 14, 28);

                // Table headers and data
                const headers = [['Name', 'Unit', 'UOM', 'Physical Count']];
                const data = ingredients.map(ingredient => [
                    ingredient.name,
                    ingredient.unit || '',
                    ingredient.unitOfMeasure || '',
                    '' // Blank Physical Count column
                ]);

                // Add table using autoTable plugin
                doc.autoTable({
                    head: headers,
                    body: data,
                    startY: 35,
                    theme: 'grid',
                    styles: {
                        fontSize: 9,
                        cellPadding: 3
                    },
                    headStyles: {
                        fillColor: [41, 128, 185],
                        textColor: 255,
                        fontStyle: 'bold'
                    },
                    columnStyles: {
                        0: { cellWidth: 70 },  // Name
                        1: { cellWidth: 30 },  // Unit
                        2: { cellWidth: 30 },  // UOM
                        3: { cellWidth: 50 }   // Physical Count
                    }
                });

                // Save PDF
                doc.save(`Ingredients_${new Date().toISOString().slice(0, 10)}.pdf`);

                showNotification('PDF exported successfully!', 'success');
            },
            onError: function (xhr, status, error) {
                console.error('Failed to export PDF:', error);
                showNotification('Failed to export PDF.', 'error');
            }
        });
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
            Unit: ingredientData.unit,
            UnitOfMeasure: ingredientData.unitOfMeasure,
            Stock: ingredientData.stock
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
    $('.form-group.editIngredientStock').hide();
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
            $('#editIngredientUnitOfMeasure').val(ingredient.unitOfMeasure);
            $('#editIngredientPrice').val(ingredient.price);

            if (ingredient.vendorId > 0) {

                // Readonly fields
                $('#editIngredientName').prop('disabled', true);
                $('#editIngredientUnit').prop('disabled', true);
                $('#editIngredientDescription').prop('disabled', true);

                // Price hide
                $('#editIngredientPrice').closest('.form-group').hide(); 
            }
            else {

                // Sab fields normal
                $('#editIngredientName').prop('disabled', false);
                $('#editIngredientDescription').prop('disabled', false);
                $('#editIngredientUnit').prop('disabled', false);
                $('#editIngredientUnitOfMeasure').prop('disabled', false);

                $('#editIngredientPrice').closest('.form-group').show();
            }

            
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
            Unit: ingredientData.unit,
            UnitOfMeasure: ingredientData.unitOfMeasure
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

// Function to toggle category active status
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
            }
        });
    }
}


// Bulk delete ingredients
function bulkDeleteIngredients() {
    const selectedIds = $('.ingredient-checkbox:checked').map(function () {
        return $(this).data('id');
    }).get();

    if (selectedIds.length === 0) {
        showNotification('Please select at least one ingredient to delete.', 'error');
        return;
    }

    if (confirm(`Are you sure you want to delete ${selectedIds.length} selected ingredient(s)?`)) {
        let completedRequests = 0;
        let errorOccurred = false;

        selectedIds.forEach(id => {
            ajaxRequest({
                url: `/api/IngredientApi/DeleteIngredientById/${id}`,
                method: 'DELETE',
                showMessage: false,
                onSuccess: function (response) {
                    completedRequests++;
                    if (completedRequests === selectedIds.length) {
                        refreshIngredientsTable();
                        showNotification(`${selectedIds.length} ingredient(s) deleted successfully.`, 'success');
                    }
                },
                onError: function (xhr, status, error) {
                    completedRequests++;
                    errorOccurred = true;
                    console.error(`Failed to delete ingredient ${id}:`, error);
                    if (completedRequests === selectedIds.length) {
                        refreshIngredientsTable();
                        showNotification(errorOccurred ? 'Some ingredients could not be deleted.' : `${selectedIds.length} ingredient(s) deleted successfully.`, errorOccurred ? 'error' : 'success');
                    }
                }
            });
        });
    }
}

// Bulk toggle ingredients
function bulkToggleIngredients() {
    const selectedIds = $('.ingredient-checkbox:checked').map(function () {
        return $(this).data('id');
    }).get();

    if (selectedIds.length === 0) {
        showNotification('Please select at least one ingredient to toggle.', 'error');
        return;
    }

    if (confirm(`Are you sure you want to toggle the active status of ${selectedIds.length} selected ingredient(s)?`)) {
        let completedRequests = 0;
        let errorOccurred = false;

        selectedIds.forEach(id => {
            ajaxRequest({
                url: `/api/IngredientApi/ToggleIngredientActive/${id}`,
                method: 'PUT',
                showMessage: false,
                onSuccess: function (response) {
                    completedRequests++;
                    if (completedRequests === selectedIds.length) {
                        refreshIngredientsTable();
                        showNotification(`${selectedIds.length} ingredient(s) toggled successfully.`, 'success');
                    }
                },
                onError: function (xhr, status, error) {
                    completedRequests++;
                    errorOccurred = true;
                    console.error(`Failed to toggle ingredient ${id}:`, error);
                    if (completedRequests === selectedIds.length) {
                        refreshIngredientsTable();
                        showNotification(errorOccurred ? 'Some ingredients could not be toggled.' : `${selectedIds.length} ingredient(s) toggled successfully.`, errorOccurred ? 'error' : 'success');
                    }
                }
            });
        });
    }
}

// Modified editIngredient function
function editIngredient(id) {
    getIngredientById(id);
}

// Function to open Add Stock modal
function addStock(ingredientId) {
    $('#addStockIngredientId').val(ingredientId);
    $('#addStock').val('');
    openModal('addStockModal');
}

// Function to save (add) stock
$('#saveStockBtn').on('click', function () {
    const ingredientId = $('#addStockIngredientId').val();
    const stock = parseFloat($('#addStock').val());

    if (!stock || stock <= 0) {
        showNotification('Please enter a valid stock.', 'error');
        return;
    }

    ajaxRequest({
        url: '/api/IngredientApi/AddIngredientStock',
        method: 'POST',
        data: {
            ingredientId: ingredientId,
            stock: stock
        },
        onSuccess: function (response) {
            closeModal('addStockModal');
            debugger
            refreshIngredientsTable();
        },
        onError: function (xhr, status, error) {
            console.error('Failed to add stock:', error);
        }
    });
});


// Modified refreshIngredientsTable
function refreshIngredientsTable() {
    ajaxRequest({
        url: '/api/IngredientApi/GetAllActiveInactiveIngredients',
        method: 'GET',
        showMessage: false,
        onSuccess: function (response) {


            // Pehle check karo agar table pehle se initialized hai
            if ($.fn.DataTable.isDataTable('.ingredients-table')) {
                $('.ingredients-table').DataTable().clear().destroy();
            }

            const ingredients = response.data;
            const tbody = $('.data-table tbody');
            tbody.empty();
            debugger
            let suppliers = new Set(); // unique suppliers

            ingredients.forEach(ingredient => {
                const createdDate = DateFormatter.formatWithComma(ingredient.createdOn);
                const supplier = ingredient.fullName != null ? ingredient.fullName : 'N/A';
                suppliers.add(supplier);

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
                        <td><span class="category-badge">${ingredient.unitOfMeasure || ''}</span></td>
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
                                     ${!ingredient.vendorId > 0
                                                            ? `<a href="#" onclick="addStock(${ingredient.ingredientId})">
                                                    <i class="fas fa-box"></i> Add Stock
                                               </a>`
                                                            : ""
                                        }

                                    <a href="#" onclick="editIngredient(${ingredient.ingredientId})"><i class="fas fa-edit"></i> Edit Details</a>

                                   
                                   ${ingredient.vendorId > 0 ? '' : `
                                    <a href="#" onclick="toggleIngredientActive(${ingredient.ingredientId})">
                                       <i class="fas fa-ban"></i>
                                       ${ingredient.isActive == 1 ? "Deactivate" : "Activate"}
                                    </a>

                                  
                                    `}

                                   

                                    

                                </div>
                            </div>
                        </td>
                    </tr>
                `;
                tbody.append(row);
            });

            // Populate supplier dropdown
            const supplierFilter = $('#supplierFilter');
            supplierFilter.empty();
            supplierFilter.append('<option value="">All Suppliers</option>');
            suppliers.forEach(supplier => {
                supplierFilter.append(`<option value="${supplier}">${supplier}</option>`);
            });

           
            initDataTable();
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
        data: { vendorId: vendorId },
        onSuccess: function (response) {
            $(this).removeClass("loading").html('<i class="fas fa-check"></i> Linked')
                .prop("disabled", true)
                .removeClass("btn-primary")
                .addClass("btn-success");
            showNotification(`${vendorName} linked successfully!`, "success");
            refreshVendors();
            refreshIngredientsTable();
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
                refreshIngredientsTable();
            },
            onError: function (xhr, status, error) {
                $(this).removeClass("loading").html('<i class="fas fa-unlink"></i> Unlink');
                console.error('Failed to unlink vendor:', error);
                showNotification(`Failed to unlink ${vendorName}.`, 'error');
            }
        });
    }
});

function initDataTable() {
    table = $('.ingredients-table').DataTable({
        paging: true,
        searching: true,
        ordering: true,
        order: [[4, 'asc']], // Supplier column
        pageLength: 10,
        lengthMenu: [10, 25, 50, 100],
        language: {
            search: '',
            searchPlaceholder: 'Search ingredients items...'
        }
    });

    // Custom search input
    $('#ingredientSearch').on('keyup', function () {
        table.search(this.value).draw();
    });

    // Supplier filter
    $('#supplierFilter').on('change', function () {
        table.column(4).search(this.value).draw(); // Supplier column index = 4
    });
}


