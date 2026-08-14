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

  /// = --primary. Trung voi mau nen icon_app.png va splash nguyen sinh cua he dieu hanh.
  static const primary = Color(0xFF1A56A8);
  static const primaryDark = Color(0xFF143F7D);
  static const primaryDarker = Color(0xFF10386F);

  /// = --primary-soft. Nen nhat cho khoi/badge lien quan toi thao tac chinh.
  static const primarySoft = Color(0xFFEAF1FB);

  /// Chua co mau nhan dien phu rieng — dung chung sac dam cua primary.
  static const secondary = primaryDark;

  static const background = Colors.white;
  static const surface = Colors.white;

  /// = --text. Chu chinh: tieu de, noi dung quan trong, gia tri du lieu.
  static const textPrimary = Color(0xFF16202C);

  /// = --text-muted. Chu phu: nhan, ghi chu, mo ta phu — mau DAC (khong trong suot), khac han
  /// Colors.black45/Colors.black54 (den trong suot) tung dung truoc day: hai mau do doc mo tren
  /// nen trang, day la mau da duoc web dung san va kiem chung de doc.
  static const textSecondary = Color(0xFF667289);

  /// = --text-faint. Nhat hon textSecondary mot chut — tuong phan ~3.3:1 tren nen trang, DUOI
  /// chuan AA (4.5:1) cho chu thuong. CHI dung cho placeholder/hint (o nhap chua co gia tri, dropdown
  /// chua chon) hoac icon nho (nguong UI component 3:1) — KHONG dung cho nhan/trang thai/badge
  /// mang thong tin that (dung textSecondary cho cac truong hop do).
  static const textFaint = Color(0xFF8B96A9);

  /// Chu tren nen mau dam (vi du tren nut primary, banner mau).
  static const textOnPrimary = Colors.white;

  /// = --success/--st-ok. Trang thai "hoan thanh/dung han" — KHONG dung primary cho y nghia nay.
  static const success = Color(0xFF1A7F52);
  static const successSoft = Color(0xFFE6F5EE);

  /// = --warn/--st-warn. Trang thai "sap den han/canh bao".
  static const warning = Color(0xFF9A6700);
  static const warningSoft = Color(0xFFFDF5E2);

  /// = --danger/--st-late. Trang thai "qua han/loi".
  static const danger = Color(0xFFB3261E);
  static const dangerSoft = Color(0xFFFBEAE9);

  /// Vien mong dung khap app (the/card, o nhap, chip).
  static const border = Color(0x14000000);
  static const borderStrong = Color(0x1F000000);
}
