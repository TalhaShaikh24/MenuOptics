/* ============================================================================
 * module-tutorials.js — Hardcoded Tutorial Video Player per Module
 * Adds interactive play icon next to Page Headers (<h1>) and displays popup
 * video tutorial modals with navigation arrows for multiple videos.
 * ==========================================================================*/

// Hardcoded Array / Config of Module Tutorial Videos
const MODULE_TUTORIALS = {
    "Ingredients": [
        {
            title: "1. Adding an Ingredient",
            description: "Learn how to add a new ingredient, including its cost and details.",
            url: "/videos/Add Ingredient final.mp4"
        }
    ],
    "Recipe Management": [
        {
            title: "1. Creating a Batch Recipe",
            description: "Learn how to create a batch recipe using your ingredients.",
            url: "/videos/Create a batch Recipe.mp4"
        },
        {
            title: "2. Creating a PDF Recipe Card",
            description: "Learn how to generate a printable PDF recipe card for a batch recipe.",
            url: "/videos/Creating a PDF recipe card batch Recipe.mp4"
        }
    ],
    "Menu Item Management": [
        {
            title: "1. Creating a Menu Item",
            description: "Learn how to create a new menu item and add it to your menu.",
            url: "/videos/Creating a menu item.mp4"
        }
    ]
};

