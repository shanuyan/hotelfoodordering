import psycopg2
import sqlite3
import sys
from datetime import datetime, timedelta, timezone

now = datetime.now(timezone.utc)
supabase_conn_str = 'postgresql://postgres.ijauuymlucjigoxdrudt:13089581Shanu@aws-0-eu-west-1.pooler.supabase.com:6543/postgres?sslmode=require'

print("Connecting to Supabase PostgreSQL...", flush=True)
p_conn = psycopg2.connect(supabase_conn_str)
p_conn.autocommit = True
p_cur = p_conn.cursor()
print("Connected to Supabase PostgreSQL successfully.", flush=True)

# 1. Fetch Users from Supabase
p_cur.execute('SELECT "Id", "Email", "FullName" FROM "AspNetUsers";')
users_list = p_cur.fetchall()
user_map = {u[1]: u[0] for u in users_list}
print("User Map:", user_map, flush=True)

admin_id = user_map.get('admin@hotel.com')
chef1_id = user_map.get('kitchen1@hotel.com')
chef2_id = user_map.get('kitchen2@hotel.com')
cashier1_id = user_map.get('cashier1@hotel.com')

# Clean down in proper reverse foreign key order
print("Cleaning old data in reverse dependency order...", flush=True)
p_cur.execute('DELETE FROM "Payments";')
p_cur.execute('DELETE FROM "Bills";')
p_cur.execute('DELETE FROM "OrderStatusHistories";')
p_cur.execute('DELETE FROM "OrderItems";')
p_cur.execute('DELETE FROM "Orders";')
p_cur.execute('DELETE FROM "TableSessions";')
p_cur.execute('DELETE FROM "FoodItems";')
p_cur.execute('DELETE FROM "FoodCategories";')
p_cur.execute('DELETE FROM "RestaurantTables";')
p_cur.execute('DELETE FROM "Notifications";')
p_cur.execute('DELETE FROM "AuditLogs";')
print("Cleaned existing operational data.", flush=True)

# 2. Categories (9 categories)
categories_data = [
    (1, 'Rice & Biryani', 'Authentic Fried Rice, Dum Biryani & Steamed Rice', 1, True),
    (2, 'Kottu Specialties', 'Signature Sri Lankan Chopped Roti & Cheese Kottu', 2, True),
    (3, 'Noodles & Pasta', 'Wok-tossed Noodles & Creamy Italian Pastas', 3, True),
    (4, 'Curries & Main Dishes', 'Rich Sri Lankan & North Indian Gravies and Devilled items', 4, True),
    (5, 'Appetizers & Starters', 'Crispy Golden Rolls, Hot Devilled Wings & Finger Foods', 5, True),
    (6, 'Fast Food & Burgers', 'Gourmet Crispy Chicken Burgers, Wraps & Seasoned Fries', 6, True),
    (7, 'Beverages & Fresh Juices', 'Chilled Fresh Fruit Juices, Smoothies & Mocktails', 7, True),
    (8, 'Hot Teas & Special Coffee', 'Ceylon Milk Tea, Masala Chai & Freshly Brewed Coffee', 8, True),
    (9, 'Desserts & Ice Creams', 'Traditional Sri Lankan Watalappan, Puddings & Sundaes', 9, True)
]

for cat in categories_data:
    p_cur.execute("""
        INSERT INTO "FoodCategories" ("Id", "Name", "Description", "DisplayOrder", "IsActive")
        VALUES (%s, %s, %s, %s, %s);
    """, cat)
print("Populated 9 Food Categories.", flush=True)

