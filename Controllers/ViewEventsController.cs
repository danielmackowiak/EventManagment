﻿using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EventManagment.Data;
using EventManagment.Models;

namespace EventManagment.Controllers
{
    public class ViewEventsController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDbContext _context;

        public ViewEventsController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult>
        Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user != null)
            {
                var userInterests
                    = await _context.YourEvent.Where(pe => pe.UserId == user.Id)
                          .Select(pe => pe.EventId)
                          .ToListAsync();

                var events = await _context.Event.Include(e => e.User)
                                 .Where(e => !userInterests.Contains(e.Id))
                                 .ToListAsync();

                var eventviewer = events
                                      .Select(e => new ViewEvent
                                      {
                                          Event = e,
                                          User = e.User,
                                      })
                                      .ToList();

                ViewBag.Eventviewer = eventviewer;

                return View(eventviewer);
            }

            var allEvents = await _context.Event.Include(e => e.User).ToListAsync();

            var allEventviewer = allEvents
                                     .Select(e => new ViewEvent
                                     {
                                         Event = e,
                                         User = e.User,
                                     })
                                     .ToList();

            ViewBag.Eventviewer = allEventviewer;

            return View(allEventviewer);
        }

        public async Task<IActionResult>
        Interested(int eventId)
        {
            IdentityUser user = await _userManager.FindByNameAsync(User.Identity.Name);

            var existingInterest = await _context.YourEvent.FirstOrDefaultAsync(
                ui => ui.UserId == user.Id && ui.EventId == eventId);

            if (existingInterest == null)
            {
                var userInterest
                    = new YourEvent { UserId = user.Id, EventId = eventId };

                _context.YourEvent.Add(userInterest);
                await _context.SaveChangesAsync();
            }
            return View("Confirmation");
        }

        public async Task<IActionResult>
        Search(string searchString)
        {
            var user = await _userManager.GetUserAsync(User);

            var events = _context.Event.AsQueryable();

            if (!String.IsNullOrEmpty(searchString))
            {
                events = events.Where(e => e.Title.Contains(searchString)
                                            || e.Description.Contains(searchString)
                                            || e.Location.Contains(searchString)
                                            || e.User.UserName.Contains(searchString));
            }

            var userInterests
                = await _context.YourEvent.Where(pe => pe.UserId == user.Id)
                      .Select(pe => pe.EventId)
                      .ToListAsync();

            events = events.Where(e => !userInterests.Contains(e.Id));

            var searchedEvents = await events.Include(e => e.User).ToListAsync();

            var eventViewers
                = searchedEvents.Select(e => new ViewEvent { Event = e }).ToList();

            ViewBag.SearchString = searchString;

            return View("Index", eventViewers);
        }
    }
}