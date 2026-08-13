# Backlog - Mobile app (BrewTask)

Danh sach viec da duoc yeu cau nhung chua lam ngay, ghi lai de ra soat sau. Moi muc ghi ngay yeu
cau va boi canh ngan gon de khong mat y khi quay lai.

## Chua lam

### 1. Danh gia ATTT (an toan thong tin) cua cac API mobile
- Yeu cau: 2026-08-14
- Pham vi: `Controllers/Api/*` (AuthApi, DashboardApi, MyWorkApi, MyProjectsApi),
  `Infrastructure/ApiAuthorizeAttribute.cs`, `Models/Work/ApiToken.cs`.
- Goi y huong ra soat khi lam: thoi han token (hien 365 ngay - co qua dai khong), token luu
  plaintext trong bang ApiTokens (khong hash), khong co co che thu hoi token tu xa (dang xuat
  moi thiet bi/dang xuat toan bo), khong rate-limit AuthApi/Login (do vet mat khau), HTTPS bat
  buoc cho ban production (hien pm.vn local dang chay HTTP thuan de test), CORS (khong can voi
  app native nhung can neu co ban web/PWA sau nay).

### 2. Man "Cai dat" (Settings) tren mobile - thiet ke lai — DA LAM 2026-08-14
- Yeu cau: 2026-08-14
- Da lam: danh sach (khong phai luoi icon — hop hon voi noi dung do dai khac nhau) gom Thong
  tin ca nhan (bottom sheet: ho ten + vai tro), Chinh sach bao mat + Dieu khoan su dung (man
  tinh, noi dung viet moi vi chua co API/trang web tuong ung), Dang ky nghi phep (dan sang
  AppRoutes.leaves da co san), Thoat (dialog xac nhan roi dang xuat).
- Con thieu: "Thong tin ca nhan" moi chi hien ho ten + vai tro suy ra tu quyen (chua co API
  /auth/me tra ho so day du de sua doi mat khau, so dien thoai... — xem lai khi co API do).

### 3. Dashboard mobile hien theo vai tro (Bao cao cong viec / Quan ly / Quan tri)
- Yeu cau: 2026-08-14
- Nguoi dung tham chieu file thiet ke rieng `Design/ThietKe/Thiet ke lai theo vai tro.dc.html`
  (mockup web, co 3 vai tro: "Bao cao cong viec", "Quan ly", "Quan tri" — moi vai tro thay doi
  ca menu ben lan noi dung trang chinh).
- Dashboard mobile hien tai (`lib/features/dashboard/dashboard_screen.dart`) moi phan biet MOT
  phan theo quyen (khoi "Toan To" chi hien khi `CanSeeTeam`/IsTeamManager) — chua co khai niem
  "vai tro" ro rang nhu file thiet ke, va bottom nav 4 tab la co dinh cho moi nguoi.
- Can lam ro voi nguoi dung: dinh nghia vai tro tren mobile co giong 3 vai tro trong file thiet
  ke web khong, hay chi can nam trong 1 man Dashboard duy nhat (nhu hien tai) voi cac khoi bat/
  tat theo quyen la du.
