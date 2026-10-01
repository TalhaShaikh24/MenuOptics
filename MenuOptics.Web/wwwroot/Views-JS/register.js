/* ============================================================================
 * register.js — Multi-step registration page (Views/User/Register.cshtml)
 * Step navigation, validation, password strength and Stripe checkout.
 *
 * Project convention — every Views-JS file follows this section order:
 *   1. Constants  2. State  3. Initialization  4. Event bindings
 *   5. AJAX / CRUD  6. Helpers  7. Utilities
 * Functions stay global: togglePassword is referenced by inline onclick.
 * ==========================================================================*/

/* ── 1. Constants ─────────────────────────────────────────────────────── */

const totalSteps = 4;

/* ── 2. State ─────────────────────────────────────────────────────────── */

let currentStep = 1;

/* ── 3. Initialization ────────────────────────────────────────────────── */

$(document).ready(function () {
    GetAllVendors();

})


// Run once on page load in case checkbox is pre-checked or browser autofills it
updateNextButtonState();




/* ── 4. Event bindings ────────────────────────────────────────────────── */

// Step navigation
$('.next-btn').on('click', function () {
    if (validateCurrentStep()) {
        if (currentStep < totalSteps) {
            currentStep++;
            updateStep();
        }
    }
});

$('.prev-btn').on('click', function () {
    if (currentStep > 1) {
        currentStep--;
        updateStep();
    }
});

// Password strength checker
$('#password').on('input', function () {
    const password = $(this).val();
    const strength = calculatePasswordStrength(password);
    updatePasswordStrength(strength);
});

// Terms + Authorize checkboxes control Next/Submit button availability
$('#authorizeTerms, #authorizeVendor').on('change', updateNextButtonState);

// Password confirmation validation
$('#confirmPassword').on('input', function () {
    const password = $('#password').val();
    const confirmPassword = $(this).val();

    if (confirmPassword && password !== confirmPassword) {
        $(this).addClass('error');
    } else {
        $(this).removeClass('error');
    }
});

// Form submission
$('#registerForm').on('submit', function (e) {
    e.preventDefault();

    if (!validateCurrentStep()) return;
    if (!isFormValid("#registerForm")) return;

    const password = $('#password').val();
    const confirmPassword = $('#confirmPassword').val();
    const selectedPlan = $('input[name="plan"]:checked').val();

    if (password !== confirmPassword) {
        showNotification('Passwords do not match', 'error');
        return;
    }

    const submitBtn = $('.submit-btn');
    const btnText = $('.btn-text');
    const btnLoading = $('.btn-loading');

    submitBtn.prop('disabled', true);
    btnText.hide();
    btnLoading.show();

    const formData = new FormData(this);

    ajaxRequest({
        url: '/api/UserApi/CreateUser',
        method: 'POST',
        data: formData,
        isQuery: false,
        showMessage: true, // Disable default success notification
        onSuccess: function (response) {

            if (response.status === 200) {
                // User created successfully - now initiate Stripe
                initiateStripeCheckout(selectedPlan, response.data.userId);
            } else {
                resetSubmitButton();
            }
        },
        onError: function (xhr) {
            const response = xhr.responseJSON;
            resetSubmitButton();
        }
    });

});

// Remove error class on input
$('input, select, textarea').on('input change', function () {
    $(this).removeClass('error');
});

/* ── 5. AJAX / CRUD ───────────────────────────────────────────────────── */

function initiateStripeCheckout(plan, userId) {
    $.ajax({
        url: '/api/UserApi/CreateCheckoutSession',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify({ plan: plan, userId: userId }),
        success: function (response) {
            if (response && response.url) {
                window.location.href = response.url;
            } else if (response && response.sessionId) {
                window.location.href = `https://checkout.stripe.com/c/pay/${response.sessionId}`;
            } else {
                showNotification('Payment session could not be created.', 'error');
                resetSubmitButton();
            }
        },
        error: function () {
            showNotification('Payment session could not be created.', 'error');
            resetSubmitButton();
        }
    });
}


