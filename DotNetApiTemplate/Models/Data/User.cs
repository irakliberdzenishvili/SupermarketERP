using Microsoft.AspNetCore.Identity;
using SupermarketERP.Models.Data;
using System.Text.Json.Serialization;

namespace DotNetApiTemplate.Models.Data
{
    public class User : IdentityUser
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;

        [JsonIgnore]
        public BranchWorker? BranchWorker { get; set; }
    }
}
