using Dapper;
using MenuOptics.Data;
using MenuOptics.Models;
using OfficeOpenXml;
using System.Data;
using System.Text.Json;
using LicenseContext = OfficeOpenXml.LicenseContext;

namespace MenuOptics.Services
{
    public class SalesService : ISalesService
    {
        private readonly DapperContext _context;
        private const decimal TAX_RATE = 0.085m; // 8.5% tax rate

        public SalesService(DapperContext context)
        {
            _context = context;
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial; // Required for EPPlus
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
                saleParameters.Add("@SaleDate", model.SaleDate);
                saleParameters.Add("@EndSaleDate", model.EndSaleDate);
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
                    sale.SaleDate,
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

        public async Task<ResponseDto> ImportSalesAsync(IFormFile file, int currentUserId)
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

                if (!file.FileName.EndsWith(".xlsx") && !file.FileName.EndsWith(".xls"))
                {
                    ResponseDto.Status = 400;
                    ResponseDto.ResponseMsg = "Invalid file format. Please upload an Excel file (.xlsx or .xls).";
                    return ResponseDto;
                }

                var sales = new List<Sale>();
                var currentSale = new Sale();
                int currentRow = 2;

                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);
                    using (var package = new ExcelPackage(stream))
                    {
                        var worksheet = package.Workbook.Worksheets[0];
                        int rowCount = worksheet.Dimension.Rows;

                        for (int row = 2; row <= rowCount; row++)
                        {
                            var saleDate = worksheet.Cells[row, 1].GetValue<DateTime?>();
                            var subtotal = worksheet.Cells[row, 2].GetValue<decimal?>();
                            var total = worksheet.Cells[row, 3].GetValue<decimal?>();
                            var menuItemId = worksheet.Cells[row, 4].GetValue<int?>();
                            var menuItemName = worksheet.Cells[row, 5].GetValue<string?>();
                            var quantity = worksheet.Cells[row, 6].GetValue<int?>();

                            if (!saleDate.HasValue || !subtotal.HasValue || !total.HasValue || !menuItemId.HasValue || !quantity.HasValue)
                            {
                                continue; // Skip incomplete rows
                            }

                            // Fetch MenuItem details
                            var menuItemParams = new DynamicParameters();
                            menuItemParams.Add("@MenuItemId", menuItemId);
                            menuItemParams.Add("@MenuItemName", menuItemId);
                            menuItemParams.Add("@Action", "SELECT");
                            var menuItem = await connection.QueryFirstOrDefaultAsync(
                                "sp_MenuCRUD",
                                menuItemParams,
                                commandType: CommandType.StoredProcedure
                            );

                            if (menuItem == null)
                            {
                                ResponseDto.Status = 400;
                                ResponseDto.ResponseMsg = $"Invalid MenuItemId {menuItemId} at row {row}.";
                                return ResponseDto;
                            }

                            var price = (decimal)menuItem.SellingPrice;
                            var itemTotal = price * quantity.Value;

                            if (currentSale.SaleDate != saleDate || row == 2)
                            {
                                if (row > 2 && currentSale.Items.Any())
                                {
                                    sales.Add(currentSale);
                                }
                                currentSale = new Sale
                                {
                                    SaleDate = saleDate.Value,
                                    Subtotal = 0,
                                    Tax = 0,
                                    Total = total.Value,
                                    Items = new List<SaleItem>()
                                };
                                currentRow = row;
                            }

                            currentSale.Subtotal += itemTotal;
                            currentSale.Items.Add(new SaleItem
                            {
                                MenuItemId = menuItemId.Value,
                                Name = menuItem.Name,
                                Quantity = quantity.Value,
                                Price = price
                            });
                        }

                        if (currentSale.Items.Any())
                        {
                            sales.Add(currentSale);
                        }
                    }
                }

                foreach (var sale in sales)
                {
                    sale.Tax = sale.Subtotal * TAX_RATE;
                    sale.Total = sale.Subtotal + sale.Tax;

                    // Insert Sale
                    var saleParameters = new DynamicParameters();
                    saleParameters.Add("@UserId", currentUserId);
                    saleParameters.Add("@SaleDate", sale.SaleDate);
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
                        itemParameters.Add("@MenuItemName", item.MenuItemName);
                        itemParameters.Add("@Quantity", item.Quantity);
                        itemParameters.Add("@Price", item.Price);
                        itemParameters.Add("@UserId", currentUserId); // Added for sp_SaleCRUD
                        itemParameters.Add("@CreatedBy", currentUserId);
                        itemParameters.Add("@Action", "INSERT_ITEM");
                        await connection.ExecuteAsync("sp_SaleCRUD", itemParameters, commandType: CommandType.StoredProcedure);
                    }
                }

                ResponseDto.Status = 200;
                ResponseDto.ResponseMsg = $"Successfully imported {sales.Count} sales.";
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
