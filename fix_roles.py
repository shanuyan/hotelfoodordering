import psycopg2

supabase_conn_str = 'postgresql://postgres.ijauuymlucjigoxdrudt:13089581Shanu@aws-0-eu-west-1.pooler.supabase.com:6543/postgres?sslmode=require'

p_conn = psycopg2.connect(supabase_conn_str)
p_conn.autocommit = True
p_cur = p_conn.cursor()

p_cur.execute('SELECT "Id", "Name" FROM "AspNetRoles";')
roles = p_cur.fetchall()
roles_by_name = {r[1]: r[0] for r in roles}
print("Roles in Supabase:", roles_by_name)

p_cur.execute('SELECT "Id", "StaffRole", "Email" FROM "AspNetUsers";')
users = p_cur.fetchall()
print(f"Users in Supabase: {len(users)}")

p_cur.execute('DELETE FROM "AspNetUserRoles";')

count = 0
for uid, role_name, email in users:
    if role_name in roles_by_name:
        rid = roles_by_name[role_name]
        p_cur.execute('INSERT INTO "AspNetUserRoles" ("UserId", "RoleId") VALUES (%s, %s);', (uid, rid))
        count += 1
        print(f"  Assigned {email} -> {role_name}")

print(f"Successfully linked {count} users to roles in Supabase.")
p_conn.close()
