using Dapper;
using MenuOptics.Data;
using MenuOptics.Models;
using Stripe.Forwarding;
using System.Data;
using System.Data.SqlClient;

namespace MenuOptics.Services.VendorApi
{
    public class IngredientService : IIngredientService
    {
        private readonly DapperContext _context;
        private readonly IActivityLogService _activityLog;
        private readonly Services.IIngredientService _mainIngredientService;

        private static readonly HashSet<string> _allowedUnits = new(StringComparer.OrdinalIgnoreCase)
        {
            "BA", "BG", "BX", "CA", "CM", "CT", "DZ", "EA",
            "FL", "FT", "GA", "GM", "HR", "IN", "KG", "LB",
            "LT", "ML", "MR", "OZ", "PA", "PC", "PR", "PT",
            "RL", "TB"
        };

        public IngredientService(DapperContext context, IActivityLogService activityLog, Services.IIngredientService mainIngredientService  )
        {
            _context = context;
            _activityLog = activityLog;
            _mainIngredientService = mainIngredientService;
        }

        public async Task<(int StatusCode, object Body)> CreateIngredientByVendorAsync(VendorIngredientDto model, int currentUserId, string requestId)
        {
            var ResponseDto = new ResponseDto();
            var payload = System.Text.Json.JsonSerializer.Serialize(model);

            if (!_allowedUnits.Contains(model.Unit.ToLower()))
            {
                ResponseDto.Status = 400;
                ResponseDto.ResponseMsg = $"Invalid unit '{model.Unit}'. Allowed units: {string.Join(", ", _allowedUnits)}";
                await _activityLog.LogAsync(currentUserId, requestId, "POST /api/IngredientApi/CreateIngredientByVendor", payload,
                    System.Text.Json.JsonSerializer.Serialize(ResponseDto), "Warning");
                return (400, ResponseDto);
            }

            try
            {
                using var connection = _context.CreateConnection();
                connection.Open();

                if ((model.Weight == null || model.Weight <= 0) && (model.PackSize == null || model.PackSize <= 0))
                {
                    ResponseDto.Status = 400;
                    ResponseDto.ResponseMsg = "Either PackSize or Weight must be provided and greater than zero.";
                    await _activityLog.LogAsync(currentUserId, requestId, "POST /api/IngredientApi/CreateIngredientByVendor", payload,
                        System.Text.Json.JsonSerializer.Serialize(ResponseDto), "Warning");
                    return (400, ResponseDto);
                }

                var parameters = new DynamicParameters();
                parameters.Add("@RestaurantId", currentUserId);
                parameters.Add("@VendorId", currentUserId);
                parameters.Add("@Name", model.Name);
                parameters.Add("@Price", model.BasePrice);
                parameters.Add("@Unit", model.Unit);
                parameters.Add("@Size", model.Size);
                parameters.Add("@PackSize", model.PackSize);
                parameters.Add("@ClassificationId", model.ClassificationId);
                parameters.Add("@ClassificationDescriptor", model.ClassificationDescriptor);
                parameters.Add("@VendorProductId", model.VendorProductId);
                parameters.Add("@CreatedBy", currentUserId);

                if (model.RestaurantPrices != null && model.RestaurantPrices.Any())
                {
                    // Dedupe by restaurantId, last one wins
                    var deduped = model.RestaurantPrices
                        .GroupBy(r => r.RestaurantId)
                        .ToDictionary(
                            g => g.Key.ToString(),
                            g => g.Last().Price
                        );

                    parameters.Add("@RestaurantIds", string.Join(",", deduped.Keys));
                    parameters.Add("@RestaurantPrices", System.Text.Json.JsonSerializer.Serialize(deduped));
                }

                parameters.Add("@Action", "INSERT_BY_VENDOR");

                var result = await connection.QuerySingleAsync(
                     "sp_IngredientCRUD", parameters, commandType: CommandType.StoredProcedure
                 );

                int ingredientId = result.IngredientId;

                string? missingRaw = (string?)result.MissingRestaurantIds;

                List<int> invalidRestaurantIds = new();
                if (!string.IsNullOrEmpty(missingRaw))
                {
                    invalidRestaurantIds = missingRaw
                        .Split(',')
                        .Select(x => int.Parse(x))
                        .ToList();
                }

                if (invalidRestaurantIds.Any())
                {
                    ResponseDto.Status = 207;
                }
                else
                {
                    ResponseDto.Status = 200;
                }
                ResponseDto.ResponseMsg = "Ingredient created successfully.";
                ResponseDto.Data = new
                {
                    IngredientId = ingredientId,
                    InvalidRestaurantIds = invalidRestaurantIds  // [] if none missing
                };

                await _activityLog.LogAsync(currentUserId, requestId, "POST /api/IngredientApi/CreateIngredientByVendor", payload,
                    System.Text.Json.JsonSerializer.Serialize(new { ResponseDto.Status, ResponseDto.ResponseMsg, ResponseDto.Data }), "Success");

                // Original controller returned Ok(ResponseDto) even when Status is 207.
                return (200, ResponseDto);
            }
            catch (Exception ex)
            {
                ResponseDto.Status = 400;
                ResponseDto.ResponseMsg = $"{ex.Message}";
                await _activityLog.LogAsync(currentUserId, requestId, "POST /api/IngredientApi/CreateIngredientByVendor", payload,
                    System.Text.Json.JsonSerializer.Serialize(ResponseDto), "Error");
                return (400, ResponseDto);
            }
        }

