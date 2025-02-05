using Microsoft.AspNetCore.Mvc;
using AnimeShop.Models;
using Microsoft.EntityFrameworkCore;

namespace AnimeShop.Controllers
{
    public class OrderController : Controller
    {
        private readonly AnimeShopContext _context;

        public OrderController(AnimeShopContext context)
        {
            _context = context;
        }

        public IActionResult CreateOrder()
        {
            var (customerId, errorResult) = GetCustomerId();
            if (errorResult != null) return errorResult;

            var order = _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefault(o => o.CustomerId == customerId && o.Status == "нове");

            if (order == null || !order.OrderItems.Any())
            {
                return RedirectToAction("Index", "Cart");
            }

            try
            {
                order.Status = "оплачене";
                order.TotalAmount = order.OrderItems.Sum(oi => oi.Quantity * oi.PricePerUnit);
                _context.SaveChanges();

                var newOrder = CreateNewOrder(customerId);
                _context.Orders.Add(newOrder);
                _context.SaveChanges();

                return RedirectToAction("OrderConfirmation", new { orderId = order.OrdersId });
            }
            catch (Exception ex)
            {
                // Логування помилки (наприклад, через ILogger)
                return RedirectToAction("Error", "Home");
            }
        }

        public IActionResult OrderConfirmation(int orderId)
        {
            var order = _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefault(o => o.OrdersId == orderId);

            if (order == null)
            {
                return RedirectToAction("Index", "Products");
            }

            return View(order);
        }

        public IActionResult OrderHistory()
        {
            var (customerId, errorResult) = GetCustomerId();
            if (errorResult != null) return errorResult;

            var orders = _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Where(o => o.CustomerId == customerId)
                .OrderByDescending(o => o.OrderDate)
                .ToList();

            return View(orders);
        }

        #region Helpers
        private (int CustomerId, IActionResult ErrorResult) GetCustomerId()
        {
            if (!Request.Cookies.TryGetValue("CustomerId", out var customerIdStr) ||
                !int.TryParse(customerIdStr, out var customerId) ||
                customerId <= 0)
            {
                return (0, RedirectToAction("Login", "Auth"));
            }

            return (customerId, null);
        }

        private Order CreateNewOrder(int customerId)
        {
            var maxOrderId = _context.Orders.Any()
                ? _context.Orders.Max(o => o.OrdersId)
                : 0;

            return new Order
            {
                OrdersId = maxOrderId + 1,
                CustomerId = customerId,
                OrderDate = DateOnly.FromDateTime(DateTime.Now),
                Status = "нове"
            };
        }
        #endregion
    }
}
