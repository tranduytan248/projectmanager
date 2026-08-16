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

  /// [todos] la danh sach "Viec can lam" nhap ngay luc tao — dung FormData (nhu sendComment) thay
  /// vi x-www-form-urlencoded de gui duoc nhieu field "todos" cung ten, ASP.NET MVC tu bind vao
  /// string[] todos ben ChecklistApiController.Create.
  Future<CreateTaskResult> create({
    required int projectId,
    required String title,
    String? code,
    required String kind,
    required String priority,
    int assigneeUserId = 0,
    DateTime? startDate,
    required DateTime dueDate,
    String? description,
    List<String> todos = const [],
  }) async {
    try {
      final response = await _http.post(
        ApiEndpoint.checklistCreate,
        data: FormData.fromMap({
          'projectId': projectId,
          'title': title,
          'code': code ?? '',
          'kind': kind,
          'priority': priority,
          'assigneeUserId': assigneeUserId,
          // Cung khuon "yyyy-MM-dd" nhu input type=date ben web gui, tranh nham lan dinh dang
          // ngay/thang theo van hoa (culture) cua may chu.
          if (startDate != null) 'startDate': DateFormat('yyyy-MM-dd').format(startDate),
          'dueDate': DateFormat('yyyy-MM-dd').format(dueDate),
          'description': description ?? '',
          'todos': todos,
        }),
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
