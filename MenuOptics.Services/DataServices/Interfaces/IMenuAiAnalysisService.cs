using MenuOptics.Models;
using System;
using System.Threading.Tasks;

namespace MenuOptics.Services
{
    public interface IMenuAiAnalysisService
    {
        Task<ResponseDto> GenerateMenuAiAnalysisAsync(int currentUserId, DateTime? startDate, DateTime? endDate);
        Task<ResponseDto> AnalyzeMenuPdfAsync(int currentUserId, MenuPdfAnalysisRequestDto request);
        Task<ResponseDto> GetMenuDesignDataAsync(int currentUserId);
        Task<ResponseDto> GenerateMenuDesignsAsync(ExtractedMenuDto menu);
        Task<ResponseDto> GenerateMenuFromIngredientsAsync(IngredientMenuRequestDto request);
    }
}
