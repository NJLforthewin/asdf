﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Hotel_Management_System.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Hotel_Management_System.Controllers
{
    [Authorize(Roles = "Admin, FrontDesk")]
    public class BookingController : Controller
    {
        private readonly HotelManagementDbContext _context;

        public BookingController(HotelManagementDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Create()
        {
            var rooms = _context.Rooms.ToList();
            ViewBag.Rooms = rooms;
            ViewBag.RoomPrices = rooms.ToDictionary(r => r.RoomId.ToString(), r => r.PricePerNight);
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Booking booking)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Rooms = _context.Rooms.ToList();
                ViewBag.RoomPrices = _context.Rooms.ToDictionary(r => r.RoomId.ToString(), r => r.PricePerNight);
                return View(booking);
            }

            var room = _context.Rooms.FirstOrDefault(r => r.RoomId == booking.RoomId);
            if (room == null)
            {
                ModelState.AddModelError("RoomId", "Invalid Room ID.");
                ViewBag.Rooms = _context.Rooms.ToList();
                ViewBag.RoomPrices = _context.Rooms.ToDictionary(r => r.RoomId.ToString(), r => r.PricePerNight);
                return View(booking);
            }

            if (booking.CheckInDate >= booking.CheckOutDate)
            {
                ModelState.AddModelError("", "Check-in date must be before Check-out date.");
                ViewBag.Rooms = _context.Rooms.ToList();
                ViewBag.RoomPrices = _context.Rooms.ToDictionary(r => r.RoomId.ToString(), r => r.PricePerNight);
                return View(booking);
            }

            booking.GuestName ??= "Guest";
            booking.TotalPrice = CalculateTotalPrice(room.PricePerNight, booking.CheckInDate, booking.CheckOutDate);
            booking.Status = "Pending";

            _context.Bookings.Add(booking);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Booking submitted and is now pending confirmation.";
            return RedirectToAction("Index", "Home");
        }

        private static decimal CalculateTotalPrice(decimal pricePerNight, DateTime checkInDate, DateTime checkOutDate)
        {
            int totalDays = (checkOutDate - checkInDate).Days;
            return totalDays > 0 ? pricePerNight * totalDays : pricePerNight;
        }

        public IActionResult DashboardBooking()
        {
            var bookings = _context.Bookings.Include(b => b.Room).ToList();
            return RedirectToAction("Dashboard", "FrontDesk");
        }

        [HttpPost]
        public IActionResult ConfirmBooking(int bookingId)
        {
            var booking = _context.Bookings.FirstOrDefault(b => b.BookingId == bookingId);
            if (booking == null) return NotFound();

            booking.Status = "Confirmed";
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Booking confirmed!";
            return RedirectToAction("Dashboard", "FrontDesk");
        }

        [HttpPost]
        public IActionResult CancelBooking(int bookingId)
        {
            var booking = _context.Bookings.FirstOrDefault(b => b.BookingId == bookingId);
            if (booking == null) return NotFound();

            booking.Status = "Pending";
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Booking status reverted to Pending.";
            return RedirectToAction("Dashboard", "FrontDesk");
        }

        [HttpPost]
        public IActionResult ConfirmCheckIn(int bookingId)
        {
            var booking = _context.Bookings.FirstOrDefault(b => b.BookingId == bookingId);
            if (booking == null || booking.Status != "Confirmed")
                return NotFound();

            booking.Status = "Checked-In";
            booking.CheckedInAt = DateTime.Now;

            var room = _context.Rooms.FirstOrDefault(r => r.RoomId == booking.RoomId);
            if (room != null)
                room.Status = "Occupied";

            _context.SaveChanges();

            TempData["SuccessMessage"] = "Guest has been checked in successfully!";
            return RedirectToAction("Dashboard", "FrontDesk");
        }

        [HttpPost]
        public IActionResult ConfirmCheckOut(int bookingId)
        {
            var booking = _context.Bookings.FirstOrDefault(b => b.BookingId == bookingId);
            if (booking == null || booking.Status != "Checked-In")
                return NotFound();

            booking.Status = "Checked-Out";
            booking.CheckedOutAt = DateTime.Now;

            var room = _context.Rooms.FirstOrDefault(r => r.RoomId == booking.RoomId);
            if (room != null)
                room.Status = "Vacant";

            _context.SaveChanges();

            TempData["SuccessMessage"] = "Guest has been checked out successfully!";
            return RedirectToAction("Dashboard", "FrontDesk");
        }
    }
}
