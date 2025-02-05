using Microsoft.AspNetCore.Mvc;
using AnimeShop.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace AnimeShop.Controllers
{
    public class CustomerController : Controller
    {
        private readonly AnimeShopContext _context;

        public CustomerController(AnimeShopContext context)
        {
            _context = context;
        }

        // GET: Customer/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            var (customerId, errorResult) = await ValidateCustomerId();
            if (errorResult != null) return errorResult;

            var customer = await _context.Customers
                .Include(c => c.Address)
                .Include(c => c.Payment)
                .FirstOrDefaultAsync(c => c.CustomerId == customerId);

            return customer == null ? NotFound() : View(customer);
        }

        // GET: Customer/Edit
        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var (customerId, errorResult) = await ValidateCustomerId();
            if (errorResult != null) return errorResult;

            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.CustomerId == customerId);

            return customer == null ? NotFound() : View(customer);
        }

        // POST: Customer/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Customer updatedCustomer)
        {
            var (customerId, errorResult) = await ValidateCustomerId();
            if (errorResult != null) return errorResult;

            if (!ModelState.IsValid)
            {
                return View(updatedCustomer);
            }

            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.CustomerId == customerId);

            if (customer == null)
            {
                return NotFound();
            }

            // Оновлення даних клієнта
            customer.Email = updatedCustomer.Email;
            customer.Phone = updatedCustomer.Phone;

            try
            {
                _context.Update(customer);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CustomerExists(customerId))
                {
                    return NotFound();
                }
                throw;
            }

            return RedirectToAction("Dashboard");
        }

        #region Helpers
        private async Task<(int CustomerId, IActionResult ErrorResult)> ValidateCustomerId()
        {
            if (!Request.Cookies.TryGetValue("CustomerId", out var customerIdStr) || 
                !int.TryParse(customerIdStr, out var customerId) || 
                customerId <= 0)
            {
                return (0, RedirectToAction("Login", "Auth"));
            }

            return (customerId, null);
        }

        private bool CustomerExists(int id)
        {
            return _context.Customers.Any(e => e.CustomerId == id);
        }
        #endregion
    }
}
