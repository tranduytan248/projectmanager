import 'package:dio/dio.dart';
import 'package:intl/intl.dart';

import '../../config/api_endpoint.dart';
import '../../config/app_http.dart';
import 'private_tasks_models.dart';

class PrivateTaskApiResult {
  const PrivateTaskApiResult._({this.item, this.error, this.isSuccess = false});

  final PrivateTaskItem? item;
  final String? error;
  final bool isSuccess;

  factory PrivateTaskApiResult.success([PrivateTaskItem? item]) =>
      PrivateTaskApiResult._(item: item, isSuccess: true);

  factory PrivateTaskApiResult.failure(String error) =>
      PrivateTaskApiResult._(error: error, isSuccess: false);
}

class PrivateTasksService {
  final _http = AppHttp();

  Future<PrivateTasksData> fetch({
    String? query,
    int? assigneeId,
    String? state,
    bool showClosed = false,
  }) async {
    final response = await _http.get(
      ApiEndpoint.privateTasks,
      params: {
        if (query != null && query.trim().isNotEmpty) 'q': query.trim(),
        if (assigneeId != null && assigneeId > 0) 'assigneeId': assigneeId,
        if (state != null && state.isNotEmpty) 'state': state,
        if (showClosed) 'showClosed': true,
      },
    );
    return PrivateTasksData.fromJson(response.data as Map<String, dynamic>);
  }

  Future<PrivateTaskFormOptions> fetchFormOptions() async {
    final response = await _http.get(ApiEndpoint.privateTasksFormOptions);
    return PrivateTaskFormOptions.fromJson(response.data as Map<String, dynamic>);
  }

  Future<PrivateTaskApiResult> create({
    required String title,
    required int assigneeUserId,
    DateTime? startDate,
    required DateTime dueDate,
    required double bonusPercent,
    required String priority,
    String? description,
  }) async {
    try {
      final response = await _http.post(
        ApiEndpoint.privateTasksCreate,
        data: FormData.fromMap({
          'title': title.trim(),
          'assigneeUserId': assigneeUserId,
          if (startDate != null) 'startDate': DateFormat('yyyy-MM-dd').format(startDate),
          'dueDate': DateFormat('yyyy-MM-dd').format(dueDate),
          'bonusPercent': bonusPercent.toStringAsFixed(1),
          'priority': priority,
          'description': description?.trim() ?? '',
        }),
      );
      return PrivateTaskApiResult.success(
        PrivateTaskItem.fromJson(response.data as Map<String, dynamic>),
      );
    } on DioException catch (e) {
      return PrivateTaskApiResult.failure(_errorMessage(e, 'Không thể giao việc riêng. Vui lòng thử lại.'));
    }
  }

  Future<PrivateTaskApiResult> update({
    required int id,
    required String title,
    required int assigneeUserId,
    DateTime? startDate,
    required DateTime dueDate,
    required double bonusPercent,
    required String priority,
    String? description,
    String? state,
    int? progress,
  }) async {
    try {
      final response = await _http.post(
        ApiEndpoint.privateTasksUpdate,
        data: FormData.fromMap({
          'id': id,
          'title': title.trim(),
          'assigneeUserId': assigneeUserId,
          if (startDate != null) 'startDate': DateFormat('yyyy-MM-dd').format(startDate),
          'dueDate': DateFormat('yyyy-MM-dd').format(dueDate),
          'bonusPercent': bonusPercent.toStringAsFixed(1),
          'priority': priority,
          'description': description?.trim() ?? '',
          if (state != null) 'state': state,
          if (progress != null) 'progress': progress,
        }),
      );
      return PrivateTaskApiResult.success(
        PrivateTaskItem.fromJson(response.data as Map<String, dynamic>),
      );
    } on DioException catch (e) {
      return PrivateTaskApiResult.failure(_errorMessage(e, 'Không thể cập nhật việc riêng. Vui lòng thử lại.'));
    }
  }

  Future<PrivateTaskApiResult> delete(int id) async {
    try {
      await _http.post(
        ApiEndpoint.privateTasksDelete,
        data: FormData.fromMap({'id': id}),
      );
      return PrivateTaskApiResult.success();
    } on DioException catch (e) {
      return PrivateTaskApiResult.failure(_errorMessage(e, 'Không thể xóa việc riêng. Vui lòng thử lại.'));
    }
  }

  String _errorMessage(DioException e, String fallback) {
    final message = e.response?.statusCode == 400
        ? (e.response?.data is Map ? e.response?.data['error'] as String? : null)
        : null;
    return message ?? fallback;
  }
}
