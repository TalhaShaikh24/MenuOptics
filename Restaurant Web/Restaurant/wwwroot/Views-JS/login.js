$(document).ready(function () {
    $('#loginForm').on('submit', function (e) {
        e.preventDefault();
        var formData = {
            Username: $('#username').val(),
            Password: $('#password').val()
        };

        ajaxRequest({
            url: '/api/UserApi/Login',
            method: 'POST',
            data: formData,
            showMessage: true,
            onSuccess: function (response) {
                var redirectUrl = response.data.roles.includes('Admin') ? '/User/Index' : '/Menu/Index';
                setTimeout(function () {
                    window.location.href = redirectUrl;
                }, 2000);
            },
            onError: function (xhr, status, error) {
                console.error('Failed to delete menu item:', error);
            }
        });

    })

});

