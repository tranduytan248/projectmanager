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

### 4. Man "Chi tiet du an" mobile — DA LAM 2026-08-14, CHUA DAY DU
- Yeu cau: 2026-08-14, doi chieu WorkProjects/Details ben web + nut Checklist.
- Da lam: `MyProjectsApi/Detail/{id}` + man Flutter voi thong tin chung, thong ke
  (thanh vien/checklist/qua han/ho tro tuan nay), danh sach nhan su, dau viec qua han, bao cao
  tuan gan day — dung logic CanViewProject nhu web (thanh vien/PM deu xem duoc).
- CO CHU DICH BO 2 phan web co ma mobile CHUA lam:
  1. "Thong tin trien khai" (Github/SVN/FTP/DB — ca mat khau): du lieu nhay cam, ban da co token
     API song 365 ngay (xem muc 1) — khong dua them credential nhay cam len thiet bi di dong
     truoc khi ra soat ATTT xong.
  2. "Tai lieu du an" (file dinh kem): can them co che tai file qua token Bearer (endpoint web
     hien dung Forms Auth cookie) hoac package rieng (url_launcher chua co trong pubspec) — chua
     lam trong luot nay.
- Nut "Checklist cong viec cua du an" dan sang `ChecklistBoardScreen` — DA LAM 2026-08-14, xem
  muc 5.
- Nut "Cap nhat nhan su" (chi PM/Quan ly To thay, dua theo p.canEdit) — DA THEM 2026-08-14 nhung
  moi la nut, bam vao chi hien toast "dang phat trien". Chua co man/API sua nhan su
  (WorkProjects/Members ben web) — can lam rieng.

### 5. Man "Checklist" (danh sach dau viec cua 1 du an) — DA LAM 2026-08-14, CHUA DAY DU
- Yeu cau: 2026-08-14, doi chieu Checklist?projectId= ben web (ban GRID, khong phai Kanban).
- Da lam: `ChecklistApi/Index?projectId=` + man Flutter danh sach PHANG (khong dung cay cha/con
  nhu web — BuildTree), loc theo tu khoa + trang thai o client, moi dong hien ma/ten, nguoi thuc
  hien, uu tien, han, tien do, badge trang thai.
- CO CHU DICH BO so voi web:
  1. Khong co Kanban (keo-tha khong hop dien thoai).
  2. Khong dung cay cha/con (thut le theo Depth) — danh sach phang, sap theo qua han truoc.
  3. Khong co Trao doi (so luong comment) va Gio cong da ghi tren tung dong — can
     WorkService.CommentCounts/TimeLogService.TotalsByTask, chua goi trong ChecklistApi.
  4. Khong co nut "Them muc"/"Import"/"Sua"/"Xoa" — man chi doc (xem, chua sua ngay tai day).
- Bam vao 1 dong dan sang `TaskDetailScreen` (route AppRoutes.taskDetail) — man do VAN con la
  placeholder, chua noi API that (info + Gio cong + Viec can lam + Trao doi cua _Detail.cshtml/
  _TimeLogs.cshtml/_Todos.cshtml) — day la viec lon nhat con lai, nen lam tiep theo.
