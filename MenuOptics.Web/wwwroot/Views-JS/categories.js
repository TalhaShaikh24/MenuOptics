// categories.js — Category management page scripts.

let table;
let activeFilterApplied = false;

$(document).ready(function () {
    table = initDataTable('.data-table', {
        processing: true,
        loadingMessage: 'Loading categories...',
        ajax: function (dtParams, callback, settings) {
            $.ajax({
                url: '/api/CategoryApi/GetAllCategories',
                method: 'GET',
                success: function (response) {
                    const categories = response.data;
                    $('#totalCategories').text(categories.filter(c => c.isActive).length);

                    const rows = activeFilterApplied
                        ? categories.filter(c => c.isActive)
                        : categories;
                    callback({ data: rows });
                },
                error: function (xhr, status, error) {
                    console.error('Failed to fetch categories:', error);
                    callback({ data: [] });
                }
            });
        },
        columns: [
            {
                data: 'name',
                render: function (name) {
                    return `<div class="category-info">
                                <p class="category-name">${name}</p>
                            </div>`;
                }
            },
            {
                data: 'createdOn',
                render: function (createdOn) {
                    return `<i class="fas fa-calendar"></i> ${DateFormatter.formatWithComma(createdOn)}`;
                }
            },
            {
                data: 'isActive',
                render: function (isActive) {
                    return `<span class="status-badge ${isActive ? 'good' : 'critical'}">${isActive ? 'Active' : 'Inactive'}</span>`;
                }
            },
            {
                data: null,
                render: function (data, type, row) {
                    const actionText = row.isActive ? 'Deactivate' : 'Activate';
                    const actionIcon = row.isActive ? 'fas fa-ban' : 'fas fa-check';
                    return `<div class="dropdown">
                                <button class="dropdown-toggle">
                                    <i class="fas fa-ellipsis-h"></i>
                                </button>
                                <div class="dropdown-menu">
                                    <a href="#" onclick="editCategory(${row.categoryId})"><i class="fas fa-edit"></i> Edit Details</a>
                                    <a href="#" onclick="toggleCategoryActive(${row.categoryId})"><i class="${actionIcon}"></i> ${actionText}</a>
                                </div>
                            </div>`;
                }
            }
        ],
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
});

function refreshCategoriesTable() {
    // Matches the old refresh: it always re-rendered the full, unfiltered list.
    activeFilterApplied = false;
    if (table) {
        table.ajax.reload(null, false);
    }
}

function toggleActiveFilter() {
    $('#activeFilter').toggleClass('active');
    activeFilterApplied = $('#activeFilter').hasClass('active');
    table.ajax.reload(null, false);
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
