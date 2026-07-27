/* ============================================================================
 * script.js — Shared layout/site script loaded on every page (_Layout.cshtml).
 * Sidebar, modals, notifications, search, keyboard shortcuts and misc UI.
 *
 * Project convention — section order:
 *   1. Constants  2. State  3. Initialization  4. Event bindings
 *   5. AJAX / CRUD  6. Helpers  7. Utilities
 * ==========================================================================*/

/* ── 3. Initialization / 4. Event bindings ────────────────────────────── */

$(document).ready(() => {
    // Initialize dashboard
    initializeDashboard()

  

    // Search functionality
    $(".search-input, .filter-search").on("input", function () {
        const searchTerm = $(this).val().toLowerCase()
        const targetId = $(this).attr("id")
        performSearch(searchTerm, targetId)
    })



    // Modal functionality
    $("#addRestaurantBtn").on("click", () => {
        openModal("addRestaurantModal")
    })

    $("#addIngredientBtn").on("click", () => {
        openModal("addIngredientModal")
    })

    $("#addUserBtn").on("click", () => {
        openModal("addUserModal")
    })
    $("#addMenuItemBtn").on("click", () => {
        openModal("addMenuItemModal")
    })

    $(".modal-close").on("click", function () {
        const modalId = $(this).closest(".modal").attr("id")
        closeModal(modalId)
    })

    // Close modal when clicking outside
    $(".modal").on("click", function (e) {
        if (e.target === this) {
            const modalId = $(this).attr("id")
            closeModal(modalId)
        }
    })

    // Settings save button
    $("#saveSettingsBtn").on("click", () => {
        saveSettings()
    })

    // Keyboard shortcuts
    $(document).on("keydown", (e) => {
        // Ctrl/Cmd + K for search
        if ((e.ctrlKey || e.metaKey) && e.key === "k") {
            e.preventDefault()
            $(".search-input").first().focus()
        }

        // Escape to close modals
        if (e.key === "Escape") {
            $(".modal.show").each(function () {
                closeModal($(this).attr("id"))
            })
        }
    })

    // Initialize charts if on analytics page
    //if (window.location.pathname.includes("analytics")) {
    //    initializeCharts()
    //}

    // Start real-time updates
    //startRealTimeUpdates()

    // Initialize notifications
   // initializeNotifications()
})


function isFormValid(formSelector) {
    let valid = true;
    $(formSelector).find('[required]').each(function () {
        const $field = $(this);
        if ($field.prop('disabled')) return; // disabled fields aren't part of the submission
        const val = $field.val();
        if (val === null || val === undefined || String(val).trim() === '') {
            valid = false;
            return false; // break out of .each
        }
    });
    return valid;
}
function initializeDashboard() {
    // Animate stats on load
    animateStats()

    // Load saved settings
    loadSettings()

    // Set up periodic data refresh
   // setInterval(refreshDashboardData, 30000) // Refresh every 30 seconds
}

function performSearch(searchTerm, targetId) {
    switch (targetId) {
        case "restaurantSearch":
            filterRestaurants(searchTerm)
            break
        case "ingredientSearch":
            filterIngredients(searchTerm)
            break
        case "userSearch":
            filterUsers(searchTerm)
            break
        default:
            // Global search
            performGlobalSearch(searchTerm)
    }
}

function filterRestaurants(searchTerm) {
    $(".restaurant-card").each(function () {
        const restaurantName = $(this).find("h3").text().toLowerCase()
        const location = $(this).find(".location").text().toLowerCase()
        const manager = $(this).find(".restaurant-details p:first").text().toLowerCase()

        if (restaurantName.includes(searchTerm) || location.includes(searchTerm) || manager.includes(searchTerm)) {
            $(this).show()
        } else {
            $(this).hide()
        }
    })
}

function filterIngredients(searchTerm) {
    $(".data-table tbody tr").each(function () {
        const ingredientName = $(this).find(".ingredient-name").text().toLowerCase()
        const category = $(this).find(".category-badge").text().toLowerCase()
        const supplier = $(this).find("td:nth-child(5)").text().toLowerCase()

        if (ingredientName.includes(searchTerm) || category.includes(searchTerm) || supplier.includes(searchTerm)) {
            $(this).show()
        } else {
            $(this).hide()
        }
    })
}

function filterUsers(searchTerm) {
    $(".data-table tbody tr").each(function () {
        const userName = $(this).find(".user-name").text().toLowerCase()
        const email = $(this).find(".user-contact span:first").text().toLowerCase()
        const role = $(this).find(".role-badge").text().toLowerCase()
        const restaurant = $(this).find("td:nth-child(3)").text().toLowerCase()

        if (
            userName.includes(searchTerm) ||
            email.includes(searchTerm) ||
            role.includes(searchTerm) ||
            restaurant.includes(searchTerm)
        ) {
            $(this).show()
        } else {
            $(this).hide()
        }
    })
}