# 3. Comprehensive Menu Items (27 items)
food_items_data = [
    # Cat 1: Rice & Biryani
    (1, 1, 'Chicken Fried Rice (Large)', 'Fragrant basmati rice stir-fried with tender chicken, eggs, and garden vegetables with chili paste.', 1200.00, '/images/chicken_rice.jpg', 15, True, True, True),
    (2, 1, 'Seafood Mixed Fried Rice', 'Jasmine rice tossed with jumbo prawns, calamari cuttlefish, egg, and fresh leeks.', 1650.00, '/images/seafood_rice.jpg', 20, True, True, True),
    (3, 1, 'Royal Chicken Dum Biryani', 'Authentic aromatic basmati rice slow-cooked with spiced chicken leg, boiled egg, mint sambol, and raita.', 1750.00, '/images/chicken_rice.jpg', 25, False, True, True),
    (4, 1, 'Vegetable Fried Rice', 'Wholesome fried rice cooked with fresh bell peppers, carrots, sweet corn, and spring onions.', 850.00, '/images/egg_rice.jpg', 12, False, True, True),
    
    # Cat 2: Kottu
    (5, 2, 'Chicken Cheese Kottu', 'Fresh godamba roti tossed with shredded roast chicken, mozzarella cheese, egg, and rich curry gravy.', 1500.00, '/images/cheese_kottu.jpg', 18, True, True, True),
    (6, 2, 'Classic Chicken Kottu', 'Traditional Sri Lankan street-style kottu chopped on hot griddle with spices and chicken curry.', 1100.00, '/images/chicken_kottu.jpg', 15, False, True, True),
    (7, 2, 'Seafood Dolphin Kottu', 'Thick roti cubes tossed with prawns, squid, spicy devilled chili gravy, and scrambled eggs.', 1700.00, '/images/cheese_kottu.jpg', 20, False, True, True),
    
    # Cat 3: Noodles & Pasta
    (8, 3, 'Wok-Fried Chicken Noodles', 'Soft wheat noodles wok-tossed with sliced chicken breast, shredded cabbage, carrots, and sesame chili.', 1050.00, '/images/chicken_noodles.jpg', 15, False, True, True),
    (9, 3, 'Spicy Prawn Garlic Noodles', 'Egg noodles infused with garlic butter, tiger prawns, crushed black pepper, and scallions.', 1400.00, '/images/prawn_noodles.jpg', 15, True, True, True),
    (10, 3, 'Creamy Chicken Penne Alfredo', 'Al dente penne pasta coated in rich garlic parmesan cream sauce with grilled chicken strips.', 1550.00, '/images/chicken_noodles.jpg', 18, False, True, True),
    
    # Cat 4: Curries & Mains
    (11, 4, 'Creamy Butter Chicken', 'Tender boneless chicken simmered in a mildly spiced velvety butter tomato gravy.', 1450.00, '/images/chicken_rice.jpg', 20, True, True, True),
    (12, 4, 'Hot Butter Cuttlefish (HBC)', 'Crispy battered cuttlefish rings tossed with butter, dried chili flakes, capsicum, and spring onions.', 1800.00, '/images/seafood_rice.jpg', 15, True, True, True),
    (13, 4, 'Sri Lankan Dhal Curry (Parippu)', 'Creamy red lentils tempered with curry leaves, mustard seeds, and rich coconut milk.', 500.00, '/images/egg_rice.jpg', 10, False, True, True),
    
    # Cat 5: Appetizers
    (14, 5, 'Crispy Vegetable Spring Rolls (4 Pcs)', 'Golden fried pastry rolls filled with seasoned Asian vegetables served with sweet chili dip.', 650.00, '/images/fruit_salad.jpg', 10, False, True, True),
    (15, 5, 'Spicy Devilled Chicken Wings', 'Crispy chicken wings glazed in a spicy sweet and sour Sri Lankan devilled chili sauce.', 950.00, '/images/chicken_kottu.jpg', 15, True, True, True),
    (16, 5, 'Cheesy Garlic Bread (4 Slices)', 'Toasted French baguette infused with garlic butter, parsley, and melted mozzarella.', 600.00, '/images/cheese_kottu.jpg', 10, False, True, True),
    
    # Cat 6: Fast Food
    (17, 6, 'Crispy Zinger Chicken Burger', 'Crispy fried chicken breast fillet with iceberg lettuce, cheddar cheese, and house mayo on brioche bun.', 1150.00, '/images/chicken_kottu.jpg', 15, True, True, True),
    (18, 6, 'Peri-Peri French Fries', 'Crispy golden potato fries seasoned with zesty African peri-peri spice dust.', 550.00, '/images/fruit_salad.jpg', 8, False, True, True),
    
    # Cat 7: Beverages & Fresh Juices
    (19, 7, 'Fresh Lime & Mint Cooler', 'Freshly squeezed lime juice blended with wild mint leaves and chilled sparkling soda.', 350.00, '/images/lime_juice.jpg', 5, False, True, True),
    (20, 7, 'Tropical Mango Milkshake', 'Creamy alphonso mango pulp blended with rich vanilla ice cream and chilled milk.', 600.00, '/images/mango_shake.jpg', 8, True, True, True),
    (21, 7, 'Passion Fruit Sparkler', 'Organic sweet passion fruit pulp shaken with crushed ice and soda water.', 450.00, '/images/lime_juice.jpg', 5, False, True, True),
    
    # Cat 8: Hot Teas & Coffee
    (22, 8, 'Classic Ceylon Milk Tea', 'Brewed pure Ceylon BOPF black tea blended with creamy condensed milk.', 250.00, '/images/mango_shake.jpg', 5, False, True, True),
    (23, 8, 'Spiced Masala Chai', 'Fragrant black tea simmered with cardamom, cinnamon, cloves, fresh ginger, and milk.', 350.00, '/images/mango_shake.jpg', 6, False, True, True),
    (24, 8, 'Caramel Iced Latte', 'Double espresso shot poured over chilled milk, ice cubes, and rich caramel syrup.', 650.00, '/images/mango_shake.jpg', 6, True, True, True),
    
    # Cat 9: Desserts
    (25, 9, 'Traditional Sri Lankan Watalappan', 'Steamed coconut jaggery custard enriched with nutmeg, cardamom, and toasted cashew nuts.', 500.00, '/images/watalappan.jpg', 5, True, True, True),
    (26, 9, 'Caramel Bread Pudding', 'Soft baked pudding soaked in vanilla caramel custard topped with raisins.', 450.00, '/images/watalappan.jpg', 5, False, True, True),
    (27, 9, 'Exotic Fruit Salad with Ice Cream', 'Diced seasonal papaya, mango, pineapple, and watermelon topped with rich vanilla ice cream.', 550.00, '/images/fruit_salad.jpg', 8, False, True, True)
]

