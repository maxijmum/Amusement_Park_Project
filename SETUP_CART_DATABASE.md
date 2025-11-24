# Shopping Cart Database Setup

## Overview
This setup creates a database-backed shopping cart with **real-time stock validation**. When customers try to add items to cart, the database trigger immediately validates stock and shows a red alert if quantity exceeds availability.

## Step 1: Run SQL Script

You need to execute the cart triggers SQL script in MySQL Workbench:

### Instructions:
1. Open **MySQL Workbench**
2. Connect to: `group6db.mysql.database.azure.com`
   - Username: `Group6Login`
   - Password: `silksonggoty!0`
   - Database: `amusement_park_db`
3. Open the file: `database/cart_triggers.sql`
4. Click **Execute** (lightning bolt icon) or press `Ctrl+Shift+Enter`
5. You should see: "Shopping cart table and triggers created successfully!"

### What This Creates:

**Table: `shopping_cart`**
- Stores customer cart items in the database (not localStorage)
- Columns: Cart_ID, Visitor_ID, Commodity_TypeID, Quantity, Size, Added_At
- Unique constraint: One entry per visitor/item/size combination

**Trigger: `check_stock_before_add_to_cart`**
- Fires BEFORE INSERT on `shopping_cart`
- Validates quantity > 0
- Validates sufficient stock available
- **Shows red alert immediately** if stock insufficient
- Error format: "❌ Cannot add {qty} items. Only {stock} available in stock for {name}"

**Trigger: `check_stock_before_update_cart`**
- Fires BEFORE UPDATE on `shopping_cart`
- Same validation as above
- Prevents updating cart to exceed stock

## Step 2: Restart Backend

After creating the database table, restart the backend server:

```bash
cd server
dotnet run
```

The backend should start on `http://localhost:5239`

## Step 3: Frontend is Ready

The frontend has been updated to use the database cart API. No changes needed!

## How It Works

### Old Flow (localStorage):
```
Customer adds 5 items (stock: 4)
  → Added to localStorage ✅
  → Goes to checkout
  → Fills out form
  → Clicks "Complete Purchase"
    → ❌ ERROR from database trigger
```

### New Flow (database cart with trigger):
```
Customer adds 5 items (stock: 4)
  → API call to add to cart
  → Database trigger checks stock
    → ❌ IMMEDIATE RED ALERT: "Cannot add 5 items. Only 4 available"
  → Customer adjusts to 4
  → Adds successfully ✅
```

## API Endpoints Created

### GET `/api/cart/{visitorId}`
Get all cart items for a visitor

### POST `/api/cart/add`
Add item to cart - **trigger validates stock**
```json
{
  "visitorId": 1,
  "commodityTypeId": 5,
  "quantity": 2,
  "size": "M"
}
```

**Response on success:**
```json
{
  "message": "Added to cart successfully",
  "cartId": 123
}
```

**Response on stock error (trigger):**
```json
{
  "message": "❌ Cannot add 5 items. Only 4 available in stock for ThrillWorld T-Shirt"
}
```

### PUT `/api/cart/{cartId}`
Update cart item quantity - **trigger validates stock**

### DELETE `/api/cart/{cartId}`
Remove item from cart

### DELETE `/api/cart/visitor/{visitorId}`
Clear entire cart

## Testing

### Test Case 1: Exceed Stock
1. Find item with 4 in stock
2. Try to add 5 to cart
3. **Expected**: Red alert appears immediately
4. **Message**: "❌ Cannot add 5 items. Only 4 available in stock for {name}"

### Test Case 2: Valid Quantity
1. Find item with 10 in stock
2. Add 5 to cart
3. **Expected**: Green success message
4. **Message**: "✅ Added 5x {name} to cart!"

### Test Case 3: Out of Stock
1. Find item with 0 stock
2. Button shows "❌ Out of Stock" (disabled)
3. **Expected**: Cannot click

## Files Modified/Created

### Backend:
1. **server/Models/ShoppingCart.cs** - Cart model
2. **server/Controllers/CartController.cs** - Cart API endpoints
3. **server/Data/ApplicationDbContext.cs** - Added ShoppingCart DbSet

### Database:
4. **database/cart_triggers.sql** - Cart table + stock validation triggers

### Frontend:
5. **frontend/src/components/CommodityPurchase.jsx** - Uses cart API
6. **frontend/src/components/Cart.jsx** - Displays trigger errors in RED

## Benefits

✅ **Real-time stock validation** - Instant feedback when adding to cart
✅ **Database triggers enforce rules** - Cannot be bypassed by frontend manipulation
✅ **Better UX** - No wasted time filling checkout forms for unavailable items
✅ **Persistent cart** - Survives page refreshes (stored in database)
✅ **Professional e-commerce** - How real websites work

## Troubleshooting

### Error: "Table 'shopping_cart' doesn't exist"
- Run the SQL script in MySQL Workbench
- Restart backend server

### Error: "Trigger not found"
- Check triggers exist: `SHOW TRIGGERS LIKE 'shopping_cart';`
- Re-run `database/cart_triggers.sql`

### Stock validation not working
- Check trigger is active in database
- Check backend error logs
- Verify frontend shows red alerts (not green)

## Summary

The shopping cart now uses **database triggers** to validate stock **immediately when adding to cart**. This provides instant feedback and prevents customers from wasting time with unavailable items.

**Key Point**: Validation happens via SQL trigger, not frontend code! The trigger is the source of truth.
