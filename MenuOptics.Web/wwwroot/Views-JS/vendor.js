// vendor.js — Admin user management page scripts.

let allUsers = [];
let currentFilters = { search: '' };
let table;

$(document).ready(function () {
    table = initDataTable('#userTable', {
        processing: true,
        loadingMessage: 'Loading users...',
        ajax: function (dtParams, callback, settings) {
            $.ajax({
                url: '/api/UserApi/GetAllUsers',
                method: 'GET',
                success: function (response) {
                    allUsers = response.data;
                    callback({ data: getFilteredUsers() });
                },
                error: function (xhr, status, error) {
                    console.error('Failed to fetch users:', error);
                    showNotification('Failed to load users.', 'error');
                    callback({ data: [] });
                }
            });
        },
        columns: [
            {
                data: null,
                render: function (data, type, row) {
                    return `<div class="user-info">
                                <div class="user-avatar">${row?.firstName[0]}${row.lastName[0]}</div>
                                <div class="user-details">
                                    <p class="user-name">${row?.firstName} ${row.lastName}</p>
                                    <div class="user-contact">
                                        <span><i class="fas fa-envelope"></i> ${row.email}</span>
                                        <span><i class="fas fa-phone"></i> ${row.phone}</span>
                                    </div>
                                </div>
                            </div>`;
                }
            },
            {
                data: 'roles',
                render: function (roles, type, row) {
                    return `<span class="role-badge ${row?.roles?.toLowerCase()}">${row?.roles}</span>`;
                }
            },
            {
                data: 'isActive',
                render: function (isActive, type, row) {
                    const status = isActive ? 'active' : 'critical';
                    return `<span class="status-badge ${status}">${row?.isActive ? 'Active' : 'Inactive'}</span>`;
                }
            },
            {
                data: 'createdOn',
                render: function (createdOn, type, row) {
                    return `<i class="fas fa-calendar"></i> ${new Date(row?.createdOn).toLocaleDateString()}`;
                }
            },
            {
                data: 'modifiedOn',
                render: function (modifiedOn) {
                    return `<i class="fas fa-clock"></i> ${modifiedOn ? new Date(modifiedOn).toLocaleDateString() : 'Never'}`;
                }
            },
            {
                data: null,
                render: function (data, type, row) {
                    return `<div class="dropdown">
                                <button class="dropdown-toggle">
                                    <i class="fas fa-ellipsis-h"></i>
                                </button>
                                <div class="dropdown-menu">
                                    <a href="#" onclick="getUserById(${row.userId})">Edit User</a>
                                    <a href="#" onclick="toggleUserActive(${row.userId})">${row.isActive ? 'Deactivate' : 'Activate'}</a>
                                </div>
                            </div>`;
                }
            }
        ],
        order: [[0, 'asc']],
        language: {
            search: '',
            searchPlaceholder: 'Search users...'
        }
    });

    refreshUserStats();

    $('#saveUserBtn').on('click', function () {
        const formData = new FormData($('#addUserForm')[0]);
        if (isFormValid('#addUserForm')) {
            addUser(formData);
        } else {
            showNotification('Please fill in all required fields.', 'error');
        }
    });

    $('#updateUserBtn').on('click', function () {
        const formData = new FormData($('#editUserForm')[0]);
        if (isFormValid('#updateUserForm')) {
            updateUser(formData);
        } else {
            showNotification('Please fill in all required fields.', 'error');
        }
    });

    $('#userSearch').on('input', function () {
        currentFilters.search = $(this).val();
        table.search(this.value).draw();
    });

    setInterval(refreshUserTable, 30000);
});

function addUser(formData) {
    const $btn = $('#saveUserBtn');
    setBtnLoading($btn, true);
    formData.isActive = formData.isActive == 'on' ? 1 : 0;
    ajaxRequest({
        url: '/api/UserApi/CreateUser',
        method: 'POST',
        data: formData,
        contentType: false,
        processData: false,
        onSuccess: function (response) {
            refreshUserStats();
            refreshUserTable();
            $('#addUserForm')[0].reset();
            $('#addProfilePicPreview').hide();
            closeModal('addUserModal');
        },
        onError: function (xhr, status, error) {
            console.error('Failed to add user:', error);
        },
        onComplete: function () { setBtnLoading($btn, false); }
    });
}

