using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using MenuOptics.Data;
using MenuOptics.Models;
using MenuOptics.Models.DTOs;
using MenuOptics.Services;
using MenuOptics.Services.EmailTemplates;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MenuOptics.Services.BackgroundServices
{
    public class WeeklyPriceAlertBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<WeeklyPriceAlertBackgroundService> _logger;

        public WeeklyPriceAlertBackgroundService(
            IServiceScopeFactory serviceScopeFactory,
            ILogger<WeeklyPriceAlertBackgroundService> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("WeeklyPriceAlertBackgroundService initialized (Testing Mode: Runs every 2 minutes).");

            // Initial 10-second delay on app startup so services and DB initialize cleanly
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("WeeklyPriceAlertBackgroundService: Executing price audit at {Time} UTC...", DateTime.UtcNow);
                    await RunWeeklyAuditAsync();
                    _logger.LogInformation("WeeklyPriceAlertBackgroundService: Price audit finished.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "WeeklyPriceAlertBackgroundService: Error executing price audit.");
                }

                // Wait 2 minutes for next run
                try
                {
                    _logger.LogInformation("WeeklyPriceAlertBackgroundService: Next audit in 2 minutes.");
                    await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            _logger.LogInformation("WeeklyPriceAlertBackgroundService stopped.");
        }

        public async Task RunWeeklyAuditAsync()
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DapperContext>();
            var menuService = scope.ServiceProvider.GetRequiredService<IMenuService>();
            var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
            var emailService = scope.ServiceProvider.GetRequiredService<ISmtpEmailService>();
            var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

            var users = await userService.GetAllRestaurantsAsync();
            if (users.Status != 200 || users.Data == null)
                return;

            var restaurants = (users.Data as IEnumerable<GetUserDTO>)
                ?.Where(u => u.IsActive 
                && (u.UserId == 28730)
                && (u.IsPriceAlertOn == true)
                && (u.IsParent == true || !u.ParentId.HasValue))
                .ToList() ?? new List<GetUserDTO>();

            if (!restaurants.Any())
                return;

            var baseUrl = config["WebUrl"] ?? config["BaseUrl"] ?? "https://menuoptics.com";

            foreach (var r in restaurants)
            {
                try
                {
                    // Check if price alerts are turned off for this restaurant
                    if (r.IsPriceAlertOn.HasValue && !r.IsPriceAlertOn.Value)
                    {
                        _logger.LogInformation("Skipping restaurant {UserId} ({RestaurantName}) because price alerts are turned off.",
                            r.UserId, r.RestaurantName);
                        continue;
                    }

                    // 1. Get active menu items and their ingredients
                    var menuResp = await menuService.GetAllMenuItemsAsync(true, r.UserId);
                    if (menuResp.Status != 200 || menuResp.Data == null)
                        continue;

                    var menuItems = (menuResp.Data as IEnumerable<GetMenuItemDTO>)
                        ?.Where(m => m.IsActive)
                        .ToList() ?? new List<GetMenuItemDTO>();

                    if (!menuItems.Any())
                        continue;

                    // Extract all ingredients used in active menu items, grouped/distinct by IngredientId
                    var comparisons = menuItems
                        .Where(m => m.Ingredients != null)
                        .SelectMany(m => m.Ingredients!.Select(ing => new
                        {
                            MenuItemName = m.Name,
                            Ingredient = ing
                        }))
                        .GroupBy(x => x.Ingredient.IngredientId)
                        .Select(g =>
                        {
                            var first = g.First().Ingredient;
                            var affectedMenus = g
                                .Select(x => x.MenuItemName)
                                .Where(name => !string.IsNullOrWhiteSpace(name))
                                .Distinct()
                                .ToList();

                            return new IngredientPriceComparisonDTO
                            {
                                IngredientId = first.IngredientId,
                                IngredientName = first.IngredientName,
                                OldPrice = first.OldPrice ?? 0,
                                CurrentPrice = first.CurrentPrice,
                                AffectedMenuItems = affectedMenus
                            };
                        })
                        .ToList();

                    if (!comparisons.Any())
                        continue;

                    // 2. Threshold from User column (Default: 10% if not set or 0)
                    decimal threshold = (r.PriceAlertThreshold.HasValue && r.PriceAlertThreshold.Value > 0)
                        ? r.PriceAlertThreshold.Value
                        : 10;

                    // 3. Filter ingredients that exceeded the threshold
                    var exceeded = comparisons
                        .Where(i => i.OldPrice > 0 && i.CurrentPrice != i.OldPrice && i.PercentageChange >= threshold)
                        .OrderByDescending(i => i.PercentageChange)
                        .ToList();

                    if (!exceeded.Any())
                        continue;

                    if (string.IsNullOrWhiteSpace(r.Email))
                    {
                        _logger.LogWarning("Restaurant {UserId} ({RestaurantName}) has {Count} exceeded ingredients but no email address configured.",
                            r.UserId, r.RestaurantName, exceeded.Count);
                        continue;
                    }

                    // 4. Build single email template with all exceeded ingredients and send
                    var htmlBody = ExceededIngredientsEmailTemplate.Build(r.RestaurantName, threshold, exceeded, baseUrl);
                    var subject = $"[Menu Optics] Price Alert: {exceeded.Count} Ingredient(s) Exceeded {threshold:F1}% Threshold";

                    await emailService.SendEmailAsync("grthamad@gmail.com", subject, htmlBody);
                    _logger.LogInformation("Weekly price alert email sent to {Email} for restaurant {RestaurantName} ({Count} items exceeding {Threshold}%).",
                        r.Email, r.RestaurantName, exceeded.Count, threshold);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error checking weekly price alert for restaurant {UserId}", r.UserId);
                }
            }
        }

        public static DateTime GetNextMondayRunTime(DateTime fromUtc)
        {
            // Monday -5:00 UTC target
            //const int targetHourUtc = -5;
            //int daysUntilMonday = ((int)DayOfWeek.Monday - (int)fromUtc.DayOfWeek + 7) % 7;

            //if (daysUntilMonday == 0)
            //{
            //    if (fromUtc.Hour < targetHourUtc)
            //    {
            //        return fromUtc.Date.AddHours(targetHourUtc);
            //    }
            //    daysUntilMonday = 7;
            //}

            //return fromUtc.Date.AddDays(daysUntilMonday).AddHours(targetHourUtc);
            return DateTime.Now.AddMinutes(1);
        }
    }
}
