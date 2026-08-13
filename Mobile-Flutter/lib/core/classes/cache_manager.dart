import 'dart:developer';

import 'package:shared_preferences/shared_preferences.dart';

/// Lop nen luu key-value cho AppCache. Chi ho tro int|String|bool truc tiep — cac gia tri
/// phuc tap hon (object, list) do lop phia tren (AppCache) tu jsonEncode thanh String roi luu.
class Cache {
  Cache._();

  static SharedPreferences? _prefs;

  static Future<void> init() async {
    _prefs ??= await SharedPreferences.getInstance();
  }

  static SharedPreferences get _instance {
    final prefs = _prefs;
    if (prefs == null) {
      throw StateError('Cache.init() chua duoc goi truoc khi dung Cache.');
    }
    return prefs;
  }

  static Future<bool> saveData(String key, dynamic value) async {
    if (value is int) return _instance.setInt(key, value);
    if (value is String) return _instance.setString(key, value);
    if (value is bool) return _instance.setBool(key, value);

    log('[Cache] Invalid Type cho key "$key": ${value.runtimeType}');
    return false;
  }

  static T? readData<T>(String key) => _instance.get(key) as T?;

  static Future<bool> deleteData(String key) => _instance.remove(key);
}
