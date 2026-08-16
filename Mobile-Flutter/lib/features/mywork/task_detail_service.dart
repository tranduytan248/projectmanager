import 'dart:typed_data';

import 'package:dio/dio.dart';
import 'package:intl/intl.dart';

import '../../config/api_endpoint.dart';
import '../../config/app_http.dart';
import '../checklist/checklist_models.dart' show AssigneeOption;
import 'task_detail_models.dart';

/// Ket qua mot thao tac tren man Chi tiet cong viec (ghi/xoa gio, them/sua/xoa/tick viec can lam,
/// gui/thu hoi trao doi). loi != null nghia la backend tu choi — hien nguyen van cho nguoi dung,
/// cung khuon voi ProfileActionResult/CreateTaskResult.
class TaskDetailActionResult<T> {
  const TaskDetailActionResult._({this.data, this.error});

  final T? data;
  final String? error;

  bool get isSuccess => error == null;
}

class TaskDetailService {
  final _http = AppHttp();

  Future<TaskFullDetail> fetch(String taskId) async {
    final response = await _http.get(ApiEndpoint.taskDetail(taskId));
    return TaskFullDetail.fromJson(response.data as Map<String, dynamic>);
  }

  Future<TaskDetailActionResult<TimeLogSummary>> logTime({
    required int taskId,
    required DateTime workDate,
    required double hours,
    String? note,
  }) async {
    try {
      final response = await _http.post(
        ApiEndpoint.taskLogTime,
        data: {
          'id': taskId,
          'workDate': DateFormat('yyyy-MM-dd').format(workDate),
          'hours': hours.toString(),
          'note': note ?? '',
        },
        options: Options(contentType: Headers.formUrlEncodedContentType),
      );
      return TaskDetailActionResult._(
          data: TimeLogSummary.fromJson(response.data as Map<String, dynamic>));
    } on DioException catch (e) {
      return TaskDetailActionResult._(
          error: _errorOf(e, 'Không ghi được giờ công. Hãy thử lại.'));
    }
  }

  Future<TaskDetailActionResult<TimeLogSummary>> deleteTimeLog({
    required int taskId,
    required int logId,
  }) async {
    try {
      final response = await _http.post(
        ApiEndpoint.taskDeleteTimeLog,
        data: {'id': taskId, 'logId': logId},
        options: Options(contentType: Headers.formUrlEncodedContentType),
      );
      return TaskDetailActionResult._(
          data: TimeLogSummary.fromJson(response.data as Map<String, dynamic>));
    } on DioException catch (e) {
      return TaskDetailActionResult._(
          error: _errorOf(e, 'Không xoá được lượt ghi giờ. Hãy thử lại.'));
    }
  }

  Future<TaskDetailActionResult<TodoSummary>> addTodo({
    required int taskId,
    required String content,
  }) async {
    try {
      final response = await _http.post(
        ApiEndpoint.taskAddTodo,
        data: {'id': taskId, 'content': content},
        options: Options(contentType: Headers.formUrlEncodedContentType),
      );
      return TaskDetailActionResult._(
          data: TodoSummary.fromJson(response.data as Map<String, dynamic>));
    } on DioException catch (e) {
      return TaskDetailActionResult._(
          error: _errorOf(e, 'Không thêm được việc cần làm. Hãy thử lại.'));
    }
  }

  Future<TaskDetailActionResult<TodoSummary>> toggleTodo({
    required int taskId,
    required int todoId,
  }) async {
    try {
      final response = await _http.post(
        ApiEndpoint.taskToggleTodo,
        data: {'id': taskId, 'todoId': todoId},
        options: Options(contentType: Headers.formUrlEncodedContentType),
      );
      return TaskDetailActionResult._(
          data: TodoSummary.fromJson(response.data as Map<String, dynamic>));
    } on DioException catch (e) {
      return TaskDetailActionResult._(
          error: _errorOf(e, 'Không cập nhật được việc cần làm. Hãy thử lại.'));
    }
  }

  Future<TaskDetailActionResult<TodoSummary>> editTodo({
    required int taskId,
    required int todoId,
    required String content,
  }) async {
    try {
      final response = await _http.post(
        ApiEndpoint.taskEditTodo,
        data: {'id': taskId, 'todoId': todoId, 'content': content},
        options: Options(contentType: Headers.formUrlEncodedContentType),
      );
      return TaskDetailActionResult._(
          data: TodoSummary.fromJson(response.data as Map<String, dynamic>));
    } on DioException catch (e) {
      return TaskDetailActionResult._(
          error: _errorOf(e, 'Không sửa được việc cần làm. Hãy thử lại.'));
    }
  }

  Future<TaskDetailActionResult<TodoSummary>> deleteTodo({
    required int taskId,
    required int todoId,
  }) async {
    try {
      final response = await _http.post(
        ApiEndpoint.taskDeleteTodo,
        data: {'id': taskId, 'todoId': todoId},
        options: Options(contentType: Headers.formUrlEncodedContentType),
      );
      return TaskDetailActionResult._(
          data: TodoSummary.fromJson(response.data as Map<String, dynamic>));
    } on DioException catch (e) {
      return TaskDetailActionResult._(
          error: _errorOf(e, 'Không xoá được việc cần làm. Hãy thử lại.'));
    }
  }

