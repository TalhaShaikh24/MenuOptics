// categories.js — Category management page scripts.

let table;

$(document).ready(function () {
    table = initDataTable('.data-table', {
        language: { searchPlaceholder: 'Search categories...' }
    });

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

    $('#categorySearch').on('input', function () {
        table.search(this.value).draw();
    });
    $('#activeFilter').on('click', toggleActiveFilter);

    refreshCategoriesTable();
});

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

function addCategory(categoryData) {
    const $btn = $('#addCategoryBtn2');
    setBtnLoading($btn, true);
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
        },
        onComplete: function () { setBtnLoading($btn, false); }
    });
}

function updateCategory(categoryData) {
    const $btn = $('#updateCategoryBtn');
    setBtnLoading($btn, true);
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
        },
        onComplete: function () { setBtnLoading($btn, false); }
    });
}

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

function editCategory(id) {
    getCategoryById(id);
}

function renderCategoriesTable(categories) {
    table.clear();

    categories.forEach(category => {
        table.row.add($(buildCategoryRow(category)));
    });

    table.draw();
}

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
