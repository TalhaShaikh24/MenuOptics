// login.js — Login page scripts.

$(document).ready(function () {
    const email = getQueryParam('email');

    if (email) {
        $('#loginIdentifier').val(decodeURIComponent(email)).focus();
    }

    if (getQueryParam('sessionExpired') === 'true') {
        showNotification('Your session has expired. Please log in again.', 'error');
        // Clean the URL so a refresh does not show the alert again
        window.history.replaceState({}, document.title, window.location.pathname);
    }

    updateCompleteSetupState();

    function showStep(stepId) {
        $('#step-email, #step-password, #step-otp, #step-setup').hide();
        $('#' + stepId).show();
    }

    function resetToEmail() {
        $('#checkEmailBtn').attr('disabled', false);
        $('#checkEmailBtn').text('Continue');
        $('#loginIdentifier').val('');
        $('#password').val('');
        $('#resolvedUsername').val('');
        $('#setupUserId').val('');
        $('#setupFirstName').val('');
        $('#setupLastName').val('');
        $('#setupPassword').val('');
        $('#setupPasswordConfirm').val('');
        $('#setupProfilePic').val('');
        $('#otpCode').val('');
        showStep('step-email');
    }

    $('#checkEmailBtn').on('click', function () {
        var identifier = $.trim($('#loginIdentifier').val());
        var $btn = $(this);

        setBtnLoading($btn, true);

        ajaxRequest({
            url: '/api/UserApi/CheckUser',
            method: 'POST',
            data: { Email: identifier },
            showMessage: false,
            onSuccess: function (response) {
                var data = response.data;

                if (data.isRegistrationCompleted) {
                    $('#confirmedIdentifier').val(identifier);
                    $('#resolvedUsername').val(data.username || identifier);
                    showStep('step-password');
                } else {
                    $('#setupUserId').val(data.userId);

                    ajaxRequest({
                        url: '/api/UserApi/SendOtp',
                        method: 'POST',
                        data: { UserId: data.userId },
                        showMessage: false,
                        onSuccess: function () {
                            showStep('step-otp');
                            setBtnLoading($btn, false);
                        }
                    });
                }
            },
            onError: function (xhr, status, error) {
                setBtnLoading($btn, false);
            }
        });
    });

    $('#loginIdentifier').on('keydown', function (e) {
        if (e.key === 'Enter') $('#checkEmailBtn').trigger('click');
    });

    $('#backToEmailBtn, #backToEmailBtnSetup, #backToEmailBtnOtp').on('click', resetToEmail);

    $('#loginFormBtn').on('click', function () {
        var username = $.trim($('#resolvedUsername').val());
        var password = $('#password').val();
        var $btn = $(this);

        setBtnLoading($btn, true);

        var formData = {
            Username: username,
            Password: password
        };

        ajaxRequest({
            url: '/api/UserApi/Login',
            method: 'POST',
            data: formData,
            showMessage: true,
            onSuccess: function (response) {
                Cookies.set('JwtToken', response.data.token);
                Cookies.set('WasLoggedIn', 'true', { expires: 30, path: '/' });

                localStorage.setItem('profilePic', response.data.profilePic);
                localStorage.setItem('restaurantName', response.data.restaurantName);
                localStorage.setItem('phone', response.data.phone);
                localStorage.setItem('address', response.data.address);

                // Store welcome video flag so the Dashboard can show the video popup
                if (response.data.hasViewedIntroVideo == false) {
                    sessionStorage.setItem('showWelcomeVideo', 'true');
                }

                var redirectUrl = response.data.roles.includes('Admin')
                    ? '/User/Index'
                    : '/Menu/Index';

                // Keep the button spinning while waiting for the redirect
                setTimeout(function () {
                    window.location.href = redirectUrl;
                }, 2000);
            },
            onError: function (xhr, status, error) {
                console.error('Login failed:', error);
                setBtnLoading($btn, false);
            }
        });
    });

    $('#password').on('keydown', function (e) {
        if (e.key === 'Enter') $('#loginFormBtn').trigger('click');
    });

    $('#verifyOtpBtn').on('click', function () {
        var userId = $('#setupUserId').val();
        var otpCode = $.trim($('#otpCode').val());

        if (!otpCode || otpCode.length !== 6) {
            return;
        }

        var $btn = $('#verifyOtpBtn');
        setBtnLoading($btn, true);

        ajaxRequest({
            url: '/api/UserApi/VerifyOtp',
            method: 'POST',
            data: { UserId: userId, Otp: otpCode },
            showMessage: false,
            onSuccess: function (response) {
                showStep('step-setup');
            },
            onError: function (xhr) {
                var msg = (xhr.responseJSON && xhr.responseJSON.responseMsg)
                    ? xhr.responseJSON.responseMsg
                    : 'Invalid or expired OTP. Please try again.';

                setBtnLoading($btn, false);
            }
        });
    });

    $('#otpCode').on('keydown', function (e) {
        if (e.key === 'Enter') $('#verifyOtpBtn').trigger('click');
    });

    $('#resendOtpLink').on('click', function (e) {
        e.preventDefault();
        var userId = $('#setupUserId').val();

        ajaxRequest({
            url: '/api/UserApi/SendOtp',
            method: 'POST',
            data: { UserId: userId },
            showMessage: false,
            onSuccess: function (res) {
                $('#otpCode').val('');
                showNotification(res.responseMsg);
            }
        });
    });

    $('#completeSetupBtn').on('click', function () {
        var userId = $('#setupUserId').val();
        var firstName = $.trim($('#setupFirstName').val());
        var lastName = $.trim($('#setupLastName').val());
        var password = $('#setupPassword').val();
        var confirm = $('#setupPasswordConfirm').val();
        var picFile = $('#setupProfilePic')[0].files[0];

        var formData = new FormData();
        formData.append('UserId', userId);
        formData.append('FirstName', firstName);
        formData.append('LastName', lastName);
        formData.append('Password', password);
        if (picFile) {
            formData.append('File', picFile);
        }

        var $btn = $('#completeSetupBtn');
        setBtnLoading($btn, true);

        $.ajax({
            url: '/api/UserApi/CompleteRegistration',
            method: 'POST',
            data: formData,
            processData: false,
            contentType: false,
            success: function (response) {
                Cookies.set('JwtToken', response.data.token);
                Cookies.set('WasLoggedIn', 'true', { expires: 30, path: '/' });
                localStorage.setItem('profilePic', response.data.profilePic);

                // Store welcome video flag so the Dashboard can show the video popup
                if (response.data.hasViewedIntroVideo == false) {
                    sessionStorage.setItem('showWelcomeVideo', 'true');
                }

                var redirectUrl = (response.data.roles && response.data.roles.includes('Admin'))
                    ? '/User/Index'
                    : '/Menu/Index';

                setTimeout(function () {
                    window.location.href = redirectUrl;
                }, 2000);
            },
            error: function (xhr) {
                setBtnLoading($btn, false);
            }
        });
    });

    $('#forgotPasswordLink').on('click', function (e) {
        e.preventDefault();
        window.location.href = '/User/ForgotPassword';
    });

    function updateCompleteSetupState() {
        const isChecked = $('input[name="terms"]').is(':checked');
        $('#completeSetupBtn').prop('disabled', !isChecked);
    }

    $('input[name="terms"]').on('change', updateCompleteSetupState);

});