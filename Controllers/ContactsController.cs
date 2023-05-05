using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AddressBook.Data;
using AddressBook.Models;
using Microsoft.AspNetCore.Authorization;
using AddressBook.Enums;
using Microsoft.AspNetCore.Identity;
using AddressBook.Services;
using AddressBook.Services.Interfaces;
using AddressBook.Models.ViewModels;

namespace AddressBook.Controllers
{
    [Authorize]
    public class ContactsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly IImageService _imageService;
        private readonly IAddressBookService _addressBookService;
        private readonly IEmailSender _emailService;

        public ContactsController(ApplicationDbContext context, UserManager<AppUser> userManager, IImageService imageService, IAddressBookService addressBookService, IEmailSender emailService)
        {
            _context = context;
            _userManager = userManager;
            _imageService = imageService;
            _addressBookService = addressBookService;
            _emailService = emailService;
        }

        // GET: Contacts
        public async Task<IActionResult> Index(int? categoryId, string? swalMessage = null)
        {
            ViewData["SwalMessage"] = swalMessage;

            string? appUserID = _userManager.GetUserId(User);

            List<Contact> contacts = new List<Contact>();
            contacts = await _context.Contacts.Where(c => c.AppUserId == appUserID).Include(c => c.Categories).ToListAsync();

            if(categoryId == null)
            {
                contacts = await _context.Contacts.Where(c => c.AppUserId == appUserID).Include(c => c.Categories).OrderBy(c => c.LastName).ThenBy(c => c.FirstName).ToListAsync();
            }
            else
            {
                contacts = (await _context.Categories.Where(c => c.AppUserId == appUserID).Include(c => c.Contacts).FirstOrDefaultAsync(c => c.Id == categoryId))!.Contacts.ToList();
            }

            ViewData["Categories"] = await GetCategoriesListAsync();

            return View(contacts);
        }

        public async Task<IActionResult> SearchContacts(string? searchString)
        {
            string? appUserID = _userManager.GetUserId(User);

            List<Contact>? contacts = new List<Contact>();

            AppUser? appUser = await _context.Users.Include(c=>c.Contacts).ThenInclude(c => c.Categories).FirstOrDefaultAsync(u=>u.Id == appUserID);

            if (string.IsNullOrEmpty(searchString))
            {
                contacts = appUser?.Contacts.OrderBy(c=>c.LastName).ThenBy(c=>c.FirstName).ToList();
            }
            else
            {
                contacts = appUser?.Contacts.Where(c => c.FullName!.ToLower().Contains(searchString.ToLower())).OrderBy(c => c.LastName).ThenBy(c => c.FirstName).ToList();
            }

            return View(nameof(Index), contacts);
        }

        // GET: Contacts/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Contacts == null)
            {
                return NotFound();
            }

            var contact = await _context.Contacts
                .Include(c => c.AppUser)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (contact == null)
            {
                return NotFound();
            }

