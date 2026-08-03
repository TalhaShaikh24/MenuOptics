/* ============================================================================
 * vendor.js — Admin user management page (Views/User/AllUsers.cshtml)
 * User table with stats, filters, add/edit modals and periodic auto-refresh.
 *
 * Project convention — every Views-JS file follows this section order:
 *   1. Constants  2. State  3. Initialization  4. Event bindings
 *   5. AJAX / CRUD  6. Helpers  7. Utilities
 * Functions stay global: they are referenced by inline onclick handlers.
 * ==========================================================================*/

/* ── 2. State ─────────────────────────────────────────────────────────── */

let allUsers = [];
let currentFilters = { role: 'all', status: 'all', search: '' };
let table;

/* ── 3. Initialization / 4. Event bindings ────────────────────────────── */

$(document).ready(function () {
    // Initialize DataTable once on page load
    initDataTable();

    // Load stats and users
    refreshUserStats();
    refreshUserTable();

    // Event handler for add user
    $('#saveUserBtn').on('click', function () {
        const formData = new FormData($('#addUserForm')[0]);
        if (validateUserData(formData)) {
            addUser(formData);
        } else {
            showNotification('Please fill in all required fields.', 'error');
        }
    });

    // Event handler for update user
    $('#updateUserBtn').on('click', function () {
        const formData = new FormData($('#editUserForm')[0]);
        if (validateUserData(formData)) {
            updateUser(formData);
        } else {
            showNotification('Please fill in all required fields.', 'error');
        }
    });

    // Search filter
    $('#userSearch').on('input', function () {
        currentFilters.search = $(this).val();
        table.search(this.value).draw();
    });

    // Auto-refresh table every 30 seconds
    setInterval(refreshUserTable, 30000);
});

/* ── Validation ───────────────────────────────────────────────────────── */

// Validate user data
function validateUserData(formData) {
    const firstName = formData.get('firstName');
    const lastName = formData.get('lastName');
    const username = formData.get('username');
    const email = formData.get('email');
    const phone = formData.get('phone');
    const role = formData.get('role');
    const password = formData.get('password');
    return firstName && lastName && username && email && phone && role && (formData.get('userId') || password);
}

/* ── 5. AJAX / CRUD ───────────────────────────────────────────────────── */

// Add user
function addUser(formData) {

    formData.isActive = formData.isActive == "on" ? 1 : 0
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
        }
    });
}

// Get user by ID
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
            $('#editUserForm [name="gender"]').val(user.gender);
            $('#editUserForm [name="role"]').val(user.roles);
            $('#editUserForm [name="isActive"]').prop('checked', user.isActive);
            $('#editUserForm [name="profilePic"]').val(user.profilePic);
            if (user.profilePic) {
                $('#editProfilePicPreview').attr('src', user.profilePic).show();
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

// Update user
function updateUser(formData) {
    formData.isActive = formData.isActive == "on" ? 1 : 0
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
        }
    });
}

// Toggle user active status
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

// Delete user
function deleteUser(id) {
    if (confirm('Are you sure you want to delete this user?')) {
        ajaxRequest({
            url: `/api/UserApi/DeleteUser/${id}`,
            method: 'DELETE',
            showMessage: false,
            onSuccess: function (response) {
                refreshUserStats();
                refreshUserTable();
                showNotification('User deleted successfully.', 'success');
            },
            onError: function (xhr, status, error) {
                console.error('Failed to delete user:', error);
                showNotification('Failed to delete user.', 'error');
            }
        });
    }
}

// Refresh user stats
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

/* ── 6. Helpers ───────────────────────────────────────────────────────── */

// Initialize DataTable
function initDataTable() {
    table = $('#userTable').DataTable({
        paging: true,
        searching: true,
        ordering: true,
        order: [[0, 'asc']], // Sort by Name column by default
        pageLength: 10,
        lengthMenu: [10, 25, 50, 100],
        language: {
            search: '',
            searchPlaceholder: 'Search users...'
        }
    });
}

// Refresh user table
function refreshUserTable() {
    ajaxRequest({
        url: '/api/UserApi/GetAllUsers',
        method: 'GET',
        showMessage: false,
        onSuccess: function (response) {
            allUsers = response.data;
            filterAndRenderUsers();
        },
        onError: function (xhr, status, error) {
            console.error('Failed to fetch users:', error);
            showNotification('Failed to load users.', 'error');
        }
    });
}

// Filter and render users
function filterAndRenderUsers() {
    const filteredUsers = allUsers.filter(user => {
        const matchesSearch = currentFilters.search === '' ||
            user.firstName.toLowerCase().includes(currentFilters.search.toLowerCase()) ||
            user.lastName.toLowerCase().includes(currentFilters.search.toLowerCase()) ||
            user.email.toLowerCase().includes(currentFilters.search.toLowerCase()) ||
            user.username.toLowerCase().includes(currentFilters.search.toLowerCase());
        const matchesRole = currentFilters.role === 'all' || user.roles.toLowerCase() === currentFilters.role.toLowerCase();
        const matchesStatus = currentFilters.status === 'all' ||
            (currentFilters.status === 'active' && user.isActive) ||
            (currentFilters.status === 'inactive' && !user.isActive);
        return matchesSearch && matchesRole && matchesStatus;
    });

    // Clear existing rows
    table.clear();

    // Add new rows
    filteredUsers.forEach(user => {
        const status = user.isActive ? 'active' : 'inactive';
        const row = `
            <tr>
                <td>
                    <div class="user-info">
                        <div class="user-avatar">${user?.firstName[0]}${user.lastName[0]}</div>
                        <div class="user-details">
                            <p class="user-name">${user?.firstName} ${user.lastName}</p>
                            <div class="user-contact">
                                <span><i class="fas fa-envelope"></i> ${user.email}</span>
                                <span><i class="fas fa-phone"></i> ${user.phone}</span>
                            </div>
                        </div>
                    </div>
                </td>
                <td><span class="role-badge ${user?.roles?.toLowerCase()}">${user?.roles}</span></td>
                <td><span class="status-badge ${status}">${user?.isActive ? 'Active' : 'Inactive'}</span></td>
                <td><i class="fas fa-calendar"></i> ${new Date(user?.createdOn).toLocaleDateString()}</td>
                <td><i class="fas fa-clock"></i> ${user.modifiedOn ? new Date(user.modifiedOn).toLocaleDateString() : 'Never'}</td>
                <td>
                    <div class="dropdown">
                        <button class="dropdown-toggle">
                            <i class="fas fa-ellipsis-h"></i>
                        </button>
                        <div class="dropdown-menu">
                            <a href="#" onclick="getUserById(${user.userId})">Edit User</a>
                            <a href="#" onclick="toggleUserActive(${user.userId})">${user.isActive ? 'Deactivate' : 'Activate'} User</a>
                            <a href="#" class="danger" onclick="deleteUser(${user.userId})">Delete User</a>
                        </div>
                    </div>
                </td>
            </tr>
        `;
        table.row.add($(row));
    });

    // Redraw the table
    table.draw();
}

// Filter users by role or status
window.filterUsers = function (type, value) {
    currentFilters[type] = value;
    $('#' + type + 'FilterLabel').text(value.charAt(0).toUpperCase() + value.slice(1));
    filterAndRenderUsers();
};

/* ── 7. Utilities ─────────────────────────────────────────────────────── */

