using Dapper;
using MenuOptics.Data;
using MenuOptics.Models;
using Stripe.Forwarding;
using System.Data;
using System.Data.SqlClient;

namespace MenuOptics.Services
{
    public class IngredientService : IIngredientService
    {
        private readonly DapperContext _context;
        private readonly IActivityLogService _activityLog;

        private static readonly HashSet<string> _allowedUnits = new(StringComparer.OrdinalIgnoreCase)
        {
            "BA", "BG", "BX", "CA", "CM", "CT", "DZ", "EA",
            "FL", "FT", "GA", "GM", "HR", "IN", "KG", "LB",
            "LT", "ML", "MR", "OZ", "PA", "PC", "PR", "PT",
            "RL", "TB"
        };

        public IngredientService(DapperContext context, IActivityLogService activityLog)
        {
            _context = context;
            _activityLog = activityLog;
        }

        public async Task<(int StatusCode, object Body)> CreateIngredientByVendorAsync(VendorIngredientDto model, int currentUserId, string requestId)
        {
            var ResponseDto = new ResponseDto();
            var payload = System.Text.Json.JsonSerializer.Serialize(model);


            if (model.Size <= 0)
            {
                ResponseDto.Status = 400;
                ResponseDto.ResponseMsg = "Size should be greater than 0";
                await _activityLog.LogAsync(currentUserId, requestId, "POST /api/IngredientApi/CreateIngredientByVendor", payload,
                    System.Text.Json.JsonSerializer.Serialize(ResponseDto), "Warning");
                return (400, ResponseDto);
            }

            if (!_allowedUnits.Contains(model.Unit?.ToLower()))
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

                decimal basePrice = 0;
                decimal size = 0;

                // When PackSize given
                if (model.PackSize != null && model.PackSize > 0)
                {
                    basePrice = model.BasePrice / model.PackSize.Value;
                    size = model.Size;
                }
                // When Weight given
                else if (model.Weight != null && model.Weight > 0)
                {
                    basePrice = model.BasePrice;
                    size = 1;
                }
                else
                {
                    ResponseDto.Status = 400;
                    ResponseDto.ResponseMsg = "Either PackSize or Weight must be provided and greater than zero.";
                    await _activityLog.LogAsync(currentUserId, requestId, "POST /api/IngredientApi/CreateIngredientByVendor", payload,
                        System.Text.Json.JsonSerializer.Serialize(ResponseDto), "Warning");
                    return (400, ResponseDto);
                } 

                var parameters = new DynamicParameters();
                parameters.Add("@UserId", currentUserId);
                parameters.Add("@VendorId", currentUserId);
                parameters.Add("@Name", model.Name);
                parameters.Add("@Price", basePrice);
                parameters.Add("@Unit", model.Unit);
                parameters.Add("@Size", size);
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
                            g => model.PackSize != null && model.PackSize > 0
                                ? g.Last().Price / model.PackSize.Value
                                : g.Last().Price
                        );

                    parameters.Add("@RestaurantIds", string.Join(",", deduped.Keys));
                    parameters.Add("@RestaurantPrices", System.Text.Json.JsonSerializer.Serialize(deduped));
                }

                parameters.Add("@Action", "INSERT_BY_VENDOR");

                var result = await connection.QuerySingleAsync(
                     "sp_IngredientCRUD", parameters, commandType: CommandType.StoredProcedure
                 );

                int ingredientId = result.IngredientId;

                string missingRaw = (string?)result.MissingRestaurantIds;

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


                Ingredient ingredient = (Ingredient)((ResponseDto)(await GetIngredientAsync(model.IngredientId)).Body).Data;

                decimal basePrice = 0;

                if (model.PackSize != null && model.PackSize > 0)
                {
                    basePrice = model.BasePrice / model.PackSize.Value;
                }
                else if (model.Weight != null && model.Weight > 0)
                {
                    basePrice = model.BasePrice;
                }
                else
                {
                    ResponseDto.Status = 400;
                    ResponseDto.ResponseMsg = "Either PackSize or Weight must be provided and greater than zero.";
                    await _activityLog.LogAsync(currentUserId, requestId, "PUT /api/IngredientApi/UpdateIngredientPrices", payload,
                        System.Text.Json.JsonSerializer.Serialize(ResponseDto), "Warning");
                    return (400, ResponseDto);
                }

