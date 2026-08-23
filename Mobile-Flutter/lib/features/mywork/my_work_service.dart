import '../../config/api_endpoint.dart';
import '../../config/app_http.dart';
import '../../core/services/data_cache.dart';
import '../dashboard/dashboard_models.dart' show TaskItem;

class MyWorkService {
  final _http = AppHttp();

  String _cacheKey(String scope, String? filter) => 'my_work_${scope}_${filter ?? "all"}';

  List<TaskItem>? getCached({String scope = 'mine', String? filter}) {
    return DataCache.instance.getStale<List<TaskItem>>(_cacheKey(scope, filter));
  }

  /// scope="team": toan bo cong viec cua Ca To (chi hieu luc voi tai khoan Quan ly To — server tu
  /// am tham tra ve "mine" cho tai khoan khac, xem MyWorkApiController.Index). filter="overdue"/
  /// "open" loc them theo trang thai.
  Future<List<TaskItem>> fetch({
    String scope = 'mine',
    String? filter,
    bool forceRefresh = false,
  }) async {
    final key = _cacheKey(scope, filter);
    final response = await _http.get(
      ApiEndpoint.myWork,
      params: {
        'scope': scope,
        if (filter != null && filter.isNotEmpty) 'filter': filter,
      },
    );
    final list = (response.data as List<dynamic>? ?? [])
        .map((e) => TaskItem.fromJson(e as Map<String, dynamic>))
        .toList();

    DataCache.instance.set(key, list, ttl: const Duration(minutes: 3));
    return list;
  }
}
