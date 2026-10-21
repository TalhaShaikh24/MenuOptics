/* ============================================================================
 * restaurant.js — Admin restaurant management page (Views/Restaurant/Index.cshtml)
 * Restaurant table with stats, filters and add/edit modals.
 *
 * Project convention — every Views-JS file follows this section order:
 *   1. Constants  2. State  3. Initialization  4. Event bindings
 *   5. AJAX / CRUD  6. Helpers  7. Utilities
 * Functions stay global: they are referenced by inline onclick handlers.
 * ==========================================================================*/

/* ── 2. State ─────────────────────────────────────────────────────────── */

let allRestaurants = [];
let currentFilters = { cuisine: 'all', status: 'all', search: '' };

/* ── 3. Initialization / 4. Event bindings ────────────────────────────── */

$(document).ready(function () {

    // Initialize DataTable once on page load
    initDataTable();

    // Load stats and restaurants
    refreshRestaurantStats();
    refreshRestaurantTable();


    // Event handler for add restaurant
    $('#saveRestaurantBtn').on('click', function () {
        const formData = new FormData($('#addRestaurantForm')[0]);
        if (isFormValid("#addRestaurantForm")) {
            addRestaurant(formData);
        } else {
            showNotification('Please fill in all required fields.', 'error');
        }
    });

    // Event handler for update restaurant
    $('#updateRestaurantBtn').on('click', function () {
        const formData = new FormData($('#editRestaurantForm')[0]);
        if (isFormValid("#editRestaurantForm")) {
            updateRestaurant(formData);
        } else {
            showNotification('Please fill in all required fields.', 'error');
        }
    });

    // Search filter
    $('#restaurantSearch').on('input', function () {
        currentFilters.search = $(this).val().toLowerCase();
        filterAndRenderRestaurants();
    });

    // Toggle filter dropdowns
    $('.filter-dropdown .btn').on('click', function (e) {
        e.preventDefault();
        const $dropdown = $(this).closest('.filter-dropdown');
        const $menu = $dropdown.find('.dropdown-menu');
        $('.dropdown-menu').not($menu).removeClass('active');
        $menu.toggleClass('active');
    });

    // Close dropdowns when clicking outside
    $(document).on('click', function (e) {
        if (!$(e.target).closest('.filter-dropdown').length) {
            $('.dropdown-menu').removeClass('active');
        }
    });

    // Toggle action dropdowns
    $('.data-table').on('click', '.dropdown-toggle', function (e) {
        e.preventDefault();
        const $menu = $(this).siblings('.dropdown-menu');
        $('.dropdown-menu').not($menu).removeClass('active');
        $menu.toggleClass('active');
    });

    // Profile picture preview
    $('#addProfilePicFile').on('change', function () {
        const file = this.files[0];
        if (file) {
            const reader = new FileReader();
            reader.onload = function (e) {
                $('#addProfilePicPreview').attr('src', e.target.result).show();
            };
            reader.readAsDataURL(file);
        }
    });

    $('#editProfilePicFile').on('change', function () {
        const file = this.files[0];
        if (file) {
            const reader = new FileReader();
            reader.onload = function (e) {
                $('#editProfilePicPreview').attr('src', e.target.result).show();
                $('#editProfilePicLabel').text('Replace profile picture');
            };
            reader.readAsDataURL(file);
        }
    });
});

/* ── Validation ───────────────────────────────────────────────────────── */

// Validate restaurant data
function validateRestaurantData(formData) {
    const restaurantName = formData.get('restaurantName');
    const cuisineType = formData.get('cuisineType');
    const restaurantSize = formData.get('restaurantSize');
    const firstName = formData.get('firstName');
    const lastName = formData.get('lastName');
    const username = formData.get('username');
    const email = formData.get('email');
    const phone = formData.get('phone');
    return restaurantName && cuisineType && restaurantSize && firstName && lastName && username && email && phone && (formData.get('userId') );
}

