using Dapper;
using MenuOptics.Data;
using MenuOptics.Models;
using System.Data;

namespace MenuOptics.Services
{
    public class EventOnlyItemService : IEventOnlyItemService
    {
        private readonly DapperContext _context;

        public EventOnlyItemService(DapperContext context)
        {
            _context = context;
        }

        public async Task<ResponseDto> GetAllAsync(int restaurantId)
        {
            var ResponseDto = new ResponseDto();

            using var connection = _context.CreateConnection();
            connection.Open();

            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@Action", "SELECT_ALL");
                parameters.Add("@RestaurantId", restaurantId);

                var eventOnlyItems = await connection.QueryAsync<EventOnlyItemDto>(
                    "sp_EventOnlyItemCRUD",
                    parameters,
                    commandType: CommandType.StoredProcedure);

                ResponseDto.Status = 200;
                ResponseDto.ResponseMsg = "Event items retrieved successfully.";
                ResponseDto.Data = eventOnlyItems;
                return ResponseDto;
            }
            catch (Exception ex)
            {
                ResponseDto.Status = 500;
                ResponseDto.ResponseMsg = ex.Message;
                return ResponseDto;
            }
        }

        public async Task<ResponseDto> CreateAsync(EventOnlyItemDto model, int restaurantId)
        {
            var ResponseDto = new ResponseDto();

            using var connection = _context.CreateConnection();
            connection.Open();

            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@Action", "INSERT");
                parameters.Add("@RestaurantId", restaurantId);
                parameters.Add("@Name", model.Name);
                parameters.Add("@DefaultCost", model.DefaultCost);

                var id = await connection.ExecuteScalarAsync<int>(
                    "sp_EventOnlyItemCRUD",
                    parameters,
                    commandType: CommandType.StoredProcedure);

                model.EventOnlyItemId = id;

                ResponseDto.Status = 200;
                ResponseDto.ResponseMsg = "Event item created successfully.";
                ResponseDto.Data = model;

                return ResponseDto;
            }
            catch (Exception ex)
            {
                ResponseDto.Status = 500;
                ResponseDto.ResponseMsg = ex.Message;

                return ResponseDto;
            }
        }
    }
}