                var parameters = new DynamicParameters();
                parameters.Add("@IngredientId", model.IngredientId);
                parameters.Add("@Price", basePrice);
                parameters.Add("@PackSize", model.PackSize);
                parameters.Add("@VendorId", basePrice);
                parameters.Add("@CreatedBy", currentUserId);
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
                                Price = model.PackSize != null && model.PackSize > 0
                                    ? Math.Abs(g.Last().Price) / model.PackSize.Value
                                    : Math.Abs(g.Last().Price),
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

        public async Task<(int StatusCode, object Body)> CreateIngredientAsync(Ingredient model, int currentUserId)
        {
            var ResponseDto = new ResponseDto();

            if (!_allowedUnits.Contains(model.Unit?.ToLower()))
            {
                ResponseDto.Status = 400;
                ResponseDto.ResponseMsg = $"Invalid unit '{model.Unit}'. Allowed units: {string.Join(", ", _allowedUnits)}";
                return (400, ResponseDto);
            }


            try
            {
                if (currentUserId == 0)
                {
                    ResponseDto.Status = 401;
                    ResponseDto.ResponseMsg = "Unauthorized";
                    return (401, ResponseDto);
                }

                using var connection = _context.CreateConnection();
                connection.Open();

                var parameters = new DynamicParameters();
                parameters.Add("@UserId", currentUserId);
                parameters.Add("@Name", model.Name);
                parameters.Add("@Price", model.Price);
                parameters.Add("@Unit", model.Unit.ToLower());
                parameters.Add("@Size", model.Size);
                parameters.Add("@PackSize", model.PackSize);
                parameters.Add("@PrivateVendorId", model.PrivateVendorId);
                parameters.Add("@CreatedBy", currentUserId);
                parameters.Add("@Action", "INSERT");

                int ingredientId = await connection.ExecuteScalarAsync<int>(
                    "sp_IngredientCRUD", parameters, commandType: CommandType.StoredProcedure
                );

                var parameters2 = new DynamicParameters();
                if (ingredientId > 0 && model.Stock != 0)
                {
                    parameters2.Add("@RestaurantId", currentUserId);
                    parameters2.Add("@VendorId", null);
                    parameters2.Add("@IngredientId", ingredientId);
                    parameters2.Add("@Stock", model.Stock);
                    parameters2.Add("@Price", (model.Stock * model.Price));
                    parameters2.Add("@CreatedBy", currentUserId);
                    parameters2.Add("@Action", "INSERT");
                }

                int inventoryId = await connection.ExecuteScalarAsync<int>(
                    "sp_InventoryCRUD", parameters2, commandType: CommandType.StoredProcedure
                );

                ResponseDto.Status = 200;
                ResponseDto.ResponseMsg = "Ingredient created successfully.";
                ResponseDto.Data = new { IngredientId = ingredientId, InventoryId = inventoryId };

                return (200, ResponseDto);
            }
            catch (Exception ex)
            {
                ResponseDto.Status = 500;
                ResponseDto.ResponseMsg = $"{ex.Message}";
                return (500, ResponseDto);
            }
        }

