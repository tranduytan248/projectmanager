import '../../config/api_endpoint.dart';
import '../../config/app_http.dart';
import 'project_detail_models.dart';

class ProjectDetailService {
  final _http = AppHttp();

  Future<ProjectDetail> fetch(String projectId) async {
    final response = await _http.get(ApiEndpoint.projectDetail(projectId));
    return ProjectDetail.fromJson(response.data as Map<String, dynamic>);
  }
}
