/* ============================================================================
 * sales.js — Point-of-sale page (Views/Sales/Index.cshtml)
 * Menu grid, order cart, sale completion, sales history and Excel import.
 *
 * Project convention — every Views-JS file follows this section order:
 *   1. Constants  2. State  3. Initialization  4. Event bindings
 *   5. AJAX / CRUD  6. Helpers  7. Utilities
 * Functions stay global: they are referenced by inline onclick handlers.
 * ==========================================================================*/

/* ── 3. Initialization / 4. Event bindings ────────────────────────────── */

$(document).ready(function () {
    initializeSalesPage();
    loadMenuItems();
    refreshCategoryTabs(); // Add this to initialize dynamic category filters

    // Set today's date as default
    const today = new Date().toISOString().split('T')[0];
    $('#salesDate').val(today);
    $('#endSalesDate').val(today); // Set default for end date as well

    // Category filter
    $('.menu-categories').on('click', '.category-btn', function () {
        $('.category-btn').removeClass('active');
        $(this).addClass('active');

        const category = $(this).data('category');
        filterMenuItems(category);
    });

    // Clear order
    $('#clearOrderBtn').on('click', function () {
        clearOrder();
    });

    // Complete sale
    $('#completeSaleBtn').on('click', function () {
        completeSale();
    });

    // Sales history button
    $('#salesHistoryBtn').on('click', function () {
        loadSalesHistory();
    });

    // Import sales button
    $('#importSalesBtn').on('click', function () {
        openModal('importSalesModal');
    });

    // Import sales form submission
    $('#importSalesSubmitBtn').on('click', function () {
        const fileInput = $('#salesFile')[0];
        if (!fileInput.files.length) {
            showNotification('Please select an Excel file to import.', 'error');
            return;
        }

        const formData = new FormData();
        formData.append('file', fileInput.files[0]);

        const importBtn = $('#importSalesSubmitBtn');
        const originalText = importBtn.html();
        importBtn.prop('disabled', true).html('<i class="fas fa-spinner fa-spin"></i> Importing...');

        ajaxRequest({
            url: '/api/SalesApi/ImportSales',
            method: 'POST',
            data: formData,
            contentType: false,
            processData: false,
            onSuccess: function (response) {
                importBtn.prop('disabled', false).html(originalText);
                $('#importSalesForm')[0].reset();
                closeModal('importSalesModal');
                loadSalesHistory(); // Refresh sales history
            },
            onError: function (xhr, status, error) {
                console.error('Failed to import sales:', error);
                importBtn.prop('disabled', false).html(originalText);
            }
        });
    });
});

/* ── 2. State ─────────────────────────────────────────────────────────── */

let currentOrder = [];
let menuItems = [];

function initializeSalesPage() {
    // Initialize any additional setup if needed
}

/* ── 5. AJAX / CRUD ───────────────────────────────────────────────────── */

function loadMenuItems() {
    ajaxRequest({
        url: '/api/MenuApi/GetAllMenuItems',
        method: 'GET',
        showMessage: false,
        onSuccess: function (response) {
            menuItems = response.data;
            displayMenuItems(menuItems);
        },
        onError: function (xhr, status, error) {
            console.error('Failed to fetch menu items:', error);
        }
    });
}

function refreshCategoryTabs() {
    ajaxRequest({
        url: '/api/CategoryApi/GetAllCategories',
        method: 'GET',
        showMessage: false,
        onSuccess: function (response) {
            const tabContainer = $('.menu-categories');
            tabContainer.empty();
            tabContainer.append('<button class="category-btn active" data-category="all">All</button>');
            response.data.forEach(cat => {
                tabContainer.append(`<button class="category-btn" data-category="${cat.categoryId}">${cat.name}</button>`);
            });
        },
        onError: function (xhr, status, error) {
            console.error('Failed to fetch categories:', error);
        }
    });
}

/* ── 6. Helpers ───────────────────────────────────────────────────────── */

function displayMenuItems(items) {
    const grid = $('#menuItemsGrid');
    grid.empty();

    if (items.length === 0) {
        grid.append(`
            <div class="no-items-message" style="text-align: center; padding: 40px; color: #6b7280; font-size: 1.2rem;">
                <i class="fas fa-utensils" style="font-size: 2rem; color: #9ca3af; margin-bottom: 10px;"></i>
                <h2 style="font-size: 1.5rem; font-weight: 500; margin-bottom: 10px;">No Menu Items Available</h2>
                <p style="color: #9ca3af;">It looks like there are no menu items available. Try adding a new item or selecting a different category.</p>
            </div>
        `);
        return;
    }

    items.forEach(item => {
        const menuItemHtml = `
            <div class="menu-item" data-id="${item.menuItemId}" data-category="${item.categoryId}">
                <div class="menu-item-image">
                    <img src="${imageBaseUrl+item.featuredImage || '/placeholder.svg?height=120&width=280&text=' + encodeURIComponent(item.name)}" alt="${item.name}">
                </div>
                <div class="menu-item-content">
                    <h4 class="menu-item-name">${item.name}</h4>
                    <p class="menu-item-description">${item.description}</p>
                    <div class="menu-item-footer">
                        <span class="menu-item-price">$${item.sellingPrice.toFixed(2)}</span>
                        <button class="add-to-order-btn" onclick="addToOrder(${item.menuItemId})">
                            <i class="fas fa-plus"></i> Add
                        </button>
                    </div>
                </div>
            </div>
        `;
        grid.append(menuItemHtml);
    });
}

