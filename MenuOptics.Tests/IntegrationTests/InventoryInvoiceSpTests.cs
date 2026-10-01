using Dapper;
using MenuOptics.Models;
using MenuOptics.Services;
using System.Data;
using System.Data.SqlClient;

namespace MenuOptics.Tests.IntegrationTests
{
    public class InventoryInvoiceSpTests : StoredProcedureTestBase
    {
        [Fact]
        public async Task Sp_InventoryInvoiceCRUD_InsertWithItems_CreatesInvoiceAndInvoiceItems()
        {
            await RunInRollbackScopeAsync(async conn =>
            {
                // 1. Get a valid ingredient for restaurant
                var ingredientId = await conn.QueryFirstOrDefaultAsync<int?>(
                    "SELECT TOP 1 IngredientId FROM Ingredients WHERE RestaurantId = @RestaurantId AND IsActive = 1",
                    new { RestaurantId = TestRestaurantId }) ?? 8728;

                // 2. Get a valid private vendor for restaurant
                var privateVendorId = await conn.QueryFirstOrDefaultAsync<int?>(
                    "SELECT TOP 1 PrivateVendorId FROM PrivateVendors WHERE RestaurantId = @RestaurantId AND IsActive = 1",
                    new { RestaurantId = TestRestaurantId }) ?? 1;

                var invoiceNumber = $"INV_{Guid.NewGuid():N}".Substring(0, 20);

                // 3. Prepare Table-Valued Parameter for InventoryInvoiceItems
                var itemsTable = new DataTable();
                itemsTable.Columns.Add("IngredientId", typeof(int));
                itemsTable.Columns.Add("Quantity", typeof(decimal));
                itemsTable.Columns.Add("PackSize", typeof(int));
                itemsTable.Columns.Add("Price", typeof(decimal));

                // Add 2 line items
                itemsTable.Rows.Add(ingredientId, 5.0m, 1, 15.50m);
                itemsTable.Rows.Add(ingredientId, 2.5m, 2, 20.00m);

                var insertParams = new DynamicParameters();
                insertParams.Add("@Action", "INSERT");
                insertParams.Add("@RestaurantId", TestRestaurantId);
                insertParams.Add("@PrivateVendorId", privateVendorId);
                insertParams.Add("@InvoiceId", invoiceNumber);
                insertParams.Add("@InvoiceDate", DateTime.UtcNow);
                insertParams.Add("@Subtotal", 127.50m);
                insertParams.Add("@Tax", 16.58m);
                insertParams.Add("@Total", 144.08m);
                insertParams.Add("@CreatedBy", TestUserId);
                insertParams.Add("@InventoryInvoiceItems", itemsTable.AsTableValuedParameter("dbo.InventoryInvoiceItemsType"));

                // Execute SP
                var invoiceId = await conn.ExecuteScalarAsync<int>("sp_InventoryInvoiceCRUD", insertParams, commandType: CommandType.StoredProcedure);
                Assert.True(invoiceId > 0);

                // Verify header inserted into InventoryInvoices table
                var header = await conn.QueryFirstOrDefaultAsync<dynamic>(
                    "SELECT * FROM InventoryInvoices WHERE InventoryInvoiceId = @Id", 
                    new { Id = invoiceId });
                Assert.NotNull(header);
                Assert.Equal(invoiceNumber, (string)header.InvoiceId);
                Assert.Equal(144.08m, (decimal)header.Total);

                // Verify line items inserted into InventoryInvoiceItems table
                var items = (await conn.QueryAsync<dynamic>(
                    "SELECT * FROM InventoryInvoiceItems WHERE InventoryInvoideId = @Id", 
                    new { Id = invoiceId })).ToList();
                Assert.NotEmpty(items);
                Assert.Equal(2, items.Count);
                Assert.Contains(items, i => (decimal)i.Price == 15.50m);
                Assert.Contains(items, i => (decimal)i.Price == 20.00m);
            });
        }

