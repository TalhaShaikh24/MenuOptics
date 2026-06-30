$(document).ready(function () {
    fetchAnalyticsData();
});

function fetchAnalyticsData() {
    $.ajax({
        url: '/api/AnalyticsApi/GetAnalyticsDashboard',
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
    debugger
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
            return; // Skip non-numeric values like peak-hours
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
    if (revenueCtx) {
        new Chart(revenueCtx, {
            type: "line",
            data: {
                labels: ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug"],
                datasets: [
                    {
                        label: "Revenue",
                        data: data.revenueTrends.map(t => t.monthlyRevenue) || [0, 0, 0, 0, 0, 0, 0, 0],
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
        new Chart(orderCtx, {
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
                        <span class="metric-label">Orders</span>
                        <span class="metric-value">${item.salesCount.toLocaleString()}</span>
                    </div>
                    <div class="metric">
                        <span class="metric-label">Rating</span>
                        <span class="metric-value">${item.rating ? `${item.rating.toFixed(1)}/5` : 'N/A'}</span>
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