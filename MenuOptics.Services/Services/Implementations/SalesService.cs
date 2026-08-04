using Dapper;
using MenuOptics.Data;
using MenuOptics.Models;
using System.Data;
using System.IO;
using System.Text.Json;

namespace MenuOptics.Services
{
    public class SalesService : ISalesService
    {
        private readonly DapperContext _context;
        private IMenuService _menuService;

        public SalesService(DapperContext context, IMenuService menuService)
        {
            _context = context;
            _menuService = menuService;
        }

        public async Task<ResponseDto> CreateSaleAsync(Sale model, int currentUserId)
        {
            var ResponseDto = new ResponseDto();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                // Insert Sale
                var saleParameters = new DynamicParameters();
                saleParameters.Add("@UserId", currentUserId);
                saleParameters.Add("@StartDate", model.StartDate);
                saleParameters.Add("@EndDate", model.EndDate);
                saleParameters.Add("@Subtotal", model.Subtotal);
                saleParameters.Add("@Tax", model.Tax);
                saleParameters.Add("@Total", model.Total);
                saleParameters.Add("@CreatedBy", currentUserId);
                saleParameters.Add("@Action", "INSERT");

                int saleId = await connection.ExecuteScalarAsync<int>("sp_SaleCRUD", saleParameters, commandType: CommandType.StoredProcedure);

                // Insert Sale Items
                foreach (var item in model.Items)
                {
                    var itemParameters = new DynamicParameters();
                    itemParameters.Add("@SaleId", saleId);
                    itemParameters.Add("@MenuItemId", item.MenuItemId);
                    itemParameters.Add("@Quantity", item.Quantity);
                    itemParameters.Add("@Price", item.Price);
                    itemParameters.Add("@Cost", item.Cost);
                    itemParameters.Add("@CreatedBy", currentUserId);
                    itemParameters.Add("@UserId", currentUserId); // Added for sp_SaleCRUD
                    itemParameters.Add("@Action", "INSERT_ITEM");
                    await connection.ExecuteAsync("sp_SaleCRUD", itemParameters, commandType: CommandType.StoredProcedure);
                }

                // Fetch sale details for ResponseDto
                var selectParameters = new DynamicParameters();
                selectParameters.Add("@SaleId", saleId);
                selectParameters.Add("@Action", "SELECT");

                var sale = await connection.QueryFirstOrDefaultAsync<Sale>(
                    "sp_SaleCRUD",
                    selectParameters,
                    commandType: CommandType.StoredProcedure
                );

                if (sale == null)
                {
                    ResponseDto.Status = 404;
                    ResponseDto.ResponseMsg = "Sale not found after creation.";
                    return ResponseDto;
                }

                // Deserialize ItemsJson to List<SaleItemDTO>
                var saleItems = string.IsNullOrEmpty(sale.ItemsJson)
                    ? new List<SaleItem>()
                    : JsonSerializer.Deserialize<List<SaleItem>>(sale.ItemsJson);

                ResponseDto.Status = 200;
                ResponseDto.ResponseMsg = "Sale created successfully.";
                ResponseDto.Data = new
                {
                    sale.SaleId,
                    sale.StartDate,
                    sale.EndDate,
                    sale.Subtotal,
                    sale.Tax,
                    sale.Total,
                    Items = saleItems
                };
                return ResponseDto;
            }
            catch (Exception ex)
            {
                ResponseDto.Status = 500;
                ResponseDto.ResponseMsg = $"Server error: {ex.Message}";
                return ResponseDto;
            }
            finally
            {
                connection.Close();
            }
        }

        public async Task<ResponseDto> GetAllSalesAsync()
        {
            var ResponseDto = new ResponseDto();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@Action", "SELECT_ALL");

                var sales = await connection.QueryAsync<Sale>(
                    "sp_SaleCRUD",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                ResponseDto.Status = 200;
                ResponseDto.ResponseMsg = "Sales retrieved successfully.";
                ResponseDto.Data = sales;
                return ResponseDto;
            }
            catch (Exception ex)
            {
                ResponseDto.Status = 500;
                ResponseDto.ResponseMsg = $"Server error: {ex.Message}";
                return ResponseDto;
            }
            finally
            {
                connection.Close();
            }
        }

