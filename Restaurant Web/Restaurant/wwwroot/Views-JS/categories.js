$(document).ready(function () {
    // Event handler for add category form submission
    $('#addCategoryBtn').on('click', function () {
        openModal('addCategoryModal');
    });

    $('#addCategoryBtn2').on('click', function () {
        const categoryData = {
            name: $('#categoryName').val(),
            description: $('#categoryDescription').val()
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
            description: $('#editCategoryDescription').val(),
            isActive: true
        };

        if (categoryData.name) {
            updateCategory(categoryData);
        } else {
            showNotification('Please fill in all required fields.', 'error');
        }
    });

    // Event handlers for filters
    $('#categorySearch').on('input', filterCategories);
    $('#activeFilter').on('click', toggleActiveFilter);

    refreshCategoriesTable();
});

// Function to add a category
function addCategory(categoryData) {
    ajaxRequest({
        url: '/api/CategoryApi/CreateCategory',
        method: 'POST',
        data: {
            Name: categoryData.name,
            Description: categoryData.description
        },
        onSuccess: function (response) {
            refreshCategoriesTable();
            $('#addCategoryForm')[0].reset();
            closeModal('addCategoryModal');
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
            $('#editCategoryDescription').val(category.description);
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
            Description: categoryData.description,
            IsActive: categoryData.isActive
        },
        onSuccess: function (response) {
            refreshCategoriesTable();
            closeModal('editCategoryModal');
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
                showNotification(response.responseMsg, 'success');
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
    ajaxRequest({
        url: '/api/CategoryApi/GetAllCategories',
        method: 'GET',
        data: null,
        showMessage: false,
        onSuccess: function (response) {
            const categories = response.data;
            const tbody = $('.data-table tbody');
            tbody.empty();
            $('#totalCategories').text(categories.filter(c => c.isActive).length);

            categories.forEach(category => {
                const createdDate = DateFormatter.formatWithComma(category.createdOn);
                const actionText = category.isActive ? 'Inactive' : 'Reactivate';
                const actionIcon = category.isActive ? 'fas fa-ban' : 'fas fa-check';
                const row = `
                    <tr>
                        <td>
                            <div class="category-info">
                                <p class="category-name">${category.name}</p>
                                <p class="category-meta">Added on: ${createdDate}</p>
                            </div>
                        </td>
                        <td>${category.description || 'No description'}</td>
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
                                    <a href="#" onclick="deleteCategory(${category.categoryId})"><i class="fas fa-trash"></i> Delete</a>
                                </div>
                            </div>
                        </td>
                    </tr>
                `;
                tbody.append(row);
            });
        },
        onError: function (xhr, status, error) {
            console.error('Failed to fetch categories:', error);
        }
    });
}

// Function to filter categories
function filterCategories() {
    const searchTerm = $('#categorySearch').val().toLowerCase();
    ajaxRequest({
        url: '/api/CategoryApi/GetAllCategories',
        method: 'GET',
        data: null,
        showMessage: false,
        onSuccess: function (response) {
            const categories = response.data.filter(category =>
                category.name.toLowerCase().includes(searchTerm) ||
                (category.description && category.description.toLowerCase().includes(searchTerm))
            );
            const tbody = $('.data-table tbody');
            tbody.empty();

            categories.forEach(category => {
                const createdDate = DateFormatter.formatWithComma(category.createdOn);
                const actionText = category.isActive ? 'Inactive' : 'Reactivate';
                const actionIcon = category.isActive ? 'fas fa-ban' : 'fas fa-check';
                const row = `
                    <tr>
                        <td>
                            <div class="category-info">
                                <p class="category-name">${category.name}</p>
                                <p class="category-meta">Added on: ${createdDate}</p>
                            </div>
                        </td>
                        <td>${category.description || 'No description'}</td>
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
                                    <a href="#" onclick="deleteCategory(${category.categoryId})"><i class="fas fa-trash"></i> Delete</a>
                                </div>
                            </div>
                        </td>
                    </tr>
                `;
                tbody.append(row);
            });
        },
        onError: function (xhr, status, error) {
            console.error('Failed to fetch categories:', error);
        }
    });
}

// Function to toggle active filter
function toggleActiveFilter() {
    const isActiveOnly = $('#activeFilter').hasClass('active');
    $('#activeFilter').toggleClass('active');

    ajaxRequest({
        url: '/api/CategoryApi/GetAllCategories',
        method: 'GET',
        data: null,
        showMessage: false,
        onSuccess: function (response) {
            const categories = isActiveOnly
                ? response.data
                : response.data.filter(c => c.isActive);
            const tbody = $('.data-table tbody');
            tbody.empty();

            categories.forEach(category => {
                const createdDate = DateFormatter.formatWithComma(category.createdOn);
                const actionText = category.isActive ? 'Inactive' : 'Reactivate';
                const actionIcon = category.isActive ? 'fas fa-ban' : 'fas fa-check';
                const row = `
                    <tr>
                        <td>
                            <div class="category-info">
                                <p class="category-name">${category.name}</p>
                                <p class="category-meta">Added on: ${createdDate}</p>
                            </div>
                        </td>
                        <td>${category.description || 'No description'}</td>
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
                                    <a href="#" onclick="deleteCategory(${category.categoryId})"><i class="fas fa-trash"></i> Delete</a>
                                </div>
                            </div>
                        </td>
                    </tr>
                `;
                tbody.append(row);
            });
        },
        onError: function (xhr, status, error) {
            console.error('Failed to fetch categories:', error);
        }
    });
}

// Utility functions
function openModal(modalId) {
    $(`#${modalId}`).addClass('active');
}

function closeModal(modalId) {
    $(`#${modalId}`).removeClass('active');
}