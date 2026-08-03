# C# Coding Rules

## Naming Convention

Class

```csharp
CustomerService
```

PascalCase.

---

Interface

```csharp
ICustomerService
```

Luôn bắt đầu bằng I.

---

Method

```csharp
GetCustomer()

CreateCustomer()

UpdateCustomer()

DeleteCustomer()
```

Tên phải bắt đầu bằng động từ.

---

Property

```csharp
FullName
```

PascalCase.

---

Private Field

```csharp
_logger

_customerRepository
```

camelCase với tiền tố _.

---

Local Variable

```csharp
customer

order

department
```

camelCase.

---

Constant

```csharp
MaxRetry
```

PascalCase.

---

Enum

```csharp
OrderStatus
```

Không dùng Magic Number.

---

Boolean

Đặt tên:

```csharp
isActive

canDelete

hasPermission
```

Không dùng:

flag

status

check

---

# Formatting

4 spaces.

Không dùng tab.

Dấu {

```csharp
public class CustomerService
{
}
```

---

# Exception

Không được:

```csharp
catch(Exception)
{
}
```

Luôn:

```csharp
catch(Exception ex)
{
    _logger.LogError(ex);
    throw;
}
```

---

# Async

Method Async phải có hậu tố

Async

Ví dụ

```csharp
GetCustomerAsync()
```

---

# LINQ

Không viết LINQ quá dài.

Nếu trên 4 phép Select/Where thì tách biến.

---

# Null

Ưu tiên

```csharp
customer?.Name
```

hoặc

```csharp
if(customer == null)
```

---

# String

Ưu tiên

```csharp
string.IsNullOrWhiteSpace()
```

Không dùng

```csharp
value == ""
```

---

# var

Được dùng khi kiểu dữ liệu rõ ràng.

```csharp
var customer = new Customer();
```

Không dùng

```csharp
var a = GetSomethingUnknown();
```

---

# Collection

Ưu tiên

```csharp
List<Customer>
```

Không dùng Array nếu không cần.

---

# Dependency Injection

Không new Service trực tiếp.

Đúng

```csharp
ICustomerService
```

Sai

```csharp
new CustomerService()
```

---

# Repository

Không viết SQL trong Controller.

Controller

↓

Service

↓

Repository

↓

Database

---

# Controller

Controller chỉ xử lý:

- Validate
- Authorization
- Gọi Service

Không viết Business.

---

# Service

Toàn bộ Business Logic nằm trong Service.

---

# Repository

Chỉ truy cập Database.

Không xử lý Business.

---

# Comment

Chỉ comment những đoạn khó hiểu.

Không comment hiển nhiên.

---

# Performance

Không query DB trong foreach.

Không gọi API trong foreach.

Ưu tiên Batch.

---

# Security

Không nối chuỗi SQL.

Không hardcode Password.

Không hardcode Token.

Không hardcode URL Production.

---

# AI Rules

AI không được:

- đổi namespace
- đổi class
- đổi folder
- đổi kiến trúc
- đổi style
- optimize khi chưa yêu cầu

AI phải:

- sinh code giống codebase
- giữ đúng convention
- hạn chế thay đổi Git Diff