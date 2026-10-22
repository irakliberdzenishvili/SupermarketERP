
using DotNetApiTemplate.DB;
using Microsoft.EntityFrameworkCore;

namespace SupermarketERP.Helpers
{
    public class WarehouseHelper(ApplicationDbContext db)
    {
        public async Task<string> GenerateWarehouseCode()
        {
            var codes = await db.Warehouses
                .IgnoreQueryFilters()
                .Where(w => w.Code.StartsWith("WH-"))
                .Select(w => w.Code)
                .ToListAsync();

            var lastNumber = codes
                .Select(code => int.TryParse(
                    code.Replace("WH-", ""), out var number)
                    ? number
                    : 0)
                .DefaultIfEmpty(0)
                .Max();

            return $"WH-{lastNumber + 1:D6}";
        }
    }
}
