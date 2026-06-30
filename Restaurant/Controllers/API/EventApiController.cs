using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MenuOptics.Data;
using MenuOptics.Models;
using System.Data;
using System.Security.Claims;

namespace MenuOptics.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,Restaurant")]
    public class EventApiController : ControllerBase
    {
        private readonly DapperContext _context;

        public EventApiController(DapperContext context)
        {
            _context = context;
        }

        // 🔹 GET ALL EVENTS
        [HttpGet("GetAllEvents")]
        public async Task<IActionResult> GetAllEvents()
        {
            var response = new Response();
            using var connection = _context.CreateConnection();
            connection.Open();

            try
            {
                var currentRestaurantId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                var parameters = new DynamicParameters();
                parameters.Add("@Action", "SELECT_ALL");
                parameters.Add("@RestaurantId", currentRestaurantId);

                var events = await connection.QueryAsync<Event>(
                    "sp_EventCRUD",
                    parameters,
                    commandType: CommandType.StoredProcedure);

                response.Status = 200;
                response.ResponseMsg = "Events retrieved successfully.";
                response.Data = events;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Status = 500;
                response.ResponseMsg = ex.Message;
                return StatusCode(500, response);
            }
        }

        // 🔹 GET EVENT BY ID (WITH ITEMS)
        [HttpGet("GetEvent/{id}")]
        public async Task<IActionResult> GetEvent(int id)
        {
            var response = new Response();
            using var connection = _context.CreateConnection();
            connection.Open();

            try
            {
                var currentRestaurantId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                var parameters = new DynamicParameters();
                parameters.Add("@EventId", id);
                parameters.Add("@RestaurantId", currentRestaurantId);
                parameters.Add("@Action", "SELECT_BY_ID");

                var evt = await connection.QuerySingleOrDefaultAsync<Event>(
                    "sp_EventCRUD",
                    parameters,
                    commandType: CommandType.StoredProcedure);

                if (evt == null)
                {
                    response.Status = 404;
                    response.ResponseMsg = "Event not found.";
                    return NotFound(response);
                }

                // 🔹 GET EVENT ITEMS
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
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Status = 500;
                response.ResponseMsg = ex.Message;
                return StatusCode(500, response);
            }
        }

        // 🔹 CREATE EVENT (WITH ITEMS)
        [HttpPost("CreateEvent")]
        public async Task<IActionResult> CreateEvent([FromBody] Event model)
        {
            var response = new Response();
            using var connection = _context.CreateConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                var restaurantId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                // 1️⃣ INSERT EVENT
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

                // 2️⃣ INSERT EVENT ITEMS
                if (model.Items != null && model.Items.Any())
                {
                    foreach (var item in model.Items)
                    {
                        var itemParams = new DynamicParameters();
                        itemParams.Add("@EventId", eventId); // or model.EventId for Update

                        // 🔹 LOGIC CHANGE: Check Source
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

                transaction.Commit();

                response.Status = 200;
                response.ResponseMsg = "Event created successfully.";
                response.Data = new { EventId = eventId };
                return Ok(response);
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                response.Status = 500;
                response.ResponseMsg = ex.Message;
                return StatusCode(500, response);
            }
        }

        // 🔹 UPDATE EVENT (WITH ITEMS)
        [HttpPut("UpdateEvent")]
        public async Task<IActionResult> UpdateEvent([FromBody] Event model)
        {
            var response = new Response();
            using var connection = _context.CreateConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                var restaurantId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                // 1️⃣ UPDATE EVENT
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

                // 2️⃣ DELETE EXISTING ITEMS
                await connection.ExecuteAsync(
                    "DELETE FROM EventItems WHERE EventId = @EventId",
                    new { EventId = model.EventId },
                    transaction: transaction);

                // 3️⃣ INSERT NEW ITEMS
                if (model.Items != null && model.Items.Any())
                {
                    foreach (var item in model.Items)
                    {
                        var itemParams = new DynamicParameters();
                        itemParams.Add("@EventId", model.EventId); // or model.EventId for Update

                        // 🔹 LOGIC CHANGE: Check Source
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

                transaction.Commit();

                response.Status = 200;
                response.ResponseMsg = "Event updated successfully.";
                return Ok(response);
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                response.Status = 500;
                response.ResponseMsg = ex.Message;
                return StatusCode(500, response);
            }
        }

        // 🔹 DUPLICATE EVENT
        [HttpPost("DuplicateEvent/{id}")]
        public async Task<IActionResult> DuplicateEvent(int id)
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
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Status = 500;
                response.ResponseMsg = ex.Message;
                return StatusCode(500, response);
            }
        }

        // 🔹 GET EVENT NOTE
        [HttpGet("GetEventNote/{eventId}")]
        public async Task<IActionResult> GetEventNote(int eventId)
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
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Status = 500;
                response.ResponseMsg = ex.Message;
                return StatusCode(500, response);
            }
        }

        // 🔹 SAVE EVENT NOTE
        [HttpPost("SaveEventNote")]
        public async Task<IActionResult> SaveEventNote([FromBody] EventNoteDTO model)
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
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Status = 500;
                response.ResponseMsg = ex.Message;
                return StatusCode(500, response);
            }
        }

        // 🔹 DELETE EVENT NOTE
        [HttpDelete("DeleteEventNote/{eventId}")]
        public async Task<IActionResult> DeleteEventNote(int eventId)
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
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.Status = 500;
                response.ResponseMsg = ex.Message;
                return StatusCode(500, response);
            }
        }
    }
}