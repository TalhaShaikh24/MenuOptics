let allMembers = [];
let currentFilters = { status: 'all', search: '' };
let table;

$(document).ready(function () {
    // Initialize DataTable once on page load
    initDataTable();

    // Load stats and members
    refreshMemberStats();
    refreshMemberTable();

    // Event handler for image upload preview in Edit Modal
    $('#editMemberProfilePicFile').on('change', function () {
        const file = this.files[0];
        if (file) {
            const reader = new FileReader();
            reader.onload = function (e) {
                $('#editMemberProfilePicPreview').attr('src', e.target.result).show();
            };
            reader.readAsDataURL(file);
            $('#editMemberProfilePicLabel').text(file.name);
        }
    });

    // Event handler for add member
    $('#saveMemberBtn').on('click', function () {
        const formEl = document.getElementById('addMemberForm');
        if (!formEl.checkValidity()) {
            formEl.reportValidity();
            return;
        }

        const formData = new FormData(formEl);
        addMember(formData);
    });

    // Event handler for update member
    $('#updateMemberBtn').on('click', function () {
        const formEl = document.getElementById('editMemberForm');
        if (!formEl.checkValidity()) {
            formEl.reportValidity();
            return;
        }

        const formData = new FormData(formEl);
        // Map checkbox value
        const isActiveCheckbox = $('#editMemberIsActive').is(':checked');
        formData.set('isActive', isActiveCheckbox ? 'true' : 'false');

        updateMember(formData);
    });

    // Search filter
    $('#memberSearch').on('input', function () {
        currentFilters.search = $(this).val().toLowerCase();
        filterAndRenderMembers();
    });

    // Auto-refresh table every 30 seconds
    setInterval(refreshMemberTable, 30000);
});

// Add member
function addMember(formData) {
    // Convert DTO fields from FormData structure if needed
    const payload = {};
    formData.forEach((value, key) => {
        payload[key] = value;
    });

    ajaxRequest({
        url: '/api/UserApi/AddRestaurantMember',
        method: 'POST',
        data: payload,
        contentType: 'application/json',
        onSuccess: function (response) {
            refreshMemberStats();
            refreshMemberTable();
            $('#addMemberForm')[0].reset();
            closeModal('addMemberModal');
        },
        onError: function (xhr, status, error) {
            console.error('Failed to add member:', error);
        }
    });
}

// Get member by ID for editing
window.editMember = function (id) {
    ajaxRequest({
        url: `/api/UserApi/GetUser/${id}`,
        method: 'GET',
        showMessage: false,
        onSuccess: function (response) {
            const member = response.data;
            const form = $('#editMemberForm');
            
            form.find('[name="memberId"]').val(member.userId);
            form.find('[name="firstName"]').val(member.firstName);
            form.find('[name="lastName"]').val(member.lastName);
            form.find('[name="username"]').val(member.username);
            form.find('[name="email"]').val(member.email);
            form.find('[name="phone"]').val(member.phone);
            form.find('[name="address"]').val(member.address);
            form.find('[name="country"]').val(member.country);
            form.find('[name="state"]').val(member.state);
            form.find('[name="city"]').val(member.city);
            form.find('[name="zip"]').val(member.zip);
            form.find('[name="gender"]').val(member.gender || "");
            form.find('[name="profilePic"]').val(member.profilePic || "");
            form.find('[name="password"]').val(""); // Reset password field

            $('#editMemberIsActive').prop('checked', member.isActive);

            if (member.profilePic) {
                $('#editMemberProfilePicPreview').attr('src', member.profilePic).show();
                $('#editMemberProfilePicLabel').text('Replace profile picture');
            } else {
                $('#editMemberProfilePicPreview').hide();
                $('#editMemberProfilePicLabel').text('Choose new profile picture');
            }

            openModal('editMemberModal');
        },
        onError: function (xhr, status, error) {
            console.error('Failed to fetch member:', error);
            showNotification('Failed to fetch member.', 'error');
        }
    });
};

// Update member details
function updateMember(formData) {
    ajaxRequest({
        url: '/api/UserApi/UpdateRestaurantMember',
        method: 'PUT',
        data: formData,
        contentType: false,
        processData: false,
        onSuccess: function (response) {
            refreshMemberStats();
            refreshMemberTable();
            closeModal('editMemberModal');
            $('#editMemberForm')[0].reset();
        },
        onError: function (xhr, status, error) {
            console.error('Failed to update member:', error);
        }
    });
}

// Toggle member active/inactive status
window.toggleMemberActive = function (id) {
    ajaxRequest({
        url: `/api/UserApi/ToggleMemberActive/${id}`,
        method: 'PUT',
        showMessage: false,
        onSuccess: function (response) {
            refreshMemberStats();
            refreshMemberTable();
            showNotification(`Member ${response.data.isActive ? 'activated' : 'deactivated'} successfully!`, 'success');
        },
        onError: function (xhr, status, error) {
            console.error('Failed to toggle active status:', error);
            showNotification('Failed to update member status.', 'error');
        }
    });
};