        public async Task<(int StatusCode, object Body)> UpdateIngredientPricesAsync(UpdateIngredientPriceDto model, int currentUserId, string requestId)
        {
            var ResponseDto = new ResponseDto();
            var payload = System.Text.Json.JsonSerializer.Serialize(model);
            try
            {
                using var connection = _context.CreateConnection();
                connection.Open();

                if ((model.Weight == null || model.Weight <= 0) && (model.PackSize == null || model.PackSize <= 0))
                {
                    ResponseDto.Status = 400;
                    ResponseDto.ResponseMsg = "Either PackSize or Weight must be provided and greater than zero.";
                    await _activityLog.LogAsync(currentUserId, requestId, "PUT /api/IngredientApi/UpdateIngredientPrices", payload,
                        System.Text.Json.JsonSerializer.Serialize(ResponseDto), "Warning");
                    return (400, ResponseDto);
                }

                var parameters = new DynamicParameters();
                parameters.Add("@IngredientId", model.IngredientId);
                parameters.Add("@Price", model.BasePrice);
                parameters.Add("@PackSize", model.PackSize);
                parameters.Add("@VendorId", currentUserId);
                parameters.Add("@ModifiedBy", currentUserId);

                if (model.RestaurantPrices != null && model.RestaurantPrices.Any())
                {
                    // Dedupe by restaurantId, last one wins
                    var restaurantPricesDictUpd = model.RestaurantPrices
                        .GroupBy(r => r.RestaurantId)
                        .ToDictionary(
                            g => g.Key,
                            g => new
                            {
                                Price = g.Last().Price,
                                IsActive = g.Last().IsActive
                            }
                        );

                    parameters.Add("@RestaurantPrices", System.Text.Json.JsonSerializer.Serialize(restaurantPricesDictUpd));
                }

                parameters.Add("@Action", "UPDATE_PRICES");
                var result = await connection.QuerySingleAsync(
                    "sp_IngredientCRUD",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                string? missingRaw = (string?)result.MissingRestaurantIds;
                List<int> invalidRestaurantIds = new();
                if (!string.IsNullOrEmpty(missingRaw))
                {
                    invalidRestaurantIds = missingRaw
                        .Split(',')
                        .Select(x => int.Parse(x))
                        .ToList();
                }

                if (model.RestaurantPrices != null && model.RestaurantPrices.Any())
                {
                    var requestedRestaurantIds = model.RestaurantPrices
                        .Select(r => r.RestaurantId)
                        .Distinct()
                        .ToList();

                    if (invalidRestaurantIds.Any() &&
                        invalidRestaurantIds.Count == requestedRestaurantIds.Count)
                    {
                        ResponseDto.Status = 400;
                        ResponseDto.ResponseMsg = "All provided restaurant IDs are invalid.";
                        ResponseDto.Data = new { InvalidRestaurantIds = invalidRestaurantIds };
                        await _activityLog.LogAsync(currentUserId, requestId, "PUT /api/IngredientApi/UpdateIngredientPrices", payload,
                            System.Text.Json.JsonSerializer.Serialize(ResponseDto), "Warning");
                        return (400, ResponseDto);
                    }
                }

                if (invalidRestaurantIds.Any())
                {
                    ResponseDto.Status = 207;
                }
                else
                {
                    ResponseDto.Status = 200;
                }
                ResponseDto.ResponseMsg = "Prices updated successfully.";
                ResponseDto.Data = new
                {
                    InvalidRestaurantIds = invalidRestaurantIds  // [] if none missing
                };
                await _activityLog.LogAsync(currentUserId, requestId, "PUT /api/IngredientApi/UpdateIngredientPrices", payload,
                    System.Text.Json.JsonSerializer.Serialize(new { ResponseDto.Status, ResponseDto.ResponseMsg }), "Success");

                // Original controller returned Ok(ResponseDto) even when Status is 207.
                return (200, ResponseDto);
            }
            catch (Exception ex)
            {
                ResponseDto.Status = 400;
                ResponseDto.ResponseMsg = ex.Message;
                await _activityLog.LogAsync(currentUserId, requestId, "PUT /api/IngredientApi/UpdateIngredientPrices", payload,
                    System.Text.Json.JsonSerializer.Serialize(ResponseDto), "Error");
                return (400, ResponseDto);
            }
        }

        public async Task<(int StatusCode, object Body)> PatchIngredientDetailsAsync(UpdateIngredientDetailsDto model, int currentUserId, string requestId)
        {
            var ResponseDto = new ResponseDto();
            var payload = System.Text.Json.JsonSerializer.Serialize(model);

            if (!string.IsNullOrWhiteSpace(model.Unit) && !_allowedUnits.Contains(model.Unit.ToLower()))
            {
                ResponseDto.Status = 400;
                ResponseDto.ResponseMsg = $"Invalid unit '{model.Unit}'. Allowed units: {string.Join(", ", _allowedUnits)}";

                await _activityLog.LogAsync(
                    currentUserId,
                    requestId,
                    "PATCH /api/IngredientApi/PatchIngredientDetails",
                    payload,
                    System.Text.Json.JsonSerializer.Serialize(ResponseDto),
                    "Warning"
                );

                return (400, ResponseDto);
            }

            using var connection = _context.CreateConnection();
            connection.Open();

            try
            {
                // Merge: use incoming value if provided, otherwise keep existing
                var parameters = new DynamicParameters();
                parameters.Add("@IngredientId", model.IngredientId);
                parameters.Add("@VendorId", currentUserId);
                parameters.Add("@Name", model.Name);
                parameters.Add("@Unit", model.Unit);
                parameters.Add("@IsActive", model.IsActive);
                parameters.Add("@Size", model.Size);
                parameters.Add("@ClassificationId", model.ClassificationId);
                parameters.Add("@ClassificationDescriptor", model.ClassificationDescriptor);
                parameters.Add("@ModifiedBy", currentUserId);
                parameters.Add("@Action", "UPDATE");

                await connection.ExecuteAsync(
                    "sp_IngredientCRUD", parameters, commandType: CommandType.StoredProcedure
                );

                ResponseDto.Status = 200;
                ResponseDto.ResponseMsg = "Ingredient details updated successfully.";
                ResponseDto.Data = new { IngredientId = model.IngredientId };

                await _activityLog.LogAsync(
                    currentUserId,
                    requestId,
                    "PATCH /api/IngredientApi/PatchIngredientDetails",
                    payload,
                    System.Text.Json.JsonSerializer.Serialize(new
                    {
                        ResponseDto.Status,
                        ResponseDto.ResponseMsg,
                        ResponseDto.Data
                    }),
                    "Success"
                );

                return (200, ResponseDto);
            }
            catch (Exception ex)
            {
                ResponseDto.Status = 400;
                ResponseDto.ResponseMsg = ex.Message;
                await _activityLog.LogAsync(
                    currentUserId,
                    requestId,
                    "PATCH /api/IngredientApi/PatchIngredientDetails",
                    payload,
                    System.Text.Json.JsonSerializer.Serialize(ResponseDto),
                    "Error"
                );
                return (400, ResponseDto);
            }
            finally
            {
                connection.Close();
            }
        }
    }
}
