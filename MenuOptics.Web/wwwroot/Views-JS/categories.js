/* ============================================================================
 * categories.js — Category management page (Views/Category/Index.cshtml)
 *
 * Project convention — every Views-JS file follows this section order:
 *   1. Constants  2. State  3. Initialization  4. Event bindings
 *   5. AJAX / CRUD  6. Helpers  7. Utilities
 * Functions stay global: they are referenced by inline onclick handlers.
 * ==========================================================================*/

/* ── 2. State ─────────────────────────────────────────────────────────── */

let table;

/* ── 3. Initialization / 4. Event bindings ────────────────────────────── */

$(document).ready(function () {
    // Initialize DataTable once on page load
    initDataTable();

    // Event handler for add category form submission
    $('#addCategoryBtn').on('click', function () {
        openModal('addCategoryModal');
    });

    $('#addCategoryBtn2').on('click', function () {
        const categoryData = {
            name: $('#categoryName').val(),
        };

        if (categoryData.name) {
            addCategory(categoryData);
        } else {
            showNotification('Please fill in all required fields.', 'error');
        }
    });

    // Event handler for update form submission
    $('#updateCategoryBtn').on('click', function () {
        const categoryData = {
            categoryId: $('#editCategoryId').val(),
            name: $('#editCategoryName').val(),
            isActive: true
        };

        if (categoryData.name) {
            updateCategory(categoryData);
        } else {
            showNotification('Please fill in all required fields.', 'error');
        }
    });

    // Event handlers for filters
    $('#categorySearch').on('input', function () {
        table.search(this.value).draw();
    });
    $('#activeFilter').on('click', toggleActiveFilter);

    refreshCategoriesTable();
});

/* ── 5. AJAX / CRUD ───────────────────────────────────────────────────── */

// Function to add a category
function addCategory(categoryData) {
    ajaxRequest({
        url: '/api/CategoryApi/CreateCategory',
        method: 'POST',
        data: {
            Name: categoryData.name
        },
        onSuccess: function (response) {
            $('#addCategoryForm')[0].reset();
            closeModal('addCategoryModal');
            refreshCategoriesTable();
        },
        onError: function (xhr, status, error) {
            console.error('Failed to add category:', error);
        }
    });
}

// Function to get category by ID
function getCategoryById(id) {
    ajaxRequest({
        url: `/api/CategoryApi/GetCategory/${id}`,
        method: 'GET',
        showMessage: false,
        onSuccess: function (response) {
            const category = response.data;
            $('#editCategoryId').val(category.categoryId);
            $('#editCategoryName').val(category.name);
            openModal('editCategoryModal');
        },
        onError: function (xhr, status, error) {
            console.error('Failed to fetch category:', error);
        }
    });
}

// Function to update a category
function updateCategory(categoryData) {
    ajaxRequest({
        url: '/api/CategoryApi/UpdateCategory',
        method: 'PUT',
        data: {
            CategoryId: categoryData.categoryId,
            Name: categoryData.name,
            IsActive: categoryData.isActive
        },
        onSuccess: function (response) {
            closeModal('editCategoryModal');
            refreshCategoriesTable();
        },
        onError: function (xhr, status, error) {
            console.error('Failed to update category:', error);
        }
    });
}

// Function to toggle category active status
function toggleCategoryActive(id) {
    if (confirm('Are you sure you want to toggle the active status of this category?')) {
        ajaxRequest({
            url: `/api/CategoryApi/ToggleCategoryActive/${id}`,
            method: 'PUT',
            showMessage: false,
            onSuccess: function (response) {
                refreshCategoriesTable();
            },
            onError: function (xhr, status, error) {
                console.error('Failed to toggle category status:', error);
            }
        });
    }
}

// Function to delete a category
function deleteCategory(id) {
    if (confirm('Are you sure you want to delete this category?')) {
        ajaxRequest({
            url: `/api/CategoryApi/DeleteCategoryById/${id}`,
            method: 'DELETE',
            showMessage: false,
            onSuccess: function (response) {
                showNotification('Category deleted successfully.', 'success');
                refreshCategoriesTable();
            },
            onError: function (xhr, status, error) {
                console.error('Failed to delete category:', error);
            }
        });
    }
}

// Function to edit a category
function editCategory(id) {
    getCategoryById(id);
}

// Function to refresh the categories table
function refreshCategoriesTable() {
    LoadingComponent.showForTable('.data-table', 'Loading categories...');
    ajaxRequest({
        url: '/api/CategoryApi/GetAllCategories',
        method: 'GET',
        data: null,
        showMessage: false,
        onSuccess: function (response) {
            const categories = response.data;
            $('#totalCategories').text(categories.filter(c => c.isActive).length);

            renderCategoriesTable(categories);
        },
        onComplete: function () { LoadingComponent.hideForTable('.data-table'); }
    });
}

// Function to toggle active filter
function toggleActiveFilter() {
    LoadingComponent.showForTable('.data-table', 'Loading categories...');
    const isActiveOnly = $('#activeFilter').hasClass('active');
    $('#activeFilter').toggleClass('active');

    ajaxRequest({
        url: '/api/CategoryApi/GetAllCategories',
        method: 'GET',
        data: null,
        showMessage: false,
        onSuccess: function (response) {
            const categories = isActiveOnly ? response.data : response.data.filter(c => c.isActive);

            renderCategoriesTable(categories);
        },
        onError: function (xhr, status, error) {
            console.error('Failed to fetch categories:', error);
        },
        onComplete: function () { LoadingComponent.hideForTable('.data-table'); }
    });
}

/* ── 6. Helpers ───────────────────────────────────────────────────────── */

// Function to initialize DataTable
function initDataTable() {
    table = $('.data-table').DataTable({
        paging: true,
        searching: true,
        ordering: false,
        pageLength: 10, // Default rows per page
        lengthMenu: [10, 25, 50, 100], // Pagination options
        language: {
            search: '', // Remove default search label
            searchPlaceholder: 'Search categories...'
        }
    });
}

// Clears the table and renders the given categories (used by refresh + filter)
function renderCategoriesTable(categories) {
    // Clear existing rows in DataTable
    table.clear();

    // Add new rows
    categories.forEach(category => {
        table.row.add($(buildCategoryRow(category)));
    });

    // Redraw the table
    table.draw();
}

// Builds a single category table row (same markup as before refactoring)
function buildCategoryRow(category) {
    const createdDate = DateFormatter.formatWithComma(category.createdOn);
    const actionText = category.isActive ? 'Deactivate' : 'Activate';
    const actionIcon = category.isActive ? 'fas fa-ban' : 'fas fa-check';
    return `
                    <tr>
                        <td>
                            <div class="category-info">
                                <p class="category-name">${category.name}</p>
                            </div>
                        </td>
                        <td><i class="fas fa-calendar"></i> ${createdDate}</td>
                        <td><span class="status-badge ${category.isActive ? 'good' : 'critical'}">${category.isActive ? 'Active' : 'Inactive'}</span></td>
                        <td>
                            <div class="dropdown">
                                <button class="dropdown-toggle">
                                    <i class="fas fa-ellipsis-h"></i>
                                </button>
                                <div class="dropdown-menu">
                                    <a href="#" onclick="editCategory(${category.categoryId})"><i class="fas fa-edit"></i> Edit Details</a>
                                    <a href="#" onclick="toggleCategoryActive(${category.categoryId})"><i class="${actionIcon}"></i> ${actionText}</a>
                                </div>
                            </div>
                        </td>
                    </tr>
                `;
}