/* ── 5. AJAX / CRUD ───────────────────────────────────────────────────── */

// Add restaurant
function addRestaurant(formData) {
    ajaxRequest({
        url: '/api/UserApi/CreateUser',
        method: 'POST',
        data: formData,
        contentType: false,
        processData: false,
        onSuccess: function (response) {
            refreshRestaurantStats();
            refreshRestaurantTable();
            $('#addRestaurantForm')[0].reset();
            $('#addProfilePicPreview').hide();
            closeModal('addRestaurantModal');
        },
        onError: function (xhr, status, error) {
            console.error('Failed to add restaurant:', error);
        }
    });
}

// Get restaurant by ID
function getRestaurantById(id) {
    ajaxRequest({
        url: `/api/UserApi/GetUser/${id}`,
        method: 'GET',
        showMessage: false,
        onSuccess: function (response) {
            const restaurant = response.data;
            $('#editRestaurantForm [name="userId"]').val(restaurant.userId);
            $('#editRestaurantForm [name="restaurantName"]').val(restaurant.restaurantName);
            $('#editRestaurantForm [name="cuisineType"]').val(restaurant.cuisineType);
            $('#editRestaurantForm [name="restaurantSize"]').val(restaurant.restaurantSize);
            $('#editRestaurantForm [name="firstName"]').val(restaurant.firstName);
            $('#editRestaurantForm [name="lastName"]').val(restaurant.lastName);
            $('#editRestaurantForm [name="username"]').val(restaurant.username).attr("disabled",true);
            $('#editRestaurantForm [name="email"]').val(restaurant.email).attr("disabled", true);;
            $('#editRestaurantForm [name="phone"]').val(restaurant.phone);
            $('#editRestaurantForm [name="address"]').val(restaurant.address);
            $('#editRestaurantForm [name="country"]').val(restaurant.country);
            $('#editRestaurantForm [name="state"]').val(restaurant.state);
            $('#editRestaurantForm [name="city"]').val(restaurant.city);
            $('#editRestaurantForm [name="zip"]').val(restaurant.zip);
            $('#editRestaurantForm [name="profilePic"]').val(restaurant.profilePic);
            if (restaurant.profilePic) {
                $('#editProfilePicPreview').attr('src', imageBaseUrl+restaurant.profilePic).show();
                $('#editProfilePicLabel').text('Replace profile picture');
            } else {
                $('#editProfilePicPreview').hide();
                $('#editProfilePicLabel').text('Choose new profile picture');
            }
            openModal('editRestaurantModal');
        },
        onError: function (xhr, status, error) {
            console.error('Failed to fetch restaurant:', error);
        }
    });
}

// Update restaurant
function updateRestaurant(formData) {
    ajaxRequest({
        url: '/api/UserApi/UpdateUser',
        method: 'PUT',
        data: formData,
        contentType: false,
        processData: false,
        onSuccess: function (response) {
            refreshRestaurantStats();
            refreshRestaurantTable();
            closeModal('editRestaurantModal');
        },
        onError: function (xhr, status, error) {
            console.error('Failed to update restaurant:', error);
        }
    });
}

// Toggle restaurant active status
function toggleRestaurantActive(id) {
    ajaxRequest({
        url: `/api/UserApi/ToggleUserActive/${id}`,
        method: 'PUT',
        showMessage: false,
        onSuccess: function (response) {
            refreshRestaurantStats();
            refreshRestaurantTable();
            showNotification(`Restaurant ${response.data.isActive ? 'activated' : 'deactivated'} successfully!`, 'success');
        },
        onError: function (xhr, status, error) {
            console.error('Failed to toggle restaurant active status:', error);
        }
    });
}



// Refresh restaurant stats
function refreshRestaurantStats() {
    ajaxRequest({
        url: '/api/UserApi/GetRestaurantStats',
        method: 'GET',
        showMessage: false,
        onSuccess: function (response) {
            const stats = response.data;
            $('#totalRestaurants').text(stats.totalCount);
            $('#activeToday').text(stats.activeCount);
            $('#inactiveRestaurants').text(stats.inactiveCount);
            $('#newThisWeek').text(stats.newThisWeekCount);

        },
        onError: function (xhr, status, error) {
            console.error('Failed to fetch restaurant stats:', error);
        }
    });
}