for item in food_items_data:
    p_cur.execute("""
        INSERT INTO "FoodItems" ("Id", "CategoryId", "Name", "Description", "Price", "ImageUrl", "PrepTimeMinutes", "IsDailyMenu", "IsAvailable", "IsActive", "CreatedAt", "UpdatedAt")
        VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s);
    """, (item[0], item[1], item[2], item[3], item[4], item[5], item[6], item[7], item[8], item[9], now, now))
print("Populated 27 Comprehensive Menu Items.", flush=True)

# 4. Restaurant Tables (16 Tables across 4 zones)
tables_data = [
    (1, 'Table 01', 4, 'AC Main Dining Hall', 2, 'table-token-01', True), # Occupied
    (2, 'Table 02', 4, 'AC Main Dining Hall', 2, 'table-token-02', True), # Occupied
    (3, 'Table 03', 4, 'AC Main Dining Hall', 2, 'table-token-03', True), # Occupied
    (4, 'Table 04', 6, 'AC Main Dining Hall', 2, 'table-token-04', True), # Occupied (Bill Requested)
    (5, 'Table 05', 6, 'AC Main Dining Hall', 1, 'table-token-05', True), # Available
    (6, 'Table 06', 6, 'AC Main Dining Hall', 1, 'table-token-06', True), # Available
    (7, 'Table 07', 6, 'Garden Terrace View', 1, 'table-token-07', True), # Available
    (8, 'Table 08', 6, 'Garden Terrace View', 1, 'table-token-08', True), # Available
    (9, 'Table 09', 8, 'Garden Terrace View', 1, 'table-token-09', True), # Available
    (10, 'Table 10', 8, 'Garden Terrace View', 1, 'table-token-10', True), # Available
    (11, 'Table 11', 10, 'VIP Family Lounge', 1, 'table-token-11', True), # Available
    (12, 'Table 12', 10, 'VIP Family Lounge', 1, 'table-token-12', True), # Available
    (13, 'Table 13', 8, 'VIP Family Lounge', 1, 'table-token-13', True), # Available
    (14, 'Table 14', 8, 'VIP Family Lounge', 1, 'table-token-14', True), # Available
    (15, 'Table 15', 2, 'Rooftop Romantic Lounge', 1, 'table-token-15', True), # Available
    (16, 'Table 16', 4, 'Rooftop Romantic Lounge', 1, 'table-token-16', True)  # Available
]

for t in tables_data:
    p_cur.execute("""
        INSERT INTO "RestaurantTables" ("Id", "TableNumber", "Capacity", "Location", "Status", "QrToken", "IsActive", "CreatedAt")
        VALUES (%s, %s, %s, %s, %s, %s, %s, %s);
    """, (t[0], t[1], t[2], t[3], t[4], t[5], t[6], now))
print("Populated 16 Restaurant Tables.", flush=True)

# 5. Table Sessions
sessions_data = [
    # Active Sessions
    (1, 'SESS-20261012-001', 1, now - timedelta(minutes=45), None, True, False, 2700.00), # Table 1
    (2, 'SESS-20261012-002', 2, now - timedelta(minutes=35), None, True, False, 3050.00), # Table 2
    (3, 'SESS-20261012-003', 3, now - timedelta(minutes=20), None, True, False, 2050.00), # Table 3
    (4, 'SESS-20261012-004', 4, now - timedelta(minutes=60), None, True, True, 4350.00),  # Table 4 (Bill Requested!)
    
    # Completed Historical Sessions
    (5, 'SESS-20261012-005', 5, now - timedelta(hours=3), now - timedelta(hours=2), False, False, 4850.00),
    (6, 'SESS-20261012-006', 7, now - timedelta(hours=4), now - timedelta(hours=3), False, False, 2650.00),
    (7, 'SESS-20261012-007', 8, now - timedelta(hours=5), now - timedelta(hours=4), False, False, 8200.00),
    (8, 'SESS-20261012-008', 9, now - timedelta(hours=6), now - timedelta(hours=5), False, False, 3400.00),
    (9, 'SESS-20261012-009', 11, now - timedelta(hours=7), now - timedelta(hours=6), False, False, 5900.00)
]