function getUserById(id) {
    ajaxRequest({
        url: `/api/UserApi/GetUser/${id}`,
        method: 'GET',
        showMessage: false,
        onSuccess: function (response) {
            const user = response.data;
            $('#editUserForm [name="userId"]').val(user.userId);
            $('#editUserForm [name="firstName"]').val(user.firstName);
            $('#editUserForm [name="lastName"]').val(user.lastName);
            $('#editUserForm [name="username"]').val(user.username);
            $('#editUserForm [name="email"]').val(user.email);
            $('#editUserForm [name="phone"]').val(user.phone);
            $('#editUserForm [name="address"]').val(user.address);
            $('#editUserForm [name="country"]').val(user.country);
            $('#editUserForm [name="state"]').val(user.state);
            $('#editUserForm [name="city"]').val(user.city);
            $('#editUserForm [name="zip"]').val(user.zip);
            $('#editUserForm [name="profilePic"]').val(user.profilePic);
            if (user.profilePic) {
                $('#editProfilePicPreview').attr('src', imageBaseUrl + user.profilePic).show();
                $('#editProfilePicLabel').text('Replace profile picture');
            } else {
                $('#editProfilePicPreview').hide();
                $('#editProfilePicLabel').text('Choose new profile picture');
            }
            openModal('editUserModal');
        },
        onError: function (xhr, status, error) {
            console.error('Failed to fetch user:', error);
            showNotification('Failed to fetch user.', 'error');
        }
    });
}

function updateUser(formData) {
    const $btn = $('#updateUserBtn');
    setBtnLoading($btn, true);
    formData.isActive = formData.isActive == 'on' ? 1 : 0;
    ajaxRequest({
        url: '/api/UserApi/UpdateUser',
        method: 'PUT',
        data: formData,
        contentType: false,
        processData: false,
        onSuccess: function (response) {
            refreshUserStats();
            refreshUserTable();
            closeModal('editUserModal');
        },
        onError: function (xhr, status, error) {
            console.error('Failed to update user:', error);
        },
        onComplete: function () { setBtnLoading($btn, false); }
    });
}

function toggleUserActive(id) {
    ajaxRequest({
        url: `/api/UserApi/ToggleUserActive/${id}`,
        method: 'PUT',
        showMessage: false,
        onSuccess: function (response) {
            refreshUserStats();
            refreshUserTable();
            showNotification(`User ${response.data.isActive ? 'activated' : 'deactivated'} successfully!`, 'success');
        },
        onError: function (xhr, status, error) {
            console.error('Failed to toggle user active status:', error);
            showNotification('Failed to toggle user active status.', 'error');
        }
    });
}

function refreshUserStats() {
    ajaxRequest({
        url: '/api/UserApi/GetUserStats',
        method: 'GET',
        showMessage: false,
        onSuccess: function (response) {
            const stats = response.data || {};
            $('#totalUsers').text(stats.totalCount);
            $('#activeUsers').text(stats.activeCount);
            $('#inactiveUsers').text(stats.inactiveCount);
            $('#newThisWeek').text(stats.newThisWeekCount);
            console.log('Restaurant stats loaded:', stats);
        },
        onError: function (xhr, status, error) {
            console.error('Failed to fetch restaurant stats:', error);
            showNotification('Failed to load restaurant stats.', 'error');
            $('#totalUsers').text(0);
            $('#activeToday').text(0);
            $('#inactiveUsers').text(0);
            $('#newThisWeek').text(0);
        }
    });
}

function refreshUserTable() {
    if (table) {
        table.ajax.reload(null, false);
    }
}

function getFilteredUsers() {
    return allUsers.filter(user => {
        const matchesSearch = currentFilters.search === '' ||
            user.firstName.toLowerCase().includes(currentFilters.search.toLowerCase()) ||
            user.lastName.toLowerCase().includes(currentFilters.search.toLowerCase()) ||
            user.email.toLowerCase().includes(currentFilters.search.toLowerCase()) ||
            user.username.toLowerCase().includes(currentFilters.search.toLowerCase());

        return matchesSearch;
    });
}

function filterAndRenderUsers() {
    if (table) {
        table.ajax.reload(null, false);
    }
}

window.filterUsers = function (type, value) {
    currentFilters[type] = value;
    $('#' + type + 'FilterLabel').text(value.charAt(0).toUpperCase() + value.slice(1));
    filterAndRenderUsers();
};
