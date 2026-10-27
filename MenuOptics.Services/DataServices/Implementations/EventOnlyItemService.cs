using Dapper;
using MenuOptics.Data;
using MenuOptics.Models;
using MenuOptics.Models.DTOs;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Data.SqlClient;

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

                var eventOnlyItems = await connection.QueryAsync<GetEventOnlyItemDTO>(
                    "sp_EventOnlyItemCRUD",
                    parameters,
                    commandType: CommandType.StoredProcedure);

                ResponseDto.Status = 200;
                ResponseDto.ResponseMsg = "Event items retrieved successfully.";
                ResponseDto.Data = eventOnlyItems;
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

        public async Task<ResponseDto> CreateAsync(CreateEventOnlyItemDTO model, int restaurantId)
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
                parameters.Add("@Cost", model.Cost);
                parameters.Add("@SellingPrice", model.SellingPrice);

                var id = await connection.ExecuteScalarAsync<int>(
                    "sp_EventOnlyItemCRUD",
                    parameters,
                    commandType: CommandType.StoredProcedure);

                var res = new GetEventOnlyItemDTO
                {
                    EventOnlyItemId = id,
                    Name = model.Name,
                    Cost = model.Cost,
                    SellingPrice = model.SellingPrice
                };

                ResponseDto.Status = 200;
                ResponseDto.ResponseMsg = "Event item created successfully.";
                ResponseDto.Data = res;

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