function GetAllVendors() {
    ajaxRequest({
        url: '/api/UserApi/GetAllVendors',
        method: 'GET',
        isQuery: false,
        showMessage: false,

        onSuccess: function (response) {
            if (response.status === 200) {

                const $vendorDropdown = $('#primaryDistributor');
                $vendorDropdown.empty();
                debugger
                $vendorDropdown.append(
                    $('<option>', {
                        value: '',
                        text: 'Select Distributor'
                    })
                );

                var activeVendors = response.data.filter(x => x.isActive == true);

                activeVendors.forEach(function (vendor) {
                    $vendorDropdown.append(
                        $('<option>', {
                            value: vendor.id,
                            text: vendor.name
                        })
                    );
                });
            }
        },

        onError: function (xhr) {
            const response = xhr.responseJSON;

            resetSubmitButton();
        }
    });
}


/* ── 6. Helpers ───────────────────────────────────────────────────────── */

function updateStep() {
    // Update step indicator
    $('.step').removeClass('active completed');
    for (let i = 1; i <= currentStep; i++) {
        $(`.step[data-step="${i}"]`).addClass(i === currentStep ? 'active' : 'completed');
    }

    // Update form steps
    $('.form-step').removeClass('active');
    $(`.form-step[data-step="${currentStep}"]`).addClass('active');

    // Update navigation buttons
    $('.prev-btn').toggle(currentStep > 1);
    $('.next-btn').toggle(currentStep < totalSteps);
    $('.submit-btn').toggle(currentStep === totalSteps);
}

// Terms + Authorize checkbox both must be checked to enable Next / Submit
function updateNextButtonState() {
    const termsChecked    = $('#authorizeTerms').is(':checked');
    const authorizeChecked = $('#authorizeVendor').is(':checked');
    const allChecked = termsChecked && authorizeChecked;
    $('.next-btn').prop('disabled', !allChecked);
    $('.submit-btn').prop('disabled', !allChecked);
}

function resetSubmitButton() {
    const submitBtn = $('.submit-btn');
    const btnText = $('.btn-text');
    const btnLoading = $('.btn-loading');

    submitBtn.prop('disabled', false);
    btnText.show();
    btnLoading.hide();
}

/* ── 7. Validation ────────────────────────────────────────────────────── */

function validateCurrentStep() {
    const currentStepElement = $(`.form-step[data-step="${currentStep}"]`);
    const requiredFields = currentStepElement.find('input[required]:not([type="checkbox"]), select[required], textarea[required]');
    let isValid = true;

    requiredFields.each(function () {
        if (!$(this).val().trim()) {
            $(this).addClass('error');
            isValid = false;
        } else {
            $(this).removeClass('error');
        }
    });

    // Validate required checkboxes in the current step
    currentStepElement.find('input[type="checkbox"][required]').each(function () {
        if (!$(this).is(':checked')) {
            $(this).closest('.checkbox-label').addClass('error');
            isValid = false;
        } else {
            $(this).closest('.checkbox-label').removeClass('error');
        }
    });

    if (!isValid) {
        showNotification('Please fill in all required fields', 'error');
    }

    return isValid;
}

function calculatePasswordStrength(password) {
    let score = 0;
    if (password.length >= 8) score++;
    if (/[a-z]/.test(password)) score++;
    if (/[A-Z]/.test(password)) score++;
    if (/[0-9]/.test(password)) score++;
    if (/[^A-Za-z0-9]/.test(password)) score++;
    return score;
}

function updatePasswordStrength(strength) {
    const strengthFill = $('.strength-fill');
    const strengthText = $('.strength-text');

    const levels = ['Very Weak', 'Weak', 'Fair', 'Good', 'Strong'];
    const colors = ['#ef4444', '#f97316', '#eab308', '#22c55e', '#16a34a'];

    strengthFill.css({
        'width': `${(strength / 5) * 100}%`,
        'background-color': colors[strength - 1] || '#e5e7eb'
    });

    strengthText.text(strength > 0 ? levels[strength - 1] : 'Password strength');
}

/* ── 8. Utilities ─────────────────────────────────────────────────────── */

// Password toggle function (called from inline onclick in the view)
function togglePassword(inputId) {
    const input = document.getElementById(inputId);
    const icon = input.nextElementSibling.querySelector('i');

    if (input.type === 'password') {
        input.type = 'text';
        icon.classList.remove('fa-eye');
        icon.classList.add('fa-eye-slash');
    } else {
        input.type = 'password';
        icon.classList.remove('fa-eye-slash');
        icon.classList.add('fa-eye');
    }
}
