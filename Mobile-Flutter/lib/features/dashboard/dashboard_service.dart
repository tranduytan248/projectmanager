import '../../config/api_endpoint.dart';
import '../../config/app_http.dart';
import '../../core/services/data_cache.dart';
import 'dashboard_models.dart';

class DashboardService {
  final _http = AppHttp();
  static const _cacheKey = 'dashboard_data';

  DashboardData? get cachedData => DataCache.instance.getStale<DashboardData>(_cacheKey);

  Future<DashboardData> fetch({bool forceRefresh = false}) async {
    final response = await _http.get(ApiEndpoint.dashboard);
    final data = DashboardData.fromJson(response.data as Map<String, dynamic>);
    DataCache.instance.set(_cacheKey, data, ttl: const Duration(minutes: 5));
    return data;
  }
}
