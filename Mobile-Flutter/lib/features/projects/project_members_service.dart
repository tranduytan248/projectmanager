import 'package:dio/dio.dart';
import 'package:intl/intl.dart';

import '../../config/api_endpoint.dart';
import '../../config/app_http.dart';
import 'project_detail_models.dart';

/// ASP.NET MVC model binder mac dinh cho DateTime doc theo CultureInfo hien hanh cua request
/// (Global.asax.cs coc dinh vi-VN toan cuc, KHONG co ModelBinder rieng cho DateTime — chi co
/// DecimalModelBinder cho so thap phan). Gui dung "yyyy-MM-dd" (khop het voi cach <input
/// type="date"> ben web dang gui, da duoc kiem chung dung) thay vi DateTime.toIso8601String()
/// (co gio/mili-giay) de tranh nguy co parse sai/loi 500 do khac dinh dang voi mong doi cua
/// binder — cung bai hoc voi ly do TimeLogService tu parse "hours" bang InvariantCulture thay vi
/// de model binder tu suy dien.
String _dateOnly(DateTime date) => DateFormat('yyyy-MM-dd').format(date);

/// Ket qua mot thao tac tren man "Nhan su du an" (them/dat PM/ket thuc/xoa). loi != null nghia la
/// backend tu choi (vi du "dang tham gia tu ngay X, hay ket thuc truoc"), hien nguyen van cho
/// nguoi dung — cung khuon voi TaskDetailActionResult.
class ProjectMemberActionResult {
  const ProjectMemberActionResult._({this.data, this.error});

  final List<ProjectMember>? data;
  final String? error;

  bool get isSuccess => error == null;
}

class ProjectMembersService {
  final _http = AppHttp();

  Future<List<ProjectMember>> fetch(int projectId) async {
    final response = await _http
        .get(ApiEndpoint.projectMembers, params: {'id': projectId});
    return (response.data as List<dynamic>)
        .map((e) => ProjectMember.fromJson(e as Map<String, dynamic>))
        .toList();
  }

  Future<ProjectMemberFormOptions> fetchAddMemberForm(int projectId) async {
    final response = await _http
        .get(ApiEndpoint.projectAddMemberForm, params: {'id': projectId});
    return ProjectMemberFormOptions.fromJson(
        response.data as Map<String, dynamic>);
  }

  Future<ProjectMemberActionResult> addMember({
    required int projectId,
    required int userId,
    String? role,
    required String phase,
    required bool isPm,
    required DateTime joinedAt,
    String? note,
  }) async {
    try {
      final response = await _http.post(
        ApiEndpoint.projectAddMember,
        data: {
          'id': projectId,
          'userId': userId,
          'role': role ?? '',
          'phase': phase,
          'isPm': isPm,
          'joinedAt': _dateOnly(joinedAt),
          'note': note ?? '',
        },
        options: Options(contentType: Headers.formUrlEncodedContentType),
      );
      return _success(response);
    } on DioException catch (e) {
      return _error(e, 'Không thêm được nhân sự. Hãy thử lại.');
    }
  }

  Future<ProjectMemberActionResult> setPm({
    required int projectId,
    required int assignmentId,
    DateTime? from,
  }) async {
    try {
      final response = await _http.post(
        ApiEndpoint.projectSetPm,
        data: {
          'id': projectId,
          'assignmentId': assignmentId,
          if (from != null) 'from': _dateOnly(from),
        },
        options: Options(contentType: Headers.formUrlEncodedContentType),
      );
      return _success(response);
    } on DioException catch (e) {
      return _error(e, 'Không đặt được làm PM. Hãy thử lại.');
    }
  }

  Future<ProjectMemberActionResult> endMember({
    required int projectId,
    required int assignmentId,
    required DateTime leftAt,
  }) async {
    try {
      final response = await _http.post(
        ApiEndpoint.projectEndMember,
        data: {
          'id': projectId,
          'assignmentId': assignmentId,
          'leftAt': _dateOnly(leftAt),
        },
        options: Options(contentType: Headers.formUrlEncodedContentType),
      );
      return _success(response);
    } on DioException catch (e) {
      return _error(e, 'Không kết thúc được tham gia. Hãy thử lại.');
    }
  }

  Future<ProjectMemberActionResult> removeMember({
    required int projectId,
    required int assignmentId,
  }) async {
    try {
      final response = await _http.post(
        ApiEndpoint.projectRemoveMember,
        data: {'id': projectId, 'assignmentId': assignmentId},
        options: Options(contentType: Headers.formUrlEncodedContentType),
      );
      return _success(response);
    } on DioException catch (e) {
      return _error(e, 'Không xoá được khỏi lịch sử. Hãy thử lại.');
    }
  }

  ProjectMemberActionResult _success(Response response) {
    final list = (response.data as List<dynamic>)
        .map((e) => ProjectMember.fromJson(e as Map<String, dynamic>))
        .toList();
    return ProjectMemberActionResult._(data: list);
  }

  ProjectMemberActionResult _error(DioException e, String fallback) {
    final message = e.response?.statusCode == 400
        ? (e.response?.data is Map ? e.response?.data['error'] as String? : null)
        : null;
    return ProjectMemberActionResult._(error: message ?? fallback);
  }
}
