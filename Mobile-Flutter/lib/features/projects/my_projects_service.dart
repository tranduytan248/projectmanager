import '../../config/api_endpoint.dart';
import '../../config/app_http.dart';
import 'my_projects_models.dart';

class MyProjectsService {
  final _http = AppHttp();

  /// scope="team": toan bo du an cua Ca To (chi hieu luc voi tai khoan Quan ly To — server tu
  /// am tham tra ve "mine" cho tai khoan khac, xem MyProjectsApiController.Index).
  Future<MyProjectsData> fetch(
      {String? query, bool showClosed = false, String scope = 'mine'}) async {
    final response = await _http.get(
      ApiEndpoint.myProjects,
      params: {
        if (query != null && query.isNotEmpty) 'q': query,
        'showClosed': showClosed,
        'scope': scope,
      },
    );
    return MyProjectsData.fromJson(response.data as Map<String, dynamic>);
  }
}