function filterMenuItems(category) {
    let visibleItems = 0;
    if (category === 'all') {
        displayMenuItems(menuItems);
        visibleItems = menuItems.length;
    } else {
        const filteredItems = menuItems.filter(item => item.categoryId == category);
        displayMenuItems(filteredItems);
        visibleItems = filteredItems.length;
    }

    const grid = $('#menuItemsGrid');
    grid.find('.no-items-message').remove();

    if (visibleItems === 0) {
        ajaxRequest({
            url: '/api/CategoryApi/GetAllCategories',
            method: 'GET',
            showMessage: false,
            onSuccess: function (response) {
                const categoryName = category === 'all' ? 'All' : response.data.find(cat => cat.categoryId == category)?.name || 'This Category';
                const message = `
                    <div class="no-items-message" style="text-align: center; padding: 40px; color: #6b7280; font-size: 1.2rem;">
                        <i class="fas fa-utensils" style="font-size: 2rem; color: #9ca3af; margin-bottom: 10px;"></i>
                        <h2 style="font-size: 1.5rem; font-weight: 500; margin-bottom: 10px;">No Menu Items in ${categoryName}</h2>
                        <p style="color: #9ca3af;">It looks like there are no menu items available for this category. Try adding a new item or selecting a different category.</p>
                    </div>
                `;
                grid.append(message);
            },
            onError: function (xhr, status, error) {
                console.error('Failed to fetch categories:', error);
                const message = `
                    <div class="no-items-message" style="text-align: center; padding: 40px; color: #6b7280; font-size: 1.2rem;">
                        <i class="fas fa-utensils" style="font-size: 2rem; color: #9ca3af; margin-bottom: 10px;"></i>
                        <h2 style="font-size: 1.5rem; font-weight: 500; margin-bottom: 10px;">No Menu Items Available</h2>
                        <p style="color: #9ca3af;">It looks like there are no menu items available. Try adding a new item or selecting a different category.</p>
                    </div>
                `;
                grid.append(message);
            }
        });
    }
}

function addToOrder(itemId) {
    const menuItem = menuItems.find(item => item.menuItemId == itemId);
    if (!menuItem) return;

    const existingItem = currentOrder.find(item => item.menuItemId == itemId);

    if (existingItem) {
        existingItem.quantity += 1;
    } else {
        currentOrder.push({
            menuItemId: menuItem.menuItemId,
            name: menuItem.name,
            description: menuItem.description,
            sellingPrice: menuItem.sellingPrice,
            categoryId: menuItem.categoryId,
            quantity: 1
        });
    }

    updateOrderDisplay();
    showNotification(`${menuItem.name} added to order`, 'success');
}

function removeFromOrder(itemId) {
    currentOrder = currentOrder.filter(item => item.menuItemId !== itemId);
    updateOrderDisplay();
}

function updateQuantity(itemId, change) {
    const item = currentOrder.find(item => item.menuItemId == itemId);
    if (!item) return;

    item.quantity += change;

    if (item.quantity <= 0) {
        removeFromOrder(itemId);
    } else {
        updateOrderDisplay();
    }
}

function updateOrderDisplay() {
    const orderItemsContainer = $('#orderItems');

    if (currentOrder.length === 0) {
        orderItemsContainer.html(`
            <div class="empty-order">
                <i class="fas fa-shopping-cart"></i>
                <p>No items in order</p>
                <small>Select items from the menu to add them here</small>
            </div>
        `);
        $('#completeSaleBtn').prop('disabled', true);
    } else {
        let orderHtml = '';

        currentOrder.forEach(item => {
            const itemTotal = item.sellingPrice * item.quantity;
            orderHtml += `
                <div class="order-item">
                    <div class="order-item-info">
                        <h5 class="order-item-name">${item.name}</h5>
                        <p class="order-item-price">$${item.sellingPrice.toFixed(2)} each</p>
                    </div>
                    <div class="quantity-controls">
                        <button class="quantity-btn" onclick="updateQuantity(${item.menuItemId}, -1)">
                            <i class="fas fa-minus"></i>
                        </button>
                        <input type="number" class="quantity-display" min="1" value="${item.quantity}" onchange="changeQuantityDirectly(${item.menuItemId}, this.value)">
                        <button class="quantity-btn" onclick="updateQuantity(${item.menuItemId}, 1)">
                            <i class="fas fa-plus"></i>
                        </button>
                        <button class="remove-item-btn" onclick="removeFromOrder(${item.menuItemId})">
                            <i class="fas fa-trash"></i>
                        </button>
                    </div>
                </div>
            `;
        });

        orderItemsContainer.html(orderHtml);
        $('#completeSaleBtn').prop('disabled', false);
    }

    updateOrderSummary();
}