// Refresh member stats cards
function refreshMemberStats() {
    ajaxRequest({
        url: '/api/UserApi/GetMemberStats',
        method: 'GET',
        showMessage: false,
        onSuccess: function (response) {
            const stats = response.data || { totalCount: 0, activeCount: 0, inactiveCount: 0};
            $('#totalMembers').text(stats.totalCount);
            $('#activeMembers').text(stats.activeCount);
            $('#inactiveMembers').text(stats.inactiveCount);
        },
        onError: function (xhr, status, error) {
            console.error('Failed to fetch stats:', error);
            $('#totalMembers').text('0');
            $('#activeMembers').text('0');
            $('#inactiveMembers').text('0');
        }
    });
}

// Initialize DataTable
function initDataTable() {
    table = $('.data-table').DataTable({
        paging: true,
        searching: false, // We handle search custom filter
        ordering: true,
        order: [[0, 'asc']], // Sort by Name column by default
        pageLength: 10,
        lengthMenu: [10, 25, 50, 100],
        language: {
            emptyTable: "No members found"
        }
    });
}

// Refresh table data
function refreshMemberTable() {
    ajaxRequest({
        url: '/api/UserApi/GetRestaurantMembers',
        method: 'GET',
        showMessage: false,
        onSuccess: function (response) {
            allMembers = response.data || [];
            filterAndRenderMembers();
        },
        onError: function (xhr, status, error) {
            console.error('Failed to fetch members:', error);
            showNotification('Failed to load members directory.', 'error');
        }
    });
}

// Filter and render members
function filterAndRenderMembers() {
    const filtered = allMembers.filter(member => {
        const matchesSearch = currentFilters.search === '' ||
            (member.firstName && member.firstName.toLowerCase().includes(currentFilters.search)) ||
            (member.lastName && member.lastName.toLowerCase().includes(currentFilters.search)) ||
            (member.email && member.email.toLowerCase().includes(currentFilters.search)) ||
            (member.username && member.username.toLowerCase().includes(currentFilters.search)) ||
            (member.phone && member.phone.toLowerCase().includes(currentFilters.search));
            
        const matchesStatus = currentFilters.status === 'all' ||
            (currentFilters.status === 'active' && member.isActive) ||
            (currentFilters.status === 'inactive' && !member.isActive);
            
        return matchesSearch && matchesStatus;
    });

    table.clear();

    filtered.forEach(member => {
        const initials = ((member.firstName ? member.firstName[0] : '') + (member.lastName ? member.lastName[0] : '')).toUpperCase() || 'RM';
        const name = `${member.firstName || ''} ${member.lastName || ''}`.trim() || member.username;
        const joinedDate = member.createdOn ? DateFormatter.formatWithComma(member.createdOn) : 'N/A';
        const modifiedDate = member.modifiedOn ? DateFormatter.formatWithComma(member.modifiedOn) : 'Never';
        const statusClass = member.isActive ? 'active' : 'inactive';
        const statusText = member.isActive ? 'Active' : 'Inactive';
        const actionIcon = member.isActive ? 'fas fa-ban' : 'fas fa-check';
        const actionText = member.isActive ? 'Deactivate' : 'Activate';

        const row = `
            <tr>
                <td>
                    <div class="user-info">
                        <div class="user-avatar">${initials}</div>
                        <div class="user-details">
                            <p class="user-name">${name}</p>
                            <span class="user-username">@${member.username}</span>
                        </div>
                    </div>
                </td>
                <td>${member.email}</td>
                <td>${member.phone || 'N/A'}</td>
                <td><span class="status-badge ${statusClass}">${statusText}</span></td>
                <td><i class="fas fa-calendar"></i> ${joinedDate}</td>
                <td><i class="fas fa-clock"></i> ${modifiedDate}</td>
                <td>
                    <div class="dropdown">
                        <button class="dropdown-toggle">
                            <i class="fas fa-ellipsis-h"></i>
                        </button>
                        <div class="dropdown-menu">
                            <a href="#" onclick="editMember(${member.userId})"><i class="fas fa-edit"></i> Edit Details</a>
                            <a href="#" onclick="toggleMemberActive(${member.userId})"><i class="${actionIcon}"></i> ${actionText}</a>
                        </div>
                    </div>
                </td>
            </tr>
        `;
        table.row.add($(row));
    });

    table.draw();
}

// Dropdown status filter
window.filterMembers = function (type, value) {
    if (type === 'status') {
        currentFilters.status = value;
        $('#memberStatusFilterLabel').text(value.charAt(0).toUpperCase() + value.slice(1));
    }
    filterAndRenderMembers();
};
