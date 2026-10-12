import psycopg2

conn = psycopg2.connect('postgresql://postgres.ijauuymlucjigoxdrudt:13089581Shanu@aws-0-eu-west-1.pooler.supabase.com:6543/postgres?sslmode=require')
conn.autocommit = True
cur = conn.cursor()

tables = [
    'Orders',
    'OrderItems',
    'OrderStatusHistories',
    'FoodCategories',
    'FoodItems',
    'RestaurantTables',
    'TableSessions',
    'Bills',
    'Payments',
    'Notifications',
    'AuditLogs',
    'KitchenStaffStatuses'
]

for t in tables:
    try:
        sql = f'''SELECT setval(pg_get_serial_sequence('"{t}"', 'Id'), COALESCE(MAX("Id"), 1)) FROM "{t}";'''
        cur.execute(sql)
        res = cur.fetchone()[0]
        print(f"Sequence for {t} reset to {res}")
    except Exception as ex:
        print(f"Error resetting sequence for {t}: {ex}")

conn.close()
print("All PostgreSQL sequences synchronized successfully!")
