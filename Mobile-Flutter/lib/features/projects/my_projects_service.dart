import '../../config/api_endpoint.dart';
import '../../config/app_http.dart';
import 'my_projects_models.dart';

class MyProjectsService {
  final _http = AppHttp();

  Future<MyProjectsData> fetch({String? query, bool showClosed = false}) async {
    final response = await _http.get(
      ApiEndpoint.myProjects,
      params: {
        if (query != null && query.isNotEmpty) 'q': query,
        'showClosed': showClosed,
      },
    );
    return MyProjectsData.fromJson(response.data as Map<String, dynamic>);
  }
}