for s in sessions_data:
    p_cur.execute("""
        INSERT INTO "TableSessions" ("Id", "SessionCode", "TableId", "StartTime", "EndTime", "IsActive", "IsBillRequested", "TotalAmount")
        VALUES (%s, %s, %s, %s, %s, %s, %s, %s);
    """, s)
print("Populated 9 Table Sessions.", flush=True)

# 6. Orders
orders_data = [
    # Order 1 (Table 1): Status 4 = Preparing (Assigned to Chef Sanath)
    (1, 'ORD-20261012-001', 1, 1, chef1_id, 4, 'Mr. Dinesh Perera', '0771234567', 'Less spicy please', 2700.00, 0, 0, 2700.00, now - timedelta(minutes=40), now - timedelta(minutes=15), now - timedelta(minutes=35), now - timedelta(minutes=30), now - timedelta(minutes=25), None, None),
    
    # Order 2 (Table 2): Status 5 = Ready (Assigned to Chef Kamal)
    (2, 'ORD-20261012-002', 2, 2, chef2_id, 5, 'Dr. Samantha Silva', '0719876543', 'Extra napkins', 3050.00, 0, 0, 3050.00, now - timedelta(minutes=32), now - timedelta(minutes=5), now - timedelta(minutes=30), now - timedelta(minutes=28), now - timedelta(minutes=20), now - timedelta(minutes=5), None),
    
    # Order 3 (Table 3): Status 1 = New (Awaiting chef claim)
    (3, 'ORD-20261012-003', 3, 3, None, 1, 'Mrs. Fatima Rizvi', '0765544332', 'Separate bowls', 2050.00, 0, 0, 2050.00, now - timedelta(minutes=15), now - timedelta(minutes=15), None, None, None, None, None),
    
    # Order 4 (Table 4): Status 6 = Served (Session Bill Requested)
    (4, 'ORD-20261012-004', 4, 4, chef1_id, 6, 'Mr. Roshan Fernando', '0754433221', 'All dishes served warm', 4350.00, 0, 0, 4350.00, now - timedelta(minutes=55), now - timedelta(minutes=20), now - timedelta(minutes=50), now - timedelta(minutes=45), now - timedelta(minutes=35), now - timedelta(minutes=22), now - timedelta(minutes=20)),
    
    # Historical Completed Orders (Status 7 = Completed)
    (5, 'ORD-20261012-005', 5, 5, chef1_id, 7, 'Eng. Anura Bandara', '0721112233', 'Quick lunch', 4850.00, 0, 0, 4850.00, now - timedelta(hours=3), now - timedelta(hours=2), now - timedelta(hours=3), now - timedelta(hours=3), now - timedelta(hours=3), now - timedelta(hours=2, minutes=30), now - timedelta(hours=2)),
    (6, 'ORD-20261012-006', 6, 7, chef2_id, 7, 'Miss Kavindi De Mel', '0778899001', '', 2650.00, 0, 0, 2650.00, now - timedelta(hours=4), now - timedelta(hours=3), now - timedelta(hours=4), now - timedelta(hours=4), now - timedelta(hours=4), now - timedelta(hours=3, minutes=30), now - timedelta(hours=3)),
    (7, 'ORD-20261012-007', 7, 8, chef1_id, 7, 'Mr. Thushara Wickrama', '0769988776', 'Birthday celebration', 8200.00, 0, 0, 8200.00, now - timedelta(hours=5), now - timedelta(hours=4), now - timedelta(hours=5), now - timedelta(hours=5), now - timedelta(hours=5), now - timedelta(hours=4, minutes=30), now - timedelta(hours=4)),
    (8, 'ORD-20261012-008', 8, 9, chef2_id, 7, 'Family Table (Mr. John)', '0714455667', '', 3400.00, 0, 0, 3400.00, now - timedelta(hours=6), now - timedelta(hours=5), now - timedelta(hours=6), now - timedelta(hours=6), now - timedelta(hours=6), now - timedelta(hours=5, minutes=30), now - timedelta(hours=5)),
    (9, 'ORD-20261012-009', 9, 11, chef1_id, 7, 'VIP Guest (Hon. Minister)', '0770001122', 'VIP service', 5900.00, 0, 0, 5900.00, now - timedelta(hours=7), now - timedelta(hours=6), now - timedelta(hours=7), now - timedelta(hours=7), now - timedelta(hours=7), now - timedelta(hours=6, minutes=30), now - timedelta(hours=6))
]