            return View(contact);
        }

        [HttpGet]
        public async Task<IActionResult> EmailContact(int? id)
        {
            if(id == null)
            {
                return NotFound();
            }

            string? appUserId = _userManager.GetUserId(User);
            Contact? contact = await _context.Contacts.Where(c=>c.AppUserId == appUserId).FirstOrDefaultAsync(c=>c.Id == id);

            if(contact == null)
            {
                return NotFound();
            }

            EmailData emailData = new EmailData()
            {
                EmailAddress = contact.Email,
                FirstName = contact.FirstName,
                LastName = contact.LastName
            };

            EmailContactViewModel viewModel = new EmailContactViewModel()
            {
                Contact = contact,
                EmailData = emailData
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EmailContact(EmailContactViewModel viewModel)
        {
            if(ModelState.IsValid)
            {
                string? swalMessage = string.Empty;

                try
                {
                    string? emailAddress = viewModel.EmailData!.EmailAddress;
                    string? emailSubject = viewModel.EmailData!.EmailSubject;
                    string? emailBody = viewModel.EmailData!.EmailBody;
                    await _emailService.SendEmailAsync(emailAddress!, emailSubject!, emailBody!);

                    swalMessage = "Success: Email Sent!";
                    return RedirectToAction(nameof(Index), new {swalMessage});
                }
                catch (Exception)
                {
                    swalMessage = "Error: Email Failed to Send.";
                    return RedirectToAction(nameof(Index), new { swalMessage });
                    throw;
                }
            }

            return View(viewModel);
        }

        // GET: Contacts/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewData["CategoryList"] = await GetCategoriesListAsync();

            ViewData["StatesList"] = new SelectList(Enum.GetValues(typeof(States)).Cast<States>());

            Contact contact = new Contact();

            return View(contact);
        }

        // POST: Contacts/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("FirstName,LastName,DateOfBirth,Address1,Address2,City,State,ZipCode,Email,PhoneNumber,ImageFile")] Contact contact, IEnumerable<int> selected)
        {
            ModelState.Remove("AppUserId");

            if (ModelState.IsValid)
            {
                contact.AppUserId = _userManager.GetUserId(User);
                contact.CreatedDate = DateTime.UtcNow;

                if(contact.ImageFile != null)
                {
                    contact.ImageData = await _imageService.ConvertFileToByteArrayAsync(contact.ImageFile);
                    contact.ImageType = contact.ImageFile.ContentType;
                }

                if(contact.DateOfBirth != null)
                {
                    contact.DateOfBirth = DateTime.SpecifyKind(contact.DateOfBirth.Value, DateTimeKind.Utc);
                }

                _context.Add(contact);
                await _context.SaveChangesAsync();

                await _addressBookService.AddCategoriesToContactAsync(selected, contact.Id);

                return RedirectToAction(nameof(Index));
            }

            ViewData["CategoryList"] = await GetCategoriesListAsync(selected);
            ViewData["StatesList"] = new SelectList(Enum.GetValues(typeof(States)).Cast<States>());
            return View(contact);
        }

        // GET: Contacts/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Contacts == null)
            {
                return NotFound();
            }

            Contact? contact = await _context.Contacts.Include(c => c.Categories).FirstOrDefaultAsync(c=>c.Id == id);

            if (contact == null)
            {
                return NotFound();
            }

            ViewData["CategoryList"] = await GetCategoriesListAsync(contact.Categories.Select(c=>c.Id));
            ViewData["StatesList"] = new SelectList(Enum.GetValues(typeof(States)).Cast<States>());

            return View(contact);
        }

        // POST: Contacts/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,CreatedDate,AppUserId,FirstName,LastName,DateOfBirth,Address1,Address2,City,State,ZipCode,Email,PhoneNumber,ImageData,ImageType,ImageFile")] Contact contact, IEnumerable<int> selected)
        {
            if (id != contact.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    contact.CreatedDate = DateTime.SpecifyKind(contact.CreatedDate, DateTimeKind.Utc);

                    if (contact.DateOfBirth != null)
                    {
                        contact.DateOfBirth = DateTime.SpecifyKind(contact.DateOfBirth.Value, DateTimeKind.Utc);
                    }

                    _context.Update(contact);
                    await _context.SaveChangesAsync();

                    if(selected != null)
                    {
                        await _addressBookService.RemoveCategoriesFromContactAsync(contact.Id);
                        await _addressBookService.AddCategoriesToContactAsync(selected, contact.Id);
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ContactExists(contact.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            ViewData["CategoryList"] = await GetCategoriesListAsync(selected);
            ViewData["StatesList"] = new SelectList(Enum.GetValues(typeof(States)).Cast<States>());
            return View(contact);
        }

        // GET: Contacts/Delete/5
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Contacts == null)
            {
                return NotFound();
            }

            var contact = await _context.Contacts
                .Include(c => c.AppUser)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (contact == null)
            {
                return NotFound();
            }

            return View(contact);
        }

        // POST: Contacts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Contacts == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Contacts'  is null.");
            }
            var contact = await _context.Contacts.FindAsync(id);
            if (contact != null)
            {
                _context.Contacts.Remove(contact);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ContactExists(int id)
        {
          return (_context.Contacts?.Any(e => e.Id == id)).GetValueOrDefault();
        }

        private async Task<MultiSelectList> GetCategoriesListAsync(IEnumerable<int> categoryIds = null!)
        {
            string? appUserId = _userManager.GetUserId(User);

            IEnumerable<Category> categories = await _context.Categories.Where(c => c.AppUserId == appUserId).ToListAsync();

            return new MultiSelectList(categories, "Id", "Name", categoryIds);
        }
    }
}
