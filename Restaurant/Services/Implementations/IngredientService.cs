using Dapper;
using MenuOptics.Data;
using MenuOptics.Models;
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

        public async Task<(int StatusCode, object Body)> CreateIngredientByVendorAsync(VendorIngredientRequest model, int currentUserId, string requestId)
        {
            var response = new Response();
            var payload = System.Text.Json.JsonSerializer.Serialize(model);

            if (model.Size <= 0 || model.Size == null)
            {
                response.Status = 400;
                response.ResponseMsg = "Size should be greater than 0";
                await _activityLog.LogAsync(currentUserId, requestId, "POST /api/IngredientApi/CreateIngredientByVendor", payload,
                    System.Text.Json.JsonSerializer.Serialize(response), "Warning");
                return (400, response);
            }

            if (!_allowedUnits.Contains(model.Unit?.ToLower()))
            {
                response.Status = 400;
                response.ResponseMsg = $"Invalid unit '{model.Unit}'. Allowed units: {string.Join(", ", _allowedUnits)}";
                await _activityLog.LogAsync(currentUserId, requestId, "POST /api/IngredientApi/CreateIngredientByVendor", payload,
                    System.Text.Json.JsonSerializer.Serialize(response), "Warning");
                return (400, response);
            }

            try
            {
                using var connection = _context.CreateConnection();
                connection.Open();

                decimal basePrice = 0;
                decimal restaurantPrice = 0;

                // When PackSize given
                if (model.PackSize != null && model.PackSize > 0)
                {
                    basePrice = model.BasePrice / model.PackSize.Value;
                }
                // When Weight given
                else if (model.Weight != null && model.Weight > 0)
                {
                    basePrice = model.BasePrice / model.Weight.Value;
                }
                else
                {
                    response.Status = 400;
                    response.ResponseMsg = "Either PackSize or Weight must be provided and greater than zero.";
                    await _activityLog.LogAsync(currentUserId, requestId, "POST /api/IngredientApi/CreateIngredientByVendor", payload,
                        System.Text.Json.JsonSerializer.Serialize(response), "Warning");
                    return (400, response);
                }

                var parameters = new DynamicParameters();
                parameters.Add("@UserId", currentUserId);
                parameters.Add("@VendorId", currentUserId);
                parameters.Add("@Name", model.Name);
                parameters.Add("@Price", basePrice);
                parameters.Add("@Unit", model.Unit);
                parameters.Add("@Size", model.Size);
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
                                : g.Last().Price / model.Weight.Value
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
                    response.Status = 207;
                }
                else
                {
                    response.Status = 200;
                }
                response.ResponseMsg = "Ingredient created successfully.";
                response.Data = new
                {
                    IngredientId = ingredientId,
                    InvalidRestaurantIds = invalidRestaurantIds  // [] if none missing
                };

                await _activityLog.LogAsync(currentUserId, requestId, "POST /api/IngredientApi/CreateIngredientByVendor", payload,
                    System.Text.Json.JsonSerializer.Serialize(new { response.Status, response.ResponseMsg, response.Data }), "Success");

                // Original controller returned Ok(response) even when Status is 207.
                return (200, response);
            }
            catch (Exception ex)
            {
                response.Status = 400;
                response.ResponseMsg = $"{ex.Message}";
                await _activityLog.LogAsync(currentUserId, requestId, "POST /api/IngredientApi/CreateIngredientByVendor", payload,
                    System.Text.Json.JsonSerializer.Serialize(response), "Error");
                return (400, response);
            }
        }

        public async Task<(int StatusCode, object Body)> UpdateIngredientPricesAsync(UpdateIngredientPriceRequest model, int currentUserId, string requestId)
        {
            var response = new Response();
            var payload = System.Text.Json.JsonSerializer.Serialize(model);
            try
            {
                using var connection = _context.CreateConnection();
                connection.Open();

                decimal basePrice = 0;

                if (model.PackSize != null && model.PackSize > 0)
                {
                    basePrice = model.BasePrice / model.PackSize.Value;
                }
                else if (model.Weight != null && model.Weight > 0)
                {
                    basePrice = model.BasePrice / model.Weight.Value;
                }
                else
                {
                    response.Status = 400;
                    response.ResponseMsg = "Either PackSize or Weight must be provided and greater than zero.";
                    await _activityLog.LogAsync(currentUserId, requestId, "PUT /api/IngredientApi/UpdateIngredientPrices", payload,
                        System.Text.Json.JsonSerializer.Serialize(response), "Warning");
                    return (400, response);
                }

                var parameters = new DynamicParameters();
                parameters.Add("@IngredientId", model.IngredientId);
                parameters.Add("@Price", basePrice);
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
                                    : Math.Abs(g.Last().Price) / model.Weight!.Value,
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
                        response.Status = 400;
                        response.ResponseMsg = "All provided restaurant IDs are invalid.";
                        response.Data = new { InvalidRestaurantIds = invalidRestaurantIds };
                        await _activityLog.LogAsync(currentUserId, requestId, "PUT /api/IngredientApi/UpdateIngredientPrices", payload,
                            System.Text.Json.JsonSerializer.Serialize(response), "Warning");
                        return (400, response);
                    }
                }

                if (invalidRestaurantIds.Any())
                {
                    response.Status = 207;
                }
                else
                {
                    response.Status = 200;
                }
                response.ResponseMsg = "Prices updated successfully.";
                response.Data = new
                {
                    InvalidRestaurantIds = invalidRestaurantIds  // [] if none missing
                };
                await _activityLog.LogAsync(currentUserId, requestId, "PUT /api/IngredientApi/UpdateIngredientPrices", payload,
                    System.Text.Json.JsonSerializer.Serialize(new { response.Status, response.ResponseMsg }), "Success");

                // Original controller returned Ok(response) even when Status is 207.
                return (200, response);
            }
            catch (Exception ex)
            {
                response.Status = 400;
                response.ResponseMsg = ex.Message;
                await _activityLog.LogAsync(currentUserId, requestId, "PUT /api/IngredientApi/UpdateIngredientPrices", payload,
                    System.Text.Json.JsonSerializer.Serialize(response), "Error");
                return (400, response);
            }
        }

        public async Task<(int StatusCode, object Body)> CreateIngredientAsync(Ingredient model, int currentUserId)
        {
            var response = new Response();

            if (!_allowedUnits.Contains(model.Unit?.ToLower()))
            {
                response.Status = 400;
                response.ResponseMsg = $"Invalid unit '{model.Unit}'. Allowed units: {string.Join(", ", _allowedUnits)}";
                return (400, response);
            }

           

            try
            {
                if (currentUserId == 0)
                {
                    response.Status = 401;
                    response.ResponseMsg = "Unauthorized";
                    return (401, response);
                }

                using var connection = _context.CreateConnection();
                connection.Open();

                var parameters = new DynamicParameters();
                parameters.Add("@UserId", currentUserId);
                parameters.Add("@Name", model.Name);
                parameters.Add("@Price", model.Price);
                parameters.Add("@Unit", model.Unit.ToLower());
                parameters.Add("@Size", model.Size);
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

                response.Status = 200;
                response.ResponseMsg = "Ingredient created successfully.";
                response.Data = new { IngredientId = ingredientId, InventoryId = inventoryId };

                return (200, response);
            }
            catch (Exception ex)
            {
                response.Status = 500;
                response.ResponseMsg = $"{ex.Message}";
                return (500, response);
            }
        }

        public async Task<(int StatusCode, object Body)> AddIngredientStockAsync(Inventory model, int currentUserId)
        {
            var response = new Response();

            // Validate stock
            if (string.IsNullOrEmpty(model.Stock.ToString()))
            {
                response.Status = 400;
                response.ResponseMsg = "Initial Stock cannot be 0, Please enter the stock";
                return (400, response);
            }

            try
            {
                if (currentUserId == 0)
                {
                    response.Status = 401;
                    response.ResponseMsg = "Unauthorized";
                    return (401, response);
                }

                using var connection = _context.CreateConnection();
                connection.Open();

                var parameters = new DynamicParameters();
                parameters.Add("@RestaurantId", currentUserId);
                parameters.Add("@VendorId", null);
                parameters.Add("@IngredientId", model.IngredientId);
                parameters.Add("@Stock", model.Stock);
                parameters.Add("@CreatedBy", currentUserId);
                parameters.Add("@Action", "INSERT");

                int inventoryId = await connection.ExecuteScalarAsync<int>(
                    "sp_InventoryCRUD", parameters, commandType: CommandType.StoredProcedure
                );

                response.Status = 200;
                response.ResponseMsg = "Ingredient created successfully.";
                response.Data = new { IngredientId = model.IngredientId, InventoryId = inventoryId };

                return (200, response);
            }
            catch (Exception ex)
            {
                response.Status = 500;
                response.ResponseMsg = $"{ex.Message}";
                return (500, response);
            }
        }

        public async Task<(int StatusCode, object Body)> GetIngredientAsync(int id)
        {
            var response = new Response();
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
                    response.Status = 404;
                    response.ResponseMsg = "Ingredient not found.";
                    return (404, response);
                }

                response.Status = 200;
                response.ResponseMsg = "Ingredient retrieved successfully.";
                response.Data = ingredient;
                return (200, response);
            }
            catch (Exception ex)
            {
                response.Status = 500;
                response.ResponseMsg = $"{ex.Message}";
                return (500, response);
            }
            finally
            {
                connection.Close();
            }
        }

        public async Task<(int StatusCode, object Body)> GetAllIngredientsAsync(IQueryCollection query)
        {
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                if (query.ContainsKey("draw"))
                {
                    var dt = ParseDataTablesQuery(query);

                    string orderBy = dt.OrderColumnIndex switch
                    {
                        1 => "Name",
                        2 => "Unit",
                        3 => "IsActive",
                        4 => "CreatedOn",
                        5 => "Price",
                        _ => "Name"
                    };

                    int pageNumber = (dt.Start / dt.Length) + 1;

                    var parameters = new DynamicParameters();
                    parameters.Add("@Action", "SELECT_ALL");
                    parameters.Add("@PageNumber", pageNumber);
                    parameters.Add("@PageSize", dt.Length);
                    parameters.Add("@SearchValue", dt.SearchValue);
                    parameters.Add("@OrderBy", orderBy);
                    parameters.Add("@OrderDir", dt.OrderDir);
                    parameters.Add("@TotalCount", dbType: DbType.Int32, direction: ParameterDirection.Output);

                    var ingredients = await connection.QueryAsync<Ingredient>(
                        "sp_IngredientCRUD", parameters, commandType: CommandType.StoredProcedure);

                    int totalCount = parameters.Get<int>("@TotalCount");

                    var dtResponse = new DataTablesResponse<Ingredient>
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
                    var response = new Response();
                    var parameters = new DynamicParameters();
                    parameters.Add("@Action", "SELECT_ALL");
                    parameters.Add("@PageSize", -1); // Return all rows

                    var ingredients = await connection.QueryAsync<Ingredient>("sp_IngredientCRUD", parameters, commandType: CommandType.StoredProcedure);
                    response.Status = 200;
                    response.ResponseMsg = "Ingredients retrieved successfully.";
                    response.Data = ingredients;
                    return (200, response);
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

        public async Task<(int StatusCode, object Body)> GetAllIngredientsByUserIdAsync(IQueryCollection query, int currentUserId)
        {
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                if (query.ContainsKey("draw"))
                {
                    var dt = ParseDataTablesQuery(query);

                    string orderBy = dt.OrderColumnIndex switch
                    {
                        1 => "Name",
                        2 => "Unit",
                        3 => "IsActive",
                        4 => "CreatedOn",
                        5 => "Price",
                        _ => "Name"
                    };

                    int pageNumber = (dt.Start / dt.Length) + 1;

                    var parameters = new DynamicParameters();
                    parameters.Add("@UserId", currentUserId);
                    parameters.Add("@Action", "SELECT_ALL_BY_USERID");
                    parameters.Add("@PageNumber", pageNumber);
                    parameters.Add("@PageSize", dt.Length);
                    parameters.Add("@SearchValue", dt.SearchValue);
                    parameters.Add("@OrderBy", orderBy);
                    parameters.Add("@OrderDir", dt.OrderDir);
                    parameters.Add("@TotalCount", dbType: DbType.Int32, direction: ParameterDirection.Output);

                    var ingredients = await connection.QueryAsync<Ingredient>(
                        "sp_IngredientCRUD", parameters, commandType: CommandType.StoredProcedure);

                    int totalCount = parameters.Get<int>("@TotalCount");

                    var dtResponse = new DataTablesResponse<Ingredient>
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
                    var response = new Response();
                    var parameters = new DynamicParameters();
                    parameters.Add("@UserId", currentUserId);
                    parameters.Add("@Action", "SELECT_ALL_BY_USERID");
                    parameters.Add("@PageSize", -1); // Return all rows

                    var ingredients = await connection.QueryAsync<Ingredient>("sp_IngredientCRUD", parameters, commandType: CommandType.StoredProcedure);
                    response.Status = 200;
                    response.ResponseMsg = "Ingredients retrieved successfully.";
                    response.Data = ingredients;
                    return (200, response);
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

                    string orderBy = dt.OrderColumnIndex switch
                    {
                        1 => "Name",
                        2 => "VendorProductId",
                        3 => "Unit",
                        4 => "IsActive",
                        5 => "FullName",
                        6 => "CreatedOn",
                        7 => "Price",
                        _ => "Name"
                    };

                    int pageNumber = (dt.Start / dt.Length) + 1;

                    var parameters = new DynamicParameters();
                    parameters.Add("@UserId", currentUserId);
                    parameters.Add("@Action", "SELECT_ALL_INGREDIENTS_BY_USERID");
                    parameters.Add("@PageNumber", pageNumber);
                    parameters.Add("@PageSize", dt.Length);
                    parameters.Add("@SearchValue", dt.SearchValue);
                    parameters.Add("@OrderBy", orderBy);
                    parameters.Add("@OrderDir", dt.OrderDir);
                    parameters.Add("@TotalCount", dbType: DbType.Int32, direction: ParameterDirection.Output);

                    var ingredients = await connection.QueryAsync<Ingredient>(
                        "sp_IngredientCRUD", parameters, commandType: CommandType.StoredProcedure);

                    int totalCount = parameters.Get<int>("@TotalCount");

                    var dtResponse = new DataTablesResponse<Ingredient>
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
                    var response = new Response();
                    var parameters = new DynamicParameters();
                    parameters.Add("@UserId", currentUserId);
                    parameters.Add("@Action", "SELECT_ALL_INGREDIENTS_BY_USERID");
                    parameters.Add("@PageSize", -1); // Return all rows

                    var ingredients = await connection.QueryAsync<Ingredient>("sp_IngredientCRUD", parameters, commandType: CommandType.StoredProcedure);
                    response.Status = 200;
                    response.ResponseMsg = "Ingredients retrieved successfully.";
                    response.Data = ingredients;
                    return (200, response);
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

        public async Task<(int StatusCode, object Body)> GetAllIngredientsByVendorIdAsync(IQueryCollection query, int vendorId)
        {
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                if (query.ContainsKey("draw"))
                {
                    var dt = ParseDataTablesQuery(query);

                    // Column indices match VendorIngredients table:
                    // 0=checkbox, 1=Name, 2=Unit, 3=Status, 4=CreatedDate, 5=Price
                    string orderBy = dt.OrderColumnIndex switch
                    {
                        1 => "Name",
                        2 => "Unit",
                        3 => "IsActive",
                        4 => "CreatedOn",
                        5 => "Price",
                        _ => "Name"
                    };

                    int pageNumber = (dt.Start / dt.Length) + 1;

                    var parameters = new DynamicParameters();
                    parameters.Add("@VendorId", vendorId);
                    parameters.Add("@Action", "SELECT_BY_VENDORID"); // Use the correct SP action
                    parameters.Add("@PageNumber", pageNumber);
                    parameters.Add("@PageSize", dt.Length);
                    parameters.Add("@SearchValue", dt.SearchValue);
                    parameters.Add("@OrderBy", orderBy);
                    parameters.Add("@OrderDir", dt.OrderDir);
                    parameters.Add("@TotalCount", dbType: DbType.Int32, direction: ParameterDirection.Output);

                    var ingredients = await connection.QueryAsync<Ingredient>(
                        "sp_IngredientCRUD", parameters, commandType: CommandType.StoredProcedure);

                    int totalCount = parameters.Get<int>("@TotalCount");

                    var dtResponse = new DataTablesResponse<Ingredient>
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
                    var response = new Response();
                    var parameters = new DynamicParameters();
                    parameters.Add("@VendorId", vendorId);
                    parameters.Add("@Action", "SELECT_BY_VENDORID");
                    parameters.Add("@PageSize", -1); // Return all rows

                    var ingredients = await connection.QueryAsync<Ingredient>("sp_IngredientCRUD", parameters, commandType: CommandType.StoredProcedure);

                    response.Status = 200;
                    response.ResponseMsg = "Ingredients retrieved successfully.";
                    response.Data = ingredients;
                    return (200, response);
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

                    string orderBy = dt.OrderColumnIndex switch
                    {
                        1 => "Name",
                        2 => "Unit",
                        _ => "Name"
                    };

                    int pageNumber = (dt.Start / dt.Length) + 1;

                    var parameters = new DynamicParameters();
                    parameters.Add("@UserId", userId);
                    parameters.Add("@Action", "SELECT_ALL_BY_USERID");
                    parameters.Add("@PageNumber", pageNumber);
                    parameters.Add("@PageSize", dt.Length);
                    parameters.Add("@SearchValue", dt.SearchValue);
                    parameters.Add("@OrderBy", orderBy);
                    parameters.Add("@OrderDir", dt.OrderDir);
                    parameters.Add("@TotalCount", dbType: DbType.Int32, direction: ParameterDirection.Output);

                    var ingredients = await connection.QueryAsync<Ingredient>(
                        "sp_IngredientCRUD", parameters, commandType: CommandType.StoredProcedure);

                    int totalCount = parameters.Get<int>("@TotalCount");

                    var dtResponse = new DataTablesResponse<Ingredient>
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
                    var response = new Response();
                    var parameters = new DynamicParameters();
                    parameters.Add("@UserId", userId);
                    parameters.Add("@Action", "SELECT_ALL_BY_USERID");
                    parameters.Add("@PageSize", -1); // Return all rows

                    var ingredients = await connection.QueryAsync<Ingredient>("sp_IngredientCRUD", parameters, commandType: CommandType.StoredProcedure);

                    response.Status = 200;
                    response.ResponseMsg = "Ingredients retrieved successfully.";
                    response.Data = ingredients;
                    return (200, response);
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
            var response = new Response();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                // Ensure IngredientId is provided for update
                if (model.IngredientId <= 0)
                {
                    response.Status = 400;
                    response.ResponseMsg = "IngredientId is required for updating an ingredient.";
                    return (400, response);
                }

                var parameters = new DynamicParameters();
                parameters.Add("@IngredientId", model.IngredientId);
                parameters.Add("@UserId", currentUserId); // Assuming UserId can be updated or is part of the model
                parameters.Add("@Name", model.Name);
                parameters.Add("@Price", model.Price);
                parameters.Add("@Unit", model.Unit.ToLower());
                parameters.Add("@Size", model.Size);
                parameters.Add("@ModifiedBy", currentUserId); // Set ModifiedBy to the current authenticated user
                parameters.Add("@IsActive", 1); // Assuming IsActive can be updated
                parameters.Add("@Action", "UPDATE");

                int affectedRows = await connection.QueryFirstOrDefaultAsync<int>("sp_IngredientCRUD", parameters, commandType: CommandType.StoredProcedure);

                if (affectedRows <= 0)
                {
                    response.Status = 400;
                    response.ResponseMsg = "No changes were made.";
                    // Original controller returned NotFound(response) while Status stayed 400.
                    return (404, response);
                }

                response.Status = 200;
                response.ResponseMsg = "Ingredient updated successfully.";
                response.Data = new { IngredientId = model.IngredientId };
                return (200, response);
            }
            catch (SqlException sqlEx)
            {
                // SQL Server specific exceptions
                response.Status = 500;
                response.ResponseMsg = $"Expection: {sqlEx.Message}";
                return (500, response);
            }
            catch (Exception ex)
            {
                response.Status = 500;
                response.ResponseMsg = $"{ex.Message}";
                return (500, response);
            }
            finally
            {
                connection.Close();
            }
        }

        public async Task<(int StatusCode, object Body)> PatchIngredientDetailsAsync(UpdateIngredientDetailsRequest model, int currentUserId, string requestId)
        {
            var response = new Response();
            var payload = System.Text.Json.JsonSerializer.Serialize(model);

            if (model.IngredientId <= 0)
            {
                response.Status = 400;
                response.ResponseMsg = "IngredientId is required.";
                await _activityLog.LogAsync(
                    0,
                    requestId,
                    "PATCH /api/IngredientApi/PatchIngredientDetails",
                    payload,
                    System.Text.Json.JsonSerializer.Serialize(response),
                    "Warning"
                );
                return (400, response);
            }

            if (model.Name == null && model.Unit == null && model.IsActive == null)
            {
                response.Status = 400;
                response.ResponseMsg = "At least one field (Name, Unit, or IsActive) must be provided.";

                await _activityLog.LogAsync(
                    0,
                    requestId,
                    "PATCH /api/IngredientApi/PatchIngredientDetails",
                    payload,
                    System.Text.Json.JsonSerializer.Serialize(response),
                    "Warning"
                );
                return (400, response);
            }

            if (!string.IsNullOrWhiteSpace(model.Unit) &&
                !_allowedUnits.Contains(model.Unit.ToLower()))
            {
                response.Status = 400;
                response.ResponseMsg = $"Invalid unit '{model.Unit}'. Allowed units: {string.Join(", ", _allowedUnits)}";

                await _activityLog.LogAsync(
                    currentUserId,
                    requestId,
                    "PATCH /api/IngredientApi/PatchIngredientDetails",
                    payload,
                    System.Text.Json.JsonSerializer.Serialize(response),
                    "Warning"
                );

                return (400, response);
            }

            if (currentUserId == 0)
            {
                response.Status = 401;
                response.ResponseMsg = "Unauthorized";

                await _activityLog.LogAsync(
                    currentUserId,
                    requestId,
                    "PATCH /api/IngredientApi/PatchIngredientDetails",
                    payload,
                    System.Text.Json.JsonSerializer.Serialize(response),
                    "Warning"
                );
                return (401, response);
            }

            using var connection = _context.CreateConnection();
            connection.Open();

            try
            {
                // Fetch current values so we only change what was explicitly supplied
                var fetchParams = new DynamicParameters();
                fetchParams.Add("@IngredientId", model.IngredientId);
                fetchParams.Add("@Action", "SELECT");
                var existing = await connection.QueryFirstOrDefaultAsync<Ingredient>(
                    "sp_IngredientCRUD", fetchParams, commandType: CommandType.StoredProcedure
                );

                if (existing == null)
                {
                    response.Status = 400;
                    response.ResponseMsg = "Ingredient not found.";
                    await _activityLog.LogAsync(
                        currentUserId,
                        requestId,
                        "PATCH /api/IngredientApi/PatchIngredientDetails",
                        payload,
                        System.Text.Json.JsonSerializer.Serialize(response),
                        "Warning"
                    );
                    // Original controller returned NotFound(response) while Status stayed 400.
                    return (404, response);
                }

                // Merge: use incoming value if provided, otherwise keep existing
                var parameters = new DynamicParameters();
                parameters.Add("@IngredientId", model.IngredientId);
                parameters.Add("@UserId", currentUserId);
                parameters.Add("@Name", model.Name ?? existing.Name);
                parameters.Add("@Unit", model.Unit ?? existing.Unit);
                parameters.Add("@IsActive", model.IsActive ?? existing.IsActive);
                parameters.Add("@Price", existing.Price);
                parameters.Add("@Size", model.Size);
                parameters.Add("@ModifiedBy", currentUserId);
                parameters.Add("@Action", "UPDATE");

                await connection.ExecuteAsync(
                    "sp_IngredientCRUD", parameters, commandType: CommandType.StoredProcedure
                );

                response.Status = 200;
                response.ResponseMsg = "Ingredient details updated successfully.";
                response.Data = new { IngredientId = model.IngredientId };
                await _activityLog.LogAsync(
                    currentUserId,
                    requestId,
                    "PATCH /api/IngredientApi/PatchIngredientDetails",
                    payload,
                    System.Text.Json.JsonSerializer.Serialize(new
                    {
                        response.Status,
                        response.ResponseMsg,
                        response.Data
                    }),
                    "Success"
                );
                return (200, response);
            }
            catch (SqlException sqlEx)
            {
                response.Status = 400;
                response.ResponseMsg = $"{sqlEx.Message}";
                await _activityLog.LogAsync(
                    currentUserId,
                    requestId,
                    "PATCH /api/IngredientApi/PatchIngredientDetails",
                    payload,
                    System.Text.Json.JsonSerializer.Serialize(response),
                    "Error"
                );
                return (400, response);
            }
            catch (Exception ex)
            {
                response.Status = 400;
                response.ResponseMsg = $"{ex.Message}";
                await _activityLog.LogAsync(
                    currentUserId,
                    requestId,
                    "PATCH /api/IngredientApi/PatchIngredientDetails",
                    payload,
                    System.Text.Json.JsonSerializer.Serialize(response),
                    "Error"
                );
                return (400, response);
            }
            finally
            {
                connection.Close();
            }
        }

        public async Task<(int StatusCode, object Body)> DeleteIngredientByIdAsync(int id, int currentUserId)
        {
            var response = new Response();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@IngredientId", id);
                parameters.Add("@Userid", currentUserId);
                parameters.Add("@Action", "DELETE");

                var affectedRows = await connection.ExecuteAsync("sp_IngredientCRUD", parameters, commandType: CommandType.StoredProcedure);
                if (affectedRows == 0)
                {
                    response.Status = 400;
                    response.ResponseMsg = "Ingredient not found.";
                    // Original controller returned NotFound(response) while Status stayed 400.
                    return (404, response);
                }

                response.Status = 200;
                response.ResponseMsg = "Ingredient deleted successfully.";
                response.Data = new { IngredientId = id };
                return (200, response);
            }
            catch (SqlException sqlEx)
            {
                // SQL Server specific exceptions
                response.Status = 500;
                response.ResponseMsg = $"Expection: {sqlEx.Message}";
                return (500, response);
            }
            catch (InvalidOperationException invalidOpEx)
            {
                response.Status = 422;
                response.ResponseMsg = $"Invalid operation: {invalidOpEx.Message}";
                return (422, response);
            }
            catch (Exception ex)
            {
                response.Status = 500;
                response.ResponseMsg = $"{ex.Message}";
                return (500, response);
            }
            finally
            {
                connection.Close();
            }
        }

        public async Task<(int StatusCode, object Body)> ToggleIngredientActiveAsync(int id, int currentUserId)
        {
            var response = new Response();
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
                    response.Status = 400;
                    response.ResponseMsg = "Ingredient not found.";
                    // Original controller returned NotFound(response) while Status stayed 400.
                    return (404, response);
                }

                parameters = new DynamicParameters();
                parameters.Add("@IngredientId", id);
                parameters.Add("@Userid", currentUserId);
                parameters.Add("@IsActive", !ingredient.IsActive);
                parameters.Add("@ModifiedBy", currentUserId);
                parameters.Add("@Action", "TOGGLE_ACTIVE");

                await connection.ExecuteAsync("sp_IngredientCRUD", parameters, commandType: CommandType.StoredProcedure);

                response.Status = 200;
                response.ResponseMsg = $"Ingredient {(ingredient.IsActive ? "deactivated" : "activated")} successfully.";
                response.Data = new { IngredientId = id, IsActive = !ingredient.IsActive };
                return (200, response);
            }
            catch (SqlException sqlEx)
            {
                // SQL Server specific exceptions
                response.Status = 500;
                response.ResponseMsg = $"Expection: {sqlEx.Message}";
                return (500, response);
            }
            catch (InvalidOperationException invalidOpEx)
            {
                response.Status = 422;
                response.ResponseMsg = $"Invalid operation: {invalidOpEx.Message}";
                return (422, response);
            }
            catch (Exception ex)
            {
                response.Status = 500;
                response.ResponseMsg = $"{ex.Message}";
                return (500, response);
            }
            finally
            {
                connection.Close();
            }
        }

        public async Task<(int StatusCode, object Body)> GetAllVendorsAsync(int currentUserId)
        {
            var response = new Response();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@UserId", currentUserId);

                var vendors = await connection.QueryAsync<User>(@"
                    SELECT
                        u.UserId,
                        u.FirstName,
                        u.LastName,
                        u.Username,
                        u.Email,
                        u.Phone,
                        u.ProfilePic,
                        u.Address,
                        u.Country,
                        u.State,
                        u.City,
                        u.Zip,
                        u.Gender,
                        u.RestaurantName,
                        u.CuisineType,
                        u.RestaurantSize,
                        u.IsActive,
                        STRING_AGG(r.RoleName, ',') AS Roles,
                        u.CreatedOn,
                        u.ModifiedOn,
                        CASE
                            WHEN l.LinkedVendorId IS NOT NULL THEN 1
                            ELSE 0
                        END AS IsLinked
                    FROM Users u
                    INNER JOIN UserRoles ur ON u.UserId = ur.UserId
                    INNER JOIN Roles r ON ur.RoleId = r.RoleId
                    LEFT JOIN UserVendorIngredientsLinks l
                        ON l.LinkedVendorId = u.UserId AND l.UserId = @UserId
                    WHERE r.RoleName = 'Vendor'
                    GROUP BY
                        u.UserId, u.FirstName, u.LastName, u.Username, u.Email, u.Phone, u.ProfilePic,
                        u.Address, u.Country, u.State, u.City, u.Zip, u.Gender, u.RestaurantName,
                        u.CuisineType, u.RestaurantSize, u.IsActive, u.CreatedOn, u.ModifiedOn,
                        l.LinkedVendorId",
                    parameters);

                response.Status = 200;
                response.ResponseMsg = "Vendors retrieved successfully.";
                response.Data = vendors;
                return (200, response);
            }
            catch (Exception ex)
            {
                response.Status = 500;
                response.ResponseMsg = $"{ex.Message}";
                return (500, response);
            }
            finally
            {
                connection.Close();
            }
        }

        public async Task<(int StatusCode, object Body)> LinkVendorAsync(LinkVendorRequest model, int currentUserId)
        {
            var response = new Response();

            try
            {
                using var connection = _context.CreateConnection();

                // Check if already linked
                var existingLink = await connection.QueryFirstOrDefaultAsync<int>(
                    "SELECT 1 FROM UserVendorIngredientsLinks WHERE UserId = @UserId AND LinkedVendorId = @VendorId",
                    new { UserId = currentUserId, VendorId = model.VendorId });

                if (existingLink == 1)
                {
                    response.Status = 400;
                    response.ResponseMsg = "Vendor is already linked.";
                    return (400, response);
                }

                // Insert link
                await connection.ExecuteAsync(
                    @"INSERT INTO UserVendorIngredientsLinks (UserId, LinkedVendorId, CreatedBy, CreatedOn)
              VALUES (@UserId, @VendorId, @CreatedBy, GETDATE())",
                    new { UserId = currentUserId, VendorId = model.VendorId, CreatedBy = currentUserId });

                response.Status = 200;
                response.ResponseMsg = "Vendor linked successfully.";
                return (200, response);
            }
            catch (Exception ex)
            {
                response.Status = 500;
                response.ResponseMsg = $"{ex.Message}";
                return (500, response);
            }
        }

        public async Task<(int StatusCode, object Body)> UnlinkVendorAsync(int vendorId, int currentUserId)
        {
            var response = new Response();

            try
            {
                using var connection = _context.CreateConnection();

                var affectedRows = await connection.ExecuteAsync(
                    "DELETE FROM UserVendorIngredientsLinks WHERE UserId = @UserId AND LinkedVendorId = @VendorId",
                    new { UserId = currentUserId, VendorId = vendorId });

                if (affectedRows == 0)
                {
                    response.Status = 404;
                    response.ResponseMsg = "Vendor link not found.";
                    return (404, response);
                }

                response.Status = 200;
                response.ResponseMsg = "Vendor unlinked successfully.";
                return (200, response);
            }
            catch (Exception ex)
            {
                response.Status = 500;
                response.ResponseMsg = $"{ex.Message}";
                return (500, response);
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
        /// empty table body (HTTP 200), plain requests get a 500 Response envelope.
        /// </summary>
        private static (int StatusCode, object Body) HandleListError(IQueryCollection query, Exception ex)
        {
            if (query.ContainsKey("draw"))
            {
                int draw = int.Parse(query["draw"].FirstOrDefault() ?? "1");
                return (200, new DataTablesResponse<Ingredient> { Draw = draw, Data = Enumerable.Empty<Ingredient>() });
            }
            return (500, new Response { Status = 500, ResponseMsg = ex.Message });
        }
    }
}
