import '../../config/api_endpoint.dart';
import '../../config/app_http.dart';
import 'dashboard_models.dart';

class DashboardService {
  final _http = AppHttp();

  Future<DashboardData> fetch() async {
    final response = await _http.get(ApiEndpoint.dashboard);
    return DashboardData.fromJson(response.data as Map<String, dynamic>);
  }
}
