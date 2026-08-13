import 'package:flutter/material.dart';

import '../classes/app_keys.dart';

/// Dialog khong can BuildContext cua man hinh hien tai — dung navigatorKey toan cuc, nen goi
/// duoc tu bat ky dau (vi du interceptor loi cua HttpManager). Tra Future<bool>: true = nguoi
/// dung bam nut xac nhan, false = bam Huy/dong ra ngoai.
class DialogService {
  DialogService._();

  static BuildContext? get _context =>
      navigatorKey.currentState?.overlay?.context;

  static Future<bool> _show({
    required String title,
    required String message,
    required Color accentColor,
    String confirmText = 'Dong y',
    String? cancelText,
  }) async {
    final context = _context;
    if (context == null) return false;

    final result = await showDialog<bool>(
      context: context,
      builder: (dialogContext) => AlertDialog(
        title: Text(title, style: TextStyle(color: accentColor)),
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

  static Future<bool> showConfirm(String message, {String title = 'Xac nhan'}) {
    return _show(
        title: title,
        message: message,
        accentColor: Colors.blue,
        cancelText: 'Huy');
  }

  static Future<bool> showWarning(String message, {String title = 'Canh bao'}) {
    return _show(title: title, message: message, accentColor: Colors.orange);
  }

  static Future<bool> showInfo(String message, {String title = 'Thong bao'}) {
    return _show(title: title, message: message, accentColor: Colors.blue);
  }

  static Future<bool> showSuccess(String message,
      {String title = 'Thanh cong'}) {
    return _show(title: title, message: message, accentColor: Colors.green);
  }

  static Future<bool> showError(String message, {String title = 'Loi'}) {
    return _show(title: title, message: message, accentColor: Colors.red);
  }
}
