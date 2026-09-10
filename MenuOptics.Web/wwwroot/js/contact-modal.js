/* ═════════════════════════════════════════════════════════════════
   Menu Optics - Contact Us "Get in Touch" Modal Controller
   Handles opening/closing modal, form validation, AJAX submission,
   auto-prefilling for logged-in users, and mailto interception.
   ═════════════════════════════════════════════════════════════════ */

(function ($) {
    'use strict';

    // Global helper: Open Contact Modal
    window.openContactModal = function (options) {
        options = options || {};

        var $overlay = $('#contactModalOverlay');
        if (!$overlay.length) return;

        var $form = $('#contactInquiryForm');
        var $success = $('#contactSuccessCard');
        var $alert = $('#contactModalAlert');

        // Reset display states
        $alert.removeClass('error').hide().empty();
        $success.removeClass('active').hide();
        $form.show();

        // Reset submit button state
        var $submitBtn = $('#btnContactSubmit');
        $submitBtn.prop('disabled', false).html('Send Message &rarr;');

        // Check if user is logged in via dataset on overlay
        var defaultFirstName = $overlay.attr('data-user-firstname') || '';
        var defaultLastName = $overlay.attr('data-user-lastname') || '';
        var defaultEmail = $overlay.attr('data-user-email') || '';

        // Populate fields if empty or passed via options
        var firstName = options.firstName !== undefined ? options.firstName : ($('#contactFirstName').val() || defaultFirstName);
        var lastName = options.lastName !== undefined ? options.lastName : ($('#contactLastName').val() || defaultLastName);
        var email = options.email !== undefined ? options.email : ($('#contactEmail').val() || defaultEmail);
        var phone = options.phone !== undefined ? options.phone : ($('#contactPhone').val() || '');
        var reason = options.reason || '';
        var message = options.message || '';

        $('#contactFirstName').val(firstName);
        $('#contactLastName').val(lastName);
        $('#contactEmail').val(email);
        $('#contactPhone').val(phone);

        if (reason) {
            $('#contactReason').val(reason);
        }

        if (message) {
            $('#contactMessage').val(message);
        }

        // Update character counter
        updateCharCount();

        // Show overlay with transition
        $overlay.css('display', 'flex');
        requestAnimationFrame(function () {
            $overlay.addClass('active');
        });
        $('body').css('overflow', 'hidden');

        // Auto focus
        setTimeout(function () {
            if (!firstName) {
                $('#contactFirstName').focus();
            } else if (!email) {
                $('#contactEmail').focus();
            } else if (!reason) {
                $('#contactReason').focus();
            } else {
                $('#contactMessage').focus();
            }
        }, 150);
    };

    // Global helper: Close Contact Modal
    window.closeContactModal = function () {
        var $overlay = $('#contactModalOverlay');
        if (!$overlay.length) return;

        $overlay.removeClass('active');
        setTimeout(function () {
            $overlay.css('display', 'none');
            $('body').css('overflow', '');
        }, 250);
    };

    function updateCharCount() {
        var len = ($('#contactMessage').val() || '').length;
        $('#contactCharCount').text(len);
    }

    function showAlert(message) {
        var $alert = $('#contactModalAlert');
        $alert.addClass('error').html('<i class="fas fa-exclamation-circle"></i> <span>' + message + '</span>').slideDown(200);
    }

    function hideAlert() {
        $('#contactModalAlert').removeClass('error').slideUp(150).empty();
    }

    $(document).ready(function () {
        // Character counter on typing
        $(document).on('input', '#contactMessage', function () {
            updateCharCount();
            hideAlert();
        });

        $(document).on('input change', '#contactInquiryForm input, #contactInquiryForm select', function () {
            hideAlert();
        });

        // Close button clicks
        $(document).on('click', '#contactModalCloseBtn, #contactModalDoneBtn', function (e) {
            e.preventDefault();
            closeContactModal();
        });

        // Click outside card to close
        $(document).on('click', '#contactModalOverlay', function (e) {
            if (e.target === this) {
                closeContactModal();
            }
        });

        // Escape key to close
        $(document).on('keydown', function (e) {
            if (e.key === 'Escape' && $('#contactModalOverlay').hasClass('active')) {
                closeContactModal();
            }
        });

        // Global Interception for .open-contact-modal and [data-open-contact]
        $(document).on('click', '.open-contact-modal, [data-open-contact="true"]', function (e) {
            e.preventDefault();
            var reason = $(this).attr('data-reason') || '';
            openContactModal({ reason: reason });
        });

        // Intercept mailto: links globally so users get the modern Contact Us modal instead of launching external mail client
        $(document).on('click', 'a[href^="mailto:"]', function (e) {
            e.preventDefault();
            var reason = $(this).attr('data-reason') || 'Support';
            openContactModal({ reason: reason });
        });

        // Handle Contact Form Submission
        $(document).on('submit', '#contactInquiryForm', function (e) {
            e.preventDefault();
            hideAlert();

            var firstName = $.trim($('#contactFirstName').val());
            var lastName = $.trim($('#contactLastName').val());
            var email = $.trim($('#contactEmail').val());
            var phone = $.trim($('#contactPhone').val());
            var reason = $.trim($('#contactReason').val());
            var message = $.trim($('#contactMessage').val());
            var preferredContactMethod = $('input[name="preferredContactMethod"]:checked').val() || 'Email';

            // Validation
            if (!firstName || !lastName) {
                showAlert('Please enter both your first name and last name.');
                return;
            }

            var emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
            if (!email || !emailRegex.test(email)) {
                showAlert('Please enter a valid email address.');
                $('#contactEmail').focus();
                return;
            }

            if (!reason) {
                showAlert('Please select a reason for your inquiry.');
                $('#contactReason').focus();
                return;
            }

            if (!message) {
                showAlert('Please enter your message details.');
                $('#contactMessage').focus();
                return;
            }

            if (message.length > 1000) {
                showAlert('Message exceeds 1,000 characters. Please shorten your message.');
                return;
            }

            var payload = {
                firstName: firstName,
                lastName: lastName,
                email: email,
                phoneNumber: phone,
                reason: reason,
                message: message,
                preferredContactMethod: preferredContactMethod
            };

            var $submitBtn = $('#btnContactSubmit');
            $submitBtn.prop('disabled', true).html('<i class="fas fa-spinner fa-spin"></i> Sending...');

            $.ajax({
                url: '/api/ContactApi/SendInquiry',
                type: 'POST',
                contentType: 'application/json; charset=utf-8',
                data: JSON.stringify(payload),
                success: function (res) {
                    // Show success state
                    $('#contactInquiryForm').slideUp(200, function () {
                        $('#contactSuccessCard').addClass('active').fadeIn(250);
                    });

                    // Clear fields (except name/email for logged in users)
                    $('#contactMessage').val('');
                    $('#contactPhone').val('');
                    $('#contactReason').val('');
                    updateCharCount();

                    if (typeof showNotification === 'function') {
                        showNotification('Message sent successfully! Our team will respond shortly.', 'success');
                    }
                },
                error: function (xhr) {
                    var errorMsg = 'Failed to send message. Please try again.';
                    if (xhr.responseJSON && xhr.responseJSON.responseMsg) {
                        errorMsg = xhr.responseJSON.responseMsg;
                    } else if (xhr.responseText) {
                        try {
                            var parsed = JSON.parse(xhr.responseText);
                            if (parsed.responseMsg) errorMsg = parsed.responseMsg;
                        } catch (e) { }
                    }
                    showAlert(errorMsg);
                    $submitBtn.prop('disabled', false).html('Send Message &rarr;');
                }
            });
        });
    });
})(jQuery);
