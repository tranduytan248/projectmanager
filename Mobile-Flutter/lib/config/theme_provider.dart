import 'package:flutter/material.dart';

/// Giu themeMode dung chung cho MaterialApp. Hien chua co UI chuyen doi sang/toi, va AppColors
/// (core/theme/app_colors.dart) MOI chi co MOT bang mau Sang — chua co bang mau Toi thuc su nao.
/// Neu de ThemeMode.system, may/trinh duyet o che do Toi se khien AppTheme.dark ap dung (nen toi
/// theo Material mac dinh) trong khi chu/mau content van cung mot bang AppColors sang co dinh —
/// ra chu toi tren nen toi, gan nhu khong doc duoc (vi du hop thoai DialogService). Khoa cung
/// ThemeMode.light cho toi khi co bang mau Toi day du — giu san ChangeNotifier de sau nay them
/// nut chuyen theme khong phai doi cho goi noi khac.
class ThemeProvider extends ChangeNotifier {
  ThemeMode _themeMode = ThemeMode.light;

  ThemeMode get themeMode => _themeMode;

  void setThemeMode(ThemeMode mode) {
    if (_themeMode == mode) return;
    _themeMode = mode;
    notifyListeners();
  }
}
