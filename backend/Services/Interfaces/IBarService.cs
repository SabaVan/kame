using backend.Models;

namespace backend.Services.Interfaces
{
    public interface IBarService
    {
        Task<Bar?> GetDefaultBar();
    }
}    