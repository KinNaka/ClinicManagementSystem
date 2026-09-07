using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Web.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
}