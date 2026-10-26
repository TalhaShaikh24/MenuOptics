using Dapper;
using MenuOptics.Data;
using MenuOptics.Models;
using MenuOptics.Models.DTOs.MenuItem;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Reflection;
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
                var saleItemsTable = new DataTable();
                saleItemsTable.Columns.Add("MenuItemId", typeof(int));
                saleItemsTable.Columns.Add("Quantity", typeof(decimal));
                saleItemsTable.Columns.Add("Revenue", typeof(decimal));
                saleItemsTable.Columns.Add("Cost", typeof(decimal));

                foreach (var item in model.Items)
                {
                    saleItemsTable.Rows.Add(
                        item.MenuItemId,
                        item.Quantity,
                        item.Revenue,
                        item.Cost
                    );
                }

                // Insert Sale
                var saleParameters = new DynamicParameters();
                saleParameters.Add("@RestaurantId", currentUserId);
                saleParameters.Add("@StartDate", model.StartDate);
                saleParameters.Add("@EndDate", model.EndDate);
                saleParameters.Add("@Subtotal", model.Subtotal);
                saleParameters.Add("@Tax", model.Tax);
                saleParameters.Add("@Total", model.Total);
                saleParameters.Add("@CreatedBy", currentUserId);
                saleParameters.Add("@SaleItems", saleItemsTable.AsTableValuedParameter("SaleItemsType"));
                saleParameters.Add("@Action", "INSERT");

                int saleId = await connection.ExecuteScalarAsync<int>("sp_SaleCRUD", saleParameters, commandType: CommandType.StoredProcedure);

                ResponseDto.Status = 200;
                ResponseDto.ResponseMsg = "Sale created successfully.";
                ResponseDto.Data = new
                {
                    saleId,
                    model.StartDate,
                    model.EndDate,
                    model.Subtotal,
                    model.Tax,
                    model.Total,
                    Items = model.Items
                };
                return ResponseDto;
            }
            catch (SqlException sqlEx)
            {
                ResponseDto.Status = 400;
                ResponseDto.ResponseMsg = sqlEx.Message;
                return ResponseDto;
            }
            catch (Exception ex)
            {
                ResponseDto.Status = 500;
                ResponseDto.ResponseMsg = $"{ex.Message}";
                return ResponseDto;
            }
            finally
            {
                connection.Close();
            }
        }
        public async Task<ResponseDto> UpdateSaleAsync(Sale model, int currentUserId)
        {
            var ResponseDto = new ResponseDto();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var saleItemsTable = new DataTable();
                saleItemsTable.Columns.Add("MenuItemId", typeof(int));
                saleItemsTable.Columns.Add("Quantity", typeof(decimal));
                saleItemsTable.Columns.Add("Revenue", typeof(decimal));
                saleItemsTable.Columns.Add("Cost", typeof(decimal));

                if (model.Items != null)
                {
                    foreach (var item in model.Items)
                    {
                        saleItemsTable.Rows.Add(
                            item.MenuItemId,
                            item.Quantity,
                            item.Revenue,
                            item.Cost
                        );
                    }
                }
                
                // Insert Sale
                var saleParameters = new DynamicParameters();
                saleParameters.Add("@SaleId", model.SaleId);
                saleParameters.Add("@RestaurantId", currentUserId);
                saleParameters.Add("@StartDate", model.StartDate);
                saleParameters.Add("@EndDate", model.EndDate);
                saleParameters.Add("@Subtotal", model.Subtotal);
                saleParameters.Add("@Tax", model.Tax);
                saleParameters.Add("@Total", model.Total);
                saleParameters.Add("@ModifiedBy", currentUserId);
                saleParameters.Add("@SaleItems", saleItemsTable.AsTableValuedParameter("SaleItemsType"));
                saleParameters.Add("@Action", "UPDATE");

                int saleId = await connection.ExecuteScalarAsync<int>("sp_SaleCRUD", saleParameters, commandType: CommandType.StoredProcedure);

                ResponseDto.Status = 200;
                ResponseDto.ResponseMsg = "Sale updated successfully.";
                ResponseDto.Data = new
                {
                    saleId,
                    model.StartDate,
                    model.EndDate,
                    model.Subtotal,
                    model.Tax,
                    model.Total,
                    Items = model.Items
                };
                return ResponseDto;
            }
            catch (SqlException sqlEx)
            {
                ResponseDto.Status = 400;
                ResponseDto.ResponseMsg = sqlEx.Message;
                return ResponseDto;
            }
            catch (Exception ex)
            {
                ResponseDto.Status = 500;
                ResponseDto.ResponseMsg = $"{ex.Message}";
                return ResponseDto;
            }
            finally
            {
                connection.Close();
            }
        }
        public async Task<ResponseDto> GetAllSalesAsync(int CurrentUserId)
        {
            var ResponseDto = new ResponseDto();
            using var connection = _context.CreateConnection();
            connection.Open();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@RestaurantId", CurrentUserId);
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
            catch (SqlException sqlEx)
            {
                ResponseDto.Status = 400;
                ResponseDto.ResponseMsg = sqlEx.Message;
                return ResponseDto;
            }
            catch (Exception ex)
            {
                ResponseDto.Status = 500;
                ResponseDto.ResponseMsg = $"{ex.Message}";
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

                    var menuItem = (GetMenuItemDTO?)(await _menuService.GetMenuItemAsync(menuItemId)).Data;
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
                        Revenue = menuItem.SellingPrice * quantity,
                        Cost = menuItem.Cost * quantity
                    });
                }

                var saleItemsTable = new DataTable();
                saleItemsTable.Columns.Add("MenuItemId", typeof(int));
                saleItemsTable.Columns.Add("Quantity", typeof(decimal));
                saleItemsTable.Columns.Add("SellingPrice", typeof(decimal));
                saleItemsTable.Columns.Add("Cost", typeof(decimal));

                foreach (var item in sale.Items)
                {
                    saleItemsTable.Rows.Add(
                        item.MenuItemId,
                        item.Quantity,
                        item.Revenue,
                        item.Cost
                    );
                }

                sale.Subtotal = sale.Items.Sum(si => si.Revenue);
                sale.Tax = 0;
                sale.Total = sale.Subtotal + sale.Tax;

                // Insert Sale
                var saleParameters = new DynamicParameters();
                saleParameters.Add("@RestaurantId", currentUserId);
                saleParameters.Add("@StartDate", sale.StartDate);
                saleParameters.Add("@EndDate", sale.EndDate);
                saleParameters.Add("@Subtotal", sale.Subtotal);
                saleParameters.Add("@Tax", sale.Tax);
                saleParameters.Add("@Total", sale.Total);
                saleParameters.Add("@CreatedBy", currentUserId);
                saleParameters.Add("@SaleItems", saleItemsTable.AsTableValuedParameter("SaleItemsType"));
                saleParameters.Add("@Action", "INSERT");

                int saleId = await connection.ExecuteScalarAsync<int>("sp_SaleCRUD", saleParameters, commandType: CommandType.StoredProcedure);

                ResponseDto.Status = 200;
                ResponseDto.ResponseMsg = $"Successfully imported {sale.Items.Count} sale menu items.";
                return ResponseDto;
            }
            catch (SqlException sqlEx)
            {
                ResponseDto.Status = 400;
                ResponseDto.ResponseMsg = sqlEx.Message;
                return ResponseDto;
            }
            catch (Exception ex)
            {
                ResponseDto.Status = 500;
                ResponseDto.ResponseMsg = $"{ex.Message}";
                return ResponseDto;
            }
            finally
            {
                connection.Close();
            }
        }
    }
}
