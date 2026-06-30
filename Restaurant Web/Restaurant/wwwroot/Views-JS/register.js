let currentStep = 1;
const totalSteps = 4;
const stripe = Stripe('pk_test_51M9O4qKR3yBF1l8fXy3z9Vvtnn8A5e4frQt5lJgfpPOBcBMx6ZZFG93mpFCWgN0EjYXL0l7ioxvtSA07AJzUUOJX00XWJkik2w');

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

function validateCurrentStep() {
    const currentStepElement = $(`.form-step[data-step="${currentStep}"]`);
    const requiredFields = currentStepElement.find('input[required], select[required], textarea[required]');
    let isValid = true;

    requiredFields.each(function () {
        if (!$(this).val().trim()) {
            $(this).addClass('error');
            isValid = false;
        } else {
            $(this).removeClass('error');
        }
    });

    if (!isValid) {
        showNotification('Please fill in all required fields', 'error');
    }

    return isValid;
}

// Password strength checker
$('#password').on('input', function () {
    const password = $(this).val();
    const strength = calculatePasswordStrength(password);
    updatePasswordStrength(strength);
});

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
            debugger;
            if (response.status === 200) {
                // User created successfully - now initiate Stripe
                initiateStripeCheckout(selectedPlan, response.data.userId);
            } else {
                $('#error-message').text(response.responseMsg).show();
                $('#success-message').hide();
                resetSubmitButton();
            }
        },
        onError: function (xhr) {
            const response = xhr.responseJSON;
            $('#error-message').text(response?.responseMsg || 'Server error occurred.').show();
            $('#success-message').hide();
            resetSubmitButton();
        }
    });

});

function resetSubmitButton() {
    const submitBtn = $('.submit-btn');
    const btnText = $('.btn-text');
    const btnLoading = $('.btn-loading');

    submitBtn.prop('disabled', false);
    btnText.show();
    btnLoading.hide();
}

function initiateStripeCheckout(plan, userId) {


    $.ajax({
        url: '/api/UserApi/CreateCheckoutSession',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify({ plan: plan, userId: userId }),
        success: function (response) {
            stripe.redirectToCheckout({ sessionId: response.sessionId });
        },
        error: function () {
            showNotification('Payment session could not be created.', 'error');
            resetSubmitButton();
        }
    });
}


// Remove error class on input
$('input, select, textarea').on('input change', function () {
    $(this).removeClass('error');
});

// Password toggle function
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