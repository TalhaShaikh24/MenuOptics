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

        public async Task<IEnumerable<EventOnlyItemDTO>> GetAllAsync(int restaurantId)
        {
            using var connection = _context.CreateConnection();

            return await connection.QueryAsync<EventOnlyItemDTO>(
                "sp_EventOnlyItemCRUD",
                new { Action = "SELECT_ALL", RestaurantId = restaurantId },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<int> CreateAsync(EventOnlyItemDTO model, int restaurantId)
        {
            using var connection = _context.CreateConnection();

            return await connection.ExecuteScalarAsync<int>(
                "sp_EventOnlyItemCRUD",
                new { Action = "INSERT", RestaurantId = restaurantId, Name = model.Name, DefaultCost = model.DefaultCost },
                commandType: CommandType.StoredProcedure
            );
        }
    }
}