        [Fact]
        public async Task InventoryService_EndToEnd_CreateInvoiceWithItems_Succeeds()
        {
            // Query IDs before starting transaction scope to avoid multi-connection DTC escalation
            int ingredientId;
            int privateVendorId;

            using (var conn = new SqlConnection(ConnectionString))
            {
                await conn.OpenAsync();
                ingredientId = await conn.QueryFirstOrDefaultAsync<int?>(
                    "SELECT TOP 1 IngredientId FROM Ingredients WHERE RestaurantId = @RestaurantId AND IsActive = 1",
                    new { RestaurantId = TestRestaurantId }) ?? 8728;

                privateVendorId = await conn.QueryFirstOrDefaultAsync<int?>(
                    "SELECT TOP 1 PrivateVendorId FROM PrivateVendors WHERE RestaurantId = @RestaurantId AND IsActive = 1",
                    new { RestaurantId = TestRestaurantId }) ?? 1;
            }

            await RunInRollbackScopeAsync(async () =>
            {
                var service = new InventoryService(DapperContext);
                var invoiceNumber = $"SvcInv_{Guid.NewGuid():N}".Substring(0, 20);

                var request = new CreateInvoiceRequestDto
                {
                    PrivateVendorId = privateVendorId,
                    InvoiceId = invoiceNumber,
                    InvoiceDate = DateTime.UtcNow,
                    Items = new List<InvoiceItemDto>
                    {
                        new InvoiceItemDto
                        {
                            IngredientId = ingredientId,
                            Quantity = 3,
                            PackSize = 1,
                            Price = 12.00m
                        },
                        new InvoiceItemDto
                        {
                            IngredientId = ingredientId,
                            Quantity = 2,
                            PackSize = 2,
                            Price = 25.00m
                        }
                    }
                };

                var response = await service.CreateInvoiceAsync(request, TestRestaurantId);
                Assert.Equal(200, response.Status);
                Assert.NotNull(response.Data);
            });
        }

        [Fact]
        public async Task Sp_InventoryInvoiceCRUD_InvalidRestaurantId_ThrowsException()
        {
            await RunInRollbackScopeAsync(async conn =>
            {
                var insertParams = new DynamicParameters();
                insertParams.Add("@Action", "INSERT");
                insertParams.Add("@RestaurantId", -99999); // Invalid
                insertParams.Add("@VendorId", TestUserId);

                var ex = await Assert.ThrowsAsync<SqlException>(async () =>
                {
                    await conn.ExecuteScalarAsync<int>("sp_InventoryInvoiceCRUD", insertParams, commandType: CommandType.StoredProcedure);
                });

                Assert.Contains("Invalid RestaurantId", ex.Message);
            });
        }

        [Fact]
        public async Task Sp_InventoryInvoiceCRUD_InvalidVendorId_ThrowsException()
        {
            await RunInRollbackScopeAsync(async conn =>
            {
                var insertParams = new DynamicParameters();
                insertParams.Add("@Action", "INSERT");
                insertParams.Add("@RestaurantId", TestRestaurantId);
                insertParams.Add("@VendorId", -88888); // Invalid vendor

                var ex = await Assert.ThrowsAsync<SqlException>(async () =>
                {
                    await conn.ExecuteScalarAsync<int>("sp_InventoryInvoiceCRUD", insertParams, commandType: CommandType.StoredProcedure);
                });

                Assert.Contains("Invalid VendorId", ex.Message);
            });
        }

        [Fact]
        public async Task Sp_InventoryInvoiceCRUD_SelectAll_ReturnsInvoices()
        {
            await RunInRollbackScopeAsync(async conn =>
            {
                var selectParams = new DynamicParameters();
                selectParams.Add("@Action", "SELECT_ALL");
                selectParams.Add("@RestaurantId", TestRestaurantId);

                var invoices = await conn.QueryAsync<object>("sp_InventoryInvoiceCRUD", selectParams, commandType: CommandType.StoredProcedure);
                Assert.NotNull(invoices);
            });
        }
    }
}
