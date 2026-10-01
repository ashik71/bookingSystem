using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Booking.Infrastructure;

/// <summary>
/// The module's single registration point. The host calls this and nothing else,
/// so it never needs to know what is inside the module.
/// </summary>
public static class BookingModule
{
    public static IServiceCollection AddBookingModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register this module's services here:
        //   - persistence (DbContext / session factory)
        //   - repositories
        //   - application use-case handlers
        //   - implementations of this module's Contracts interfaces
        return services;
    }
}