        public async Task<(int StatusCode, object Body)> AddIngredientStockAsync(Inventory model, int currentUserId)
        {
            var ResponseDto = new ResponseDto();

            // Validate stock
            if (string.IsNullOrEmpty(model.Stock.ToString()))
            {
                ResponseDto.Status = 400;
                ResponseDto.ResponseMsg = "Initial Stock cannot be 0, Please enter the stock";
                return (400, ResponseDto);
            }

            try
            {
                if (currentUserId == 0)
                {
                    ResponseDto.Status = 401;
                    ResponseDto.ResponseMsg = "Unauthorized";
                    return (401, ResponseDto);
                }

                using var connection = _context.CreateConnection();
                connection.Open();

                Ingredient ingredient = (Ingredient)((ResponseDto)(await GetIngredientAsync(model.IngredientId)).Body).Data;

                var parameters = new DynamicParameters();
                parameters.Add("@RestaurantId", currentUserId);
                parameters.Add("@VendorId", null);
                parameters.Add("@IngredientId", model.IngredientId);
                parameters.Add("@Stock", model.Stock);
                parameters.Add("@Price", (model.Stock * ingredient.Price));
                parameters.Add("@CreatedBy", currentUserId);
                parameters.Add("@Action", "INSERT");

                int inventoryId = await connection.ExecuteScalarAsync<int>(
                    "sp_InventoryCRUD", parameters, commandType: CommandType.StoredProcedure
                );

                ResponseDto.Status = 200;
                ResponseDto.ResponseMsg = "Ingredient created successfully.";
                ResponseDto.Data = new { IngredientId = model.IngredientId, InventoryId = inventoryId };

                return (200, ResponseDto);
            }
            catch (Exception ex)
            {
                ResponseDto.Status = 500;
                ResponseDto.ResponseMsg = $"{ex.Message}";
                return (500, ResponseDto);
            }
        }

