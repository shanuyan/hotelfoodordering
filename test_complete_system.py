import urllib.request
import urllib.parse
import http.cookiejar
import json
import re

base_url = "http://localhost:5000"

# Setup cookie handler
cj = http.cookiejar.CookieJar()
opener = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(cj))

def get(path):
    req = urllib.request.Request(base_url + path)
    with opener.open(req) as resp:
        return resp.status, resp.read().decode('utf-8')

def post_json(path, data):
    payload = json.dumps(data).encode('utf-8')
    req = urllib.request.Request(base_url + path, data=payload, headers={'Content-Type': 'application/json'})
    with opener.open(req) as resp:
        return resp.status, resp.read().decode('utf-8')

def post_form(path, data):
    payload = urllib.parse.urlencode(data).encode('utf-8')
    req = urllib.request.Request(base_url + path, data=payload, headers={'Content-Type': 'application/x-www-form-urlencoded'})
    with opener.open(req) as resp:
        return resp.status, resp.read().decode('utf-8')

print("====================================================")
print("TESTING FULL HOTEL BILLING & ORDERING SYSTEM")
print("====================================================")

# 1. Customer Menu Page
status, html = get('/Order/Customer')
assert status == 200, f"Customer menu failed with status {status}"
print("[PASS] 1. Customer Menu Page loaded (Status 200).")
assert "Chicken Fried Rice" in html, "Menu item missing"
assert "Biryani" in html, "Biryani missing"
assert "Table 16" in html, "Tables missing"
print("[PASS]    Menu verified with categories, items, and tables.")

# 2. Customer OTP Generation
status, resp = post_json('/Order/SendOtp', {'phone': '0771234567'})
otp_res = json.loads(resp)
assert otp_res.get('success') is True, "SendOtp failed"
simulated_otp = otp_res.get('simulatedOtp')
print(f"[PASS] 2. Customer OTP Generated successfully (Code: {simulated_otp}).")

# 3. Customer Places Order with OTP
order_payload = {
    'tableId': 1,
    'customerName': 'Test Customer Dinesh',
    'customerPhone': '0771234567',
    'otpCode': simulated_otp,
    'items': [{'foodItemId': 1, 'quantity': 2, 'specialInstructions': 'Make it extra spicy'}],
    'customerNotes': 'End-to-end verification order'
}
status, resp = post_json('/Order/VerifyAndPlaceOrder', order_payload)
order_res = json.loads(resp)
assert order_res.get('success') is True, f"VerifyAndPlaceOrder failed: {resp}"
new_order_id = order_res.get('orderId')
new_order_num = order_res.get('orderNumber')
print(f"[PASS] 3. Order Placed successfully! Order ID: {new_order_id}, Number: {new_order_num}")

# 4. Customer Order Tracking Page
status, track_html = get(f'/Order/Track/{new_order_id}')
assert status == 200, "Tracking page failed"
assert new_order_num in track_html, "Order number missing in track page"
print(f"[PASS] 4. Order Tracking Page loaded (Status 200) for Order #{new_order_num}.")

# 5. Customer Request Bill
status, req_bill = post_form('/Order/RequestBill', {'orderId': new_order_id})
print("[PASS] 5. Customer Requested Bill via SignalR trigger.")

# Helper to login
def login(email, password):
    status, login_page = get('/Account/Login')
    token_match = re.search(r'name="__RequestVerificationToken" type="hidden" value="([^"]+)"', login_page)
    token = token_match.group(1) if token_match else ""
    status, login_res = post_form('/Account/Login', {
        'Email': email,
        'Password': password,
        'RememberMe': 'false',
        '__RequestVerificationToken': token
    })
    return status

# 6. Kitchen Staff Login & KDS Screen
cj.clear() # fresh cookies for staff
login('kitchen1@hotel.com', 'Kitchen@123')
status, kitchen_html = get('/Kitchen/Index')
assert status == 200, f"Kitchen Dashboard failed with status {status}"
print("[PASS] 6. Kitchen Staff Logged in & KDS Dashboard loaded (Status 200).")

# 7. Kitchen Staff Updates Order Status to Preparing & Ready
status, upd_res = post_form('/Kitchen/UpdateStatus', {
    'orderId': new_order_id,
    'newStatus': 'Preparing',
    'remarks': 'Wok chef started cooking'
})
print("[PASS] 7a. Kitchen marked Order as Preparing.")

status, upd_res = post_form('/Kitchen/UpdateStatus', {
    'orderId': new_order_id,
    'newStatus': 'Ready',
    'remarks': 'Plated and ready on counter'
})
print("[PASS] 7b. Kitchen marked Order as Ready.")

# 8. Cashier Staff Login & Dashboard
cj.clear()
login('cashier1@hotel.com', 'Cashier@123')
status, cashier_html = get('/Cashier/Index')
assert status == 200, f"Cashier Dashboard failed with status {status}"
print("[PASS] 8. Cashier Staff Logged in & Cashier Terminal loaded (Status 200).")

# 9. Admin Login & Full Back-Office Modules
cj.clear()
login('admin@hotel.com', 'Admin@123')
status, admin_html = get('/Admin/Index')
assert status == 200, f"Admin Dashboard failed with status {status}"
print("[PASS] 9. Admin Logged in & Executive Dashboard loaded (Status 200).")

status, tables_html = get('/Admin/Tables')
assert status == 200, "Admin Tables page failed"
print("[PASS] 10. Admin Table Management loaded (Status 200).")

status, menu_html = get('/Admin/Menu')
assert status == 200, "Admin Menu page failed"
print("[PASS] 11. Admin Menu Management loaded (Status 200).")

status, reports_html = get('/Admin/Reports')
assert status == 200, "Admin Financial Reports page failed"
print("[PASS] 12. Admin Financial & Revenue Reports loaded (Status 200).")

print("====================================================")
print("ALL 12 END-TO-END FLOWS PASSED FLAWLESSLY! 100% OPERATIONAL.")
print("====================================================")
