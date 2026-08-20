/// Loai thay doi trong mot dot phat hanh — dung de chon mau/nhan hien thi phu hop
/// (xem `_ChangeTypeBadge` trong `version_history_screen.dart`).
enum VersionChangeType {
  /// Tinh nang moi duoc them vao.
  newFeature,

  /// Loi da duoc sua.
  bugFix,
}

/// Mot dong thay doi trong changelog cua mot phien ban.
class VersionHistoryChange {
  const VersionHistoryChange({required this.type, required this.description});

  final VersionChangeType type;

  /// Mo ta ngan gon noi dung thay doi, hien thi truc tiep tren man — phai la tieng Viet co dau.
  final String description;
}

/// Mot dot phat hanh, gom so hieu phien ban, ngay phat hanh va danh sach thay doi.
class VersionHistoryEntry {
  const VersionHistoryEntry({
    required this.version,
    required this.releasedAt,
    required this.changes,
  });

  /// So hieu phien ban hien thi (vi du "1.1.0") — khong bat buoc khop tuyet doi voi
  /// `pubspec.yaml`, do nguoi cap nhat danh sach nay tu tay dien theo tung dot phat hanh.
  final String version;

  final DateTime releasedAt;

  final List<VersionHistoryChange> changes;
}

/// Danh sach lich su phien ban hien thi tren man "Cac phien ban cap nhat".
///
/// QUAN TRONG: de TRONG that su — noi dung changelog thuc te (dot nao co tinh nang gi, sua loi
/// gi) se duoc nguoi phu trach cung cap va dien vao day o mot phien lam viec sau, KHONG duoc tu
/// bia noi dung. Man hinh da duoc dung san de xu ly dung truong hop danh sach rong (xem trang
/// thai rong trong `version_history_screen.dart`).
const List<VersionHistoryEntry> versionHistoryEntries = [];
