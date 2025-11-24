using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using AmusementParkAPI.Data;
using AmusementParkAPI.Models;

namespace AmusementParkAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CartController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CartController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/cart/{visitorId}
        [HttpGet("{visitorId}")]
        public async Task<ActionResult<IEnumerable<object>>> GetCart(int visitorId)
        {
            var cartItems = await _context.ShoppingCarts
                .Where(c => c.VisitorId == visitorId)
                .Join(_context.CommodityTypes,
                    cart => cart.CommodityTypeId,
                    commodity => commodity.Commodity_TypeID,
                    (cart, commodity) => new
                    {
                        cartId = cart.CartId,
                        commodityTypeId = cart.CommodityTypeId,
                        commodityName = commodity.Commodity_Name,
                        basePrice = commodity.Base_Price,
                        quantity = cart.Quantity,
                        size = cart.Size,
                        stockQuantity = commodity.Stock_Quantity,
                        imageUrl = commodity.Image_Url,
                        addedAt = cart.AddedAt
                    })
                .OrderByDescending(c => c.addedAt)
                .ToListAsync();

            return Ok(cartItems);
        }

        // POST: api/cart/add
        [HttpPost("add")]
        public async Task<ActionResult> AddToCart([FromBody] AddToCartDto request)
        {
            try
            {
                // Check if item with same size already exists in cart
                var existingItem = await _context.ShoppingCarts
                    .FirstOrDefaultAsync(c =>
                        c.VisitorId == request.VisitorId &&
                        c.CommodityTypeId == request.CommodityTypeId &&
                        c.Size == request.Size);

                if (existingItem != null)
                {
                    // Update quantity - trigger will validate stock
                    existingItem.Quantity += request.Quantity;
                    await _context.SaveChangesAsync();

                    return Ok(new { message = "Cart updated successfully", cartId = existingItem.CartId });
                }
                else
                {
                    // Add new item - trigger will validate stock
                    var cartItem = new ShoppingCart
                    {
                        VisitorId = request.VisitorId,
                        CommodityTypeId = request.CommodityTypeId,
                        Quantity = request.Quantity,
                        Size = request.Size,
                        AddedAt = DateTime.Now
                    };

                    _context.ShoppingCarts.Add(cartItem);
                    await _context.SaveChangesAsync();

                    return Ok(new { message = "Added to cart successfully", cartId = cartItem.CartId });
                }
            }
            catch (DbUpdateException ex)
            {
                // Database trigger threw an error
                var innerMessage = ex.InnerException?.Message ?? ex.Message;

                // Extract the custom error message from the trigger
                if (innerMessage.Contains("out of stock") ||
                    innerMessage.Contains("Cannot add") ||
                    innerMessage.Contains("available in stock") ||
                    innerMessage.Contains("Quantity must be greater than 0"))
                {
                    return BadRequest(new { message = innerMessage });
                }

                return BadRequest(new { message = "Failed to add to cart: " + innerMessage });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred: " + ex.Message });
            }
        }

        // PUT: api/cart/{cartId}
        [HttpPut("{cartId}")]
        public async Task<ActionResult> UpdateCartItem(int cartId, [FromBody] UpdateCartDto request)
        {
            try
            {
                var cartItem = await _context.ShoppingCarts.FindAsync(cartId);
                if (cartItem == null)
                {
                    return NotFound(new { message = "Cart item not found" });
                }

                // Update quantity - trigger will validate stock
                cartItem.Quantity = request.Quantity;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Cart updated successfully" });
            }
            catch (DbUpdateException ex)
            {
                // Database trigger threw an error
                var innerMessage = ex.InnerException?.Message ?? ex.Message;

                if (innerMessage.Contains("Cannot update") ||
                    innerMessage.Contains("available in stock") ||
                    innerMessage.Contains("Quantity must be greater than 0"))
                {
                    return BadRequest(new { message = innerMessage });
                }

                return BadRequest(new { message = "Failed to update cart: " + innerMessage });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred: " + ex.Message });
            }
        }

        // DELETE: api/cart/{cartId}
        [HttpDelete("{cartId}")]
        public async Task<ActionResult> RemoveFromCart(int cartId)
        {
            var cartItem = await _context.ShoppingCarts.FindAsync(cartId);
            if (cartItem == null)
            {
                return NotFound(new { message = "Cart item not found" });
            }

            _context.ShoppingCarts.Remove(cartItem);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Item removed from cart" });
        }

        // DELETE: api/cart/visitor/{visitorId}
        [HttpDelete("visitor/{visitorId}")]
        public async Task<ActionResult> ClearCart(int visitorId)
        {
            var cartItems = await _context.ShoppingCarts
                .Where(c => c.VisitorId == visitorId)
                .ToListAsync();

            _context.ShoppingCarts.RemoveRange(cartItems);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Cart cleared successfully" });
        }
    }

    public class AddToCartDto
    {
        [Required]
        public int VisitorId { get; set; }

        [Required]
        public int CommodityTypeId { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; } = 1;

        [MaxLength(10)]
        public string? Size { get; set; }
    }

    public class UpdateCartDto
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }
    }
}