for o in orders_data:
    p_cur.execute("""
        INSERT INTO "Orders" ("Id", "OrderNumber", "TableSessionId", "TableId", "AssignedKitchenStaffId", "Status", "CustomerName", "CustomerPhone", "CustomerNotes", "SubTotal", "TaxAmount", "ServiceCharge", "TotalAmount", "CreatedAt", "UpdatedAt", "AssignedAt", "AcceptedAt", "PreparingAt", "ReadyAt", "ServedAt")
        VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s);
    """, o)
print("Populated 9 Orders (4 Active + 5 Historical).", flush=True)

# 7. Order Items
order_items_data = [
    # Order 1 (Table 1): Total 2700 (Chicken Fried Rice 1200 + Cheese Kottu 1500)
    (1, 1, 1, 'Chicken Fried Rice (Large)', 1200.00, 1, 1200.00, 'Less spicy'),
    (2, 1, 5, 'Chicken Cheese Kottu', 1500.00, 1, 1500.00, 'Extra cheese gravy'),
    
    # Order 2 (Table 2): Total 3050 (Seafood Rice 1650 + Spicy Prawn Noodles 1400)
    (3, 2, 2, 'Seafood Mixed Fried Rice', 1650.00, 1, 1650.00, 'Extra chili paste'),
    (4, 2, 9, 'Spicy Prawn Garlic Noodles', 1400.00, 1, 1400.00, 'Well fried prawns'),
    
    # Order 3 (Table 3): Total 2050 (Chicken Biryani 1750 + Lime Cooler 300)
    (5, 3, 3, 'Royal Chicken Dum Biryani', 1750.00, 1, 1750.00, 'Chicken leg piece'),
    (6, 3, 19, 'Fresh Lime & Mint Cooler', 300.00, 1, 300.00, 'No sugar'),
    
    # Order 4 (Table 4): Total 4350 (Butter Chicken 1450 + HBC 1800 + Zinger Burger 1100)
    (7, 4, 11, 'Creamy Butter Chicken', 1450.00, 1, 1450.00, ''),
    (8, 4, 12, 'Hot Butter Cuttlefish (HBC)', 1800.00, 1, 1800.00, 'Crispy hot'),
    (9, 4, 17, 'Crispy Zinger Chicken Burger', 1100.00, 1, 1100.00, 'Extra sauce'),
    
    # Historical items
    (10, 5, 2, 'Seafood Mixed Fried Rice', 1650.00, 2, 3300.00, ''),
    (11, 5, 20, 'Tropical Mango Milkshake', 600.00, 2, 1200.00, ''),
    (12, 5, 19, 'Fresh Lime & Mint Cooler', 350.00, 1, 350.00, ''),
    
    (13, 6, 6, 'Classic Chicken Kottu', 1100.00, 2, 2200.00, ''),
    (14, 6, 26, 'Caramel Bread Pudding', 450.00, 1, 450.00, ''),
    
    (15, 7, 3, 'Royal Chicken Dum Biryani', 1750.00, 4, 7000.00, ''),
    (16, 7, 20, 'Tropical Mango Milkshake', 600.00, 2, 1200.00, ''),
    
    (17, 8, 8, 'Wok-Fried Chicken Noodles', 1050.00, 2, 2100.00, ''),
    (18, 8, 17, 'Crispy Zinger Chicken Burger', 1150.00, 1, 1150.00, ''),
    (19, 8, 22, 'Classic Ceylon Milk Tea', 150.00, 1, 150.00, ''),
    
    (20, 9, 12, 'Hot Butter Cuttlefish (HBC)', 1800.00, 2, 3600.00, ''),
    (21, 9, 1, 'Chicken Fried Rice (Large)', 1200.00, 1, 1200.00, ''),
    (22, 9, 21, 'Passion Fruit Sparkler', 450.00, 2, 900.00, ''),
    (23, 9, 25, 'Traditional Sri Lankan Watalappan', 500.00, 2, 1000.00, '')
]

for item in order_items_data:
    p_cur.execute("""
        INSERT INTO "OrderItems" ("Id", "OrderId", "FoodItemId", "FoodName", "UnitPrice", "Quantity", "SubTotal", "SpecialInstructions")
        VALUES (%s, %s, %s, %s, %s, %s, %s, %s);
    """, item)
