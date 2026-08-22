import '../../config/api_endpoint.dart';
import '../../config/app_http.dart';
import 'kpi_models.dart';

class KpiService {
  final _http = AppHttp();

  Future<KpiIndexData> fetchIndex({int? year, int? month, int? userId}) async {
    final response = await _http.get(
      ApiEndpoint.kpiIndex,
      params: {
        if (year != null) 'year': year,
        if (month != null) 'month': month,
        if (userId != null && userId > 0) 'userId': userId,
      },
    );

    final data = response.data;
    if (data is Map<String, dynamic>) {
      if (data.containsKey('data') && data['data'] is Map<String, dynamic>) {
        return KpiIndexData.fromJson(data['data'] as Map<String, dynamic>);
      }
      return KpiIndexData.fromJson(data);
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
    if (data is Map<String, dynamic>) {
      if (data.containsKey('data') && data['data'] is Map<String, dynamic>) {
        return KpiDetailData.fromJson(data['data'] as Map<String, dynamic>);
      }
      return KpiDetailData.fromJson(data);
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

    final data = response.data;
    if (data is Map<String, dynamic>) {
      return (data['message'] ?? 'Đã tính lại KPI cho nhân sự.').toString();
    }
    return 'Đã tính lại KPI cho nhân sự.';
  }
}