function performGlobalSearch(searchTerm) {
    // Implement global search across all data
    console.log("Performing global search for:", searchTerm)
}

// Modal open/close logic
function openModal(modalId) {
   
    $('#' + modalId).addClass('show').hide().fadeIn(200);
}

function closeModal(modalId) {
    $('#' + modalId).fadeOut(200, function () {
        $(this).removeClass('show');
    });
}

// Open modal on button click
$(document).ready(function () {
    // Add User Modal
    $('#addUserBtn').on('click', function () {
        openModal('addUserModal');
    });

    // Add Restaurant Modal
    $('#addRestaurantBtn').on('click', function () {
        openModal('addRestaurantModal');
    });

    // Add Ingredient Modal
    $('#addIngredientBtn').on('click', function () {
        openModal('addIngredientModal');
    });

    // Close modal when clicking the close button or outside modal content
    $('.modal-close').on('click', function () {
        $(this).closest('.modal').fadeOut(200);
    });

    $('.modal').on('click', function (e) {
        if ($(e.target).is('.modal')) {
            $(this).fadeOut(200);
        }
    });
});

// Prevent form submission (for demo)
$('#addUserForm, #addRestaurantForm, #addIngredientForm').on('submit', function (e) {
    e.preventDefault();
});

// Example save functions (replace with your logic)
function saveUser() {
    // ...save user logic...
    closeModal('addUserModal');
}
function saveRestaurant() {
    // ...save restaurant logic...
    closeModal('addRestaurantModal');
}
function saveIngredient() {
    // ...save ingredient logic...
    closeModal('addIngredientModal');
}

function animateStats() {
    $(".stat-value").each(function () {
        const $this = $(this)
        const finalValue = $this.text()
        const numericValue = Number.parseFloat(finalValue.replace(/[^0-9.-]+/g, ""))

        if (!isNaN(numericValue)) {
            $this.text("0")
            $({ value: 0 }).animate(
                { value: numericValue },
                {
                    duration: 2000,
                    easing: "swing",
                    step: function () {
                        if (finalValue.includes("$")) {
                            $this.text("$" + Math.floor(this.value).toLocaleString())
                        } else if (finalValue.includes("%")) {
                            $this.text(Math.floor(this.value) + "%")
                        } else {
                            $this.text(Math.floor(this.value).toLocaleString())
                        }
                    },
                    complete: () => {
                        $this.text(finalValue)
                    },
                },
            )
        }
    })
}

//function initializeCharts() {
//    // Revenue Chart
//    const revenueCtx = document.getElementById("revenueChart")
//    if (revenueCtx) {
//        new Chart(revenueCtx, {
//            type: "line",
//            data: {
//                labels: ["Jan", "Feb", "Mar", "Apr", "May", "Jun"],
//                datasets: [
//                    {
//                        label: "Revenue",
//                        data: [65000, 78000, 85000, 92000, 105000, 124000],
//                        borderColor: "#f08d11",
//                        backgroundColor: "rgba(240, 141, 17, 0.1)",
//                        tension: 0.4,
//                        fill: true,
//                    },
//                ],
//            },
//            options: {
//                responsive: true,
//                maintainAspectRatio: false,
//                plugins: {
//                    legend: {
//                        display: false,
//                    },
//                },
//                scales: {
//                    y: {
//                        beginAtZero: true,
//                        ticks: {
//                            callback: (value) => "$" + value.toLocaleString(),
//                        },
//                    },
//                },
//            },
//        })
//    }

//    // Order Distribution Chart
//    const orderCtx = document.getElementById("orderChart")
//    if (orderCtx) {
//        new Chart(orderCtx, {
//            type: "doughnut",
//            data: {
//                labels: ["Downtown Bistro", "Seaside Grill", "Urban Kitchen", "Garden Cafe"],
//                datasets: [
//                    {
//                        data: [35, 25, 25, 15],
//                        backgroundColor: ["#f08d11", "#2563eb", "#10b981", "#8b5cf6"],
//                        borderWidth: 0,
//                    },
//                ],
//            },
//            options: {
//                responsive: true,
//                maintainAspectRatio: false,
//                plugins: {
//                    legend: {
//                        position: "bottom",
//                    },
//                },
//            },
//        })
//    }
//}

