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
final List<VersionHistoryEntry> versionHistoryEntries = [
  VersionHistoryEntry(
    version: '1.01.001',
    releasedAt: DateTime(2026, 9, 3),
    changes: const [
      VersionHistoryChange(
        type: VersionChangeType.newFeature,
        description:
            'Giao diện Dark Theme mới toanh chuẩn VS Code, cực ngầu và dịu mắt khi chạy việc đêm ☕✨',
      ),
      VersionHistoryChange(
        type: VersionChangeType.newFeature,
        description:
            'Bộ đôi tiến độ tròn & thanh ngang mới, theo dõi checklist công việc trực quan hơn 🎯',
      ),
      VersionHistoryChange(
        type: VersionChangeType.newFeature,
        description:
            'Nâng cấp trợ năng WCAG 2.2, hỗ trợ đọc màn hình chu đáo hơn cho mọi người dùng 💖',
      ),
      VersionHistoryChange(
        type: VersionChangeType.bugFix,
        description:
            'Tối ưu bộ nhớ đệm DataCache, mở app và tải việc siêu tốc, êm ái không độ trễ 🚀',
      ),
      VersionHistoryChange(
        type: VersionChangeType.bugFix,
        description:
            'Dọn sạch các lỗi giao diện nhỏ, thao tác bấm chạm mượt mà chuẩn xác từng pixel ✨',
      ),
    ],
  ),
  VersionHistoryEntry(
    version: '1.0.0',
    releasedAt: DateTime(2026, 8, 15),
    changes: const [
      VersionHistoryChange(
        type: VersionChangeType.newFeature,
        description:
            'Chào sân BrewTask! Ứng dụng quản lý dự án, công việc và KPI dành riêng cho Tổ NCPT 🎉',
      ),
    ],
  ),
];
