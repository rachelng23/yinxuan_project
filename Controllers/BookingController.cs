using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using yinxuan_project.Data;
using yinxuan_project.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace yinxuan_project.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class BookingController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<BookingController> _logger;

        public BookingController(ApplicationDbContext context, ILogger<BookingController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Admins and Members can view all bookings
        [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.User}")]
        [HttpGet("GetAll")]
        public IActionResult GetAll()
        {
            var bookings = _context.Bookings.ToList();

            if (bookings == null || !bookings.Any())
            {
                return Problem("No booking data available.", statusCode: 500);
            }

            return Ok(bookings);
        }

        // Admins and Members can view a specific booking by ID
        [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.User}")]
        [HttpGet("GetById/{id}")]
        public IActionResult GetById(int id)
        {
            var booking = _context.Bookings.FirstOrDefault(b => b.BookingID == id);

            if (booking == null)
            {
                return NotFound($"Booking with ID {id} not found.");
            }

            return Ok(booking);
        }

        // Admins and Members can create a new booking
        [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.User}")]
        [HttpPost("Create")]
        public async Task<IActionResult> PostAsync(Booking booking)
        {
            if (booking == null)
            {
                return BadRequest("Booking data is null.");
            }

            if (string.IsNullOrEmpty(booking.FacilityDescription) || string.IsNullOrEmpty(booking.BookedBy)
                || booking.BookingDateFrom == default || booking.BookingDateTo == default
                || booking.BookingDateFrom >= booking.BookingDateTo)
            {
                return BadRequest("Invalid booking data.");
            }

            // Calculate and assign dynamic price
            booking.Price = CalculateDynamicPrice(booking);
            _logger.LogInformation($"Calculated Price: {booking.Price}");

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = booking.BookingID }, booking);
        }

        // Admins and Members can update a booking
        [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.User}")]
        [HttpPut("Update/{id}")]
        public IActionResult Put(int id, Booking booking)
        {
            var entity = _context.Bookings.FirstOrDefault(b => b.BookingID == id);

            if (entity == null)
            {
                return NotFound($"Booking with ID {id} not found.");
            }

            if (string.IsNullOrEmpty(booking.FacilityDescription) || string.IsNullOrEmpty(booking.BookedBy)
                || booking.BookingDateFrom == default || booking.BookingDateTo == default
                || booking.BookingDateFrom >= booking.BookingDateTo)
            {
                return BadRequest("Invalid booking data.");
            }

            entity.FacilityDescription = booking.FacilityDescription;
            entity.BookingDateFrom = booking.BookingDateFrom;
            entity.BookingDateTo = booking.BookingDateTo;
            entity.BookedBy = booking.BookedBy;
            entity.BookingStatus = booking.BookingStatus;

            // Recalculate and assign dynamic price
            entity.Price = CalculateDynamicPrice(booking);
            _logger.LogInformation($"Updated Price: {entity.Price}");

            _context.SaveChanges();

            return Ok(entity);
        }

        // Admins and Members can delete a booking
        [Authorize(Roles = UserRoles.Admin)]
        [HttpDelete("Delete/{id}")]
        public IActionResult Delete(int id)
        {
            if (id <= 0)
            {
                return BadRequest("Invalid ID. The ID must be a positive integer.");
            }

            var entity = _context.Bookings.FirstOrDefault(b => b.BookingID == id);

            if (entity == null)
            {
                return NotFound($"Booking with ID {id} not found.");
            }

            _context.Bookings.Remove(entity);
            _context.SaveChanges();

            return Ok(entity);
        }

        // New API Endpoint: Check Facility Availability
        [Authorize(Roles = $"{UserRoles.Admin}")]
        [HttpGet("CheckAvailability")]
        public async Task<IActionResult> CheckAvailability(string facilityDescription, DateTime bookingDateFrom, DateTime bookingDateTo)
        {
            if (string.IsNullOrEmpty(facilityDescription) || bookingDateFrom == default || bookingDateTo == default)
            {
                return BadRequest("Invalid parameters.");
            }

            if (bookingDateFrom >= bookingDateTo)
            {
                return BadRequest("Invalid date range.");
            }

            var overlappingBookings = await _context.Bookings
                .Where(b => b.FacilityDescription == facilityDescription &&
                            ((b.BookingDateFrom < bookingDateTo && b.BookingDateTo > bookingDateFrom) ||
                             (b.BookingDateFrom == bookingDateFrom && b.BookingDateTo == bookingDateTo)))
                .ToListAsync();

            if (overlappingBookings.Any())
            {
                return Ok(new { Available = false, Message = "Facility is already booked for the specified date range." });
            }

            return Ok(new { Available = true, Message = "Facility is available for booking." });
        }

        // New API Endpoint: Calculate Price
        [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.User}")]
        [HttpGet("CalculatePrice")]
        public IActionResult CalculatePrice(string facilityDescription, DateTime bookingDateFrom, DateTime bookingDateTo)
        {
            if (string.IsNullOrEmpty(facilityDescription) || bookingDateFrom == default || bookingDateTo == default)
            {
                return BadRequest("Invalid parameters.");
            }

            if (bookingDateFrom >= bookingDateTo)
            {
                return BadRequest("Invalid date range.");
            }

            decimal price = CalculateDynamicPrice(new Booking
            {
                FacilityDescription = facilityDescription,
                BookingDateFrom = bookingDateFrom,
                BookingDateTo = bookingDateTo
            });

            return Ok(new { price });
        }

        // Calculate dynamic price based on pricing rules
        private decimal CalculateDynamicPrice(Booking booking)
        {
            _logger.LogInformation("CalculateDynamicPrice method called.");
            _logger.LogInformation($"Booking Facility: {booking.FacilityDescription}");

            // Define base prices for each facility
            var facilityBasePrices = new Dictionary<string, decimal>
            {
                { "Karaoke Rooms", 25 },
                { "Private Movie Room", 30 },
                { "Gaming Lounges", 30 },
                { "Virtual Reality (VR) Rooms", 35 }
            };

            if (!facilityBasePrices.TryGetValue(booking.FacilityDescription, out decimal basePrice))
            {
                basePrice = 20; // Default base price if the facility is not listed
            }

            // Define the surcharge start and end times
            TimeSpan surchargeStartTime = new TimeSpan(20, 30, 0); // 8:30 PM
            TimeSpan surchargeEndTime = new TimeSpan(3, 0, 0);     // 3:00 AM

            decimal totalPrice = 0;
            DateTime current = booking.BookingDateFrom;

            while (current < booking.BookingDateTo)
            {
                DateTime nextHour = current.AddHours(1);
                if (nextHour > booking.BookingDateTo)
                {
                    nextHour = booking.BookingDateTo;
                }

                TimeSpan currentTimeOfDay = current.TimeOfDay;
                bool isSurcharge = (currentTimeOfDay >= surchargeStartTime || currentTimeOfDay < surchargeEndTime);

                decimal hourlyPrice = basePrice;
                if (isSurcharge)
                {
                    hourlyPrice *= 1.2m; // Apply a 20% surcharge
                }

                decimal hoursInThisPeriod = (decimal)(nextHour - current).TotalHours;
                totalPrice += hourlyPrice * hoursInThisPeriod;

                current = nextHour;
            }

            return totalPrice;
        }
    }
}