//function refreshDashboardData() {
//    // Simulate real-time data updates
//    updateOrderCounts()
//    updateRevenue()
//    updateStockLevels()
//}

//function updateOrderCounts() {
//    // Simulate random order count changes
//    $(".restaurant-item .orders").each(function () {
//        const currentOrders = Number.parseInt($(this).text().split(" ")[0])
//        const change = Math.floor(Math.random() * 3) - 1 // -1, 0, or 1
//        const newOrders = Math.max(0, currentOrders + change)
//        $(this).text(newOrders + " orders")
//    })
//}

//function updateRevenue() {
//    // Simulate revenue updates
//    const revenueElement = $(".stat-value").first()
//    const currentRevenue = Number.parseFloat(revenueElement.text().replace(/[^0-9.-]+/g, ""))
//    const change = Math.floor(Math.random() * 1000) - 500
//    const newRevenue = Math.max(0, currentRevenue + change)
//    revenueElement.text("$" + newRevenue.toLocaleString())
//}

//function updateStockLevels() {
//    // Simulate stock level changes
//    $(".progress-fill").each(function () {
//        const currentWidth = Number.parseFloat($(this).css("width"))
//        const change = Math.random() * 10 - 5 // -5% to +5%
//        const newWidth = Math.max(0, Math.min(100, currentWidth + change))
//        $(this).css("width", newWidth + "%")

//        // Update status based on new width
//        const statusBadge = $(this).closest("tr").find(".status-badge")
//        if (newWidth < 30) {
//            statusBadge.removeClass("good low").addClass("critical").text("Critical")
//        } else if (newWidth < 60) {
//            statusBadge.removeClass("good critical").addClass("low").text("Low")
//        } else {
//            statusBadge.removeClass("critical low").addClass("good").text("Good")
//        }
//    })
//}

//function startRealTimeUpdates() {
//    // Update data every 30 seconds
//    setInterval(refreshDashboardData, 30000)

//    // Update timestamps
//    setInterval(updateTimestamps, 60000)
//}

//function updateTimestamps() {
//    $(".order-time").each(function () {
//        const currentText = $(this).text()
//        if (currentText.includes("min ago")) {
//            const minutes = Number.parseInt(currentText.split(" ")[0]) + 1
//            $(this).text(minutes + " min ago")
//        }
//    })
//}

function initializeNotifications() {
    // Check for low stock alerts
    checkLowStockAlerts()

    // Check for expiring items
    checkExpiringItems()

    // Set up periodic checks
    setInterval(checkLowStockAlerts, 300000) // Every 5 minutes
    setInterval(checkExpiringItems, 600000) // Every 10 minutes
}

function checkLowStockAlerts() {
    $(".progress-fill").each(function () {
        const width = Number.parseFloat($(this).css("width"))
        if (width < 30) {
            const ingredientName = $(this).closest("tr").find(".ingredient-name").text()
            showNotification(`Low stock alert: ${ingredientName}`, "error")
        }
    })
}

function checkExpiringItems() {
    // Simulate expiring item check
    const expiringItems = ["Milk", "Bread", "Lettuce"]
    expiringItems.forEach((item) => {
        if (Math.random() < 0.1) {
            // 10% chance
            showNotification(`${item} expires soon!`, "info")
        }
    })
}

function showNotification(message, type = "info", statusCode = null, title = "Info") {
    const icons = {
        success: "fas fa-check-circle",
        error: "fas fa-exclamation-circle",
        info: "fas fa-info-circle",
    };


    if (statusCode === 200) {
        title = "Success";
    } else if (statusCode !== null) {
        title = "Failure";
    } else {
        if (type === "success") title = "Success";
        else if (type === "error") title = "Failure";
    }

    const notification = $(`
        <div class="notification notification-${type}">
            <i class="${icons[type]}"></i>
            <div class="notification-content">
                <h5>${title}</h5>
                <span>${message}</span>
            </div>
            <button class="notification-close">
                <i class="fas fa-times"></i>
            </button>
        </div>
    `);

    $("body").append(notification);

    // Auto remove after 5 seconds
    setTimeout(() => {
        notification.remove();
    }, 5000);

    // Manual close
    notification.find(".notification-close").on("click", () => {
        notification.remove();
    });
}


