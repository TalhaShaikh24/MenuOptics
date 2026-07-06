using Dapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using MenuOptics.Data;
using MenuOptics.Middleware;
using MenuOptics.Models;
using MenuOptics.Services;
using Stripe.Forwarding;
using System.Data;
using System.Data.SqlClient;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace MenuOptics.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class IngredientApiController : ControllerBase
    {
        private readonly DapperContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;
        private readonly IActivityLogService _activityLog;

        private static readonly HashSet<string> _allowedUnits = new(StringComparer.OrdinalIgnoreCase)
        {
            "BA", "BG", "BX", "CA", "CM", "CT", "DZ", "EA",
            "FL", "FT", "GA", "GM", "HR", "IN", "KG", "LB",
            "LT", "ML", "MR", "OZ", "PA", "PC", "PR", "PT",
            "RL", "TB"
        };


        public IngredientApiController(DapperContext context, IWebHostEnvironment environment, IConfiguration configuration, IActivityLogService activityLog)
        {
            _context = context;
            _environment = environment;
            _configuration = configuration;
            _activityLog = activityLog;
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [Authorize(Roles = "Vendor")]
        [HttpPost("CreateIngredientByVendor")]
        public async Task<IActionResult> CreateIngredientByVendor([FromBody] VendorIngredientRequest model)
        {
            var response = new Response();
            var requestId = HttpContext.Items[RequestIdMiddleware.RequestIdItemKey]?.ToString() ?? Guid.NewGuid().ToString();
            var payload = System.Text.Json.JsonSerializer.Serialize(model);
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            if (!ModelState.IsValid)
            {
                response.Status = 400;
                response.ResponseMsg = "Invalid input.";
                await _activityLog.LogAsync(currentUserId, requestId, "POST /api/IngredientApi/CreateIngredientByVendor", payload,
                    System.Text.Json.JsonSerializer.Serialize(response), "Warning");
                return BadRequest(response);
            }

            if (!_allowedUnits.Contains(model.Unit?.ToLower()))
            {
                response.Status = 400;
                response.ResponseMsg = $"Invalid unit '{model.Unit}'. Allowed units: {string.Join(", ", _allowedUnits)}";
                await _activityLog.LogAsync(currentUserId, requestId, "POST /api/IngredientApi/CreateIngredientByVendor", payload,
                    System.Text.Json.JsonSerializer.Serialize(response), "Warning");
                return BadRequest(response);
            }

            try
            {
                using var connection = _context.CreateConnection();
                connection.Open();


                decimal basePrice = 0;
                decimal restaurantPrice = 0;

                //When Packsize given
                if (model.PackSize != null && model.PackSize > 0)
                {
                    basePrice = model.BasePrice / model.PackSize.Value;
                }
                //When Weight given
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
                    return BadRequest(response);
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

                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Status = 400;
                response.ResponseMsg = $"{ex.Message}";
                await _activityLog.LogAsync(currentUserId, requestId, "POST /api/IngredientApi/CreateIngredientByVendor", payload,
                    System.Text.Json.JsonSerializer.Serialize(response), "Error");
                return StatusCode(400, response);
            }
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [Authorize(Roles = "Vendor")]
        [HttpPut("UpdateIngredientPrices")]
        public async Task<IActionResult> UpdateIngredientPrices([FromBody] UpdateIngredientPriceRequest model)
        {
            var response = new Response();
            var requestId = HttpContext.Items[RequestIdMiddleware.RequestIdItemKey]?.ToString() ?? Guid.NewGuid().ToString();
            var payload = System.Text.Json.JsonSerializer.Serialize(model);
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
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
                    return BadRequest(response);
                }

               

                var parameters = new DynamicParameters();
                parameters.Add("@IngredientId", model.IngredientId);
                parameters.Add("@Price", basePrice);
                parameters.Add("@VendorId", basePrice);
                parameters.Add("@Size", model.Size);
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
                        return BadRequest(response);
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
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Status = 400;
                response.ResponseMsg = ex.Message;
                await _activityLog.LogAsync(currentUserId, requestId, "PUT /api/IngredientApi/UpdateIngredientPrices", payload,
                    System.Text.Json.JsonSerializer.Serialize(response), "Error");
                return StatusCode(400, response);
            }
        }


        [HttpPost("CreateIngredient")]
        //// [Authorize(Roles = "Vendor")]
        [HttpPost]
        public async Task<IActionResult> CreateIngredient([FromBody] Ingredient model)
        {
            var response = new Response();

            // Validate model
            if (!ModelState.IsValid)
            {
                response.Status = 400;
                response.ResponseMsg = "Invalid input.";
                response.Data = ModelState;
                return BadRequest(response);
            }

            if (!_allowedUnits.Contains(model.Unit?.ToLower()))
            {
                response.Status = 400;
                response.ResponseMsg = $"Invalid unit '{model.Unit}'. Allowed units: {string.Join(", ", _allowedUnits)}";
                return BadRequest(response);
            }

            // ✅ Validate stock
            if (string.IsNullOrEmpty(model.Stock))
            {
                response.Status = 400;
                response.ResponseMsg = "Initial Stock cannot be 0, Please enter the stock";
                return BadRequest(response);
            }

            try
            {
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                if (currentUserId == 0)
                {
                    response.Status = 401;
                    response.ResponseMsg = "Unauthorized";
                    return Unauthorized(response);
                }

                using var connection = _context.CreateConnection();
                connection.Open();

                var parameters = new DynamicParameters();
                parameters.Add("@UserId", currentUserId);
                parameters.Add("@Name", model.Name);
                parameters.Add("@Price", model.Price);
                parameters.Add("@Unit", model.Unit.ToLower());
                parameters.Add("@UnitOfMeasure", model.UnitOfMeasure.ToLower());
                parameters.Add("@CreatedBy", currentUserId);
                parameters.Add("@Action", "INSERT");

                int ingredientId = await connection.ExecuteScalarAsync<int>(
                    "sp_IngredientCRUD", parameters, commandType: CommandType.StoredProcedure
                );

                var parameters2 = new DynamicParameters();
                if (ingredientId > 0)
                {

                    parameters2.Add("@RestaurantId", currentUserId);
                    parameters2.Add("@VendorId", null);
                    parameters2.Add("@IngredientId", ingredientId);
                    parameters2.Add("@Stock", model.Stock);
                    parameters2.Add("@CreatedBy", currentUserId);
                    parameters2.Add("@Action", "INSERT");
                }

                int inventoryId = await connection.ExecuteScalarAsync<int>(
                    "sp_InventoryCRUD", parameters2, commandType: CommandType.StoredProcedure
                );

                response.Status = 200;
                response.ResponseMsg = "Ingredient created successfully.";
                response.Data = new { IngredientId = ingredientId, InventoryId = inventoryId };

                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Status = 500;
                response.ResponseMsg = $"{ex.Message}";
                return StatusCode(500, response);
            }
        }

        [HttpPost("AddIngredientStock")]
        //// [Authorize(Roles = "Vendor")]
        public async Task<IActionResult> AddIngredientStock([FromBody] Inventory model)
        {
            var response = new Response();

            // Validate model
            if (!ModelState.IsValid)
            {
                response.Status = 400;
                response.ResponseMsg = "Invalid input.";
                response.Data = ModelState;
                return BadRequest(response);
            }

            // ✅ Validate stock
            if (string.IsNullOrEmpty(model.Stock.ToString()))
            {
                response.Status = 400;
                response.ResponseMsg = "Initial Stock cannot be 0, Please enter the stock";
                return BadRequest(response);
            }

            try
            {
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                if (currentUserId == 0)
                {
                    response.Status = 401;
                    response.ResponseMsg = "Unauthorized";
                    return Unauthorized(response);
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

                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Status = 500;
                response.ResponseMsg = $"{ex.Message}";
                return StatusCode(500, response);
            }
        }


        [HttpGet("GetIngredient/{id}")]
        // [Authorize(Roles = "Vendor")]
        public async Task<IActionResult> GetIngredient(int id)
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
                    return NotFound(response);
                }

                response.Status = 200;
                response.ResponseMsg = "Ingredient retrieved successfully.";
                response.Data = ingredient;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Status = 500;
                response.ResponseMsg = $"{ex.Message}";
                return StatusCode(500, response);
            }
            finally
            {
                connection.Close();
            }
        }

        [HttpGet("GetAllIngredients")]
        public async Task<IActionResult> GetAllIngredients()
        {
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                if (Request.Query.ContainsKey("draw"))
                {
                    int draw = int.Parse(Request.Query["draw"]);
                    int start = int.Parse(Request.Query["start"]);
                    int length = int.Parse(Request.Query["length"]);
                    string searchValue = Request.Query["searchValue"].ToString() ?? "";
                    
                    int orderColumnIndex = 1;
                    if (Request.Query.ContainsKey("orderColumn"))
                    {
                        orderColumnIndex = int.Parse(Request.Query["orderColumn"]);
                    }
                    string orderDir = "asc";
                    if (Request.Query.ContainsKey("orderDir"))
                    {
                        orderDir = Request.Query["orderDir"].ToString();
                    }

                    string orderBy = orderColumnIndex switch
                    {
                        1 => "Name",
                        2 => "Unit",
                        3 => "UnitOfMeasure",
                        4 => "IsActive",
                        6 => "CreatedOn",
                        7 => "Price",
                        _ => "Name"
                    };

                    int pageNumber = (start / length) + 1;

                    var parameters = new DynamicParameters();
                    parameters.Add("@Action", "SELECT_ALL");
                    parameters.Add("@PageNumber", pageNumber);
                    parameters.Add("@PageSize", length);
                    parameters.Add("@SearchValue", searchValue);
                    parameters.Add("@OrderBy", orderBy);
                    parameters.Add("@OrderDir", orderDir);
                    parameters.Add("@TotalCount", dbType: DbType.Int32, direction: ParameterDirection.Output);

                    var ingredients = await connection.QueryAsync<Ingredient>(
                        "sp_IngredientCRUD", parameters, commandType: CommandType.StoredProcedure);

                    int totalCount = parameters.Get<int>("@TotalCount");

                    var dtResponse = new DataTablesResponse<Ingredient>
                    {
                        Draw = draw,
                        RecordsTotal = totalCount,
                        RecordsFiltered = totalCount,
                        Data = ingredients
                    };

                    return Ok(dtResponse);
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
                    return Ok(response);
                }
            }
            catch (Exception ex)
            {
                if (Request.Query.ContainsKey("draw"))
                {
                    int draw = int.Parse(Request.Query["draw"].FirstOrDefault() ?? "1");
                    return Ok(new DataTablesResponse<Ingredient> { Draw = draw, Data = Enumerable.Empty<Ingredient>() });
                }
                var response = new Response { Status = 500, ResponseMsg = ex.Message };
                return StatusCode(500, response);
            }
            finally
            {
                connection.Close();
            }
        }

        [HttpGet("GetAllIngredientsByUserId")]
        public async Task<IActionResult> GetAllIngredientsByUserId()
        {
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "1");
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                if (Request.Query.ContainsKey("draw"))
                {
                    int draw = int.Parse(Request.Query["draw"]);
                    int start = int.Parse(Request.Query["start"]);
                    int length = int.Parse(Request.Query["length"]);
                    string searchValue = Request.Query["searchValue"].ToString() ?? "";
                    
                    int orderColumnIndex = 1;
                    if (Request.Query.ContainsKey("orderColumn"))
                    {
                        orderColumnIndex = int.Parse(Request.Query["orderColumn"]);
                    }
                    string orderDir = "asc";
                    if (Request.Query.ContainsKey("orderDir"))
                    {
                        orderDir = Request.Query["orderDir"].ToString();
                    }

                    string orderBy = orderColumnIndex switch
                    {
                        1 => "Name",
                        2 => "Unit",
                        3 => "UnitOfMeasure",
                        4 => "IsActive",
                        6 => "CreatedOn",
                        7 => "Price",
                        _ => "Name"
                    };

                    int pageNumber = (start / length) + 1;

                    var parameters = new DynamicParameters();
                    parameters.Add("@UserId", currentUserId);
                    parameters.Add("@Action", "SELECT_ALL_BY_USERID");
                    parameters.Add("@PageNumber", pageNumber);
                    parameters.Add("@PageSize", length);
                    parameters.Add("@SearchValue", searchValue);
                    parameters.Add("@OrderBy", orderBy);
                    parameters.Add("@OrderDir", orderDir);
                    parameters.Add("@TotalCount", dbType: DbType.Int32, direction: ParameterDirection.Output);

                    var ingredients = await connection.QueryAsync<Ingredient>(
                        "sp_IngredientCRUD", parameters, commandType: CommandType.StoredProcedure);

                    int totalCount = parameters.Get<int>("@TotalCount");

                    var dtResponse = new DataTablesResponse<Ingredient>
                    {
                        Draw = draw,
                        RecordsTotal = totalCount,
                        RecordsFiltered = totalCount,
                        Data = ingredients
                    };

                    return Ok(dtResponse);
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
                    return Ok(response);
                }
            }
            catch (Exception ex)
            {
                if (Request.Query.ContainsKey("draw"))
                {
                    int draw = int.Parse(Request.Query["draw"].FirstOrDefault() ?? "1");
                    return Ok(new DataTablesResponse<Ingredient> { Draw = draw, Data = Enumerable.Empty<Ingredient>() });
                }
                var response = new Response { Status = 500, ResponseMsg = ex.Message };
                return StatusCode(500, response);
            }
            finally
            {
                connection.Close();
            }
        }
        [HttpGet("GetAllActiveInactiveIngredients")]
        public async Task<IActionResult> GetAllActiveInactiveIngredients()
        {
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "1");
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                if (Request.Query.ContainsKey("draw"))
                {
                    int draw = int.Parse(Request.Query["draw"]);
                    int start = int.Parse(Request.Query["start"]);
                    int length = int.Parse(Request.Query["length"]);
                    string searchValue = Request.Query["searchValue"].ToString() ?? "";
                    
                    int orderColumnIndex = 1;
                    if (Request.Query.ContainsKey("orderColumn"))
                    {
                        orderColumnIndex = int.Parse(Request.Query["orderColumn"]);
                    }
                    string orderDir = "asc";
                    if (Request.Query.ContainsKey("orderDir"))
                    {
                        orderDir = Request.Query["orderDir"].ToString();
                    }

                    // Map column index to column name:
                    // 0=checkbox, 1=Name, 2=Unit, 3=UnitOfMeasure, 4=Status, 5=Supplier, 6=CreatedDate, 7=Price
                    string orderBy = orderColumnIndex switch
                    {
                        1 => "Name",
                        2 => "VendorProductId",
                        3 => "Unit",
                        4 => "UnitOfMeasure",
                        6 => "IsActive",
                        7 => "FullName",
                        8 => "CreatedOn",
                        9 => "Price",
                        _ => "Name"
                    };

                    int pageNumber = (start / length) + 1;

                    var parameters = new DynamicParameters();
                    parameters.Add("@UserId", currentUserId);
                    parameters.Add("@Action", "SELECT_ALL_INGREDIENTS_BY_USERID");
                    parameters.Add("@PageNumber", pageNumber);
                    parameters.Add("@PageSize", length);
                    parameters.Add("@SearchValue", searchValue);
                    parameters.Add("@OrderBy", orderBy);
                    parameters.Add("@OrderDir", orderDir);
                    parameters.Add("@TotalCount", dbType: DbType.Int32, direction: ParameterDirection.Output);

                    var ingredients = await connection.QueryAsync<Ingredient>(
                        "sp_IngredientCRUD", parameters, commandType: CommandType.StoredProcedure);

                    int totalCount = parameters.Get<int>("@TotalCount");

                    var dtResponse = new DataTablesResponse<Ingredient>
                    {
                        Draw = draw,
                        RecordsTotal = totalCount,
                        RecordsFiltered = totalCount,
                        Data = ingredients
                    };

                    return Ok(dtResponse);
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
                    return Ok(response);
                }
            }
            catch (Exception ex)
            {
                if (Request.Query.ContainsKey("draw"))
                {
                    int draw = int.Parse(Request.Query["draw"].FirstOrDefault() ?? "1");
                    return Ok(new DataTablesResponse<Ingredient> { Draw = draw, Data = Enumerable.Empty<Ingredient>() });
                }
                var response = new Response { Status = 500, ResponseMsg = ex.Message };
                return StatusCode(500, response);
            }
            finally
            {
                connection.Close();
            }
        }

        [HttpGet("GetAllIngredientsByVendorId")]
        // [Authorize(Roles = "Vendor")]
        public async Task<IActionResult> GetAllIngredientsByVendorId()
        {
            var vendorId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "1");
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                if (Request.Query.ContainsKey("draw"))
                {
                    int draw = int.Parse(Request.Query["draw"]);
                    int start = int.Parse(Request.Query["start"]);
                    int length = int.Parse(Request.Query["length"]);
                    string searchValue = Request.Query["searchValue"].ToString() ?? "";
                    
                    int orderColumnIndex = 1;
                    if (Request.Query.ContainsKey("orderColumn"))
                    {
                        orderColumnIndex = int.Parse(Request.Query["orderColumn"]);
                    }
                    string orderDir = "asc";
                    if (Request.Query.ContainsKey("orderDir"))
                    {
                        orderDir = Request.Query["orderDir"].ToString();
                    }

                    // Column indices match VendorIngredients table:
                    // 0=checkbox, 1=Name, 2=Unit, 3=Status, 4=CreatedDate, 5=Price
                    string orderBy = orderColumnIndex switch
                    {
                        1 => "Name",
                        2 => "Unit",
                        3 => "IsActive",
                        4 => "CreatedOn",
                        5 => "Price",
                        _ => "Name"
                    };

                    int pageNumber = (start / length) + 1;

                    var parameters = new DynamicParameters();
                    parameters.Add("@VendorId", vendorId);
                    parameters.Add("@Action", "SELECT_BY_VENDORID"); // Use the correct SP action
                    parameters.Add("@PageNumber", pageNumber);
                    parameters.Add("@PageSize", length);
                    parameters.Add("@SearchValue", searchValue);
                    parameters.Add("@OrderBy", orderBy);
                    parameters.Add("@OrderDir", orderDir);
                    parameters.Add("@TotalCount", dbType: DbType.Int32, direction: ParameterDirection.Output);

                    var ingredients = await connection.QueryAsync<Ingredient>(
                        "sp_IngredientCRUD", parameters, commandType: CommandType.StoredProcedure);

                    int totalCount = parameters.Get<int>("@TotalCount");

                    var dtResponse = new DataTablesResponse<Ingredient>
                    {
                        Draw = draw,
                        RecordsTotal = totalCount,
                        RecordsFiltered = totalCount,
                        Data = ingredients
                    };

                    return Ok(dtResponse);
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
                    return Ok(response);
                }
            }
            catch (Exception ex)
            {
                if (Request.Query.ContainsKey("draw"))
                {
                    int draw = int.Parse(Request.Query["draw"].FirstOrDefault() ?? "1");
                    return Ok(new DataTablesResponse<Ingredient> { Draw = draw, Data = Enumerable.Empty<Ingredient>() });
                }
                var response = new Response { Status = 500, ResponseMsg = ex.Message };
                return StatusCode(500, response);
            }
            finally
            {
                connection.Close();
            }
        }

        [HttpGet("GetAllIngredientsByUserId/{userId}")]
        // [Authorize(Roles = "Vendor")]
        public async Task<IActionResult> GetAllIngredientsByUserId(int userId)
        {
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                if (Request.Query.ContainsKey("draw"))
                {
                    int draw = int.Parse(Request.Query["draw"]);
                    int start = int.Parse(Request.Query["start"]);
                    int length = int.Parse(Request.Query["length"]);
                    string searchValue = Request.Query["searchValue"].ToString() ?? "";
                    
                    int orderColumnIndex = 1;
                    if (Request.Query.ContainsKey("orderColumn"))
                    {
                        orderColumnIndex = int.Parse(Request.Query["orderColumn"]);
                    }
                    string orderDir = "asc";
                    if (Request.Query.ContainsKey("orderDir"))
                    {
                        orderDir = Request.Query["orderDir"].ToString();
                    }

                    string orderBy = orderColumnIndex switch
                    {
                        1 => "Name",
                        2 => "Unit",
                        _ => "Name"
                    };

                    int pageNumber = (start / length) + 1;

                    var parameters = new DynamicParameters();
                    parameters.Add("@UserId", userId);
                    parameters.Add("@Action", "SELECT_ALL_BY_USERID");
                    parameters.Add("@PageNumber", pageNumber);
                    parameters.Add("@PageSize", length);
                    parameters.Add("@SearchValue", searchValue);
                    parameters.Add("@OrderBy", orderBy);
                    parameters.Add("@OrderDir", orderDir);
                    parameters.Add("@TotalCount", dbType: DbType.Int32, direction: ParameterDirection.Output);

                    var ingredients = await connection.QueryAsync<Ingredient>(
                        "sp_IngredientCRUD", parameters, commandType: CommandType.StoredProcedure);

                    int totalCount = parameters.Get<int>("@TotalCount");

                    var dtResponse = new DataTablesResponse<Ingredient>
                    {
                        Draw = draw,
                        RecordsTotal = totalCount,
                        RecordsFiltered = totalCount,
                        Data = ingredients
                    };

                    return Ok(dtResponse);
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
                    return Ok(response);
                }
            }
            catch (Exception ex)
            {
                if (Request.Query.ContainsKey("draw"))
                {
                    int draw = int.Parse(Request.Query["draw"].FirstOrDefault() ?? "1");
                    return Ok(new DataTablesResponse<Ingredient> { Draw = draw, Data = Enumerable.Empty<Ingredient>() });
                }
                var response = new Response { Status = 500, ResponseMsg = ex.Message };
                return StatusCode(500, response);
            }
            finally
            {
                connection.Close();
            }
        }

        [HttpPut("UpdateIngredient")]
        // [Authorize(Roles = "Vendor")] // Uncomment if authorization is needed
        public async Task<IActionResult> UpdateIngredient([FromBody] Ingredient model)
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
                    return BadRequest(response);
                }



                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                var parameters = new DynamicParameters();
                parameters.Add("@IngredientId", model.IngredientId);
                parameters.Add("@UserId", currentUserId); // Assuming UserId can be updated or is part of the model
                parameters.Add("@Name", model.Name);
                parameters.Add("@Price", model.Price);
                parameters.Add("@Unit", model.Unit.ToLower());
                parameters.Add("@UnitOfMeasure", model.UnitOfMeasure ?? null);
                parameters.Add("@ModifiedBy", currentUserId); // Set ModifiedBy to the current authenticated user
                parameters.Add("@IsActive", 1); // Assuming IsActive can be updated
                parameters.Add("@Action", "UPDATE");

                int affectedRows = await connection.ExecuteAsync("sp_IngredientCRUD", parameters, commandType: CommandType.StoredProcedure);

                if (affectedRows == 0)
                {
                    response.Status = 400;
                    response.ResponseMsg = "Ingredient not found or no changes were made.";
                    return NotFound(response);
                }

                response.Status = 200;
                response.ResponseMsg = "Ingredient updated successfully.";
                response.Data = new { IngredientId = model.IngredientId };
                return Ok(response);
            }
            catch (SqlException sqlEx)
            {
                // SQL Server specific exceptions
                response.Status = 500;
                response.ResponseMsg = $"Expection: {sqlEx.Message}";
                return StatusCode(500, response);
            }
            catch (Exception ex)
            {
                response.Status = 500;
                response.ResponseMsg = $"{ex.Message}";
                return StatusCode(500, response);
            }
            finally
            {
                connection.Close();
            }
        }


        // PATCH api/Ingredient/PatchIngredientDetails
        // Updates one or more of: Name, Unit, IsActive. Only provided (non-null) fields are applied.
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [Authorize(Roles = "Vendor")]
        [HttpPatch("PatchIngredientDetails")]
        public async Task<IActionResult> PatchIngredientDetails([FromBody] UpdateIngredientDetailsRequest model)
        {
            var response = new Response();
            var requestId = HttpContext.Items[RequestIdMiddleware.RequestIdItemKey]?.ToString() ?? Guid.NewGuid().ToString();
            var payload = System.Text.Json.JsonSerializer.Serialize(model);
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

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
                return BadRequest(response);
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
                return BadRequest(response);
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

                return BadRequest(response);
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
                return Unauthorized(response);
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
                    return NotFound(response);
                }

                // Merge: use incoming value if provided, otherwise keep existing
                var parameters = new DynamicParameters();
                parameters.Add("@IngredientId", model.IngredientId);
                parameters.Add("@UserId", currentUserId);
                parameters.Add("@Name", model.Name ?? existing.Name);
                parameters.Add("@Unit", model.Unit ?? existing.Unit);
                parameters.Add("@IsActive", model.IsActive ?? existing.IsActive);
                parameters.Add("@Price", existing.Price);
                parameters.Add("@UnitOfMeasure", existing.UnitOfMeasure);
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
                return Ok(response);
            }
            catch (System.Data.SqlClient.SqlException sqlEx)
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
                return StatusCode(400, response);
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
                return StatusCode(400, response);
            }
            finally
            {
                connection.Close();
            }
        }

        [HttpDelete("DeleteIngredientById/{id}")]
        // [Authorize(Roles = "Vendor")]
        public async Task<IActionResult> DeleteIngredientById(int id)
        {

            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
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
                    return NotFound(response);
                }

                response.Status = 200;
                response.ResponseMsg = "Ingredient deleted successfully.";
                response.Data = new { IngredientId = id };
                return Ok(response);
            }
            catch (SqlException sqlEx)
            {
                // SQL Server specific exceptions
                response.Status = 500;
                response.ResponseMsg = $"Expection: {sqlEx.Message}";
                return StatusCode(500, response);
            }
            catch (InvalidOperationException invalidOpEx)
            {
                response.Status = 422;
                response.ResponseMsg = $"Invalid operation: {invalidOpEx.Message}";
                return StatusCode(422, response);
            }
            catch (Exception ex)
            {
                response.Status = 500;
                response.ResponseMsg = $"{ex.Message}";
                return StatusCode(500, response);
            }

            finally
            {
                connection.Close();
            }
        }

        [HttpPut("ToggleIngredientActive/{id}")]
        public async Task<IActionResult> ToggleIngredientActive(int id)
        {
            var response = new Response();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                var parameters = new DynamicParameters();
                parameters.Add("@IngredientId", id);
                parameters.Add("@Action", "SELECT");

                var ingredient = await connection.QueryFirstOrDefaultAsync<Ingredient>("sp_IngredientCRUD", parameters, commandType: CommandType.StoredProcedure);

                if (ingredient == null)
                {
                    response.Status = 400;
                    response.ResponseMsg = "Ingredient not found.";
                    return NotFound(response);
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
                return Ok(response);
            }
            catch (SqlException sqlEx)
            {
                // SQL Server specific exceptions
                response.Status = 500;
                response.ResponseMsg = $"Expection: {sqlEx.Message}";
                return StatusCode(500, response);
            }
            catch (InvalidOperationException invalidOpEx)
            {
                response.Status = 422;
                response.ResponseMsg = $"Invalid operation: {invalidOpEx.Message}";
                return StatusCode(422, response);
            }
            catch (Exception ex)
            {
                response.Status = 500;
                response.ResponseMsg = $"{ex.Message}";
                return StatusCode(500, response);
            }
            finally
            {
                connection.Close();
            }
        }


        [HttpGet("GetAllVendors")]
        public async Task<IActionResult> GetAllVendors()
        {
            var response = new Response();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "1");
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
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Status = 500;
                response.ResponseMsg = $"{ex.Message}";
                return StatusCode(500, response);
            }
            finally
            {
                connection.Close();
            }
        }

        [HttpPost("LinkVendor")]
        public async Task<IActionResult> LinkVendor([FromBody] LinkVendorRequest model)
        {
            var response = new Response();

            try
            {
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                using var connection = _context.CreateConnection();

                // Check if already linked
                var existingLink = await connection.QueryFirstOrDefaultAsync<int>(
                    "SELECT 1 FROM UserVendorIngredientsLinks WHERE UserId = @UserId AND LinkedVendorId = @VendorId",
                    new { UserId = currentUserId, VendorId = model.VendorId });

                if (existingLink == 1)
                {
                    response.Status = 400;
                    response.ResponseMsg = "Vendor is already linked.";
                    return BadRequest(response);
                }

                // Insert link
                await connection.ExecuteAsync(
                    @"INSERT INTO UserVendorIngredientsLinks (UserId, LinkedVendorId, CreatedBy, CreatedOn) 
              VALUES (@UserId, @VendorId, @CreatedBy, GETDATE())",
                    new { UserId = currentUserId, VendorId = model.VendorId, CreatedBy = currentUserId });

                response.Status = 200;
                response.ResponseMsg = "Vendor linked successfully.";
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Status = 500;
                response.ResponseMsg = $"{ex.Message}";
                return StatusCode(500, response);
            }
        }

        [HttpDelete("UnlinkVendor/{vendorId}")]
        public async Task<IActionResult> UnlinkVendor(int vendorId)
        {
            var response = new Response();

            try
            {
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                using var connection = _context.CreateConnection();

                var affectedRows = await connection.ExecuteAsync(
                    "DELETE FROM UserVendorIngredientsLinks WHERE UserId = @UserId AND LinkedVendorId = @VendorId",
                    new { UserId = currentUserId, VendorId = vendorId });

                if (affectedRows == 0)
                {
                    response.Status = 404;
                    response.ResponseMsg = "Vendor link not found.";
                    return NotFound(response);
                }

                response.Status = 200;
                response.ResponseMsg = "Vendor unlinked successfully.";
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Status = 500;
                response.ResponseMsg = $"{ex.Message}";
                return StatusCode(500, response);
            }
        }

    }
}