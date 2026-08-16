import '../../config/api_endpoint.dart';
import '../../config/app_http.dart';
import '../dashboard/dashboard_models.dart' show TaskItem;

class MyWorkService {
  final _http = AppHttp();

  /// scope="team": toan bo cong viec cua Ca To (chi hieu luc voi tai khoan Quan ly To — server tu
  /// am tham tra ve "mine" cho tai khoan khac, xem MyWorkApiController.Index). filter="overdue"/
  /// "open" loc them theo trang thai.
  Future<List<TaskItem>> fetch({String scope = 'mine', String? filter}) async {
    final response = await _http.get(
      ApiEndpoint.myWork,
      params: {
        'scope': scope,
        if (filter != null && filter.isNotEmpty) 'filter': filter,
      },
    );
    return (response.data as List<dynamic>? ?? [])
        .map((e) => TaskItem.fromJson(e as Map<String, dynamic>))
        .toList();
  }
}
