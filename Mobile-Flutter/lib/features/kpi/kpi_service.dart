import '../../config/api_endpoint.dart';
import '../../config/app_http.dart';
import '../../core/services/data_cache.dart';
import 'kpi_models.dart';

class KpiService {
  final _http = AppHttp();

  String _indexCacheKey(int? year, int? month, int? userId) =>
      'kpi_index_${year ?? 0}_${month ?? 0}_${userId ?? 0}';

  KpiIndexData? getCachedIndex({int? year, int? month, int? userId}) {
    return DataCache.instance.getStale<KpiIndexData>(_indexCacheKey(year, month, userId));
  }

  Future<KpiIndexData> fetchIndex({
    int? year,
    int? month,
    int? userId,
    bool forceRefresh = false,
  }) async {
    final key = _indexCacheKey(year, month, userId);
    final response = await _http.get(
      ApiEndpoint.kpiIndex,
      params: {
        if (year != null) 'year': year,
        if (month != null) 'month': month,
        if (userId != null && userId > 0) 'userId': userId,
      },
    );

    final data = response.data;
    if (data is Map) {
      final map = Map<String, dynamic>.from(data);
      final result = map.containsKey('data') && map['data'] is Map
          ? KpiIndexData.fromJson(Map<String, dynamic>.from(map['data'] as Map))
          : KpiIndexData.fromJson(map);
      DataCache.instance.set(key, result, ttl: const Duration(minutes: 5));
      return result;
    }
    throw Exception('Dữ liệu KPI trả về từ máy chủ không hợp lệ.');
  }

  Future<KpiDetailData> fetchDetail({
    required int userId,
    int? year,
    int? month,
  }) async {
    final response = await _http.get(
      ApiEndpoint.kpiDetail,
      params: {
        'userId': userId,
        if (year != null) 'year': year,
        if (month != null) 'month': month,
      },
    );

    final data = response.data;
    if (data is Map) {
      final map = Map<String, dynamic>.from(data);
      if (map.containsKey('data') && map['data'] is Map) {
        return KpiDetailData.fromJson(Map<String, dynamic>.from(map['data'] as Map));
      }
      return KpiDetailData.fromJson(map);
    }
    throw Exception('Dữ liệu chi tiết KPI trả về từ máy chủ không hợp lệ.');
  }

  Future<String> calculateAll({required int year, required int month}) async {
    final response = await _http.post(
      ApiEndpoint.kpiCalculate,
      data: {
        'year': year,
        'month': month,
      },
    );

    DataCache.instance.clear();
    final data = response.data;
    if (data is Map<String, dynamic>) {
      return (data['message'] ?? 'Đã chốt tính KPI tháng $month/$year.').toString();
    }
    return 'Đã chốt tính KPI tháng $month/$year.';
  }

  Future<String> recalculateUser({
    required int year,
    required int month,
    required int userId,
  }) async {
    final response = await _http.post(
      ApiEndpoint.kpiRecalculate,
      data: {
        'year': year,
        'month': month,
        'userId': userId,
      },
    );

    DataCache.instance.clear();
    final data = response.data;
    if (data is Map<String, dynamic>) {
      return (data['message'] ?? 'Đã tính lại KPI cho nhân sự.').toString();
    }
    return 'Đã tính lại KPI cho nhân sự.';
  }
}
