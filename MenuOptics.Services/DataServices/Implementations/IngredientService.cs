using Dapper;
using MenuOptics.Data;
using MenuOptics.Models;
using Stripe.Forwarding;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Text;

namespace MenuOptics.Services
{
    public class IngredientService : IIngredientService
    {
        private readonly DapperContext _context;

        private static readonly HashSet<string> _allowedUnits = new(StringComparer.OrdinalIgnoreCase)
        {
            "BA", "BG", "BX", "CA", "CM", "CT", "DZ", "EA",
            "FL", "FT", "GA", "GM", "HR", "IN", "KG", "LB",
            "LT", "ML", "MR", "OZ", "PA", "PC", "PR", "PT",
            "RL", "TB"
        };

        public IngredientService(DapperContext context)
        {
            _context = context;
        }

        public async Task<(int StatusCode, object Body)> CreateIngredientAsync(Ingredient model, int currentUserId, int actualUserId)
        {
            var ResponseDto = new ResponseDto();

            if (!_allowedUnits.Contains(model.Unit?.ToLower()))
            {
                ResponseDto.Status = 400;
                ResponseDto.ResponseMsg = $"Invalid unit '{model.Unit}'. Allowed units: {string.Join(", ", _allowedUnits)}";
                return (400, ResponseDto);
            }

            using var connection = _context.CreateConnection();
            connection.Open();

            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@RestaurantId", currentUserId);
                parameters.Add("@Name", model.Name);
                parameters.Add("@Price", model.Price);
                parameters.Add("@Unit", model.Unit.ToUpper());
                parameters.Add("@Size", model.Size);
                parameters.Add("@PackSize", model.PackSize);
                parameters.Add("@PrivateVendorId", model.PrivateVendorId);
                parameters.Add("@VendorProductId", model.VendorProductId);
                parameters.Add("@CreatedBy", actualUserId);
                parameters.Add("@Action", "INSERT");

                int ingredientId = await connection.ExecuteScalarAsync<int>(
                    "sp_IngredientCRUD", parameters, commandType: CommandType.StoredProcedure
                );

                ResponseDto.Status = 200;
                ResponseDto.ResponseMsg = "Ingredient created successfully.";
                ResponseDto.Data = new { IngredientId = ingredientId};

                return (200, ResponseDto);
            }
            catch (SqlException sqlEx)
            {
                ResponseDto.Status = 400;
                ResponseDto.ResponseMsg = sqlEx.Message;
                return (400, ResponseDto);
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

        public async Task<(int StatusCode, object Body)> UpdateIngredientAsync(Ingredient model, int currentUserId, int actualUserId)
        {
            var ResponseDto = new ResponseDto();
            using var connection = _context.CreateConnection();
            connection.Open();

            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@IngredientId", model.IngredientId);
                parameters.Add("@RestaurantId", currentUserId);
                parameters.Add("@Name", model.Name);
                parameters.Add("@Price", model.Price);
                parameters.Add("@Unit", model.Unit.ToUpper());
                parameters.Add("@Size", model.Size);
                parameters.Add("@PackSize", model.PackSize);
                parameters.Add("@VendorProductId", model.VendorProductId);
                parameters.Add("@ModifiedBy", actualUserId);
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
                ResponseDto.Status = 400;
                ResponseDto.ResponseMsg = sqlEx.Message;
                return (400, ResponseDto);
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

                ResponseDto.Status = 200;
                ResponseDto.ResponseMsg = "Ingredient retrieved successfully.";
                ResponseDto.Data = ingredient;
                return (200, ResponseDto);
            }
            catch (SqlException sqlEx)
            {
                ResponseDto.Status = 400;
                ResponseDto.ResponseMsg = sqlEx.Message;
                return (400, ResponseDto);
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
                    parameters.Add("@RestaurantId", currentUserId);
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
                    parameters.Add("@RestaurantId", currentUserId);
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
            catch (SqlException sqlEx)
            {
                if (query.ContainsKey("draw"))
                {
                    int draw = int.Parse(query["draw"].FirstOrDefault() ?? "1");
                    return (200, new DataTablesResponseDto<Ingredient> { Draw = draw, Data = Enumerable.Empty<Ingredient>() });
                }
                return (400, new ResponseDto { Status = 400, ResponseMsg = sqlEx.Message });
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
                    parameters.Add("@RestaurantId", currentUserId);
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
                    parameters.Add("@RestaurantId", currentUserId);
                    parameters.Add("@Action", "SELECT_ALL");
                    parameters.Add("@PageSize", -1); // Return all rows

                    var ingredients = await connection.QueryAsync<Ingredient>("sp_IngredientCRUD", parameters, commandType: CommandType.StoredProcedure);
                    ResponseDto.Status = 200;
                    ResponseDto.ResponseMsg = "Ingredients retrieved successfully.";
                    ResponseDto.Data = ingredients;
                    return (200, ResponseDto);
                }
            }
            catch (SqlException sqlEx)
            {
                if (query.ContainsKey("draw"))
                {
                    int draw = int.Parse(query["draw"].FirstOrDefault() ?? "1");
                    return (200, new DataTablesResponseDto<Ingredient> { Draw = draw, Data = Enumerable.Empty<Ingredient>() });
                }
                return (400, new ResponseDto { Status = 400, ResponseMsg = sqlEx.Message });
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
                    parameters.Add("@RestaurantId", currentUserId);
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
                    parameters.Add("@RestaurantId", currentUserId);
                    if (isPrivate)
                        parameters.Add("@PrivateVendorId", vendorId);
                    else
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
            catch (SqlException sqlEx)
            {
                if (query.ContainsKey("draw"))
                {
                    int draw = int.Parse(query["draw"].FirstOrDefault() ?? "1");
                    return (200, new DataTablesResponseDto<Ingredient> { Draw = draw, Data = Enumerable.Empty<Ingredient>() });
                }
                return (400, new ResponseDto { Status = 400, ResponseMsg = sqlEx.Message });
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
                    parameters.Add("@RestaurantId", userId);
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
                    parameters.Add("@RestaurantId", userId);
                    parameters.Add("@Action", "SELECT_ALL");
                    parameters.Add("@PageSize", -1); // Return all rows

                    var ingredients = await connection.QueryAsync<Ingredient>("sp_IngredientCRUD", parameters, commandType: CommandType.StoredProcedure);

                    ResponseDto.Status = 200;
                    ResponseDto.ResponseMsg = "Ingredients retrieved successfully.";
                    ResponseDto.Data = ingredients;
                    return (200, ResponseDto);
                }
            }
            catch (SqlException sqlEx)
            {
                if (query.ContainsKey("draw"))
                {
                    int draw = int.Parse(query["draw"].FirstOrDefault() ?? "1");
                    return (200, new DataTablesResponseDto<Ingredient> { Draw = draw, Data = Enumerable.Empty<Ingredient>() });
                }
                return (400, new ResponseDto { Status = 400, ResponseMsg = sqlEx.Message });
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

        public async Task<(int StatusCode, object Body)> ToggleIngredientActiveAsync(int id, int currentUserId, int actualUserId)
        {
            var ResponseDto = new ResponseDto();
            using var connection = _context.CreateConnection();
            connection.Open();

            try
            {
                var parameters = new DynamicParameters();
                
                parameters = new DynamicParameters();
                parameters.Add("@RestaurantId", currentUserId);
                parameters.Add("@IngredientId", id);
                parameters.Add("@ModifiedBy", actualUserId);
                parameters.Add("@Action", "TOGGLE_ACTIVE");

                var result = await connection.QueryFirstAsync("sp_IngredientCRUD", parameters, commandType: CommandType.StoredProcedure);

                ResponseDto.Status = 200;
                ResponseDto.ResponseMsg = $"Ingredient {(result.IsActive ? "activated" : "deactivated")} successfully.";
                ResponseDto.Data = new 
                { 
                    IngredientId = id, 
                    IsActive = result.isActive 
                };
                return (200, ResponseDto);
            }
            catch (SqlException sqlEx)
            {
                ResponseDto.Status = 400;
                ResponseDto.ResponseMsg = sqlEx.Message;
                return (400, ResponseDto);
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
            return (500, new ResponseDto { Status = 500, ResponseMsg = $"{ex.Message}" });
        }

        public async Task<(int StatusCode, object Body)> ImportIngredientPricesAsync(IFormFile file, int currentUserId, int actualUserId)
        {
            var responseDto = new ResponseDto();

            if (file == null || file.Length == 0)
            {
                responseDto.Status = 400;
                responseDto.ResponseMsg = "No file uploaded.";
                return (400, responseDto);
            }

            if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                responseDto.Status = 400;
                responseDto.ResponseMsg = "Invalid file format. Please upload a CSV file (.csv).";
                return (400, responseDto);
            }

            using var connection = _context.CreateConnection();
            connection.Open();

            try
            {
                using var reader = new StreamReader(file.OpenReadStream(), Encoding.UTF8);

                string? headerLine = await reader.ReadLineAsync();

                if (string.IsNullOrWhiteSpace(headerLine))
                {
                    responseDto.Status = 400;
                    responseDto.ResponseMsg = "The uploaded CSV file is empty.";
                    return (400, responseDto);
                }

                var table = new DataTable();

                table.Columns.Add("IngredientId", typeof(int));
                table.Columns.Add("Price", typeof(decimal));

                string? line;
                int rowNumber = 1;

                while ((line = await reader.ReadLineAsync()) != null)
                {
                    rowNumber++;

                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    var cols = ParseCsvLine(line);

                    if (cols.Count < 3)
                        continue;

                    if (!int.TryParse(cols[1], out int ingredientId) || ingredientId <= 0)
                        continue;

                    string priceStr = cols[2];

                    if (string.IsNullOrWhiteSpace(priceStr))
                        continue;

                    decimal price;

                    if (!decimal.TryParse(
                            priceStr,
                            NumberStyles.Any,
                            CultureInfo.InvariantCulture,
                            out price) &&
                        !decimal.TryParse(priceStr, out price))
                    {
                        continue;
                    }

                    if (price < 0)
                        continue;

                    table.Rows.Add(ingredientId, price);
                }

                if (table.Rows.Count == 0)
                {
                    responseDto.Status = 400;
                    responseDto.ResponseMsg = "No valid ingredient prices were found in the CSV.";
                    return (400, responseDto);
                }

                var parameters = new DynamicParameters();

                parameters.Add(
                    "@IngredientPrices",
                    table.AsTableValuedParameter("dbo.IngredientPriceType")
                );

                parameters.Add("@RestaurantId", currentUserId);
                parameters.Add("@Action", currentUserId);

                int updatedCount = await connection.ExecuteAsync(
                    "sp_IngredientCRUD",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                responseDto.Status = 200;
                responseDto.ResponseMsg =
                    $"Successfully updated {table.Rows.Count} ingredient price(s).";

                return (200, responseDto);
            }
            catch (SqlException sqlEx)
            {
                responseDto.Status = 400;
                responseDto.ResponseMsg = sqlEx.Message;
                return (400, responseDto);
            }
            catch (Exception ex)
            {
                responseDto.Status = 500;
                responseDto.ResponseMsg = ex.Message;
                return (500, responseDto);
            }
            finally
            {
                connection.Close();
            }
        }

        private static List<string> ParseCsvLine(string line)
        {
            var result = new List<string>();
            if (line == null) return result;
            var sb = new StringBuilder();
            bool inQuotes = false;
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        sb.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(sb.ToString().Trim());
                    sb.Clear();
                }
                else
                {
                    sb.Append(c);
                }
            }
            result.Add(sb.ToString().Trim());
            return result;
        }
    }
}

