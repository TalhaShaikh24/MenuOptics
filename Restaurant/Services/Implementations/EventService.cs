using Dapper;
using MenuOptics.Data;
using MenuOptics.Models;
using System.Data;

namespace MenuOptics.Services
{
    public class EventService : IEventService
    {
        private readonly DapperContext _context;

        public EventService(DapperContext context)
        {
            _context = context;
        }

        public async Task<Response> GetAllEventsAsync(int restaurantId)
        {
            var response = new Response();
            using var connection = _context.CreateConnection();
            connection.Open();

            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@Action", "SELECT_ALL");
                parameters.Add("@RestaurantId", restaurantId);

                var events = await connection.QueryAsync<Event>(
                    "sp_EventCRUD",
                    parameters,
                    commandType: CommandType.StoredProcedure);

                response.Status = 200;
                response.ResponseMsg = "Events retrieved successfully.";
                response.Data = events;
                return response;
            }
            catch (Exception ex)
            {
                response.Status = 500;
                response.ResponseMsg = ex.Message;
                return response;
            }
        }

        public async Task<Response> GetEventAsync(int id, int restaurantId)
        {
            var response = new Response();
            using var connection = _context.CreateConnection();
            connection.Open();

            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@EventId", id);
                parameters.Add("@RestaurantId", restaurantId);
                parameters.Add("@Action", "SELECT_BY_ID");

                var evt = await connection.QuerySingleOrDefaultAsync<Event>(
                    "sp_EventCRUD",
                    parameters,
                    commandType: CommandType.StoredProcedure);

                if (evt == null)
                {
                    response.Status = 404;
                    response.ResponseMsg = "Event not found.";
                    return response;
                }

                // Load the event's items
                var itemParams = new DynamicParameters();
                itemParams.Add("@EventId", id);
                itemParams.Add("@Action", "SELECT_BY_EVENT");

                var items = await connection.QueryAsync<EventItem>(
                    "sp_EventItemCRUD",
                    itemParams,
                    commandType: CommandType.StoredProcedure);

                evt.Items = items.ToList();

                response.Status = 200;
                response.ResponseMsg = "Event retrieved successfully.";
                response.Data = evt;
                return response;
            }
            catch (Exception ex)
            {
                response.Status = 500;
                response.ResponseMsg = ex.Message;
                return response;
            }
        }

        public async Task<Response> CreateEventAsync(Event model, int restaurantId)
        {
            var response = new Response();
            using var connection = _context.CreateConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                // 1. Insert the event
                var parameters = new DynamicParameters();
                parameters.Add("@RestaurantId", restaurantId);
                parameters.Add("@EventName", model.EventName);
                parameters.Add("@EventDate", model.EventDate);
                parameters.Add("@Location", model.Location);
                parameters.Add("@ContactName", model.ContactName);
                parameters.Add("@ContactEmail", model.ContactEmail);
                parameters.Add("@ContactPhone", model.ContactPhone);
                parameters.Add("@NumberOfGuests", model.NumberOfGuests);
                parameters.Add("@PricePerGuest", model.PricePerGuest);
                parameters.Add("@TotalFoodCost", model.TotalFoodCost);
                parameters.Add("@TotalRevenue", model.TotalRevenue);
                parameters.Add("@Action", "INSERT");

                int eventId = await connection.ExecuteScalarAsync<int>(
                    "sp_EventCRUD",
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    transaction: transaction);

                // 2. Insert the event items
                if (model.Items != null && model.Items.Any())
                {
                    foreach (var item in model.Items)
                    {
                        await InsertEventItemAsync(connection, transaction, eventId, item);
                    }
                }

                transaction.Commit();

                response.Status = 200;
                response.ResponseMsg = "Event created successfully.";
                response.Data = new { EventId = eventId };
                return response;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                response.Status = 500;
                response.ResponseMsg = ex.Message;
                return response;
            }
        }

        public async Task<Response> UpdateEventAsync(Event model, int restaurantId)
        {
            var response = new Response();
            using var connection = _context.CreateConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                // 1. Update the event
                var parameters = new DynamicParameters();
                parameters.Add("@EventId", model.EventId);
                parameters.Add("@EventName", model.EventName);
                parameters.Add("@EventDate", model.EventDate);
                parameters.Add("@Location", model.Location);
                parameters.Add("@ContactName", model.ContactName);
                parameters.Add("@ContactEmail", model.ContactEmail);
                parameters.Add("@ContactPhone", model.ContactPhone);
                parameters.Add("@NumberOfGuests", model.NumberOfGuests);
                parameters.Add("@PricePerGuest", model.PricePerGuest);
                parameters.Add("@TotalFoodCost", model.TotalFoodCost);
                parameters.Add("@TotalRevenue", model.TotalRevenue);
                parameters.Add("@ModifiedBy", restaurantId);
                parameters.Add("@Action", "UPDATE");

                await connection.ExecuteAsync(
                    "sp_EventCRUD",
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    transaction: transaction);

                // 2. Delete existing items
                await connection.ExecuteAsync(
                    "DELETE FROM EventItems WHERE EventId = @EventId",
                    new { EventId = model.EventId },
                    transaction: transaction);

                // 3. Insert new items
                if (model.Items != null && model.Items.Any())
                {
                    foreach (var item in model.Items)
                    {
                        await InsertEventItemAsync(connection, transaction, model.EventId, item);
                    }
                }

                transaction.Commit();

                response.Status = 200;
                response.ResponseMsg = "Event updated successfully.";
                return response;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                response.Status = 500;
                response.ResponseMsg = ex.Message;
                return response;
            }
        }

        public async Task<Response> DuplicateEventAsync(int id)
        {
            var response = new Response();
            using var connection = _context.CreateConnection();
            connection.Open();

            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@EventId", id);
                parameters.Add("@Action", "DUPLICATE");

                int newEventId = await connection.ExecuteScalarAsync<int>(
                    "sp_EventCRUD",
                    parameters,
                    commandType: CommandType.StoredProcedure);

                response.Status = 200;
                response.ResponseMsg = "Event duplicated successfully.";
                response.Data = new { EventId = newEventId };
                return response;
            }
            catch (Exception ex)
            {
                response.Status = 500;
                response.ResponseMsg = ex.Message;
                return response;
            }
        }

        public async Task<Response> GetEventNoteAsync(int eventId)
        {
            var response = new Response();
            using var connection = _context.CreateConnection();
            connection.Open();

            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@EventId", eventId);
                parameters.Add("@Action", "SELECT_BY_EVENT");

                var note = await connection.QuerySingleOrDefaultAsync<EventNoteDTO>(
                    "sp_EventNoteCRUD",
                    parameters,
                    commandType: CommandType.StoredProcedure);

                response.Status = 200;
                response.ResponseMsg = "Note retrieved successfully.";
                response.Data = note;
                return response;
            }
            catch (Exception ex)
            {
                response.Status = 500;
                response.ResponseMsg = ex.Message;
                return response;
            }
        }

        public async Task<Response> SaveEventNoteAsync(EventNoteDTO model)
        {
            var response = new Response();
            using var connection = _context.CreateConnection();
            connection.Open();

            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@EventId", model.EventId);
                parameters.Add("@NoteText", model.NoteText);
                parameters.Add("@Action", "UPSERT");

                int noteId = await connection.ExecuteScalarAsync<int>(
                    "sp_EventNoteCRUD",
                    parameters,
                    commandType: CommandType.StoredProcedure);

                response.Status = 200;
                response.ResponseMsg = "Note saved successfully.";
                response.Data = new { EventNoteId = noteId };
                return response;
            }
            catch (Exception ex)
            {
                response.Status = 500;
                response.ResponseMsg = ex.Message;
                return response;
            }
        }

        public async Task<Response> DeleteEventNoteAsync(int eventId)
        {
            var response = new Response();
            using var connection = _context.CreateConnection();
            connection.Open();

            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@EventId", eventId);
                parameters.Add("@Action", "DELETE");

                await connection.ExecuteAsync(
                    "sp_EventNoteCRUD",
                    parameters,
                    commandType: CommandType.StoredProcedure);

                response.Status = 200;
                response.ResponseMsg = "Note deleted successfully.";
                return response;
            }
            catch (Exception ex)
            {
                response.Status = 500;
                response.ResponseMsg = ex.Message;
                return response;
            }
        }

        /// <summary>
        /// Inserts a single event item, mapping either MenuItemId or EventOnlyItemId based on the item source.
        /// </summary>
        private static async Task InsertEventItemAsync(IDbConnection connection, IDbTransaction transaction, int eventId, EventItem item)
        {
            var itemParams = new DynamicParameters();
            itemParams.Add("@EventId", eventId);

            if (item.ItemSource == "Menu")
            {
                itemParams.Add("@MenuItemId", item.MenuItemId);
                itemParams.Add("@EventOnlyItemId", null);
            }
            else
            {
                itemParams.Add("@MenuItemId", null);
                itemParams.Add("@EventOnlyItemId", item.EventOnlyItemId);
            }

            itemParams.Add("@ItemName", item.ItemName ?? "");
            itemParams.Add("@Quantity", item.Qty);
            itemParams.Add("@QuantityType", item.QtyType);
            itemParams.Add("@UnitCost", item.UnitCost);
            itemParams.Add("@TotalCost", item.TotalCost);
            itemParams.Add("@Action", "INSERT");

            await connection.ExecuteAsync(
                "sp_EventItemCRUD",
                itemParams,
                commandType: CommandType.StoredProcedure,
                transaction: transaction);
        }
    }
}
