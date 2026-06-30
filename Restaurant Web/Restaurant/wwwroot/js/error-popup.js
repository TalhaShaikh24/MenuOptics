(function ($) {
    $.showCustomPopup = function (options) {
        // Default settings
        var settings = $.extend({
            title: "Something Went Wrong",
            iconClass: "fas fa-redo-alt",
            message: `
                <p>We're currently experiencing an issue. Our team is actively investigating and working on a fix. We apologise for the inconvenience and appreciate your patience.</p>
                <p>If the issue continues to persist, please contact our support at 
                    <a href="mailto:support@propertelligent.com" class="support-link">
                        support@propertelligent.com
                    </a>
                </p>`,
            overlayId: "popup-overlay"
        }, options);

        // If popup already exists, remove it to avoid duplication
        $('#' + settings.overlayId).remove();

        // Create popup HTML
        var popupHTML = `
        <div id="${settings.overlayId}" class="popup-overlay show">
            <div class="error-modal">
                <button class="close-btn" id="close-popup">
                    <i class="fas fa-times"></i>
                </button>
                <div class="error-modal-content">
                    <div class="icon-container">
                        <div class="icon-circle">
                            <i class="${settings.iconClass}"></i>
                        </div>
                    </div>
                    <h2 class="error-modal-title">${settings.title}</h2>
                    <div class="error-modal-description">
                        ${settings.message}
                    </div>
                </div>
            </div>
        </div>`;

        // Append to body
        $('body').append(popupHTML);
        $('body').css('overflow', 'hidden');

        // Close Handlers
        $('#close-popup').on('click', closePopup);
        $('#' + settings.overlayId).on('click', function (e) {
            if (e.target === this) closePopup();
        });
        $(document).on('keydown.popup', function (e) {
            if (e.key === 'Escape') closePopup();
        });

        function closePopup() {
            $('#' + settings.overlayId).remove();
            $('body').css('overflow', 'auto');
            $(document).off('keydown.popup'); // Clean up
        }
    };
})(jQuery);