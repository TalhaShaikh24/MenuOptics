let revenueChartInstance = null;
let orderChartInstance = null;


$(document).ready(function () {

    $('#dateRange').customRangeFilter({
        onApplyFilter: function (filterType, selectedDates) {

            startDate = DateFormatter.formatDashUS(selectedDates.startDate);
            endDate = DateFormatter.formatDashUS(selectedDates.endDate);
            console.log('Filtesaasr 1 Applied:', filterType, selectedDates);

            fetchAnalyticsData(startDate, endDate);
        },
        initialFilter: "Last 30 Days"
    });

    // Ensure DataTable is loaded before proceeding
    if (typeof $.fn.DataTable === 'undefined') {
        console.error('jQuery DataTable library is not loaded. Please check the CDN or script inclusion.');
        showNotification('DataTable library failed to load.', 'error');
        return;
    }


    fetchAnalyticsData();
});

function fetchAnalyticsData(startDate = null, endDate = null) {

    let url = `/api/AnalyticsApi/GetAnalyticsDashboard?`;

    if (startDate) {
        url += `startDate=${startDate}&`;
        if (endDate) {
            url += `endDate=${endDate}`;
        }
    }


    $.ajax({
        url: url,
        method: 'GET',
        success: function (response) {
            if (response.status === 200) {
                const data = response.data;
              
                updateDashboard(data);
                initializeCharts(data);
                populateRestaurantPerformance(data);
            } else {
                console.error('Failed to fetch analytics:', response.responseMsg);
                showNotification(response.responseMsg, 'error');
            }
        },
        error: function (xhr, status, error) {
            console.error('AJAX error:', error);
            showNotification('An error occurred while fetching analytics data.', 'error');
        }
    });
}

function updateDashboard(data) {
    // Update values
    $('#revenue').text(data.currentRevenue ? `$${data.currentRevenue.toLocaleString()}` : '$0');
    $('#sales-count').text(data.currentSalesCount ? data.currentSalesCount.toLocaleString() : '0');
    $('#average-order-value').text(data.currentAvgOrderValue ? `$${data.currentAvgOrderValue.toFixed(2)}` : '$0');
    $('#total-items-sold').text(data.currentItemsSold ? data.currentItemsSold.toLocaleString() : '0');

    // Update changes
    updateChange('#revenue-change', data.percentageChangeRevenue);
    updateChange('#sales-count-change', data.percentageChangeSales);
    updateChange('#average-order-value-change', data.percentageChangeAvgOrder);
    updateChange('#total-items-sold-change', data.percentageChangeItems);
}

function updateChange(selector, change) {
   
    const isPositive = change >= 0;
    const formatted = `${isPositive ? '+' : '-'}${Math.abs(change).toFixed(1)}%`;

    $(selector).find('span').text(formatted);
    $(selector).removeClass('positive negative').addClass(isPositive ? 'positive' : 'negative');
}



function animateStats() {
    $(".stat-value").each(function () {
        const $this = $(this);
        const finalValue = $this.text();
        let numericValue;

        if (finalValue.includes('$')) {
            numericValue = Number.parseFloat(finalValue.replace(/[^0-9.-]+/g, ""));
        } else if (finalValue.includes('/5')) {
            numericValue = Number.parseFloat(finalValue.split('/')[0]);
        } else {
            return; 
        }

        if (!isNaN(numericValue)) {
            $this.text(finalValue.includes('$') ? "$0" : "0/5");
            $({ value: 0 }).animate(
                { value: numericValue },
                {
                    duration: 2000,
                    easing: "swing",
                    step: function () {
                        if (finalValue.includes('$')) {
                            $this.text("$" + Math.floor(this.value).toLocaleString());
                        } else if (finalValue.includes('/5')) {
                            $this.text(this.value.toFixed(1) + "/5");
                        }
                    },
                    complete: () => {
                        $this.text(finalValue);
                    },
                }
            );
        }
    });
}



function initializeCharts(data) {
    // Revenue Chart
    const revenueCtx = document.getElementById("revenueChart");

    if (revenueCtx && data.revenueTrends) {
        // Destroy previous instance if exists
        if (revenueChartInstance) {
            revenueChartInstance.destroy();
        }

        const monthNames = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];
        const currentMonth = new Date().getMonth() + 1;
        const labels = monthNames.slice(0, currentMonth);

        const revenueMap = new Map();
        data.revenueTrends.forEach(item => {
            revenueMap.set(item.monthNumber, item.monthlyRevenue);
        });

        const revenueData = [];
        for (let month = 1; month <= currentMonth; month++) {
            revenueData.push(revenueMap.get(month) || 0);
        }

        revenueChartInstance = new Chart(revenueCtx, {
            type: "line",
            data: {
                labels: labels,
                datasets: [
                    {
                        label: "Revenue",
                        data: revenueData,
                        borderColor: "#f08d11",
                        backgroundColor: "rgba(240, 141, 17, 0.1)",
                        tension: 0.4,
                        fill: true,
                    },
                ],
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        display: false,
                    },
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        ticks: {
                            callback: (value) => "$" + value.toLocaleString(),
                        },
                    },
                },
            },
        });
    }

    // Order Distribution Chart
    const orderCtx = document.getElementById("orderChart");
    if (orderCtx) {
        if (orderChartInstance) {
            orderChartInstance.destroy();
        }

        orderChartInstance = new Chart(orderCtx, {
            type: "doughnut",
            data: {
                labels: data.revenueDistribution.map(r => r.categoryName) || ["N/A"],
                datasets: [
                    {
                        data: data.revenueDistribution.map(r => r.percentageShare) || [100],
                        backgroundColor: ["#f08d11", "#2563eb", "#10b981", "#8b5cf6"],
                        borderWidth: 0,
                    },
                ],
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        position: "bottom",
                    },
                },
            },
        });
    }
}


function populateRestaurantPerformance(data) {
    const $performanceGrid = $('.performance-grid');
    $performanceGrid.empty();

    data.menuPerformances.forEach(item => {
        const scoreClass = item.percentage >= 90 ? 'excellent' : 'good';
        const card = `
            <div class="performance-item">
                <div class="performance-header">
                    <h4>${item.menuName}</h4>
                    <span class="performance-score ${scoreClass}">${item.percentage.toFixed(1)}%</span>
                </div>
                <div class="performance-metrics">
                    <div class="metric">
                        <span class="metric-label">Revenue</span>
                        <span class="metric-value">$${item.revenue.toLocaleString()}</span>
                    </div>
                    <div class="metric">
                        <span class="metric-label">Cost%</span>
                        <span class="metric-value">${item?.costPercentage.toFixed(2)+'%'}</span>
                    </div>
                    <div class="metric">
                        <span class="metric-label">Margin</span>
                        <span class="metric-value">${item?.contributionMargin ?? 'N/A'}</span>
                    </div>
                </div>
            </div>
        `;
        $performanceGrid.append(card);
    });

    // Re-run animateStats for new metric-value elements
    animateStats();
}

// Helper function
function showNotification(message, type) {
    console.log(`${type}: ${message}`);
    // Implement your notification logic here
}