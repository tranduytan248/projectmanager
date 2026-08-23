/// Bo nho dem In-Memory nhe nhang voi TTL de toi uu hoa toc do tai du lieu API.
/// Giup cac man hinh hien thi du lieu ngay lap tuc (0ms) khi chuyen tab (Stale-While-Revalidate).
class DataCache {
  DataCache._();

  static final DataCache instance = DataCache._();

  final Map<String, _CacheEntry<dynamic>> _cache = {};

  /// Luu du lieu vao bo nho dem voi thoi gian song mac dinh 5 phut
  void set<T>(String key, T value, {Duration ttl = const Duration(minutes: 5)}) {
    _cache[key] = _CacheEntry<T>(
      value: value,
      expiresAt: DateTime.now().add(ttl),
    );
  }

  /// Lay du lieu tu bo nho dem neu con han
  T? get<T>(String key) {
    final entry = _cache[key];
    if (entry == null) return null;

    if (entry.isExpired) {
      _cache.remove(key);
      return null;
    }

    return entry.value as T?;
  }

  /// Lay du lieu bat ke da het han hay chua (dung de hien thi ngay trong luc cho reload ngam)
  T? getStale<T>(String key) {
    return _cache[key]?.value as T?;
  }

  /// Xoa mot khoa cu the
  void remove(String key) {
    _cache.remove(key);
  }

  /// Xoa toan bo cache khi dang xuat hoac pull-to-refresh toan bo
  void clear() {
    _cache.clear();
  }
}

class _CacheEntry<T> {
  _CacheEntry({required this.value, required this.expiresAt});

  final T value;
  final DateTime expiresAt;

  bool get isExpired => DateTime.now().isAfter(expiresAt);
}
