import '../../config/api_endpoint.dart';
import '../../config/app_http.dart';
import 'team_dashboard_models.dart';

class TeamDashboardService {
  final _http = AppHttp();

  Future<TeamDashboardData> fetchDashboard({int? year, int? month}) async {
    final response = await _http.get(
      ApiEndpoint.teamDashboard,
      params: {
        if (year != null) 'year': year,
        if (month != null) 'month': month,
      },
    );

    final data = response.data;
    if (data is Map<String, dynamic>) {
      if (data.containsKey('data') && data['data'] is Map<String, dynamic>) {
        return TeamDashboardData.fromJson(data['data'] as Map<String, dynamic>);
      }
      return TeamDashboardData.fromJson(data);
    }
    throw Exception('Dữ liệu trả về từ máy chủ không hợp lệ.');
  }

  Future<TeamMemberTasksResult> fetchMemberTasks({
    required int userId,
    required int year,
    required int month,
    required String kind,
  }) async {
    final response = await _http.get(
      ApiEndpoint.teamDashboardMemberTasks,
      params: {
        'userId': userId,
        'year': year,
        'month': month,
        'kind': kind,
      },
    );

    final data = response.data;
    if (data is Map<String, dynamic>) {
      if (data.containsKey('data') && data['data'] is Map<String, dynamic>) {
        return TeamMemberTasksResult.fromJson(data['data'] as Map<String, dynamic>);
      }
      return TeamMemberTasksResult.fromJson(data);
    }
    throw Exception('Dữ liệu trả về từ máy chủ không hợp lệ.');
  }
}