        public async Task<(int StatusCode, object Body)> GetIngredientAsync(int id)
        {
            var ResponseDto = new ResponseDto();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@IngredientId", id);
                parameters.Add("@Action", "SELECT");

                var ingredient = await connection.QueryFirstOrDefaultAsync<Ingredient>("sp_IngredientCRUD", parameters, commandType: CommandType.StoredProcedure);

                if (ingredient == null)
                {
                    ResponseDto.Status = 404;
                    ResponseDto.ResponseMsg = "Ingredient not found.";
                    return (404, ResponseDto);
                }

                ResponseDto.Status = 200;
                ResponseDto.ResponseMsg = "Ingredient retrieved successfully.";
                ResponseDto.Data = ingredient;
                return (200, ResponseDto);
            }
            catch (Exception ex)
            {
                ResponseDto.Status = 500;
                ResponseDto.ResponseMsg = $"{ex.Message}";
                return (500, ResponseDto);
            }
            finally
            {
                connection.Close();
            }
        }

        public async Task<(int StatusCode, object Body)> GetAllActiveIngredientsByUserIdAsync(IQueryCollection query, int currentUserId)
        {
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                if (query.ContainsKey("draw"))
                {
                    var dt = ParseDataTablesQuery(query);
                    int pageNumber = (dt.Start / dt.Length) + 1;

                    var parameters = new DynamicParameters();
                    parameters.Add("@UserId", currentUserId);
                    parameters.Add("@IsActive", 1);
                    parameters.Add("@Action", "SELECT_ALL");
                    parameters.Add("@PageNumber", pageNumber);
                    parameters.Add("@PageSize", dt.Length);
                    parameters.Add("@SearchValue", dt.SearchValue);
                    parameters.Add("@TotalCount", dbType: DbType.Int32, direction: ParameterDirection.Output);

                    var ingredients = await connection.QueryAsync<Ingredient>(
                        "sp_IngredientCRUD", parameters, commandType: CommandType.StoredProcedure);

                    int totalCount = parameters.Get<int>("@TotalCount");

                    var dtResponse = new DataTablesResponseDto<Ingredient>
                    {
                        Draw = dt.Draw,
                        RecordsTotal = totalCount,
                        RecordsFiltered = totalCount,
                        Data = ingredients
                    };

                    return (200, dtResponse);
                }
                else
                {
                    var ResponseDto = new ResponseDto();
                    var parameters = new DynamicParameters();
                    parameters.Add("@UserId", currentUserId);
                    parameters.Add("@IsActive", 1);
                    parameters.Add("@Action", "SELECT_ALL");
                    parameters.Add("@PageSize", -1); // Return all rows

                    var ingredients = await connection.QueryAsync<Ingredient>("sp_IngredientCRUD", parameters, commandType: CommandType.StoredProcedure);
                    ResponseDto.Status = 200;
                    ResponseDto.ResponseMsg = "Ingredients retrieved successfully.";
                    ResponseDto.Data = ingredients;
                    return (200, ResponseDto);
                }
            }
            catch (Exception ex)
            {
                return HandleListError(query, ex);
            }
            finally
            {
                connection.Close();
            }
        }

        public async Task<(int StatusCode, object Body)> GetAllActiveInactiveIngredientsAsync(IQueryCollection query, int currentUserId)
        {
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                if (query.ContainsKey("draw"))
                {
                    var dt = ParseDataTablesQuery(query);

                    int pageNumber = (dt.Start / dt.Length) + 1;

                    var parameters = new DynamicParameters();
                    parameters.Add("@UserId", currentUserId);
                    parameters.Add("@Action", "SELECT_ALL");
                    parameters.Add("@PageNumber", pageNumber);
                    parameters.Add("@PageSize", dt.Length);
                    parameters.Add("@SearchValue", dt.SearchValue);
                    parameters.Add("@TotalCount", dbType: DbType.Int32, direction: ParameterDirection.Output);

                    var ingredients = await connection.QueryAsync<Ingredient>(
                        "sp_IngredientCRUD", parameters, commandType: CommandType.StoredProcedure);

                    int totalCount = parameters.Get<int>("@TotalCount");

                    var dtResponse = new DataTablesResponseDto<Ingredient>
                    {
                        Draw = dt.Draw,
                        RecordsTotal = totalCount,
                        RecordsFiltered = totalCount,
                        Data = ingredients
                    };

                    return (200, dtResponse);
                }
                else
                {
                    var ResponseDto = new ResponseDto();
                    var parameters = new DynamicParameters();
                    parameters.Add("@UserId", currentUserId);
                    parameters.Add("@Action", "SELECT_ALL");
                    parameters.Add("@PageSize", -1); // Return all rows

                    var ingredients = await connection.QueryAsync<Ingredient>("sp_IngredientCRUD", parameters, commandType: CommandType.StoredProcedure);
                    ResponseDto.Status = 200;
                    ResponseDto.ResponseMsg = "Ingredients retrieved successfully.";
                    ResponseDto.Data = ingredients;
                    return (200, ResponseDto);
                }
            }
            catch (Exception ex)
            {
                return HandleListError(query, ex);
            }
            finally
            {
                connection.Close();
            }
        }

        public async Task<(int StatusCode, object Body)> GetAllIngredientsByVendorIdAsync(IQueryCollection query, int vendorId, int currentUserId, bool isPrivate)
        {
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                if (query.ContainsKey("draw"))
                {
                    var dt = ParseDataTablesQuery(query);
                    int pageNumber = (dt.Start / dt.Length) + 1;

                    var parameters = new DynamicParameters();
                    parameters.Add("@UserId", currentUserId);
                    if (isPrivate)
                        parameters.Add("@PrivateVendorId", vendorId);
                    else
                        parameters.Add("@VendorId", vendorId);
                    parameters.Add("@Action", "SELECT_ALL"); // Use the correct SP action
                    parameters.Add("@PageNumber", pageNumber);
                    parameters.Add("@PageSize", dt.Length);
                    parameters.Add("@SearchValue", dt.SearchValue);
                    parameters.Add("@TotalCount", dbType: DbType.Int32, direction: ParameterDirection.Output);

                    var ingredients = await connection.QueryAsync<Ingredient>(
                        "sp_IngredientCRUD", parameters, commandType: CommandType.StoredProcedure);

                    int totalCount = parameters.Get<int>("@TotalCount");

                    var dtResponse = new DataTablesResponseDto<Ingredient>
                    {
                        Draw = dt.Draw,
                        RecordsTotal = totalCount,
                        RecordsFiltered = totalCount,
                        Data = ingredients
                    };

                    return (200, dtResponse);
                }
                else
                {
                    var ResponseDto = new ResponseDto();
                    var parameters = new DynamicParameters();
                    parameters.Add("@UserId", currentUserId);
                    parameters.Add("@VendorId", vendorId);
                    parameters.Add("@Action", "SELECT_ALL");
                    parameters.Add("@PageSize", -1); // Return all rows

                    var ingredients = await connection.QueryAsync<Ingredient>("sp_IngredientCRUD", parameters, commandType: CommandType.StoredProcedure);

                    ResponseDto.Status = 200;
                    ResponseDto.ResponseMsg = "Ingredients retrieved successfully.";
                    ResponseDto.Data = ingredients;
                    return (200, ResponseDto);
                }
            }
            catch (Exception ex)
            {
                return HandleListError(query, ex);
            }
            finally
            {
                connection.Close();
            }
        }

        public async Task<(int StatusCode, object Body)> GetAllIngredientsByUserIdParamAsync(int userId, IQueryCollection query)
        {
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                if (query.ContainsKey("draw"))
                {
                    var dt = ParseDataTablesQuery(query);
                    int pageNumber = (dt.Start / dt.Length) + 1;

                    var parameters = new DynamicParameters();
                    parameters.Add("@UserId", userId);
                    parameters.Add("@Action", "SELECT_ALL");
                    parameters.Add("@PageNumber", pageNumber);
                    parameters.Add("@PageSize", dt.Length);
                    parameters.Add("@SearchValue", dt.SearchValue);
                    parameters.Add("@TotalCount", dbType: DbType.Int32, direction: ParameterDirection.Output);

                    var ingredients = await connection.QueryAsync<Ingredient>(
                        "sp_IngredientCRUD", parameters, commandType: CommandType.StoredProcedure);

                    int totalCount = parameters.Get<int>("@TotalCount");

                    var dtResponse = new DataTablesResponseDto<Ingredient>
                    {
                        Draw = dt.Draw,
                        RecordsTotal = totalCount,
                        RecordsFiltered = totalCount,
                        Data = ingredients
                    };

                    return (200, dtResponse);
                }
                else
                {
                    var ResponseDto = new ResponseDto();
                    var parameters = new DynamicParameters();
                    parameters.Add("@UserId", userId);
                    parameters.Add("@Action", "SELECT_ALL");
                    parameters.Add("@PageSize", -1); // Return all rows

                    var ingredients = await connection.QueryAsync<Ingredient>("sp_IngredientCRUD", parameters, commandType: CommandType.StoredProcedure);

                    ResponseDto.Status = 200;
                    ResponseDto.ResponseMsg = "Ingredients retrieved successfully.";
                    ResponseDto.Data = ingredients;
                    return (200, ResponseDto);
                }
            }
            catch (Exception ex)
            {
                return HandleListError(query, ex);
            }
            finally
            {
                connection.Close();
            }
        }

        public async Task<(int StatusCode, object Body)> UpdateIngredientAsync(Ingredient model, int currentUserId)
        {
            var ResponseDto = new ResponseDto();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                // Ensure IngredientId is provided for update
                if (model.IngredientId <= 0)
                {
                    ResponseDto.Status = 400;
                    ResponseDto.ResponseMsg = "IngredientId is required for updating an ingredient.";
                    return (400, ResponseDto);
                }


                var parameters = new DynamicParameters();
                parameters.Add("@IngredientId", model.IngredientId);
                parameters.Add("@UserId", currentUserId); 
                parameters.Add("@Name", model.Name);
                parameters.Add("@Price", model.Price);
                parameters.Add("@Unit", model.Unit.ToLower());
                parameters.Add("@Size", model.Size);
                parameters.Add("@PackSize", model.PackSize);
                parameters.Add("@ModifiedBy", currentUserId); 
                parameters.Add("@IsActive", 1); 
                parameters.Add("@Action", "UPDATE");

                await connection.QueryFirstOrDefaultAsync<int>("sp_IngredientCRUD", parameters, commandType: CommandType.StoredProcedure);

               
                ResponseDto.Status = 200;
                ResponseDto.ResponseMsg = "Ingredient updated successfully.";
                ResponseDto.Data = new { IngredientId = model.IngredientId };
                return (200, ResponseDto);
            }
            catch (SqlException sqlEx)
            {
                // SQL Server specific exceptions
                ResponseDto.Status = 500;
                ResponseDto.ResponseMsg = $"Expection: {sqlEx.Message}";
                return (500, ResponseDto);
            }
            catch (Exception ex)
            {
                ResponseDto.Status = 500;
                ResponseDto.ResponseMsg = $"{ex.Message}";
                return (500, ResponseDto);
            }
            finally
            {
                connection.Close();
            }
        }

        public async Task<(int StatusCode, object Body)> PatchIngredientDetailsAsync(UpdateIngredientDetailsDto model, int currentUserId, string requestId)
        {
            var ResponseDto = new ResponseDto();
            var payload = System.Text.Json.JsonSerializer.Serialize(model);

            if (model.IngredientId <= 0)
            {
                ResponseDto.Status = 400;
                ResponseDto.ResponseMsg = "IngredientId is required.";
                await _activityLog.LogAsync(
                    0,
                    requestId,
                    "PATCH /api/IngredientApi/PatchIngredientDetails",
                    payload,
                    System.Text.Json.JsonSerializer.Serialize(ResponseDto),
                    "Warning"
                );
                return (400, ResponseDto);
            }

            if (model.Name == null && model.Unit == null && model.IsActive == null)
            {
                ResponseDto.Status = 400;
                ResponseDto.ResponseMsg = "At least one field (Name, Unit, or IsActive) must be provided.";

                await _activityLog.LogAsync(
                    0,
                    requestId,
                    "PATCH /api/IngredientApi/PatchIngredientDetails",
                    payload,
                    System.Text.Json.JsonSerializer.Serialize(ResponseDto),
                    "Warning"
                );
                return (400, ResponseDto);
            }

            if (!string.IsNullOrWhiteSpace(model.Unit) &&
                !_allowedUnits.Contains(model.Unit.ToLower()))
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

            if (currentUserId == 0)
            {
                ResponseDto.Status = 401;
                ResponseDto.ResponseMsg = "Unauthorized";

                await _activityLog.LogAsync(
                    currentUserId,
                    requestId,
                    "PATCH /api/IngredientApi/PatchIngredientDetails",
                    payload,
                    System.Text.Json.JsonSerializer.Serialize(ResponseDto),
                    "Warning"
                );
                return (401, ResponseDto);
            }

            using var connection = _context.CreateConnection();
            connection.Open();

            try
            {
                Ingredient ingredient = (Ingredient)((ResponseDto)(await GetIngredientAsync(model.IngredientId)).Body).Data;

                if (ingredient == null)
                {
                    ResponseDto.Status = 400;
                    ResponseDto.ResponseMsg = "Ingredient not found.";
                    await _activityLog.LogAsync(
                        currentUserId,
                        requestId,
                        "PATCH /api/IngredientApi/PatchIngredientDetails",
                        payload,
                        System.Text.Json.JsonSerializer.Serialize(ResponseDto),
                        "Warning"
                    );
                    // Original controller returned NotFound(ResponseDto) while Status stayed 400.
                    return (404, ResponseDto);
                }

                decimal price = 0;
                decimal size = 0;

                // When PackSize given
                if (model.Size!=null)
                {
                    if (ingredient.PackSize!=null)
                    {
                        price = (ingredient.Price/ingredient.Size)*model.Size.Value;
                        size=model.Size.Value;
                    }
                    else
                    {
                        size = 1;
                    }
                }

                // Merge: use incoming value if provided, otherwise keep existing
                var parameters = new DynamicParameters();
                parameters.Add("@IngredientId", model.IngredientId);
                parameters.Add("@UserId", currentUserId);
                parameters.Add("@Name", model.Name ?? ingredient.Name);
                parameters.Add("@Unit", model.Unit ?? ingredient.Unit);
                parameters.Add("@IsActive", model.IsActive ?? ingredient.IsActive);
                parameters.Add("@Price", price);
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
            catch (SqlException sqlEx)
            {
                ResponseDto.Status = 400;
                ResponseDto.ResponseMsg = $"{sqlEx.Message}";
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
            catch (Exception ex)
            {
                ResponseDto.Status = 400;
                ResponseDto.ResponseMsg = $"{ex.Message}";
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


        public async Task<(int StatusCode, object Body)> ToggleIngredientActiveAsync(int id, int currentUserId)
        {
            var ResponseDto = new ResponseDto();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@IngredientId", id);
                parameters.Add("@Action", "SELECT");

                var ingredient = await connection.QueryFirstOrDefaultAsync<Ingredient>("sp_IngredientCRUD", parameters, commandType: CommandType.StoredProcedure);

                if (ingredient == null)
                {
                    ResponseDto.Status = 404;
                    ResponseDto.ResponseMsg = "Ingredient not found.";
                    return (404, ResponseDto);
                }

                parameters = new DynamicParameters();
                parameters.Add("@IngredientId", id);
                parameters.Add("@Userid", currentUserId);
                parameters.Add("@IsActive", !ingredient.IsActive);
                parameters.Add("@ModifiedBy", currentUserId);
                parameters.Add("@Action", "TOGGLE_ACTIVE");

                await connection.ExecuteAsync("sp_IngredientCRUD", parameters, commandType: CommandType.StoredProcedure);

                ResponseDto.Status = 200;
                ResponseDto.ResponseMsg = $"Ingredient {(ingredient.IsActive ? "deactivated" : "activated")} successfully.";
                ResponseDto.Data = new { IngredientId = id, IsActive = !ingredient.IsActive };
                return (200, ResponseDto);
            }
            catch (SqlException sqlEx)
            {
                // SQL Server specific exceptions
                ResponseDto.Status = 500;
                ResponseDto.ResponseMsg = $"Expection: {sqlEx.Message}";
                return (500, ResponseDto);
            }
            catch (InvalidOperationException invalidOpEx)
            {
                ResponseDto.Status = 422;
                ResponseDto.ResponseMsg = $"Invalid operation: {invalidOpEx.Message}";
                return (422, ResponseDto);
            }
            catch (Exception ex)
            {
                ResponseDto.Status = 500;
                ResponseDto.ResponseMsg = $"{ex.Message}";
                return (500, ResponseDto);
            }
            finally
            {
                connection.Close();
            }
        }

       

        /// <summary>
        /// Parses the standard DataTables paging/sorting parameters from the query string.
        /// Parsing failures throw (and are handled by the caller) exactly as before.
        /// </summary>
        private static (int Draw, int Start, int Length, string SearchValue, int OrderColumnIndex, string OrderDir) ParseDataTablesQuery(IQueryCollection query)
        {
            int draw = int.Parse(query["draw"]);
            int start = int.Parse(query["start"]);
            int length = int.Parse(query["length"]);
            string searchValue = query["searchValue"].ToString() ?? "";

            int orderColumnIndex = 1;
            if (query.ContainsKey("orderColumn"))
            {
                orderColumnIndex = int.Parse(query["orderColumn"]);
            }
            string orderDir = "asc";
            if (query.ContainsKey("orderDir"))
            {
                orderDir = query["orderDir"].ToString();
            }

            return (draw, start, length, searchValue, orderColumnIndex, orderDir);
        }

        /// <summary>
        /// Error shape shared by the ingredient list endpoints: DataTables requests get an
        /// empty table body (HTTP 200), plain requests get a 500 ResponseDto envelope.
        /// </summary>
        private static (int StatusCode, object Body) HandleListError(IQueryCollection query, Exception ex)
        {
            if (query.ContainsKey("draw"))
            {
                int draw = int.Parse(query["draw"].FirstOrDefault() ?? "1");
                return (200, new DataTablesResponseDto<Ingredient> { Draw = draw, Data = Enumerable.Empty<Ingredient>() });
            }
            return (500, new ResponseDto { Status = 500, ResponseMsg = ex.Message });
        }
    }
}