print("Populated 23 Order Items.", flush=True)

# 8. Order Status Histories
histories_data = [
    (1, 1, 1, 2, admin_id, now - timedelta(minutes=35), 'Assigned to Chef Sanath'),
    (2, 1, 2, 3, chef1_id, now - timedelta(minutes=30), 'Chef Sanath accepted'),
    (3, 1, 3, 4, chef1_id, now - timedelta(minutes=25), 'Preparing dishes on wok'),
    
    (4, 2, 1, 4, chef2_id, now - timedelta(minutes=28), 'Chef Kamal started cooking'),
    (5, 2, 4, 5, chef2_id, now - timedelta(minutes=5), 'Order plated and ready for server'),
    
    (6, 4, 1, 4, chef1_id, now - timedelta(minutes=45), 'Chef Sanath cooked order'),
    (7, 4, 4, 5, chef1_id, now - timedelta(minutes=22), 'Ready on pass'),
    (8, 4, 5, 6, cashier1_id, now - timedelta(minutes=20), 'Food served to customer')
]

for h in histories_data:
    p_cur.execute("""
        INSERT INTO "OrderStatusHistories" ("Id", "OrderId", "PreviousStatus", "NewStatus", "ChangedByUserId", "ChangedAt", "Remarks")
        VALUES (%s, %s, %s, %s, %s, %s, %s);
    """, h)
print("Populated Order Status Histories.", flush=True)

# 9. Completed Bills & Payments
bills_data = [
    (1, 'INV-20261012-001', 5, 5, 4850.00, 0, '', 0, 0, 4850.00, 2, 2, cashier1_id, now - timedelta(hours=2)),
    (2, 'INV-20261012-002', 6, 7, 2650.00, 0, '', 0, 0, 2650.00, 1, 2, cashier1_id, now - timedelta(hours=3)),
    (3, 'INV-20261012-003', 7, 8, 8200.00, 0, '', 0, 0, 8200.00, 3, 2, cashier1_id, now - timedelta(hours=4)),
    (4, 'INV-20261012-004', 8, 9, 3400.00, 0, '', 0, 0, 3400.00, 1, 2, cashier1_id, now - timedelta(hours=5)),
    (5, 'INV-20261012-005', 9, 11, 5900.00, 0, '', 0, 0, 5900.00, 2, 2, cashier1_id, now - timedelta(hours=6))
]

for b in bills_data:
    p_cur.execute("""
        INSERT INTO "Bills" ("Id", "InvoiceNumber", "TableSessionId", "TableId", "SubTotal", "DiscountAmount", "DiscountReason", "TaxAmount", "ServiceCharge", "GrandTotal", "PaymentMethod", "PaymentStatus", "CashierUserId", "CreatedAt")
        VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s);
    """, b)
print("Populated 5 Paid Bills.", flush=True)

payments_data = [
    (1, 1, 4850.00, 2, 2, 'TXN-CARD-9921', now - timedelta(hours=2), cashier1_id),
    (2, 2, 2650.00, 1, 2, 'CASH-REC-0042', now - timedelta(hours=3), cashier1_id),
    (3, 3, 8200.00, 3, 2, 'LANKAQR-88301', now - timedelta(hours=4), cashier1_id),
    (4, 4, 3400.00, 1, 2, 'CASH-REC-0043', now - timedelta(hours=5), cashier1_id),
    (5, 5, 5900.00, 2, 2, 'TXN-CARD-9928', now - timedelta(hours=6), cashier1_id)
]

for pay in payments_data:
    p_cur.execute("""
        INSERT INTO "Payments" ("Id", "BillId", "Amount", "PaymentMethod", "PaymentStatus", "TransactionReference", "PaidAt", "ProcessedByUserId")
        VALUES (%s, %s, %s, %s, %s, %s, %s, %s);
    """, pay)
print("Populated 5 Payment Records.", flush=True)

# 10. Live Notifications
notifications_data = [
    (1, 'KitchenStaff', chef1_id, '🍳 Order In Preparation', 'Order #ORD-20261012-001 (Table 01) is currently being prepared by Chef Sanath.', '/Kitchen/Index', False, now - timedelta(minutes=25)),
    (2, 'CashierStaff', cashier1_id, '🔔 Bill Requested by Table 04', 'Table 04 requested their final invoice of Rs. 4,350.00.', '/Cashier/Index', False, now - timedelta(minutes=10)),
    (3, 'Admin', admin_id, '💰 Daily Sales Milestone', 'Today total settled revenue has reached Rs. 25,000+ across 5 dining sessions.', '/Admin/Reports', False, now - timedelta(hours=1)),
    (4, 'KitchenStaff', None, '🔥 New Order Placed', 'Table 03 placed order #ORD-20261012-003 for Royal Chicken Biryani.', '/Kitchen/Index', False, now - timedelta(minutes=15))
]