function saveSettings() {
    const settings = {
        companyName: $('input[name="companyName"]').val(),
        timeZone: $('select[name="timeZone"]').val(),
        currency: $('select[name="currency"]').val(),
        language: $('select[name="language"]').val(),
        emailNotifications: $("#emailNotifications").is(":checked"),
        lowStockAlerts: $("#lowStockAlerts").is(":checked"),
        orderNotifications: $("#orderNotifications").is(":checked"),
        performanceReports: $("#performanceReports").is(":checked"),
        twoFactorAuth: $("#twoFactorAuth").is(":checked"),
        sessionTimeout: $('select[name="sessionTimeout"]').val(),
        darkMode: $("#darkMode").is(":checked"),
        compactView: $("#compactView").is(":checked"),
        itemsPerPage: $('select[name="itemsPerPage"]').val(),
        autoBackup: $("#autoBackup").is(":checked"),
        backupFrequency: $('select[name="backupFrequency"]').val(),
    }

    // Show loading state
    const saveBtn = $("#saveSettingsBtn")
    saveBtn.addClass("loading").text("Saving...")

    // Simulate API call
    setTimeout(() => {
        localStorage.setItem("dashboardSettings", JSON.stringify(settings))

        // Apply settings
        applySettings(settings)

        // Reset button state
        saveBtn.removeClass("loading").text("Save Changes")

        // Show success notification
        showNotification("Settings saved successfully!", "success")
    }, 1000)
}

function loadSettings() {
    const savedSettings = localStorage.getItem("dashboardSettings")
    if (savedSettings) {
        const settings = JSON.parse(savedSettings)
        applySettings(settings)
        populateSettingsForm(settings)
    }
}

function applySettings(settings) {
    // Apply dark mode
    if (settings.darkMode) {
        $("body").addClass("dark-mode")
    } else {
        $("body").removeClass("dark-mode")
    }

    // Apply compact view
    if (settings.compactView) {
        $("body").addClass("compact-view")
    } else {
        $("body").removeClass("compact-view")
    }

    // Apply other settings as needed
    console.log("Applied settings:", settings)
}

function populateSettingsForm(settings) {
    // Populate form fields with saved settings
    Object.keys(settings).forEach((key) => {
        const element = $(`[name="${key}"], #${key}`)
        if (element.is(":checkbox")) {
            element.prop("checked", settings[key])
        } else {
            element.val(settings[key])
        }
    })
}

// Utility functions
function formatCurrency(amount) {
    return new Intl.NumberFormat("en-US", {
        style: "currency",
        currency: "USD",
    }).format(amount)
}

function formatDate(date) {
    return new Intl.DateTimeFormat("en-US", {
        year: "numeric",
        month: "short",
        day: "numeric",
    }).format(new Date(date))
}

function formatTime(date) {
    return new Intl.DateTimeFormat("en-US", {
        hour: "2-digit",
        minute: "2-digit",
    }).format(new Date(date))
}

function debounce(func, wait) {
    let timeout
    return function executedFunction(...args) {
        const later = () => {
            clearTimeout(timeout)
            func(...args)
        }
        clearTimeout(timeout)
        timeout = setTimeout(later, wait)
    }
}


//EXTRA IMPLEMENTATION

// Initialize dropdown toggle
$(document).on('click', '.dropdown-toggle', function (e) {
   
    e.preventDefault();
    e.stopPropagation();
    const $dropdown = $(this).siblings('.dropdown-menu');
    $('.dropdown-menu').not($dropdown).removeClass('show'); // Close other open dropdowns
    $dropdown.toggleClass('show'); // Toggle the clicked dropdown
});

// Close dropdown when clicking outside
$(document).on('click', function (e) {
    if (!$(e.target).closest('.dropdown').length) {
        $('.dropdown-menu').removeClass('show');
    }
});


function showDeleteConfirmation() {
    return new Promise((resolve, reject) => {
        const modal = $('#deleteConfirmModal');
        modal.addClass('show');

        // Handlers for the dialog buttons
        const confirmHandler = () => {
            modal.removeClass('show');
            resolve(true);
            unbindEvents();
        };

        const cancelHandler = () => {
            modal.removeClass('show');
            reject(new Error('Deletion cancelled by user.'));
            unbindEvents();
        };

        // Function to remove all event listeners
        const unbindEvents = () => {
            modal.find('.confirm-btn').off('click', confirmHandler);
            modal.find('.cancel-btn').off('click', cancelHandler);
            modal.find('.close-btn').off('click', cancelHandler);
            // Also handle clicks outside the modal content
            modal.off('click', outsideClickHandler);
        };

        // Event listener for clicks outside the modal content
        const outsideClickHandler = (event) => {
            if ($(event.target).is(modal)) {
                cancelHandler();
            }
        };

        // Attach the event listeners
        modal.find('.confirm-btn').on('click', confirmHandler);
        modal.find('.cancel-btn').on('click', cancelHandler);
        modal.find('.close-btn').on('click', cancelHandler);
        modal.on('click', outsideClickHandler);
    });
}