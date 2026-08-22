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

  /// = Visual Studio Code Primary Accent (Status Bar / Action Blue).
  static const primary = Color(0xFF007ACC);
  static const primaryDark = Color(0xFF0E639C);
  static const primaryDarker = Color(0xFF114A71);

  /// Nền container nhạt mang sắc thái xanh VS Code.
  static const primarySoft = Color(0xFF1B2B3E);

  /// Nền secondary (Sidebar panel).
  static const secondary = Color(0xFF252526);

  /// Nền ứng dụng chính: VS Code Editor Background (#1E1E1E).
  static const background = Color(0xFF1E1E1E);

  /// Nền thẻ card / panel: VS Code Sidebar Background (#252526).
  static const surface = Color(0xFF252526);

  /// Nền ô nhập liệu, activity bar (#2D2D2D).
  static const surfaceVariant = Color(0xFF2D2D2D);

  /// Chữ chính: VS Code Default Foreground (#D4D4D4) — tương phản 11.8:1 trên nền #1E1E1E.
  static const textPrimary = Color(0xFFD4D4D4);

  /// Chữ phụ: VS Code Muted / Comment Text (#9DA5B4) — tương phản 5.5:1 trên #1E1E1E.
  static const textSecondary = Color(0xFF9DA5B4);

  /// Chữ mờ: Placeholder, hint (#6A737D).
  static const textFaint = Color(0xFF6A737D);

  /// Chữ trên nền nút màu đậm.
  static const textOnPrimary = Colors.white;

  /// Trạng thái Hoàn thành / Đúng hạn: VS Code Class/Type Mint (#4EC9B0).
  static const success = Color(0xFF4EC9B0);
  static const successSoft = Color(0xFF17332B);
  static const successOnSoft = Color(0xFF4EC9B0);

  /// Trạng thái Cảnh báo / Đang làm: VS Code Function Gold (#DCDCAA).
  static const warning = Color(0xFFDCDCAA);
  static const warningSoft = Color(0xFF33301B);
  static const warningOnSoft = Color(0xFFDCDCAA);

  /// Trạng thái Quá hạn / Lỗi: VS Code Error Red (#F14C4C).
  static const danger = Color(0xFFF14C4C);
  static const dangerSoft = Color(0xFF3A1E1E);
  static const dangerOnSoft = Color(0xFFF14C4C);

  /// Chữ lỗi trên nền tối.
  static const errorOnDark = Color(0xFFFF7B72);

  /// Viền thẻ card / widget: VS Code Widget Border (#333333).
  static const border = Color(0xFF333333);

  /// Viền đậm / Divider focus: (#454545).
  static const borderStrong = Color(0xFF454545);

  /// Nền thanh điều hướng đáy / Status bar tối (#181818).
  static const navBackground = Color(0xFF181818);

  /// Xanh sáng cho link / tab đang chọn / focus viền (#3794FF).
  static const accentBlue = Color(0xFF3794FF);

  /// Nền nút bấm phụ (Secondary Button) (#333333).
  static const buttonSecondary = Color(0xFF333333);

  /// Trạng thái Thông tin / Khá: VS Code Info Blue (#0284C7 / #38BDF8).
  static const info = Color(0xFF38BDF8);
  static const infoSoft = Color(0xFF163244);
  static const infoOnSoft = Color(0xFF38BDF8);

  /// Huy hiệu xếp hạng (Rank Badges):
  static const rankGoldBg = Color(0xFF3E371C);
  static const rankGoldText = Color(0xFFDCDCAA);
  static const rankSilverBg = Color(0xFF2D3139);
  static const rankSilverText = Color(0xFF9DA5B4);
  static const rankBronzeBg = Color(0xFF38261D);
  static const rankBronzeText = Color(0xFFCE9178);

  /// Nền cho media viewer toàn màn hình.
  static const mediaViewerBackground = Color(0xFF121212);
}