for notif in notifications_data:
    p_cur.execute("""
        INSERT INTO "Notifications" ("Id", "TargetRole", "TargetUserId", "Title", "Message", "LinkUrl", "IsRead", "CreatedAt")
        VALUES (%s, %s, %s, %s, %s, %s, %s, %s);
    """, notif)
print("Populated 4 Notifications.", flush=True)

# 11. Audit Logs
audit_logs_data = [
    (1, admin_id, 'Hotel Administrator', 'SYSTEM_INIT', 'System', '0', 'Database and application system initialized successfully.', now - timedelta(hours=8)),
    (2, cashier1_id, 'Cashier Ruwan', 'BILL_SETTLED', 'Bill', 'INV-20261012-001', 'Settled invoice #INV-20261012-001 for Rs. 4,850 via Card.', now - timedelta(hours=2)),
    (3, cashier1_id, 'Cashier Ruwan', 'BILL_SETTLED', 'Bill', 'INV-20261012-003', 'Settled invoice #INV-20261012-003 for Rs. 8,200 via LankaQR Online.', now - timedelta(hours=4)),
    (4, chef1_id, 'Chef Sanath', 'ORDER_PREPARED', 'Order', 'ORD-20261012-004', 'Completed cooking and served order for Table 04.', now - timedelta(minutes=20)),
    (5, admin_id, 'Hotel Administrator', 'MENU_UPDATE', 'FoodMenu', '0', 'Synchronized 27 menu items across 9 categories.', now - timedelta(minutes=5))
]

for log in audit_logs_data:
    p_cur.execute("""
        INSERT INTO "AuditLogs" ("Id", "UserId", "UserName", "Action", "EntityName", "EntityId", "Details", "Timestamp")
        VALUES (%s, %s, %s, %s, %s, %s, %s, %s);
    """, log)
print("Populated 5 Audit Logs.", flush=True)

# 12. Update Kitchen Staff Status
p_cur.execute("""
    UPDATE "KitchenStaffStatuses" SET "CurrentStatus" = 2, "LastStatusUpdate" = %s WHERE "UserId" = %s;
""", (now, chef1_id))
p_cur.execute("""
    UPDATE "KitchenStaffStatuses" SET "CurrentStatus" = 1, "LastStatusUpdate" = %s WHERE "UserId" = %s;
""", (now, chef2_id))
print("Kitchen staff statuses updated in Supabase.", flush=True)

# 13. Reset all sequences to MAX(Id) so new orders/items auto-increment without duplicate key errors
seq_tables = ['Orders', 'OrderItems', 'OrderStatusHistories', 'FoodCategories', 'FoodItems', 'RestaurantTables', 'TableSessions', 'Bills', 'Payments', 'Notifications', 'AuditLogs', 'KitchenStaffStatuses']
for t in seq_tables:
    try:
        p_cur.execute(f'''SELECT setval(pg_get_serial_sequence('"{t}"', 'Id'), COALESCE(MAX("Id"), 1)) FROM "{t}";''')
    except Exception as sq_err:
        pass
print("All auto-increment sequences reset to MAX(Id).", flush=True)

p_conn.close()
print("All Supabase data committed successfully!", flush=True)

# 13. Sync all data into local SQLite database (pannaum_hotel.db) as seamless offline fallback
print("\nSyncing data to local SQLite database...", flush=True)
s_conn = sqlite3.connect('HotelBillingSystem/pannaum_hotel.db')
s_cur = s_conn.cursor()

s_cur.execute('DELETE FROM Payments;')
s_cur.execute('DELETE FROM Bills;')
s_cur.execute('DELETE FROM OrderStatusHistories;')
s_cur.execute('DELETE FROM OrderItems;')
s_cur.execute('DELETE FROM Orders;')
s_cur.execute('DELETE FROM TableSessions;')
s_cur.execute('DELETE FROM FoodItems;')
s_cur.execute('DELETE FROM FoodCategories;')
s_cur.execute('DELETE FROM RestaurantTables;')
s_cur.execute('DELETE FROM Notifications;')
s_cur.execute('DELETE FROM AuditLogs;')

for cat in categories_data:
    s_cur.execute('INSERT INTO FoodCategories (Id, Name, Description, DisplayOrder, IsActive) VALUES (?, ?, ?, ?, ?);', (cat[0], cat[1], cat[2], cat[3], 1 if cat[4] else 0))

