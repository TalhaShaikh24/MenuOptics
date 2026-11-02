// restaurant-members.js — Restaurant members page scripts.

let allMembers = [];
let membersLoaded = false;
let currentFilters = { search: '' };
let table;

$(document).ready(function () {
    table = initDataTable('#membersTable', {
        processing: true,
        loadingMessage: 'Loading members...',
        searching: false,
        ajax: function (dtParams, callback, settings) {
            if (membersLoaded) {
                callback({ data: getFilteredMembers() });
                return;
            }
            $.ajax({
                url: '/api/UserApi/GetRestaurantMembers',
                method: 'GET',
                success: function (response) {
                    allMembers = response.data || [];
                    membersLoaded = true;
                    callback({ data: getFilteredMembers() });
                },
                error: function (xhr, status, error) {
                    console.error('Failed to fetch members:', error);
                    showNotification('Failed to load members directory.', 'error');
                    callback({ data: [] });
                }
            });
        },
        columns: [
            {
                data: null,
                render: function (data, type, row) {
                    const initials = ((row.firstName ? row.firstName[0] : '') + (row.lastName ? row.lastName[0] : '')).toUpperCase() || 'RM';
                    const name = `${row.firstName || ''} ${row.lastName || ''}`.trim() || row.username;
                    return `<div class="user-info">
                                <div class="user-avatar">${initials}</div>
                                <div class="user-details">
                                    <p class="user-name">${name}</p>
                                    <span class="user-username">@${row.username}</span>
                                </div>
                            </div>`;
                }
            },
            { data: 'email' },
            {
                data: 'phone',
                render: function (phone) {
                    return phone || 'N/A';
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
                render: function (createdOn) {
                    const joinedDate = createdOn ? DateFormatter.formatWithComma(createdOn) : 'N/A';
                    return `<i class="fas fa-calendar"></i> ${joinedDate}`;
                }
            },
            {
                data: 'modifiedOn',
                render: function (modifiedOn) {
                    const modifiedDate = modifiedOn ? DateFormatter.formatWithComma(modifiedOn) : 'Never';
                    return `<i class="fas fa-clock"></i> ${modifiedDate}`;
                }
            },
            {
                data: null,
                render: function (data, type, row) {
                    const actionIcon = row.isActive ? 'fas fa-ban' : 'fas fa-check';
                    const actionText = row.isActive ? 'Deactivate' : 'Activate';
                    return `<div class="dropdown">
                                <button class="dropdown-toggle">
                                    <i class="fas fa-ellipsis-h"></i>
                                </button>
                                <div class="dropdown-menu">
                                    <a href="#" onclick="editMember(${row.userId})"><i class="fas fa-edit"></i> Edit Details</a>
                                    <a href="#" onclick="toggleMemberActive(${row.userId})"><i class="${actionIcon}"></i> ${actionText}</a>
                                </div>
                            </div>`;
                }
            }
        ],
        language: {
            emptyTable: 'No members found'
        }
    });

    refreshMemberStats();

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

    $('#saveMemberBtn').on('click', function () {
        const formEl = document.getElementById('addMemberForm');
        if (!formEl.checkValidity()) {
            formEl.reportValidity();
            return;
        }

        const formData = new FormData(formEl);
        addMember(formData);
    });

    $('#updateMemberBtn').on('click', function () {
        const formEl = document.getElementById('editMemberForm');
        if (!formEl.checkValidity()) {
            formEl.reportValidity();
            return;
        }

        const formData = new FormData(formEl);
        updateMember(formData);
    });

    $('#memberSearch').on('input', function () {
        currentFilters.search = $(this).val().toLowerCase();
        filterAndRenderMembers();
    });

    setInterval(refreshMemberTable, 30000);
});

function addMember(formData) {
    const $btn = $('#saveMemberBtn');
    setBtnLoading($btn, true);

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
        },
        onComplete: function () { setBtnLoading($btn, false); }
    });
}

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
            form.find('[name="profilePic"]').val(member.profilePic || '');
            form.find('[name="password"]').val('');

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

function updateMember(formData) {
    const $btn = $('#updateMemberBtn');
    setBtnLoading($btn, true);
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
        },
        onComplete: function () { setBtnLoading($btn, false); }
    });
}

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

function refreshMemberStats() {
    ajaxRequest({
        url: '/api/UserApi/GetMemberStats',
        method: 'GET',
        showMessage: false,
        onSuccess: function (response) {
            const stats = response.data || { totalCount: 0, activeCount: 0, inactiveCount: 0 };
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

function refreshMemberTable() {
    membersLoaded = false;
    if (table) {
        table.ajax.reload(null, false);
    }
}

function getFilteredMembers() {
    return allMembers.filter(member => {
        const matchesSearch = currentFilters.search === '' ||
            (member.firstName && member.firstName.toLowerCase().includes(currentFilters.search)) ||
            (member.lastName && member.lastName.toLowerCase().includes(currentFilters.search)) ||
            (member.email && member.email.toLowerCase().includes(currentFilters.search)) ||
            (member.username && member.username.toLowerCase().includes(currentFilters.search)) ||
            (member.phone && member.phone.toLowerCase().includes(currentFilters.search));

        return matchesSearch;
    });
}

function filterAndRenderMembers() {
    if (table) {
        table.ajax.reload(null, false);
    }
}

window.filterMembers = function (type, value) {
    filterAndRenderMembers();
};