function changeQuantityDirectly(itemId, value) {
    const quantity = parseInt(value);
    if (isNaN(quantity) || quantity <= 0) {
        removeFromOrder(itemId);
    } else {
        const item = currentOrder.find(item => item.menuItemId == itemId);
        if (item) {
            item.quantity = quantity;
            updateOrderDisplay();
        }
    }
}

function updateOrderSummary() {
    const total = currentOrder.reduce((sum, item) => sum + (item.sellingPrice * item.quantity), 0);
    $('#total').text('$' + total.toFixed(2));
}

function clearOrder() {
    if (currentOrder.length === 0) return;

    if (confirm('Are you sure you want to clear the current order?')) {
        currentOrder = [];
        updateOrderDisplay();
        showNotification('Order cleared', 'info');
    }
}

function completeSale() {
    if (currentOrder.length === 0) {
        showNotification('Please add items to the order first', 'error');
        return;
    }

    const salesDate = $('#salesDate').val();
    if (!salesDate) {
        showNotification('Please select a start date', 'error');
        return;
    }
    const endSalesDate = $('#endSalesDate').val();
    if (!endSalesDate) {
        showNotification('Please select an end date', 'error');
        return;
    }

    const subTotal = currentOrder.reduce((sum, item) => sum + (item.sellingPrice * item.quantity), 0);
    const tax = 0;

    const saleData = {
        saleDate: salesDate,
        endSaleDate: endSalesDate,
        subTotal: subTotal,
        tax: tax,
        total: subTotal + tax,
        items: currentOrder.map(item => ({
            menuItemId: item.menuItemId,
            quantity: item.quantity,
            price: item.sellingPrice * item.quantity,
            cost: item.cost
        }))
    };

    const completeBtn = $('#completeSaleBtn');
    const originalText = completeBtn.html();
    completeBtn.prop('disabled', true).html('<i class="fas fa-spinner fa-spin"></i> Processing...');

    ajaxRequest({
        url: '/api/SalesApi/CreateSale',
        method: 'POST',
        data: saleData,
        onSuccess: function (response) {
            currentOrder = [];
            updateOrderDisplay();
            completeBtn.prop('disabled', false).html(originalText);
            showNotification(`Sale completed successfully! Total: $${total.toFixed(2)}`, 'success');
        },
        onError: function (xhr, status, error) {
            console.error('Failed to complete sale:', error);
            completeBtn.prop('disabled', false).html(originalText);
        }
    });
}

function loadSalesHistory() {
    ajaxRequest({
        url: '/api/SalesApi/GetAllSales',
        method: 'GET',
        showMessage: false,
        onSuccess: function (response) {
            const sales = response.data;
            const tbody = $('#salesHistoryTableBody');
            tbody.empty();

            if (sales.length === 0) {
                tbody.append(`
                    <tr>
                        <td colspan="5" class="text-center">No sales found.</td>
                    </tr>
                `);
            } else {
                sales.forEach(sale => {
                    const items = JSON.parse(sale.itemsJson || '[]');
                    const itemList = items.map(item =>
                        `<span class="status-badge good">${item.Name} (x${item.Quantity})</span>`
                    ).join(' ');
                    const row = `
                        <tr>
                            <td>${DateFormatter.formatUSWithTime(sale.saleDate)}</td>
                            <td>$${sale.total.toFixed(2)}</td>
                            <td>${itemList || 'N/A'}</td>
                        </tr>
                    `;
                    tbody.append(row);
                });
            }

            openModal('salesHistoryModal');
        },
        onError: function (xhr, status, error) {
            console.error('Failed to fetch sales history:', error);
        }
    });
}

/* ── 7. Utilities ─────────────────────────────────────────────────────── */

function showNotification(message, type = 'info') {
    const icons = {
        success: 'fas fa-check-circle',
        error: 'fas fa-exclamation-circle',
        info: 'fas fa-info-circle'
    };

    const notification = $(`
        <div class="notification notification-${type}">
            <i class="${icons[type]}"></i>
            <span>${message}</span>
            <button class="notification-close">
                <i class="fas fa-times"></i>
            </button>
        </div>
    `);

    $('body').append(notification);

    setTimeout(() => {
        notification.remove();
    }, 5000);

    notification.find('.notification-close').on('click', () => {
        notification.remove();
    });
}

function openModal(modalId) {
    $(`#${modalId}`).addClass('active');
}

function closeModal(modalId) {
    $(`#${modalId}`).removeClass('active');
}