for item in food_items_data:
    s_cur.execute('''INSERT INTO FoodItems (Id, CategoryId, Name, Description, Price, ImageUrl, PrepTimeMinutes, IsDailyMenu, IsAvailable, IsActive, CreatedAt, UpdatedAt)
                     VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?);''',
                  (item[0], item[1], item[2], item[3], item[4], item[5], item[6], 1 if item[7] else 0, 1 if item[8] else 0, 1 if item[9] else 0, now.isoformat(), now.isoformat()))

for t in tables_data:
    s_cur.execute('INSERT INTO RestaurantTables (Id, TableNumber, Capacity, Location, Status, QrToken, IsActive, CreatedAt) VALUES (?, ?, ?, ?, ?, ?, ?, ?);',
                  (t[0], t[1], t[2], t[3], t[4], t[5], 1 if t[6] else 0, now.isoformat()))

for s in sessions_data:
    s_cur.execute('INSERT INTO TableSessions (Id, SessionCode, TableId, StartTime, EndTime, IsActive, IsBillRequested, TotalAmount) VALUES (?, ?, ?, ?, ?, ?, ?, ?);',
                  (s[0], s[1], s[2], s[3].isoformat(), s[4].isoformat() if s[4] else None, 1 if s[5] else 0, 1 if s[6] else 0, s[7]))

for o in orders_data:
    s_cur.execute('''INSERT INTO Orders (Id, OrderNumber, TableSessionId, TableId, AssignedKitchenStaffId, Status, CustomerName, CustomerPhone, CustomerNotes, SubTotal, TaxAmount, ServiceCharge, TotalAmount, CreatedAt, UpdatedAt, AssignedAt, AcceptedAt, PreparingAt, ReadyAt, ServedAt)
                     VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?);''',
                  (o[0], o[1], o[2], o[3], o[4], o[5], o[6], o[7], o[8], o[9], o[10], o[11], o[12], o[13].isoformat(), o[14].isoformat(),
                   o[15].isoformat() if o[15] else None, o[16].isoformat() if o[16] else None, o[17].isoformat() if o[17] else None, o[18].isoformat() if o[18] else None, o[19].isoformat() if o[19] else None))

for it in order_items_data:
    s_cur.execute('INSERT INTO OrderItems (Id, OrderId, FoodItemId, FoodName, UnitPrice, Quantity, SubTotal, SpecialInstructions) VALUES (?, ?, ?, ?, ?, ?, ?, ?);', it)

for h in histories_data:
    s_cur.execute('INSERT INTO OrderStatusHistories (Id, OrderId, PreviousStatus, NewStatus, ChangedByUserId, ChangedAt, Remarks) VALUES (?, ?, ?, ?, ?, ?, ?);',
                  (h[0], h[1], h[2], h[3], h[4], h[5].isoformat(), h[6]))

for b in bills_data:
    s_cur.execute('''INSERT INTO Bills (Id, InvoiceNumber, TableSessionId, TableId, SubTotal, DiscountAmount, DiscountReason, TaxAmount, ServiceCharge, GrandTotal, PaymentMethod, PaymentStatus, CashierUserId, CreatedAt)
                     VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?);''',
                  (b[0], b[1], b[2], b[3], b[4], b[5], b[6], b[7], b[8], b[9], b[10], b[11], b[12], b[13].isoformat()))

for pay in payments_data:
    s_cur.execute('INSERT INTO Payments (Id, BillId, Amount, PaymentMethod, PaymentStatus, TransactionReference, PaidAt, ProcessedByUserId) VALUES (?, ?, ?, ?, ?, ?, ?, ?);',
                  (pay[0], pay[1], pay[2], pay[3], pay[4], pay[5], pay[6].isoformat(), pay[7]))

for notif in notifications_data:
    s_cur.execute('INSERT INTO Notifications (Id, TargetRole, TargetUserId, Title, Message, LinkUrl, IsRead, CreatedAt) VALUES (?, ?, ?, ?, ?, ?, ?, ?);',
                  (notif[0], notif[1], notif[2], notif[3], notif[4], notif[5], 1 if notif[6] else 0, notif[7].isoformat()))

for log in audit_logs_data:
    s_cur.execute('INSERT INTO AuditLogs (Id, UserId, UserName, Action, EntityName, EntityId, Details, Timestamp) VALUES (?, ?, ?, ?, ?, ?, ?, ?);',
                  (log[0], log[1], log[2], log[3], log[4], log[5], log[6], log[7].isoformat()))

s_conn.commit()
s_conn.close()
print("Local SQLite database completely synchronized as well!", flush=True)
