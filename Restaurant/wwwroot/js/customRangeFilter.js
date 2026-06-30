(function($) {
    $.fn.customRangeFilter = function(options) {
        // Default settings
        var settings = $.extend({
            onApplyFilter: function(filterType, selectedDates) {},
            initialFilter: "Last 30 Days"
        }, options);

        // Constants
        const FILTER_OPTIONS = ["Last 30 Days", "Last 3 Months", "Last 6 Months", "Last Year", "Custom"];
        const DAYS_OF_WEEK = ["Mo", "Tu", "We", "Th", "Fr", "Sa", "Su"];
        const MONTHS = ["January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December"];

        return this.each(function() {
            var $element = $(this);
            
            // State
            let selectedFilter = settings.initialFilter;
            let currentMonth = new Date().getMonth();
            let currentYear = new Date().getFullYear();
            let startDate = null;
            let endDate = null;
            let isCalendarOpen = false;

            // Initialize HTML structure
            $element.addClass('wrapper').html(`
                <button class="toggleButton time-range-button">
                    <span class="buttonText">${selectedFilter}</span>
                    <svg width="16" height="16" viewBox="0 0 16 16" fill="none" xmlns="http://www.w3.org/2000/svg" class="chevron">
                        <path d="M4 6L8 10L12 6" stroke="#64748B" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round" />
                    </svg>
                </button>
                <div class="container" style="display: none;">
                    <div class="rangesidebar">
                        <h3 class="filterTitle">Filter By</h3>
                        <div class="filterOptions">
                            ${FILTER_OPTIONS.map(option => `
                                <button class="filterOption ${option === selectedFilter ? 'activeFilter' : ''}">${option}</button>
                            `).join('')}
                        </div>
                    </div>
                    <div class="calendarSection">
                        <div class="calendarHeader">
                            <button class="navButton prevMonth">&lsaquo;</button>
                            <div class="dateDisplay">
                                <h3 class="monthTitle"></h3>
                                <div class="yearNavigation">
                                    <button class="yearNavButton prevYear">&lsaquo;</button>
                                    <span class="yearTitle"></span>
                                    <button class="yearNavButton nextYear">&rsaquo;</button>
                                </div>
                            </div>
                            <button class="navButton nextMonth">&rsaquo;</button>
                        </div>
                        <div class="daysOfWeek">
                            ${DAYS_OF_WEEK.map(day => `<div class="dayOfWeek">${day}</div>`).join('')}
                        </div>
                        <div class="calendarGrid"></div>
                        <button class="applyButton">Apply Filter</button>
                    </div>
                </div>
            `);

            // Initialize default range
            (function initialize() {
                const today = new Date();
                startDate = new Date(today);
                startDate.setDate(today.getDate() - 29);
                endDate = today;
                updateButtonText();
                renderCalendar();
            })();

            // Calculate calendar position
            function calculateCalendarPosition() {
                const $button = $element.find('.time-range-button');
                const buttonRect = $button[0].getBoundingClientRect();
                const viewportWidth = $(window).width();
                const calendarWidth = 800;
                const position = {};

                const spaceOnRight = viewportWidth - buttonRect.right;
                const spaceOnLeft = buttonRect.left;

                if (spaceOnRight >= calendarWidth) {
                    position.left = '0';
                } else if (spaceOnLeft >= calendarWidth) {
                    position.right = '0';
                } else {
                    position.left = '50%';
                    position.transform = 'translateX(-50%)';
                }

                position.top = '100%';
                position.marginTop = '8px';
                return position;
            }

            // Toggle calendar
            $element.on('click', '.time-range-button', function() {
                isCalendarOpen = !isCalendarOpen;
                $element.find('.container').css(calculateCalendarPosition()).toggle(isCalendarOpen);
                $element.find('.chevron').toggleClass('chevronUp', isCalendarOpen);
            });

            // Handle click outside
            $(document).on('mousedown.customRangeFilter', function(e) {
                if (!$(e.target).closest($element.find('.container, .time-range-button')).length) {
                    isCalendarOpen = false;
                    $element.find('.container').hide();
                    $element.find('.chevron').removeClass('chevronUp');
                }
            });

            // Cleanup event listeners on destroy
            $element.on('destroy.customRangeFilter', function() {
                $(document).off('mousedown.customRangeFilter');
            });

            // Filter options click
            $element.on('click', '.filterOption', function() {
                $element.find('.filterOption').removeClass('activeFilter');
                $(this).addClass('activeFilter');
                selectedFilter = $(this).text();
                
                if (selectedFilter !== "Custom") {
                    const range = calculatePresetRange(selectedFilter);
                    startDate = range.startDate;
                    endDate = range.endDate;
                    currentMonth = startDate.getMonth();
                    currentYear = startDate.getFullYear();
                } else {
                    startDate = null;
                    endDate = null;
                }
                
                updateButtonText();
                renderCalendar();
            });

            // Navigation buttons
            $element.on('click', '.prevMonth', function() {
                if (currentMonth === 0) {
                    currentMonth = 11;
                    currentYear--;
                } else {
                    currentMonth--;
                }
                renderCalendar();
            });

            $element.on('click', '.nextMonth', function() {
                if (currentMonth === 11) {
                    currentMonth = 0;
                    currentYear++;
                } else {
                    currentMonth++;
                }
                renderCalendar();
            });

            $element.on('click', '.prevYear', function() {
                currentYear--;
                renderCalendar();
            });

            $element.on('click', '.nextYear', function() {
                currentYear++;
                renderCalendar();
            });

            // Apply filter
            $element.on('click', '.applyButton', function () {
              
                // If only the start date is selected and there's no end date, set endDate to null
                if (startDate && !endDate) {
                    endDate = null;
                }

                // Apply filter with updated start and end dates
                settings.onApplyFilter(selectedFilter, { startDate, endDate });
                isCalendarOpen = false;
                $element.find('.container').hide();
                $element.find('.chevron').removeClass('chevronUp');
                updateButtonText();
            });


            // Calendar day click
            $element.on('click', '.calendarDay', function() {
                const day = parseInt($(this).text());
                const clickedDate = new Date(currentYear, currentMonth, day);

                if (selectedFilter !== "Custom") {
                    selectedFilter = "Custom";
                    $element.find('.filterOption').removeClass('activeFilter');
                    $element.find('.filterOption:contains("Custom")').addClass('activeFilter');
                }

                if (!startDate || (startDate && endDate)) {
                    startDate = clickedDate;
                    endDate = null;
                } else if (startDate && !endDate) {
                    if (clickedDate < startDate) {
                        endDate = startDate;
                        startDate = clickedDate;
                    } else {
                        endDate = clickedDate;
                    }
                }

                updateButtonText();
                renderCalendar();
            });

            function calculatePresetRange(filterType) {
                const today = new Date();
                const startDateObj = new Date(today);
                const endDateObj = new Date(today);

                switch (filterType) {
                    case "Last 30 Days":
                        startDateObj.setDate(today.getDate() - 29);
                        break;
                    case "Last 3 Months":
                        startDateObj.setMonth(today.getMonth() - 3);
                        break;
                    case "Last 6 Months":
                        startDateObj.setMonth(today.getMonth() - 6);
                        break;
                    case "Last Year":
                        startDateObj.setFullYear(today.getFullYear() - 1);
                        break;
                }

                return { startDate: startDateObj, endDate: endDateObj };
            }

            function getDaysInMonth(month, year) {
                return new Date(year, month + 1, 0).getDate();
            }

            function getFirstDayOfMonth(month, year) {
                const firstDay = new Date(year, month, 1).getDay();
                return firstDay === 0 ? 6 : firstDay - 1;
            }

            function isDateInRange(date) {
                if (!startDate) return false;
                const currentDate = new Date(currentYear, currentMonth, date);
                if (!endDate) return currentDate.getTime() === startDate.getTime();
                return currentDate >= startDate && currentDate <= endDate;
            }

            function isRangeEdge(date) {
                if (!startDate) return false;
                const currentDate = new Date(currentYear, currentMonth, date);
                const isStart = currentDate.getTime() === startDate.getTime();
                const isEnd = endDate && currentDate.getTime() === endDate.getTime();
                return isStart || isEnd;
            }

            function formatDate(date) {
                return date.toLocaleDateString("en-US", {
                    month: "short",
                    day: "numeric",
                    year: "numeric"
                });
            }

            function updateButtonText() {
                let text = selectedFilter;
                if (selectedFilter === "Custom") {
                    if (startDate && endDate) {
                        text = `${formatDate(startDate)} - ${formatDate(endDate)}`;
                    } else if (startDate) {
                        text = `From ${formatDate(startDate)}`;
                    } else {
                        text = "Select Date Range";
                    }
                }
                $element.find('.buttonText').text(text);
            }

            function renderCalendar() {
                $element.find('.monthTitle').text(MONTHS[currentMonth]);
                $element.find('.yearTitle').text(currentYear);
                
                const daysInMonth = getDaysInMonth(currentMonth, currentYear);
                const firstDay = getFirstDayOfMonth(currentMonth, currentYear);
                let html = '';

                for (let i = 0; i < firstDay; i++) {
                    html += '<div class="emptyDay"></div>';
                }

                for (let day = 1; day <= daysInMonth; day++) {
                    const isInRange = isDateInRange(day);
                    const isEdge = isRangeEdge(day);
                    let classes = 'calendarDay';
                    if (isEdge) classes += ' rangeEdge';
                    else if (isInRange) classes += ' selectedDay';
                    html += `<button class="${classes}">${day}</button>`;
                }

                $element.find('.calendarGrid').html(html);
            }
        });
    };

    // Destroy method to clean up
    $.fn.customRangeFilter.destroy = function() {
        return this.each(function() {
            $(this).trigger('destroy.customRangeFilter').off('.customRangeFilter').empty();
        });
    };
})(jQuery);