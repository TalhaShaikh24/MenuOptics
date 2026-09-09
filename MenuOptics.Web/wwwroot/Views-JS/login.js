/* ============================================================================
 * login.js — Multi-step login page (Views/User/Login.cshtml)
 * Steps: email check → password (existing) / OTP + account setup (first-time)
 *
 * Project convention — every Views-JS file follows this section order:
 *   1. Constants  2. State  3. Initialization  4. Event bindings
 *   5. AJAX / CRUD  6. Helpers  7. Utilities
 * ==========================================================================*/

/* ── 3. Initialization / 4. Event bindings ────────────────────────────── */

$(document).ready(function () {


    const urlParams = new URLSearchParams(window.location.search);
    const email = urlParams.get('email');

    if (email) {
        $('#loginIdentifier').val(decodeURIComponent(email)).focus();
    }

    updateCompleteSetupState();

    // ──────────────────────────────────────────────────────────────
    // Helpers: show/hide steps
    // ──────────────────────────────────────────────────────────────
    function showStep(stepId) {
        $('#step-email, #step-password, #step-otp, #step-setup').hide();
        $('#' + stepId).show();
    }

    function resetToEmail() {
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

    // ──────────────────────────────────────────────────────────────
    // STEP 1 — Check Email  (unchanged logic)
    // ──────────────────────────────────────────────────────────────
    $('#checkEmailBtn').on('click', function () {
        var identifier = $.trim($('#loginIdentifier').val());
        var $btn = $(this);

        // Disable button and show loading spinner
        $btn.find('.btn-text').hide();
        $btn.find('.btn-loading').show();
        $btn.prop('disabled', true);

        ajaxRequest({
            url: '/api/UserApi/CheckUser',
            method: 'POST',
            data: { Email: identifier },
            showMessage: false,
            onSuccess: function (response) {
                var data = response.data;

                

                if (data.isRegistrationCompleted) {
                    // ── Already registered → password step
                    $('#confirmedIdentifier').val(identifier);
                    $('#resolvedUsername').val(data.username || identifier);
                    showStep('step-password');
                } else {
                    // ── First-time member → send OTP, then show OTP step
                    $('#setupUserId').val(data.userId);

                    // Send OTP to the user's email
                    ajaxRequest({
                        url: '/api/UserApi/SendOtp',
                        method: 'POST',
                        data: { UserId: data.userId },
                        showMessage: false,
                        onSuccess: function () {
                            showStep('step-otp');
                            // Reset button state in case the user navigates "Back" later
                            $btn.find('.btn-text').show();
                            $btn.find('.btn-loading').hide();
                            $btn.prop('disabled', false);
                        }
                    });
                }
            },
            onError: function (xhr, status, error) {
                // Reset button state if the request fails
                $btn.find('.btn-text').show();
                $btn.find('.btn-loading').hide();
                $btn.prop('disabled', false);
            }
        });
    });

    // Allow pressing Enter on the identifier field
    $('#loginIdentifier').on('keydown', function (e) {
        if (e.key === 'Enter') $('#checkEmailBtn').trigger('click');
    });

    // ──────────────────────────────────────────────────────────────
    // "Back" buttons
    // ──────────────────────────────────────────────────────────────
    $('#backToEmailBtn, #backToEmailBtnSetup, #backToEmailBtnOtp').on('click', resetToEmail);

    // ──────────────────────────────────────────────────────────────
    // STEP 2a — Normal Login  (unchanged)
    // ──────────────────────────────────────────────────────────────
    $('#loginFormBtn').on('click', function () {
        var username = $.trim($('#resolvedUsername').val());
        var password = $('#password').val();
        var $btn = $(this);

        // Disable button and show loading spinner
        $btn.find('.btn-text').hide();
        $btn.find('.btn-loading').show();
        $btn.prop('disabled', true);

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
                
                localStorage.setItem('profilePic', response.data.profilePic);
                localStorage.setItem('restaurantName', response.data.restaurantName);
                localStorage.setItem('phone', response.data.phone);
                localStorage.setItem('address', response.data.address);

                // Store welcome video flag so the Dashboard can show the video popup
                if (response.data.introVideoViewedOn == true) {
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

                // Reset button state so the user can try again
                $btn.find('.btn-text').show();
                $btn.find('.btn-loading').hide();
                $btn.prop('disabled', false);
            }
        });
    });

    // Allow pressing Enter on the password field
    $('#password').on('keydown', function (e) {
        if (e.key === 'Enter') $('#loginFormBtn').trigger('click');
    });

    // ──────────────────────────────────────────────────────────────
    // STEP 2c — Verify OTP  (NEW)
    // On success → show the setup form (step-setup)
    // ──────────────────────────────────────────────────────────────
    $('#verifyOtpBtn').on('click', function () {
        var userId  = $('#setupUserId').val();
        var otpCode = $.trim($('#otpCode').val());

        if (!otpCode || otpCode.length !== 6) {
            $('#error-message').text('Please enter the 6-digit code.').show();
            setTimeout(function () { $('#error-message').hide(); }, 3000);
            return;
        }

        var $btn = $('#verifyOtpBtn');
        $btn.find('.btn-text').hide();
        $btn.find('.btn-loading').show();
        $btn.prop('disabled', true);

        ajaxRequest({
            url: '/api/UserApi/VerifyOtp',
            method: 'POST',
            data: { UserId: userId, Otp: otpCode },
            showMessage: false,
            onSuccess: function (response) {
                // OTP is valid — now show the account setup form
                showStep('step-setup');
            },
            onError: function (xhr) {
                var msg = (xhr.responseJSON && xhr.responseJSON.responseMsg)
                    ? xhr.responseJSON.responseMsg
                    : 'Invalid or expired OTP. Please try again.';
                $('#error-message').text(msg).show();
                setTimeout(function () { $('#error-message').hide(); }, 4000);

                $btn.find('.btn-text').show();
                $btn.find('.btn-loading').hide();
                $btn.prop('disabled', false);
            }
        });
    });

    // Allow pressing Enter on the OTP field
    $('#otpCode').on('keydown', function (e) {
        if (e.key === 'Enter') $('#verifyOtpBtn').trigger('click');
    });

    // ── Resend OTP link ───────────────────────────────────────────
    $('#resendOtpLink').on('click', function (e) {
        e.preventDefault();
        var userId = $('#setupUserId').val();

        ajaxRequest({
            url: '/api/UserApi/SendOtp',
            method: 'POST',
            data: { UserId: userId },
            showMessage: false,
            onSuccess: function () {
                $('#otpCode').val('');
                $('#success-message').text('A new code has been sent to your email.').show();
                setTimeout(function () { $('#success-message').hide(); }, 4000);
            }
        });
    });

    // ──────────────────────────────────────────────────────────────
    // STEP 2b — Complete Registration  (unchanged)
    // ──────────────────────────────────────────────────────────────
    $('#completeSetupBtn').on('click', function () {
        var userId  = $('#setupUserId').val();
        var firstName = $.trim($('#setupFirstName').val());
        var lastName  = $.trim($('#setupLastName').val());
        var password  = $('#setupPassword').val();
        var confirm   = $('#setupPasswordConfirm').val();
        var picFile   = $('#setupProfilePic')[0].files[0];

        var formData = new FormData();
        formData.append('UserId',    userId);
        formData.append('FirstName', firstName);
        formData.append('LastName',  lastName);
        formData.append('Password',  password);
        if (picFile) {
            formData.append('File', picFile);
        }

        var $btn = $('#completeSetupBtn');
        $btn.find('.btn-text').hide();
        $btn.find('.btn-loading').show();
        $btn.prop('disabled', true);

        $.ajax({
            url: '/api/UserApi/CompleteRegistration',
            method: 'POST',
            data: formData,
            processData: false,
            contentType: false,
            success: function (response) {
                Cookies.set('JwtToken', response.data.token);
                localStorage.setItem('profilePic', response.data.profilePic);

                // Store welcome video flag so the Dashboard can show the video popup
                if (response.data.introVideoViewedOn == true) {
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
                $btn.find('.btn-text').show();
                $btn.find('.btn-loading').hide();
                $btn.prop('disabled', false);
            }
        });
    });

    // ──────────────────────────────────────────────────────────────
    // Forgot Password  (unchanged)
    // ──────────────────────────────────────────────────────────────
    $('#forgotPasswordLink').on('click', function (e) {
        e.preventDefault();
        window.location.href = '/User/ForgotPassword';
    });



    // Terms checkbox controls Next button availability
    function updateCompleteSetupState() {
        const isChecked = $('input[name="terms"]').is(':checked');
        $('#completeSetupBtn').prop('disabled', !isChecked);
    }

    $('input[name="terms"]').on('change', updateCompleteSetupState);

});