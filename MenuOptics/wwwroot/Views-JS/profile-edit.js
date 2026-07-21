/* ============================================================================
 * profile-edit.js — Profile edit page (Views/Profile/Index.cshtml)
 *
 * Project convention — every Views-JS file follows this section order:
 *   1. Constants  2. State  3. Initialization  4. Event bindings
 *   5. AJAX / CRUD  6. Helpers  7. Utilities
 * ==========================================================================*/

/* ── 3. Initialization / 4. Event bindings ────────────────────────────── */

$(document).ready(function () {
    loadCurrentProfile();

    // Image preview logic
    $('#profileFile').on('change', function () {
        const file = this.files[0];
        if (file) {
            const reader = new FileReader();
            reader.onload = function (e) {
                $('#profilePreview').attr('src', e.target.result);
            };
            reader.readAsDataURL(file);
        }
    });

    // Form Submit
    $('#saveProfileBtn').on('click', function () {
        const formData = new FormData(document.getElementById('profileEditForm'));

        ajaxRequest({
            url: '/api/ProfileApi/UpdateProfile',
            method: 'PUT',
            data: formData,
            contentType: false,
            processData: false,
            onSuccess: function (response) {
                localStorage.setItem("profilePic", response.data.profilePic);
                showNotification('Profile updated successfully!', 'success');
            }
        });
    });

});

/* ── 5. AJAX / CRUD ───────────────────────────────────────────────────── */

function loadCurrentProfile() {
    ajaxRequest({
        url: '/api/ProfileApi/GetCurrentUser',
        method: 'GET',
        showMessage: false,
        onSuccess: function (response) {
            const user = response.data;
            const form = $('#profileEditForm');

            form.find('[name="firstName"]').val(user.firstName);
            form.find('[name="lastName"]').val(user.lastName);
            form.find('[name="username"]').val(user.username);
            form.find('[name="email"]').val(user.email);
            form.find('[name="phone"]').val(user.phone);
            form.find('[name="address"]').val(user.address);
            form.find('[name="city"]').val(user.city);
            form.find('[name="zip"]').val(user.zip);
            form.find('#existingProfilePic').val(user.profilePic);

            if (user.profilePic) {
                $('#profilePreview').attr('src', user.profilePic);
            }
        }
    });
}
