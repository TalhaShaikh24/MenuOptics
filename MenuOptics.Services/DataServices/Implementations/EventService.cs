using Dapper;
using MenuOptics.Data;
using MenuOptics.Models;
using System.Data;
using System.Data.SqlClient;

namespace MenuOptics.Services
{
    public class EventService : IEventService
    {
        private readonly DapperContext _context;

        public EventService(DapperContext context)
        {
            _context = context;
        }

        public async Task<ResponseDto> GetAllEventsAsync(int currentUserId)
        {
            var ResponseDto = new ResponseDto();
            using var connection = _context.CreateConnection();
            connection.Open();

            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@Action", "SELECT_ALL");
                parameters.Add("@RestaurantId", currentUserId);

                var events = await connection.QueryAsync<Event>(
                    "sp_EventCRUD",
                    parameters,
                    commandType: CommandType.StoredProcedure);

                ResponseDto.Status = 200;
                ResponseDto.ResponseMsg = "Events retrieved successfully.";
                ResponseDto.Data = events;
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

        public async Task<ResponseDto> GetEventAsync(int id, int currentUserId)
        {
            var ResponseDto = new ResponseDto();
            using var connection = _context.CreateConnection();
            connection.Open();

            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@EventId", id);
                parameters.Add("@RestaurantId", currentUserId);
                parameters.Add("@Action", "SELECT_BY_ID");

                var evt = await connection.QuerySingleOrDefaultAsync<Event>(
                    "sp_EventCRUD",
                    parameters,
                    commandType: CommandType.StoredProcedure);


                // Load the event's items
                var itemParams = new DynamicParameters();
                itemParams.Add("@EventId", id);
                itemParams.Add("@Action", "SELECT_BY_EVENT");

                var items = await connection.QueryAsync<EventItem>(
                    "sp_EventItemCRUD",
                    itemParams,
                    commandType: CommandType.StoredProcedure);

                evt.Items = items.ToList();

                ResponseDto.Status = 200;
                ResponseDto.ResponseMsg = "Event retrieved successfully.";
                ResponseDto.Data = evt;
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

        public async Task<ResponseDto> CreateEventAsync(Event model, int currentUserId, int actualUserId)
        {
            var ResponseDto = new ResponseDto();
            using var connection = _context.CreateConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                // 1. Insert the event
                var parameters = new DynamicParameters();
                parameters.Add("@RestaurantId", currentUserId);
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
                parameters.Add("@CreatedBy", actualUserId);
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

                ResponseDto.Status = 200;
                ResponseDto.ResponseMsg = "Event created successfully.";
                ResponseDto.Data = new { EventId = eventId };
                return ResponseDto;
            }
            catch (SqlException sqlEx)
            {
                transaction.Rollback();
                ResponseDto.Status = 400;
                ResponseDto.ResponseMsg = sqlEx.Message;
                return ResponseDto;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                ResponseDto.Status = 500;
                ResponseDto.ResponseMsg = $"{ex.Message}";
                return ResponseDto;
            }
            finally
            {
                connection.Close();
            }
        }

        public async Task<ResponseDto> UpdateEventAsync(Event model, int currentUserId, int actualUserId)
        {
            var ResponseDto = new ResponseDto();
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
                parameters.Add("@ModifiedBy", actualUserId);
                parameters.Add("@Action", "UPDATE");

                await connection.ExecuteAsync(
                    "sp_EventCRUD",
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    transaction: transaction);

                // 2. Delete existing items
                var parameters2 = new DynamicParameters();
                parameters2.Add("@EventId", model.EventId);
                parameters2.Add("@Action", "DELETE_BY_EVENT");
                await connection.ExecuteAsync(
                   "sp_EventItemCRUD",
                   parameters2,
                   commandType: CommandType.StoredProcedure,
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

                ResponseDto.Status = 200;
                ResponseDto.ResponseMsg = "Event updated successfully.";
                return ResponseDto;
            }
            catch (SqlException sqlEx)
            {
                transaction.Rollback();
                ResponseDto.Status = 400;
                ResponseDto.ResponseMsg = sqlEx.Message;
                return ResponseDto;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                ResponseDto.Status = 500;
                ResponseDto.ResponseMsg = $"{ex.Message}";
                return ResponseDto;
            }
            finally
            {
                connection.Close();
            }
        }

        public async Task<ResponseDto> DuplicateEventAsync(int id)
        {
            var ResponseDto = new ResponseDto();
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

                ResponseDto.Status = 200;
                ResponseDto.ResponseMsg = "Event duplicated successfully.";
                ResponseDto.Data = new { EventId = newEventId };
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

        public async Task<ResponseDto> GetEventNoteAsync(int eventId)
        {
            var ResponseDto = new ResponseDto();
            using var connection = _context.CreateConnection();
            connection.Open();

            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@EventId", eventId);
                parameters.Add("@Action", "SELECT_BY_EVENT");

                var note = await connection.QuerySingleOrDefaultAsync<EventNoteDto>(
                    "sp_EventNoteCRUD",
                    parameters,
                    commandType: CommandType.StoredProcedure);

                ResponseDto.Status = 200;
                ResponseDto.ResponseMsg = "Note retrieved successfully.";
                ResponseDto.Data = note;
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

        public async Task<ResponseDto> SaveEventNoteAsync(EventNoteDto model)
        {
            var ResponseDto = new ResponseDto();
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

                ResponseDto.Status = 200;
                ResponseDto.ResponseMsg = "Note saved successfully.";
                ResponseDto.Data = new { EventNoteId = noteId };
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

        public async Task<ResponseDto> DeleteEventNoteAsync(int eventId)
        {
            var ResponseDto = new ResponseDto();
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

                ResponseDto.Status = 200;
                ResponseDto.ResponseMsg = "Note deleted successfully.";
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

        public async Task<ResponseDto> ToggleEventActiveAsync(int id, int currentUserId, int actualUserId)
        {
            var ResponseDto = new ResponseDto();
            using var connection = _context.CreateConnection();
            connection.Open();

            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@EventId", id);
                parameters.Add("@ModifiedBy", actualUserId);
                parameters.Add("@Action", "TOGGLE_ACTIVE");

                var result = await connection.QueryFirstAsync<Event>("sp_EventCRUD",parameters,commandType: CommandType.StoredProcedure);

                ResponseDto.Status = 200;
                ResponseDto.ResponseMsg = $"Event {(result.IsActive ? "activated" : "deactivated")} successfully.";
                ResponseDto.Data = new { EventId = id, IsActive = result.IsActive };
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