$(document).ready(function () {
    let currentModuleKey = null;
    let currentVideoIndex = 0;
    let currentVideos = [];

    // 1. Automatically detect module header and append play icon with tooltip
    initModuleHeaderButton();

    function initModuleHeaderButton() {
        const $header = $('.header-text h1').first();
        const $headerText = $header.text().trim();
      
        // Match against MODULE_TUTORIALS keys
        let matchedKey = null;
        for (const key in MODULE_TUTORIALS) {
            if ($headerText.toLowerCase() === key.toLowerCase()) {
                matchedKey = key;
                break;
            }
        }

       

        if (matchedKey && MODULE_TUTORIALS[matchedKey]) {
            currentModuleKey = matchedKey;

            // Check if play button already appended
            if ($header.find('.header-tutorial-btn').length === 0) {
                const btnHtml = `
                    <button type="button" class="header-tutorial-btn" id="btnOpenModuleTutorial" data-module="${matchedKey}" title="Watch Tutorial Video" aria-label="Watch Tutorial Video">
                        <i class="fas fa-play-circle"></i>
                        <span class="header-tutorial-tooltip">Watch Tutorial</span>
                    </button>
                `;
                $header.append(btnHtml);
            }
        }
    }

    // 2. Click Handler to open modal
    $(document).on('click', '#btnOpenModuleTutorial', function (e) {
        e.preventDefault();
        const moduleKey = $(this).attr('data-module') || currentModuleKey || "Ingredients";
        openTutorialModal(moduleKey, 0);
    });

    // 3. Open Modal Function
    function openTutorialModal(moduleKey, startIndex) {
        currentModuleKey = moduleKey;
        currentVideos = MODULE_TUTORIALS[moduleKey] || [];

        if (!currentVideos.length) return;

        currentVideoIndex = startIndex || 0;
        ensureModalMarkupExists();

        updateModalContent();

        const $modal = $('#moduleTutorialModal');
        $modal.css('display', 'flex');
        requestAnimationFrame(function () {
            $modal.addClass('tutorial-modal-visible');
        });

        playCurrentVideo();
    }

    // 4. Update Modal Content for Current Video Index
    function updateModalContent() {
        if (!currentVideos.length) return;
        const videoData = currentVideos[currentVideoIndex];
        const total = currentVideos.length;

        $('#tutModalModuleName').text(currentModuleKey + " Tutorial");
        $('#tutModalBadge').text(`${currentVideoIndex + 1} of ${total}`);
        $('#tutVideoTitle').text(videoData.title);
        $('#tutVideoDesc').text(videoData.description || "");

        // Set video source
        const $video = $('#tutVideoPlayer');
        const videoElement = $video.get(0);

        if (videoElement) {
            if (!videoElement.src.endsWith(videoData.url)) {
                $video.find('source').attr('src', videoData.url);
                videoElement.src = videoData.url;
                videoElement.load();
            }
        }

        // Toggle Prev/Next arrow visibility & state
        if (total > 1) {
            $('.tut-arrow-btn').show();
            $('#tutPrevBtn').prop('disabled', currentVideoIndex === 0).toggleClass('disabled', currentVideoIndex === 0);
            $('#tutNextBtn').prop('disabled', currentVideoIndex === total - 1).toggleClass('disabled', currentVideoIndex === total - 1);
            $('#tutFooterNav').show();
        } else {
            $('.tut-arrow-btn').hide();
            $('#tutFooterNav').hide();
        }

        // Render Dots Pagination
        renderDots();
    }

    function renderDots() {
        const $dotsContainer = $('#tutDotsContainer');
        $dotsContainer.empty();

        if (currentVideos.length <= 1) return;

        currentVideos.forEach((vid, idx) => {
            const activeClass = idx === currentVideoIndex ? 'active' : '';
            $dotsContainer.append(`
                <button type="button" class="tut-dot ${activeClass}" data-index="${idx}" title="${vid.title}"></button>
            `);
        });
    }

    function playCurrentVideo() {
        const videoElement = document.getElementById('tutVideoPlayer');
        if (videoElement) {
            videoElement.play().catch(function () {
                // Autoplay policy fallback
            });
        }
    }

    function closeTutorialModal() {
        const $modal = $('#moduleTutorialModal');
        const videoElement = document.getElementById('tutVideoPlayer');
        if (videoElement) {
            videoElement.pause();
        }
        $modal.removeClass('tutorial-modal-visible');
        setTimeout(function () {
            $modal.css('display', 'none');
        }, 300);
    }

    // 5. Navigation Handlers
    $(document).on('click', '#tutPrevBtn, #tutFooterPrev', function () {
        if (currentVideoIndex > 0) {
            currentVideoIndex--;
            updateModalContent();
            playCurrentVideo();
        }
    });

    $(document).on('click', '#tutNextBtn, #tutFooterNext', function () {
        if (currentVideoIndex < currentVideos.length - 1) {
            currentVideoIndex++;
            updateModalContent();
            playCurrentVideo();
        }
    });

    $(document).on('click', '.tut-dot', function () {
        const idx = parseInt($(this).attr('data-index'), 10);
        if (!isNaN(idx) && idx !== currentVideoIndex) {
            currentVideoIndex = idx;
            updateModalContent();
            playCurrentVideo();
        }
    });

    $(document).on('click', '#tutCloseBtn', function () {
        closeTutorialModal();
    });


    // Keyboard Shortcuts (Left/Right Arrows, Escape)
    $(document).on('keydown', function (e) {
        const $modal = $('#moduleTutorialModal');
        if (!$modal.hasClass('tutorial-modal-visible')) return;

        if (e.key === 'ArrowLeft') {
            $('#tutPrevBtn').trigger('click');
        } else if (e.key === 'ArrowRight') {
            $('#tutNextBtn').trigger('click');
        } else if (e.key === 'Escape') {
            closeTutorialModal();
        }
    });

    // 6. Dynamically Append Modal DOM if not present
    function ensureModalMarkupExists() {
        if ($('#moduleTutorialModal').length > 0) return;

        const modalHtml = `
            <div id="moduleTutorialModal" class="tut-modal-overlay" style="display:none;" role="dialog" aria-modal="true">
                <div class="tut-modal-card">
                    <!-- Header -->
                    <div class="tut-modal-header">
                        <div class="tut-header-title-group">
                            <div class="tut-header-icon">
                                <i class="fa-solid fa-video"></i>
                            </div>
                            <div>
                                <h3 id="tutModalModuleName" class="tut-modal-title">Module Tutorial</h3>
                                <p id="tutVideoTitle" class="tut-modal-subtitle">Tutorial Video</p>
                            </div>
                        </div>
                        <div class="tut-header-right">
                            <span id="tutModalBadge" class="tut-video-badge">1 of 1</span>
                            <button type="button" class="tut-modal-close" id="tutCloseBtn" aria-label="Close tutorial">
                                <i class="fas fa-times"></i>
                            </button>
                        </div>
                    </div>

                    <!-- Video Container with Side Navigation Arrows -->
                    <div class="tut-modal-body">
                        <button type="button" class="tut-arrow-btn tut-arrow-left" id="tutPrevBtn" aria-label="Previous Video" title="Previous Video">
                            <i class="fas fa-chevron-left"></i>
                        </button>
                        
                        <div class="tut-video-wrapper">
                            <video id="tutVideoPlayer" controls playsinline preload="metadata">
                                <source src="" type="video/mp4" />
                                Your browser does not support the video tag.
                            </video>
                        </div>

                        <button type="button" class="tut-arrow-btn tut-arrow-right" id="tutNextBtn" aria-label="Next Video" title="Next Video">
                            <i class="fas fa-chevron-right"></i>
                        </button>
                    </div>

                    <!-- Footer Details & Dots Navigation -->
                    <div class="tut-modal-footer">
                        <div class="tut-footer-info">
                            <p id="tutVideoDesc" class="tut-video-desc"></p>
                        </div>
                        <div id="tutFooterNav" class="tut-footer-controls" style="display:none;">
                            <div id="tutDotsContainer" class="tut-dots-wrapper"></div>
                            <div class="tut-footer-btn-group">
                                <button type="button" class="btn-tut-nav" id="tutFooterPrev">
                                    <i class="fas fa-arrow-left"></i> Prev
                                </button>
                                <button type="button" class="btn-tut-nav btn-tut-next" id="tutFooterNext">
                                    Next <i class="fas fa-arrow-right"></i>
                                </button>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        `;

        $('body').append(modalHtml);
    }
});
