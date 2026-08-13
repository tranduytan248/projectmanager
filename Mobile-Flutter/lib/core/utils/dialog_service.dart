import 'package:flutter/material.dart';
import 'package:phosphor_icons/phosphor_icons.dart';

import '../../config/app_theme.dart';
import '../classes/app_keys.dart';

/// Dialog không cần BuildContext của màn hình hiện tại — dùng navigatorKey toàn cục, nên gọi
/// được từ bất kỳ đâu (ví dụ interceptor lỗi của HttpManager). Trả Future<bool>: true = người
/// dùng bấm nút xác nhận, false = bấm Hủy/đóng ra ngoài.
class DialogService {
  DialogService._();

  static BuildContext? get _context =>
      navigatorKey.currentState?.overlay?.context;

  static Future<bool> _show({
    required String title,
    required String message,
    required IconData icon,
    required Color accentColor,
    String confirmText = 'Đồng ý',
    String? cancelText,
  }) async {
    final context = _context;
    if (context == null) return false;

    final result = await showDialog<bool>(
      context: context,
      builder: (dialogContext) => AlertDialog(
        // Tieu de luon mau den (quy uoc chung toan app) — accentColor chi con dung lam mau icon
        // truoc tieu de de phan biet loai dialog, khong to mau chu nua.
        title: Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            PhosphorIcon(icon, color: accentColor, size: 22),
            const SizedBox(width: 8),
            Expanded(
              child: Text(title, style: const TextStyle(color: Colors.black87)),
            ),
          ],
        ),
        content: Text(message),
        actions: [
          if (cancelText != null)
            TextButton(
              onPressed: () => Navigator.of(dialogContext).pop(false),
              child: Text(cancelText),
            ),
          FilledButton(
            onPressed: () => Navigator.of(dialogContext).pop(true),
            child: Text(confirmText),
          ),
        ],
      ),
    );

    return result ?? false;
  }

  static Future<bool> showConfirm(String message, {String title = 'Xác nhận'}) {
    return _show(
      title: title,
      message: message,
      icon: PhosphorIconsRegular.question,
      accentColor: AppTheme.brandBlue,
      cancelText: 'Hủy',
    );
  }

  static Future<bool> showWarning(String message, {String title = 'Cảnh báo'}) {
    return _show(
      title: title,
      message: message,
      icon: PhosphorIconsRegular.warningCircle,
      accentColor: AppTheme.statusWarning,
    );
  }

  static Future<bool> showInfo(String message, {String title = 'Thông báo'}) {
    return _show(
      title: title,
      message: message,
      icon: PhosphorIconsRegular.info,
      accentColor: AppTheme.brandBlue,
    );
  }

  static Future<bool> showSuccess(String message,
      {String title = 'Thành công'}) {
    return _show(
      title: title,
      message: message,
      icon: PhosphorIconsRegular.checkCircle,
      accentColor: AppTheme.statusSuccess,
    );
  }

  static Future<bool> showError(String message, {String title = 'Lỗi'}) {
    return _show(
      title: title,
      message: message,
      icon: PhosphorIconsRegular.xCircle,
      accentColor: AppTheme.statusDanger,
    );
  }
}