  /// Gui mot luot trao doi. [contentHtml] la HTML da qua AppRichEditor.toHtml() (co the rong neu
  /// chi gui dinh kem). [attachmentPath] la duong dan file da chon qua file_picker — luon dung
  /// FormData (multipart) du co dinh kem hay khong, giong het cach <form multipart> ben web luon
  /// hoat dong bat ke co chon file hay chua.
  Future<TaskDetailActionResult<CommentsSummary>> sendComment({
    required int taskId,
    required String contentHtml,
    String? attachmentPath,
  }) async {
    try {
      final form = FormData.fromMap({
        'id': taskId,
        'content': contentHtml,
        if (attachmentPath != null)
          'file': await MultipartFile.fromFile(attachmentPath),
      });
      final response = await _http.post(ApiEndpoint.taskComment, data: form);
      return TaskDetailActionResult._(
          data:
              CommentsSummary.fromJson(response.data as Map<String, dynamic>));
    } on DioException catch (e) {
      return TaskDetailActionResult._(
          error: _errorOf(e, 'Không gửi được trao đổi. Hãy thử lại.'));
    }
  }

  /// Tai byte cua mot tep dinh kem — dung chung mot AppHttp (Dio) nen tu dong gan Bearer token,
  /// khong can xu ly xac thuc rieng.
  Future<Uint8List> downloadAttachment(int commentId) async {
    final response = await _http.get(
      ApiEndpoint.taskAttachment(commentId),
      options: Options(responseType: ResponseType.bytes),
    );
    return response.data as Uint8List;
  }

  Future<TaskDetailActionResult<CommentsSummary>> recallComment({
    required int taskId,
    required int commentId,
  }) async {
    try {
      final response = await _http.post(
        ApiEndpoint.taskRecallComment,
        data: {'id': taskId, 'commentId': commentId},
        options: Options(contentType: Headers.formUrlEncodedContentType),
      );
      return TaskDetailActionResult._(
          data:
              CommentsSummary.fromJson(response.data as Map<String, dynamic>));
    } on DioException catch (e) {
      return TaskDetailActionResult._(
          error: _errorOf(e, 'Không thu hồi được trao đổi. Hãy thử lại.'));
    }
  }

  /// Doi trang thai + tien do — mirror form "Bao cao tien do" ben web (MyWork/Report). loi != null
  /// khi backend tu choi, vi du chua ghi gio ma doi sang "Dang lam"/"Hoan thanh" (TimeLogService.
  /// ValidateStateChange), hien nguyen van cho nguoi dung.
  Future<TaskDetailActionResult<TaskFullDetail>> updateStatus({
    required int taskId,
    required String state,
    required int progress,
    String? note,
  }) async {
    try {
      final response = await _http.post(
        ApiEndpoint.taskUpdateStatus,
        data: {
          'id': taskId,
          'state': state,
          'progress': progress,
          'note': note ?? '',
        },
        options: Options(contentType: Headers.formUrlEncodedContentType),
      );
      return TaskDetailActionResult._(
          data: TaskFullDetail.fromJson(response.data as Map<String, dynamic>));
    } on DioException catch (e) {
      return TaskDetailActionResult._(
          error: _errorOf(e, 'Không cập nhật được trạng thái. Hãy thử lại.'));
    }
  }

  /// Nhat ky thao tac cua mot dau viec, moi nhat truoc.
  Future<List<TaskActivityLogEntry>> fetchActivityLog(int taskId) async {
    final response = await _http.get(ApiEndpoint.taskActivityLog('$taskId'));
    return (response.data as List<dynamic>)
        .map((e) => TaskActivityLogEntry.fromJson(e as Map<String, dynamic>))
        .toList();
  }

  /// Danh sach thanh vien du an de chon "Nguoi thuc hien" moi — chi goi duoc khi
  /// TaskDetail.canEditAll = true (server tu kiem lai, 404 neu khong co quyen).
  Future<List<AssigneeOption>> fetchAssigneeOptions(int taskId) async {
    final response =
        await _http.get(ApiEndpoint.taskAssigneeOptions('$taskId'));
    return (response.data as List<dynamic>)
        .map((e) => AssigneeOption.fromJson(e as Map<String, dynamic>))
        .toList();
  }

  /// Doi nguoi thuc hien — chi PM du an hoac Quan ly To (CanEditAllOfTask ben backend).
  /// assigneeUserId = 0 nghia la go giao.
  Future<TaskDetailActionResult<TaskFullDetail>> updateAssignee({
    required int taskId,
    required int assigneeUserId,
  }) async {
    try {
      final response = await _http.post(
        ApiEndpoint.taskUpdateAssignee,
        data: {'id': taskId, 'assigneeUserId': assigneeUserId},
        options: Options(contentType: Headers.formUrlEncodedContentType),
      );
      return TaskDetailActionResult._(
          data: TaskFullDetail.fromJson(response.data as Map<String, dynamic>));
    } on DioException catch (e) {
      return TaskDetailActionResult._(
          error: _errorOf(e, 'Không đổi được người thực hiện. Hãy thử lại.'));
    }
  }

  String _errorOf(DioException e, String fallback) {
    final message = e.response?.statusCode == 400
        ? (e.response?.data is Map
            ? e.response?.data['error'] as String?
            : null)
        : null;
    return message ?? fallback;
  }
}