// Initialize DataTable
function initDataTable() {
    table = $('#restaurantTable').DataTable({
        paging: true,
        searching: true,
        ordering: false,
        order: [[0, 'asc']], // Sort by Name column by default
        pageLength: 10,
        lengthMenu: [10, 25, 50, 100],
        language: {
            search: '',
            searchPlaceholder: 'Search restaurant...'
        }
    });
}


// Refresh restaurant table
function refreshRestaurantTable() {
    
    ajaxRequest({
        url: '/api/UserApi/GetAllRestaurants',
        method: 'GET',
        showMessage: false,
        onSuccess: function (response) {
            allRestaurants = response.data;
            debugger
            filterAndRenderRestaurants();
        },
        onError: function (xhr, status, error) {
            console.error('Failed to fetch restaurants:', error);
            showNotification('Failed to load restaurants.', 'error');
        }
    });
}

/* ── 6. Helpers ───────────────────────────────────────────────────────── */

// Filter and render restaurants
function filterAndRenderRestaurants() {
    const filteredRestaurants = allRestaurants.filter(restaurant => {
        const search = currentFilters.search;
        const matchesSearch = search === '' ||
            (restaurant.restaurantName || '').toLowerCase().includes(search) ||
            (restaurant.email || '').toLowerCase().includes(search) ||
            (restaurant.username || '').toLowerCase().includes(search);
        const matchesCuisine = currentFilters.cuisine === 'all' ||
            (restaurant.cuisineType || '').toLowerCase() === currentFilters.cuisine.toLowerCase();
        const matchesStatus = currentFilters.status === 'all' ||
            (currentFilters.status === 'active' && restaurant.isActive) ||
            (currentFilters.status === 'inactive' && !restaurant.isActive);
        return matchesSearch && matchesCuisine && matchesStatus;
    });

    // Clear existing rows
    table.clear();

    filteredRestaurants.forEach(restaurant => {
        const status = restaurant.isActive ? 'active' : 'critical';
        const row = `
            <tr>
                <td>
                    <div class="user-info">
                        <div class="user-avatar">${restaurant?.restaurantName?.[0] ?? ""}</div>
                        <div class="user-details">
                            <p class="user-name">${restaurant.restaurantName}</p>
                            <div class="user-contact">
                                <span><i class="fas fa-envelope"></i> ${restaurant.email}</span>
                                <span><i class="fas fa-phone"></i> ${restaurant.phone}</span>
                            </div>
                        </div>
                    </div>
                </td>
                <td><span class="status-badge ${status}">${restaurant.isActive ? 'Active' : 'Inactive'}</span></td>
                <td><i class="fas fa-calendar"></i> ${new Date(restaurant.createdOn).toLocaleDateString()}</td>
                <td><i class="fas fa-clock"></i> ${restaurant.modifiedOn ? new Date(restaurant.modifiedOn).toLocaleDateString() : 'Never'}</td>
                <td>
                    <div class="dropdown">
                        <button class="dropdown-toggle">
                            <i class="fas fa-ellipsis-h"></i>
                        </button>
                        <div class="dropdown-menu">
                            <a href="#" onclick="getRestaurantById(${restaurant.userId})">Edit Restaurant</a>
                            <a href="#" onclick="toggleRestaurantActive(${restaurant.userId})">${restaurant.isActive ? 'Deactivate' : 'Activate'}</a>
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

// Filter restaurants by cuisine or status
window.filterRestaurants = function (type, value) {
    currentFilters[type] = value;
    $('#' + type + 'FilterLabel').text(value.charAt(0).toUpperCase() + value.slice(1));
    filterAndRenderRestaurants();
};

