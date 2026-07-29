# UC lần 2 - Đặc tả chức năng (Bản cập nhật)

## Mục tiêu

Cập nhật tài liệu theo góp ý của Dev: bổ sung đặc tả Use Case, Business
Rule, CRUD, Workflow, Data Dictionary, State và NFR.

## Cấu trúc tài liệu

1.  Giới thiệu
2.  Actor
3.  Phân quyền (CRUD Matrix)
4.  Danh sách Module
5.  Use Case Specification
6.  Business Rule
7.  Data Dictionary
8.  State Machine
9.  Workflow
10. API Mapping
11. Non Functional Requirement

------------------------------------------------------------------------

## Use Case Specification (Mẫu chuẩn)

### UC0201 - Thêm Project

**Actor:** Quản lý tổ

**Mục tiêu:** Tạo mới dự án.

### Input

  Field         Required   Validation
  ------------- ---------- ------------------------
  ProjectCode   Yes        Duy nhất
  ProjectName   Yes        Không rỗng
  ProjectType   Yes        Danh mục
  PM            Yes        Người dùng có quyền PM
  StartDate     Yes        
  EndDate       Yes        \>= StartDate

### Business Rule

-   BR-PROJECT-001: Mã dự án duy nhất.
-   BR-PROJECT-002: Một dự án chỉ có một PM.
-   BR-PROJECT-003: Không được xóa dự án đã có Checklist.

### Validation

-   Không lưu khi ProjectCode trùng.
-   Không lưu khi EndDate \< StartDate.

### Exception

-   Rollback khi lỗi CSDL.

### Audit Log

-   Action
-   User
-   OldValue
-   NewValue
-   Time

### API

POST /api/project

### Database

-   Project
-   ProjectHistory
-   AuditLog

------------------------------------------------------------------------

## Các nội dung cần bổ sung theo góp ý Dev

### 1. Data Dictionary

-   Project
-   ProjectMember
-   Checklist
-   WeeklyReport
-   KPI
-   Comment
-   Attachment
-   Notification

### 2. State

-   Project: Planning → Doing → Pending → Done → Closed
-   Checklist: Todo → Doing → Waiting → Done → Cancelled
-   KPI: Draft → Calculated → PM Approved → Manager Approved → Locked

### 3. CRUD Matrix

-   Manager: CRUD Project/KPI
-   PM: CRUD Checklist, Report
-   Member: Update Task, Report Progress

### 4. Workflow KPI

Generate → PM Review → PM Approve → Manager Approve → Lock → Export →
Email

### 5. Business Rules

-   Một nhân sự có thể tham gia nhiều dự án.
-   Checklist không giao cho nhân sự đã rời dự án.
-   KPI sau khi Lock không được sửa.

### 6. Non Functional

-   Audit Log
-   Soft Delete
-   Paging
-   Performance
-   Security
-   Session Timeout

## Kế hoạch hoàn thiện

-   Khoảng 40 Use Case Specification.
-   Data Dictionary.
-   State Diagram.
-   Activity/Sequence Diagram.
-   Wireframe.
-   API Mapping.
