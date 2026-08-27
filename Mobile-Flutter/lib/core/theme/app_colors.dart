import 'package:flutter/material.dart';

/// Bang mau theo VAI TRO (khong theo ten mau), dung `.claude/rules/FLUTTER_RULES.md`.
///
/// Gia tri dong bo voi bien --primary/--success/--text-muted... trong
/// TTKDGP.ProjectManager/Content/site.css — web va mobile dung chung mot he mau thuong hieu, sua
/// o site.css thi nho sua lai o day cho khop.
///
/// Cac man hinh (features/) chi duoc doc mau tu day (hoac gian tiep qua widget App*) — khong
/// hard-code Colors.black45/Colors.blue... rai rac.
class AppColors {
  AppColors._();

  /// = Sắc xanh sáng rõ nét độ tương phản cao (Sky Blue 400 - #38BDF8).
  /// Đạt chuẩn WCAG AAA trên nền đen (#1E1E1E), hiển thị sắc nét trên mọi loại màn hình.
  static const primary = Color(0xFF38BDF8);
  static const primaryDark = Color(0xFF0284C7);
  static const primaryDarker = Color(0xFF0369A1);

  /// Nền container nhạt mang sắc thái xanh rõ ràng, không bị đen đục.
  static const primarySoft = Color(0xFF0C2E4E);

  /// Nền secondary (Sidebar panel).
  static const secondary = Color(0xFF252526);

  /// Nền ứng dụng chính: VS Code Editor Background (#1E1E1E).
  static const background = Color(0xFF1E1E1E);

  /// Nền thẻ card / panel: VS Code Sidebar Background (#252526).
  static const surface = Color(0xFF252526);

  /// Nền ô nhập liệu, activity bar (#2D2D2D).
  static const surfaceVariant = Color(0xFF2D2D2D);

  /// Chữ chính: Trắng sáng (#F1F5F9) — tương phản > 14:1 trên nền tối.
  static const textPrimary = Color(0xFFF1F5F9);

  /// Chữ phụ: Xám sáng (#CBD5E1) — tương phản > 7:1, không bị chìm trên màn hình tối.
  static const textSecondary = Color(0xFFCBD5E1);

  /// Chữ mờ: Placeholder, hint (#94A3B8).
  static const textFaint = Color(0xFF94A3B8);

  /// Chữ trên nền nút màu đậm hoặc nút xanh sáng.
  static const textOnPrimary = Color(0xFF0F172A);

  /// Trạng thái Hoàn thành / Đúng hạn: Mint sáng (#4ADE80 / #4EC9B0).
  static const success = Color(0xFF4ADE80);
  static const successSoft = Color(0xFF143823);
  static const successOnSoft = Color(0xFF4ADE80);

  /// Trạng thái Cảnh báo / Đang làm: Vàng sáng (#FDE047 / #DCDCAA).
  static const warning = Color(0xFFFDE047);
  static const warningSoft = Color(0xFF3B3314);
  static const warningOnSoft = Color(0xFFFDE047);

  /// Trạng thái Quá hạn / Lỗi: Đỏ sáng (#F87171 / #F14C4C).
  static const danger = Color(0xFFF87171);
  static const dangerSoft = Color(0xFF3F1D1D);
  static const dangerOnSoft = Color(0xFFF87171);

  /// Chữ lỗi trên nền tối.
  static const errorOnDark = Color(0xFFFF8B8B);

  /// Viền thẻ card / widget: (#383838).
  static const border = Color(0xFF383838);

  /// Viền đậm / Divider focus: (#4B5563).
  static const borderStrong = Color(0xFF4B5563);

  /// Nền thanh điều hướng đáy / Status bar tối (#181818).
  static const navBackground = Color(0xFF181818);

  /// Xanh sáng nổi bật cho link / tab đang chọn / focus viền (#60A5FA / #38BDF8).
  static const accentBlue = Color(0xFF60A5FA);

  /// Nền nút bấm phụ (Secondary Button) (#333333).
  static const buttonSecondary = Color(0xFF333333);

  /// Trạng thái Thông tin / Khá: Sky Blue (#38BDF8).
  static const info = Color(0xFF38BDF8);
  static const infoSoft = Color(0xFF0C2E4E);
  static const infoOnSoft = Color(0xFF38BDF8);

  /// Huy hiệu xếp hạng (Rank Badges):
  static const rankGoldBg = Color(0xFF3E371C);
  static const rankGoldText = Color(0xFFFDE047);
  static const rankSilverBg = Color(0xFF2D3139);
  static const rankSilverText = Color(0xFFCBD5E1);
  static const rankBronzeBg = Color(0xFF38261D);
  static const rankBronzeText = Color(0xFFFDBA74);

  /// Nền cho media viewer toàn màn hình.
  static const mediaViewerBackground = Color(0xFF121212);
}
