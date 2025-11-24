-- ========================================
-- SHOPPING CART STOCK VALIDATION TRIGGERS
-- ========================================
-- These triggers validate stock availability when adding items to cart
-- This provides immediate feedback to customers before checkout
-- ========================================

USE theme_park;

-- Drop existing cart table and triggers if they exist
DROP TRIGGER IF EXISTS check_stock_before_add_to_cart;
DROP TABLE IF EXISTS shopping_cart;

-- ========================================
-- CREATE SHOPPING CART TABLE
-- ========================================
CREATE TABLE shopping_cart (
    Cart_ID INT AUTO_INCREMENT PRIMARY KEY,
    Visitor_ID INT NOT NULL,
    Commodity_TypeID INT NOT NULL,
    Quantity INT NOT NULL,
    Size VARCHAR(10) NULL, -- For apparel items (S, M, L, XL, XXL)
    Added_At TIMESTAMP DEFAULT CURRENT_TIMESTAMP,

    FOREIGN KEY (Visitor_ID) REFERENCES visitor(Visitor_ID) ON DELETE CASCADE,
    FOREIGN KEY (Commodity_TypeID) REFERENCES commodity_type(Commodity_TypeID) ON DELETE CASCADE,

    -- Unique constraint: one cart entry per visitor per item per size
    UNIQUE KEY unique_cart_item (Visitor_ID, Commodity_TypeID, Size)
);

-- ========================================
-- TRIGGER: CHECK STOCK BEFORE ADD TO CART
-- ========================================
-- This trigger fires BEFORE INSERT/UPDATE on shopping_cart
-- It validates that:
--   1. Quantity is greater than 0
--   2. Sufficient stock is available
-- If validation fails, it raises an error and prevents adding to cart

DELIMITER $$

CREATE TRIGGER check_stock_before_add_to_cart
BEFORE INSERT ON shopping_cart
FOR EACH ROW
BEGIN
    DECLARE available_stock INT;
    DECLARE commodity_name VARCHAR(255);
    DECLARE existing_quantity INT DEFAULT 0;

    -- Check if quantity is valid (must be > 0)
    IF NEW.Quantity <= 0 THEN
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'Quantity must be greater than 0';
    END IF;

    -- Get current stock and commodity name
    SELECT Stock_Quantity, Commodity_Name
    INTO available_stock, commodity_name
    FROM commodity_type
    WHERE Commodity_TypeID = NEW.Commodity_TypeID;

    -- Check if commodity exists
    IF available_stock IS NULL THEN
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'Product not found';
    END IF;

    -- Check if item is out of stock
    IF available_stock <= 0 THEN
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = CONCAT('❌ ', commodity_name, ' is out of stock!');
    END IF;

    -- Check if sufficient stock is available
    IF available_stock < NEW.Quantity THEN
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = CONCAT('❌ Cannot add ', NEW.Quantity, ' items. Only ',
                                 available_stock, ' available in stock for ', commodity_name);
    END IF;
END$$

DELIMITER ;

-- ========================================
-- TRIGGER: UPDATE STOCK CHECK (FOR CART UPDATES)
-- ========================================
DELIMITER $$

CREATE TRIGGER check_stock_before_update_cart
BEFORE UPDATE ON shopping_cart
FOR EACH ROW
BEGIN
    DECLARE available_stock INT;
    DECLARE commodity_name VARCHAR(255);

    -- Check if quantity is valid (must be > 0)
    IF NEW.Quantity <= 0 THEN
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'Quantity must be greater than 0';
    END IF;

    -- Get current stock and commodity name
    SELECT Stock_Quantity, Commodity_Name
    INTO available_stock, commodity_name
    FROM commodity_type
    WHERE Commodity_TypeID = NEW.Commodity_TypeID;

    -- Check if sufficient stock is available
    IF available_stock < NEW.Quantity THEN
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = CONCAT('❌ Cannot update to ', NEW.Quantity, ' items. Only ',
                                 available_stock, ' available in stock for ', commodity_name);
    END IF;
END$$

DELIMITER ;

-- ========================================
-- VERIFICATION
-- ========================================

SELECT 'Shopping cart table and triggers created successfully!' AS status;

-- Show trigger information
SELECT
    TRIGGER_NAME,
    EVENT_MANIPULATION,
    EVENT_OBJECT_TABLE,
    ACTION_TIMING,
    CREATED
FROM INFORMATION_SCHEMA.TRIGGERS
WHERE TRIGGER_SCHEMA = DATABASE()
AND TRIGGER_NAME IN (
    'check_stock_before_add_to_cart',
    'check_stock_before_update_cart'
)
ORDER BY TRIGGER_NAME;
