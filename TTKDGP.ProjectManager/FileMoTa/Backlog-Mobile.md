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

### 2. Man "Cai dat" (Settings) tren mobile - thiet ke lai
- Yeu cau: 2026-08-14
- Gom cac muc: Thong tin ca nhan, Chinh sach bao mat, Dieu khoan su dung, Dang ky nghi phep,
  Thoat (dang xuat).
- Kieu trinh bay: list hoac luoi icon (chua chot, can hoi lai khi bat tay lam).
- Man hien tai (`lib/features/profile/profile_screen.dart`) chi la placeholder trong (ten +
  TODO + nut Dang xuat).

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
