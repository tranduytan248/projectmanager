import 'package:flutter_test/flutter_test.dart';
import 'package:ttkdgp_mobile/core/services/data_cache.dart';

void main() {
  group('DataCache - Bộ nhớ đệm In-Memory với TTL', () {
    late DataCache cache;

    setUp(() {
      cache = DataCache.instance;
      cache.clear(); // Xóa sạch bộ nhớ đệm trước mỗi ca kiểm thử
    });

    tearDown(() {
      cache.clear();
    });

    // =========================================================================
    // 1. HAPPY PATH: Các luồng hoạt động chuẩn mực
    // =========================================================================
    group('1. Happy Path (Dòng xử lý chuẩn)', () {
      test('Lưu và lấy dữ liệu String thành công khi còn hạn TTL', () {
        // Arrange
        const key = 'user_name';
        const value = 'Trần Duy Tân';

        // Act
        cache.set(key, value, ttl: const Duration(minutes: 5));
        final result = cache.get<String>(key);

        // Assert
        expect(result, equals(value));
      });

      test('Lưu và lấy dữ liệu phức tạp (Map, List) thành công', () {
        // Arrange
        const listKey = 'task_ids';
        final listValue = [1, 2, 3, 4, 5];

        const mapKey = 'project_info';
        final mapValue = {
          'id': 101,
          'name': 'Dự án Quản lý nhân sự',
          'is_active': true,
        };

        // Act
        cache.set(listKey, listValue);
        cache.set(mapKey, mapValue);

        final retrievedList = cache.get<List<int>>(listKey);
        final retrievedMap = cache.get<Map<String, dynamic>>(mapKey);

        // Assert
        expect(retrievedList, equals(listValue));
        expect(retrievedList?.length, equals(5));
        expect(retrievedMap, equals(mapValue));
        expect(retrievedMap?['name'], equals('Dự án Quản lý nhân sự'));
      });

      test('Xóa một khóa cụ thể thành công bằng remove()', () {
        // Arrange
        cache.set('key_1', 'Giá trị 1');
        cache.set('key_2', 'Giá trị 2');

        // Act
        cache.remove('key_1');

        // Assert
        expect(cache.get<String>('key_1'), isNull);
        expect(cache.get<String>('key_2'), equals('Giá trị 2'));
      });

      test('Xóa toàn bộ bộ nhớ đệm thành công bằng clear()', () {
        // Arrange
        cache.set('a', 100);
        cache.set('b', 200);
        cache.set('c', 300);

        // Act
        cache.clear();

        // Assert
        expect(cache.get<int>('a'), isNull);
        expect(cache.get<int>('b'), isNull);
        expect(cache.get<int>('c'), isNull);
      });
    });

    // =========================================================================
    // 2. EDGE CASES: Các trường hợp biên và dữ liệu dị biệt
    // =========================================================================
    group('2. Edge Cases (Trường hợp biên)', () {
      test('Truy xuất khóa không tồn tại phải trả về null', () {
        // Act
        final result = cache.get<String>('non_existent_key');

        // Assert
        expect(result, isNull);
      });

      test('Lưu trữ và lấy giá trị rỗng (chuỗi rỗng, mảng rỗng, map rỗng)', () {
        // Arrange
        const emptyStrKey = 'empty_str';
        const emptyListKey = 'empty_list';
        const emptyMapKey = 'empty_map';

        // Act
        cache.set(emptyStrKey, '');
        cache.set(emptyListKey, <String>[]);
        cache.set(emptyMapKey, <String, dynamic>{});

        // Assert
        expect(cache.get<String>(emptyStrKey), equals(''));
        expect(cache.get<List<String>>(emptyListKey), isEmpty);
        expect(cache.get<Map<String, dynamic>>(emptyMapKey), isEmpty);
      });

      test('Ghi đè giá trị cho khóa đã tồn tại (Overwrite)', () {
        // Arrange
        const key = 'status';
        cache.set(key, 'Pending');

        // Act: Ghi đè bằng giá trị mới
        cache.set(key, 'Approved');

        // Assert
        expect(cache.get<String>(key), equals('Approved'));
      });

      test('Khóa là chuỗi rỗng vẫn hoạt động chính xác', () {
        // Arrange
        const emptyKey = '';

        // Act
        cache.set(emptyKey, 'Dữ liệu của key rỗng');

        // Assert
        expect(cache.get<String>(emptyKey), equals('Dữ liệu của key rỗng'));
      });
    });

    // =========================================================================
    // 3. ERROR HANDLING & LIFECYCLE: Hết hạn TTL & Cơ chế Stale-While-Revalidate
    // =========================================================================
    group('3. Error Handling & TTL Expiry (Vòng đời & Hết hạn TTL)', () {
      test('Khóa hết hạn TTL: get() tự động dọn dẹp và trả về null', () async {
        // Arrange: Thiết lập TTL cực ngắn (20ms)
        const key = 'short_lived_token';
        cache.set(key, 'secret_token_123', ttl: const Duration(milliseconds: 20));

        // Kiểm tra ngay lúc vừa set: còn hạn
        expect(cache.get<String>(key), equals('secret_token_123'));

        // Chờ hết hạn (40ms)
        await Future<void>.delayed(const Duration(milliseconds: 40));

        // Act: get() sau khi đã quá hạn TTL
        final expiredResult = cache.get<String>(key);

        // Assert
        expect(expiredResult, isNull);
      });

      test('Khóa hết hạn TTL: getStale() vẫn lấy được dữ liệu cũ (Stale-While-Revalidate)', () async {
        // Arrange: Thiết lập TTL 20ms
        const key = 'kpi_dashboard_data';
        cache.set(key, 'Dữ liệu KPI cũ', ttl: const Duration(milliseconds: 20));

        // Chờ cho hết hạn TTL
        await Future<void>.delayed(const Duration(milliseconds: 40));

        // Act
        final normalGet = cache.get<String>(key); // Sẽ dọn dẹp cache nếu gọi get()
        // Lưu ý: Nếu getStale() được gọi trước khi get() xóa, hoặc gọi trực tiếp:
        cache.set(key, 'Dữ liệu KPI cũ 2', ttl: const Duration(milliseconds: 20));
        await Future<void>.delayed(const Duration(milliseconds: 40));

        final staleResult = cache.getStale<String>(key);

        // Assert: getStale() không kiểm tra isExpired, vẫn trả về dữ liệu để hiển thị tức thì
        expect(normalGet, isNull);
        expect(staleResult, equals('Dữ liệu KPI cũ 2'));
      });

      test('getStale() trên khóa không tồn tại trả về null', () {
        // Act
        final result = cache.getStale<String>('never_cached_key');

        // Assert
        expect(result, isNull);
      });

      test('Tính nhất quán của Singleton instance', () {
        // Arrange
        final instanceA = DataCache.instance;
        final instanceB = DataCache.instance;

        // Assert
        expect(identical(instanceA, instanceB), isTrue);
      });
    });
  });
}
