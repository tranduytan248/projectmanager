import 'package:flutter/material.dart';

/// Giu themeMode dung chung cho MaterialApp. Hien chua co UI chuyen doi sang/toi — luon theo he
/// dieu hanh (ThemeMode.system), giu san ChangeNotifier de sau nay them nut chuyen theme khong
/// phai doi cho goi noi khac.
class ThemeProvider extends ChangeNotifier {
  ThemeMode _themeMode = ThemeMode.system;

  ThemeMode get themeMode => _themeMode;

  void setThemeMode(ThemeMode mode) {
    if (_themeMode == mode) return;
    _themeMode = mode;
    notifyListeners();
  }
}
