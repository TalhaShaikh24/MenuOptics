using Dapper;
using MenuOptics.Data;
using MenuOptics.Models;
using Newtonsoft.Json;
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
                parameters.Add("@Action", "SELECT");

                var evt = await connection.QuerySingleOrDefaultAsync<Event>(
                    "sp_EventCRUD",
                    parameters,
                    commandType: CommandType.StoredProcedure);

                if (evt != null)
                {
                    if (!string.IsNullOrWhiteSpace(evt.ItemsJson))
                    {
                        var items = JsonConvert.DeserializeObject<List<EventItem>>(evt.ItemsJson);
                        if (items != null)
                        {
                            evt.Items = items;
                        }
                    }
                }

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

            try
            {
                var itemsTable = new DataTable();
                itemsTable.Columns.Add("MenuItemId", typeof(int));
                itemsTable.Columns.Add("EventOnlyItemId", typeof(int));
                itemsTable.Columns.Add("Quantity", typeof(decimal));
                itemsTable.Columns.Add("UnitCost", typeof(decimal));
                itemsTable.Columns.Add("TotalCost", typeof(decimal));

                if (model.Items != null && model.Items.Any())
                {
                    foreach (var item in model.Items)
                    {
                        object menuItemId = (item.MenuItemId > 0) ? (object)item.MenuItemId : DBNull.Value;
                        object eventOnlyItemId = (item.EventOnlyItemId > 0) ? (object)item.EventOnlyItemId : DBNull.Value;

                        decimal qty = item.Qty != 0 ? item.Qty : item.Quantity;
                        decimal totalCost = item.TotalCost != 0 ? item.TotalCost : (qty * item.UnitCost);

                        itemsTable.Rows.Add(
                            menuItemId,
                            eventOnlyItemId,
                            qty,
                            item.UnitCost,
                            totalCost
                        );
                    }
                }

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
                parameters.Add("@Notes", model.Notes);
                parameters.Add("@CreatedBy", actualUserId);
                parameters.Add("@EventItems", itemsTable.AsTableValuedParameter("dbo.EventItemsType"));
                parameters.Add("@Action", "INSERT");

                int eventId = await connection.ExecuteScalarAsync<int>(
                    "sp_EventCRUD",
                    parameters,
                    commandType: CommandType.StoredProcedure);

                ResponseDto.Status = 200;
                ResponseDto.ResponseMsg = "Event created successfully.";
                ResponseDto.Data = new { EventId = eventId };
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

        public async Task<ResponseDto> UpdateEventAsync(Event model, int currentUserId, int actualUserId)
        {
            var ResponseDto = new ResponseDto();
            using var connection = _context.CreateConnection();
            connection.Open();

            try
            {
                var itemsTable = new DataTable();
                itemsTable.Columns.Add("MenuItemId", typeof(int));
                itemsTable.Columns.Add("EventOnlyItemId", typeof(int));
                itemsTable.Columns.Add("Quantity", typeof(decimal));
                itemsTable.Columns.Add("UnitCost", typeof(decimal));
                itemsTable.Columns.Add("TotalCost", typeof(decimal));

                if (model.Items != null && model.Items.Any())
                {
                    foreach (var item in model.Items)
                    {
                        object menuItemId = (item.MenuItemId > 0) ? (object)item.MenuItemId : DBNull.Value;
                        object eventOnlyItemId = (item.EventOnlyItemId > 0) ? (object)item.EventOnlyItemId : DBNull.Value;

                        decimal qty = item.Qty != 0 ? item.Qty : item.Quantity;
                        decimal totalCost = item.TotalCost != 0 ? item.TotalCost : (qty * item.UnitCost);

                        itemsTable.Rows.Add(
                            menuItemId,
                            eventOnlyItemId,
                            qty,
                            item.UnitCost,
                            totalCost
                        );
                    }
                }

                var parameters = new DynamicParameters();
                parameters.Add("@EventId", model.EventId);
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
                parameters.Add("@Notes", model.Notes);
                parameters.Add("@ModifiedBy", actualUserId);
                parameters.Add("@EventItems", itemsTable.AsTableValuedParameter("dbo.EventItemsType"));
                parameters.Add("@Action", "UPDATE");

                int eventId = await connection.ExecuteScalarAsync<int>(
                    "sp_EventCRUD",
                    parameters,
                    commandType: CommandType.StoredProcedure);
                ResponseDto.Status = 200;
                ResponseDto.ResponseMsg = "Event updated successfully.";
                ResponseDto.Data = new { EventId = eventId };
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

    }
}
