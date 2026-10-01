using Dapper;
using MenuOptics.Models;
using System.Data;

namespace MenuOptics.Tests.IntegrationTests
{
    public class EventSpTests : StoredProcedureTestBase
    {
        [Fact]
        public async Task Sp_EventCRUD_InsertAndSelect_ReturnsCreatedEvent()
        {
            await RunInRollbackScopeAsync(async conn =>
            {
                var eventName = $"TestEvent_{Guid.NewGuid():N}".Substring(0, 30);

                var insertParams = new DynamicParameters();
                insertParams.Add("@Action", "INSERT");
                insertParams.Add("@RestaurantId", TestRestaurantId);
                insertParams.Add("@EventName", eventName);
                insertParams.Add("@EventDate", DateTime.UtcNow.AddDays(7));
                insertParams.Add("@Location", "Banquet Hall");
                insertParams.Add("@ContactName", "John Doe");
                insertParams.Add("@ContactEmail", "john@test.com");
                insertParams.Add("@ContactPhone", "1234567890");
                insertParams.Add("@NumberOfGuests", 50);
                insertParams.Add("@PricePerGuest", 45.00m);
                insertParams.Add("@TotalFoodCost", 1200.00m);
                insertParams.Add("@TotalRevenue", 2250.00m);
                insertParams.Add("@CreatedBy", TestUserId);

                var eventId = await conn.ExecuteScalarAsync<int>("sp_EventCRUD", insertParams, commandType: CommandType.StoredProcedure);
                Assert.True(eventId > 0);

                var selectParams = new DynamicParameters();
                selectParams.Add("@Action", "SELECT_BY_ID");
                selectParams.Add("@EventId", eventId);
                selectParams.Add("@RestaurantId", TestRestaurantId);

                var evt = await conn.QueryFirstOrDefaultAsync<Event>("sp_EventCRUD", selectParams, commandType: CommandType.StoredProcedure);
                Assert.NotNull(evt);
                Assert.Equal(eventName, evt.EventName);
                Assert.Equal(50, evt.NumberOfGuests);
                Assert.Equal(45.00m, evt.PricePerGuest);
            });
        }

        [Fact]
        public async Task Sp_EventCRUD_DuplicateEvent_CreatesCopySuccessfully()
        {
            await RunInRollbackScopeAsync(async conn =>
            {
                var originalName = $"OrigEvent_{Guid.NewGuid():N}".Substring(0, 25);

                var insertParams = new DynamicParameters();
                insertParams.Add("@Action", "INSERT");
                insertParams.Add("@RestaurantId", TestRestaurantId);
                insertParams.Add("@EventName", originalName);
                insertParams.Add("@NumberOfGuests", 20);
                insertParams.Add("@PricePerGuest", 30.00m);
                insertParams.Add("@CreatedBy", TestUserId);

                var eventId = await conn.ExecuteScalarAsync<int>("sp_EventCRUD", insertParams, commandType: CommandType.StoredProcedure);

                var dupParams = new DynamicParameters();
                dupParams.Add("@Action", "DUPLICATE");
                dupParams.Add("@EventId", eventId);

                var duplicatedEventId = await conn.ExecuteScalarAsync<int>("sp_EventCRUD", dupParams, commandType: CommandType.StoredProcedure);
                Assert.True(duplicatedEventId > 0);
                Assert.NotEqual(eventId, duplicatedEventId);

                var selectParams = new DynamicParameters();
                selectParams.Add("@Action", "SELECT_BY_ID");
                selectParams.Add("@EventId", duplicatedEventId);
                selectParams.Add("@RestaurantId", TestRestaurantId);

                var duplicatedEvent = await conn.QueryFirstOrDefaultAsync<Event>("sp_EventCRUD", selectParams, commandType: CommandType.StoredProcedure);
                Assert.NotNull(duplicatedEvent);
                Assert.Contains("(Copy)", duplicatedEvent.EventName);
            });
        }

        [Fact]
        public async Task Sp_EventCRUD_SelectAll_ReturnsEventsList()
        {
            await RunInRollbackScopeAsync(async conn =>
            {
                var selectAllParams = new DynamicParameters();
                selectAllParams.Add("@Action", "SELECT_ALL");
                selectAllParams.Add("@RestaurantId", TestRestaurantId);

                var events = await conn.QueryAsync<Event>("sp_EventCRUD", selectAllParams, commandType: CommandType.StoredProcedure);
                Assert.NotNull(events);
            });
        }
    }
}
