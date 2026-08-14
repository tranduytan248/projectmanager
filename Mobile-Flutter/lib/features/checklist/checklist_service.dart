import 'package:dio/dio.dart';
import 'package:intl/intl.dart';

import '../../config/api_endpoint.dart';
import '../../config/app_http.dart';
import '../dashboard/dashboard_models.dart';
import 'checklist_models.dart';

/// Ket qua goi ChecklistApi/Create. loi != null nghia la backend tu choi (thieu Ten/Han hoan
/// thanh, nguoi thuc hien khong thuoc du an...) — hien nguyen van cho nguoi dung, khop thong
/// diep BadRequest ben backend.
class CreateTaskResult {
  const CreateTaskResult._({this.task, this.error});

  final TaskItem? task;
  final String? error;

  bool get isSuccess => task != null;
}

class ChecklistService {
  final _http = AppHttp();

  Future<ChecklistData> fetch(int projectId) async {
    final response = await _http.get(
      ApiEndpoint.checklist,
      params: {'projectId': projectId},
    );
    return ChecklistData.fromJson(response.data as Map<String, dynamic>);
  }

  Future<CreateTaskResult> create({
    required int projectId,
    required String title,
    String? code,
    required String kind,
    required String priority,
    int assigneeUserId = 0,
    required DateTime dueDate,
    String? description,
  }) async {
    try {
      final response = await _http.post(
        ApiEndpoint.checklistCreate,
        data: {
          'projectId': projectId,
          'title': title,
          'code': code ?? '',
          'kind': kind,
          'priority': priority,
          'assigneeUserId': assigneeUserId,
          // Cung khuon "yyyy-MM-dd" nhu input type=date ben web gui, tranh nham lan dinh dang
          // ngay/thang theo van hoa (culture) cua may chu.
          'dueDate': DateFormat('yyyy-MM-dd').format(dueDate),
          'description': description ?? '',
        },
        options: Options(contentType: Headers.formUrlEncodedContentType),
      );
      return CreateTaskResult._(task: TaskItem.fromJson(response.data as Map<String, dynamic>));
    } on DioException catch (e) {
      final message = e.response?.statusCode == 400
          ? (e.response?.data is Map ? e.response?.data['error'] as String? : null)
          : null;
      return CreateTaskResult._(error: message ?? 'Không tạo được công việc. Hãy thử lại.');
    }
  }
}