        public async Task<ResponseDto> ImportSalesAsync(IFormFile file, DateTime startDate, DateTime endDate, int currentUserId)
        {
            var ResponseDto = new ResponseDto();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                if (file == null || file.Length == 0)
                {
                    ResponseDto.Status = 400;
                    ResponseDto.ResponseMsg = "No file uploaded.";
                    return ResponseDto;
                }

                if (!file.FileName.EndsWith(".csv"))
                {
                    ResponseDto.Status = 400;
                    ResponseDto.ResponseMsg = "Invalid file format. Please upload an CSV file (.csv).";
                    return ResponseDto;
                }

                var sale = new Sale
                {
                    StartDate = startDate,
                    EndDate = endDate,
                    Subtotal = 0,
                    Tax = 0,
                    Total = 0,
                    Items = new List<SaleItem>()
                };

                using var reader = new StreamReader(file.OpenReadStream());

                string? line;
                int row = 0;
                
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    row++;

                    if (row == 1) continue;//Skip header

                    string[] columns = line.Split(',');
                    int menuItemId = Convert.ToInt32(columns[1]);
                    int quantity = Convert.ToInt32(columns[2]);
                    
                    if (quantity <= 0)
                    {
                        ResponseDto.Status = 400;
                        ResponseDto.ResponseMsg = $"Quantity Sold must be greater than 0 at row {row}.";
                        return ResponseDto;
                    }

                    var menuItem = (Menu?)(await _menuService.GetMenuItemAsync(menuItemId)).Data;
                    if (menuItem == null)
                    {
                        ResponseDto.Status = 400;
                        ResponseDto.ResponseMsg = $"Invalid MenuItemId {menuItemId} at row {row}.";
                        return ResponseDto;
                    }

                    sale.Items.Add(new SaleItem
                    {
                        MenuItemId = menuItemId,
                        Quantity = quantity,
                        Price = menuItem.SellingPrice * quantity,
                        Cost = menuItem.Cost * quantity
                    });
                }

                sale.Subtotal = sale.Items.Sum(si => si.Price);
                sale.Tax = 0;
                sale.Total = sale.Subtotal + sale.Tax;

                // Insert Sale
                var saleParameters = new DynamicParameters();
                saleParameters.Add("@UserId", currentUserId);
                saleParameters.Add("@StartDate", sale.StartDate);
                saleParameters.Add("@EndDate", sale.EndDate);
                saleParameters.Add("@Subtotal", sale.Subtotal);
                saleParameters.Add("@Tax", sale.Tax);
                saleParameters.Add("@Total", sale.Total);
                saleParameters.Add("@CreatedBy", currentUserId);
                saleParameters.Add("@Action", "INSERT");

                int saleId = await connection.ExecuteScalarAsync<int>("sp_SaleCRUD", saleParameters, commandType: CommandType.StoredProcedure);

                // Insert Sale Items
                foreach (var item in sale.Items)
                {
                    var itemParameters = new DynamicParameters();
                    itemParameters.Add("@SaleId", saleId);
                    itemParameters.Add("@MenuItemId", item.MenuItemId);
                    itemParameters.Add("@Quantity", item.Quantity);
                    itemParameters.Add("@Price", item.Price);
                    itemParameters.Add("@Cost", item.Cost);
                    itemParameters.Add("@CreatedBy", currentUserId);
                    itemParameters.Add("@UserId", currentUserId); // Added for sp_SaleCRUD
                    itemParameters.Add("@Action", "INSERT_ITEM");
                    await connection.ExecuteAsync("sp_SaleCRUD", itemParameters, commandType: CommandType.StoredProcedure);
                }

                ResponseDto.Status = 200;
                ResponseDto.ResponseMsg = $"Successfully imported {sale.Items.Count} sale menu items.";
                return ResponseDto;
            }
            catch (Exception ex)
            {
                ResponseDto.Status = 500;
                ResponseDto.ResponseMsg = $"Server error: {ex.Message}";
                return ResponseDto;
            }
            finally
            {
                connection.Close();
            }
        }
    }
}
