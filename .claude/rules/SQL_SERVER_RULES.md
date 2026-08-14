# SQL Server Rules

## Naming

Table

```sql
Customer

Order

Department
```

PascalCase.

---

Primary Key

```sql
Id
```

---

Foreign Key

```sql
CustomerId

DepartmentId
```

---

Stored Procedure

```sql
usp_GetCustomer
```

---

View

```sql
vw_Customer
```

---

Function

```sql
fn_GetCustomer
```

---

# SELECT

Không dùng

```sql
SELECT *
```

Luôn chỉ định cột.

---

# Alias

```sql
Customer c

Order o
```

---

# JOIN

Luôn ghi rõ

```sql
INNER JOIN

LEFT JOIN
```

Không dùng

```sql
FROM A,B
```

---

# WHERE

Không viết

```sql
WHERE 1=1
```

Trừ Dynamic SQL.

---

# Parameter

Không Hard Code.

Sai

```sql
WHERE Status=1
```

Đúng

```sql
WHERE Status=@Status
```

---

# Transaction

Nếu nhiều Update.

```sql
BEGIN TRY

BEGIN TRAN

...

COMMIT

END TRY

BEGIN CATCH

ROLLBACK

THROW

END CATCH
```

---

# Error

Không bỏ qua lỗi.

Luôn THROW.

---

# Index

Không tạo index trùng.

Không index mọi cột.

Đánh index theo Query.

---

# Query

Không query trong Cursor nếu có thể thay bằng Set-based.

Ưu tiên

JOIN

CTE

Window Function

---

# Performance

Không SELECT toàn bộ dữ liệu.

Có Paging.

Có Filter.

Có Index phù hợp.

Không dùng Function trong WHERE nếu làm mất Index.

---

# Unicode

Dùng

```sql
NVARCHAR
```

cho dữ liệu tiếng Việt.

Chuỗi Unicode

```sql
N'Khánh Hòa'
```

---

# Delete

Không

```sql
DELETE Customer
```

Nếu không có WHERE.

---

# Update

Không

```sql
UPDATE Customer
```

Nếu không có WHERE.

---

# Dynamic SQL

Luôn dùng

```sql
sp_executesql
```

Không nối chuỗi trực tiếp.

---

# Security

Không SQL Injection.

Luôn Parameter.

---

# AI Rules

AI phải:

- giữ nguyên tên bảng
- giữ nguyên Stored Procedure
- giữ nguyên View
- giữ nguyên Function
- không đổi schema
- không đổi kiểu dữ liệu nếu chưa được yêu cầu
- không sinh SELECT *
- ưu tiên hiệu năng
- ưu tiên Index Seek
- không làm thay đổi dữ liệu ngoài phạm vi yêu cầu