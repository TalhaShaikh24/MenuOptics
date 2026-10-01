using Dapper;
using MenuOptics.Models;
using System.Data;

namespace MenuOptics.Tests.IntegrationTests
{
    public class EventExtraSpTests : StoredProcedureTestBase
    {
        [Fact]
        public async Task Sp_EventOnlyItemCRUD_InsertAndSelectAll_WorksCorrectly()
        {
            await RunInRollbackScopeAsync(async conn =>
            {
                var itemName = $"EventItem_{Guid.NewGuid():N}".Substring(0, 20);

                var insertParams = new DynamicParameters();
                insertParams.Add("@Action", "INSERT");
                insertParams.Add("@RestaurantId", TestRestaurantId);
                insertParams.Add("@Name", itemName);
                insertParams.Add("@DefaultCost", 25.00m);
                insertParams.Add("@IsActive", true);

                var itemId = await conn.ExecuteScalarAsync<int>("sp_EventOnlyItemCRUD", insertParams, commandType: CommandType.StoredProcedure);
                Assert.True(itemId > 0);

                var selectParams = new DynamicParameters();
                selectParams.Add("@Action", "SELECT_ALL");
                selectParams.Add("@RestaurantId", TestRestaurantId);

                var items = await conn.QueryAsync<EventOnlyItemDto>("sp_EventOnlyItemCRUD", selectParams, commandType: CommandType.StoredProcedure);
                Assert.NotNull(items);
                Assert.Contains(items, i => i.Name == itemName);
            });
        }

        [Fact]
        public async Task Sp_EventNoteCRUD_UpsertAndSelect_WorksCorrectly()
        {
            await RunInRollbackScopeAsync(async conn =>
            {
                // First create an event to attach note to
                var insertEventParams = new DynamicParameters();
                insertEventParams.Add("@Action", "INSERT");
                insertEventParams.Add("@RestaurantId", TestRestaurantId);
                insertEventParams.Add("@EventName", "Note Test Event");
                insertEventParams.Add("@CreatedBy", TestUserId);

                var eventId = await conn.ExecuteScalarAsync<int>("sp_EventCRUD", insertEventParams, commandType: CommandType.StoredProcedure);
                Assert.True(eventId > 0);

                // Upsert note
                var noteParams = new DynamicParameters();
                noteParams.Add("@Action", "UPSERT");
                noteParams.Add("@EventId", eventId);
                noteParams.Add("@NoteText", "Test note text for integration test");

                await conn.ExecuteAsync("sp_EventNoteCRUD", noteParams, commandType: CommandType.StoredProcedure);

                // Select note
                var selectParams = new DynamicParameters();
                selectParams.Add("@Action", "SELECT_BY_EVENT");
                selectParams.Add("@EventId", eventId);

                var note = await conn.QueryFirstOrDefaultAsync<dynamic>("sp_EventNoteCRUD", selectParams, commandType: CommandType.StoredProcedure);
                Assert.NotNull(note);
                Assert.Equal("Test note text for integration test", (string)note.NoteText);
            });
        }
    }
}
