import sqlite3
import psycopg2

sqlite_path = 'HotelBillingSystem/pannaum_hotel.db'
supabase_conn_str = 'postgresql://postgres.ijauuymlucjigoxdrudt:13089581Shanu@aws-0-eu-west-1.pooler.supabase.com:6543/postgres?sslmode=require'

s_conn = sqlite3.connect(sqlite_path)
s_conn.row_factory = sqlite3.Row
s_cur = s_conn.cursor()

p_conn = psycopg2.connect(supabase_conn_str)
p_conn.autocommit = True
p_cur = p_conn.cursor()

# Get all boolean columns in public schema
p_cur.execute("""
    SELECT table_name, column_name 
    FROM information_schema.columns 
    WHERE table_schema = 'public' AND data_type = 'boolean';
""")
bool_cols_map = {}
for tname, cname in p_cur.fetchall():
    if tname not in bool_cols_map:
        bool_cols_map[tname] = set()
    bool_cols_map[tname].add(cname)

print("Found boolean columns:", bool_cols_map)

tables = [
    'AspNetRoles',
    'AspNetUsers',
    'AspNetUserRoles',
    'FoodCategories',
    'FoodItems',
    'RestaurantTables',
    'KitchenStaffStatuses'
]

for table in tables:
    try:
        s_cur.execute(f'SELECT * FROM "{table}"')
        rows = s_cur.fetchall()
        if not rows:
            print(f"{table}: No rows in SQLite")
            continue
        
        col_names = [d[0] for d in s_cur.description]
        quoted_cols = [f'"{col}"' for col in col_names]
        placeholders = ['%s'] * len(col_names)
        
        cols_str = ', '.join(quoted_cols)
        vals_str = ', '.join(placeholders)
        
        insert_sql = f'INSERT INTO "{table}" ({cols_str}) VALUES ({vals_str}) ON CONFLICT DO NOTHING'
        
        table_bools = bool_cols_map.get(table, set())
        
        count = 0
        for r in rows:
            converted_vals = []
            for col_idx, col_name in enumerate(col_names):
                val = r[col_idx]
                if col_name in table_bools and val is not None:
                    converted_vals.append(bool(val))
                else:
                    converted_vals.append(val)
            try:
                p_cur.execute(insert_sql, tuple(converted_vals))
                count += 1
            except Exception as item_err:
                print(f"  Row error in {table}: {item_err}")
        print(f"Migrated {count} rows into {table} successfully.")
    except Exception as ex:
        print(f"Error migrating {table}: {ex}")

s_conn.close()
p_conn.close()
print("All tables successfully synchronized to Supabase PostgreSQL!")
