import 'package:dio/dio.dart';
import 'package:intl/intl.dart';

import '../../config/api_endpoint.dart';
import '../../config/app_http.dart';
import 'leave_models.dart';

/// Ket qua goi LeavesApi/Create, LeavesApi/Update hoac LeavesApi/Cancel. loi != null nghia la
/// backend tu choi (ngay khong hop le, trung don khac, don da duoc xu ly nen khong sua/thu hoi
/// duoc nua...) — hien nguyen van cho nguoi dung, khop thong diep BadRequest ben backend. Cung
/// mau voi CreateProjectApiResult (my_projects_service.dart)/CreateTaskResult (checklist_service.dart).
class LeaveApiResult {
  const LeaveApiResult._({this.item, this.error});

  final LeaveRequestItem? item;
  final String? error;

  bool get isSuccess => item != null;
}

class LeaveService {
  final _http = AppHttp();

  /// [year] <= 0 hoac null va [state] rong hoac null nghia la "tat ca" — khop dung quy uoc
  /// LeavesApiController.Index(year=0, state=null).
  Future<LeavesData> fetch({int? year, String? state}) async {
    final response = await _http.get(
      ApiEndpoint.leaves,
      params: {
        if (year != null && year > 0) 'year': year,
        if (state != null && state.isNotEmpty) 'state': state,
      },
    );
    return LeavesData.fromJson(response.data as Map<String, dynamic>);
  }

  Future<LeaveApiResult> create({
    required String kind,
    required DateTime fromDate,
    required DateTime toDate,
    required bool isHalfDay,
    String? halfDaySession,
    String? reason,
  }) async {
    try {
      final response = await _http.post(
        ApiEndpoint.leaveCreate,
        data: FormData.fromMap(_formFields(
          kind: kind,
          fromDate: fromDate,
          toDate: toDate,
          isHalfDay: isHalfDay,
          halfDaySession: halfDaySession,
          reason: reason,
        )),
      );
      return LeaveApiResult._(
          item: LeaveRequestItem.fromJson(response.data as Map<String, dynamic>));
    } on DioException catch (e) {
      return LeaveApiResult._(
          error: _errorMessage(e, 'Không tạo được đơn nghỉ phép. Hãy thử lại.'));
    }
  }

  /// Chi sua duoc khi don dang o trang thai Cho duyet — backend tu choi (400) neu don da duoc
  /// xu ly, thong diep tra ve hien nguyen van cho nguoi dung.
  Future<LeaveApiResult> update({
    required int id,
    required String kind,
    required DateTime fromDate,
    required DateTime toDate,
    required bool isHalfDay,
    String? halfDaySession,
    String? reason,
  }) async {
    try {
      final response = await _http.post(
        ApiEndpoint.leaveUpdate,
        data: FormData.fromMap({
          'id': id,
          ..._formFields(
            kind: kind,
            fromDate: fromDate,
            toDate: toDate,
            isHalfDay: isHalfDay,
            halfDaySession: halfDaySession,
            reason: reason,
          ),
        }),
      );
      return LeaveApiResult._(
          item: LeaveRequestItem.fromJson(response.data as Map<String, dynamic>));
    } on DioException catch (e) {
      return LeaveApiResult._(
          error: _errorMessage(e, 'Không lưu được đơn nghỉ phép. Hãy thử lại.'));
    }
  }

  /// Thu hoi don cua chinh minh khi chua duoc duyet — backend tu chan neu don khong con o Cho duyet.
  Future<LeaveApiResult> cancel(int id) async {
    try {
      final response = await _http.post(
        ApiEndpoint.leaveCancel,
        data: FormData.fromMap({'id': id}),
      );
      return LeaveApiResult._(
          item: LeaveRequestItem.fromJson(response.data as Map<String, dynamic>));
    } on DioException catch (e) {
      return LeaveApiResult._(
          error: _errorMessage(e, 'Không thu hồi được đơn nghỉ phép. Hãy thử lại.'));
    }
  }

  Map<String, dynamic> _formFields({
    required String kind,
    required DateTime fromDate,
    required DateTime toDate,
    required bool isHalfDay,
    String? halfDaySession,
    String? reason,
  }) {
    return {
      'kind': kind,
      // Cung khuon "yyyy-MM-dd" nhu input type=date ben web gui, tranh nham lan dinh dang
      // ngay/thang theo van hoa (culture) cua may chu — cung mau voi MyProjectsService/ChecklistService.
      'fromDate': DateFormat('yyyy-MM-dd').format(fromDate),
      'toDate': DateFormat('yyyy-MM-dd').format(toDate),
      'isHalfDay': isHalfDay,
      if (isHalfDay && halfDaySession != null) 'halfDaySession': halfDaySession,
      'reason': reason ?? '',
    };
  }

  /// Lay danh sach don nghi phep toan To de duyet.
  Future<LeaveApprovalsData> fetchApprovals({
    String? q,
    int? userId,
    String? state,
    int? year,
    int? month,
  }) async {
    final response = await _http.get(
      ApiEndpoint.leaveApprovals,
      params: {
        if (q != null && q.isNotEmpty) 'q': q,
        if (userId != null && userId > 0) 'userId': userId,
        if (state != null && state.isNotEmpty) 'state': state,
        if (year != null && year > 0) 'year': year,
        if (month != null && month > 0) 'month': month,
      },
    );
    return LeaveApprovalsData.fromJson(response.data as Map<String, dynamic>);
  }

  /// Duyet hoac Tu choi don nghi phep (yeu cau quyen leaves.approve).
  Future<LeaveApiResult> setState({
    required int id,
    required String state,
    String? note,
  }) async {
    try {
      final response = await _http.post(
        ApiEndpoint.leaveSetState,
        data: FormData.fromMap({
          'id': id,
          'state': state,
          'note': note ?? '',
        }),
      );
      return LeaveApiResult._(
          item: LeaveRequestItem.fromJson(response.data as Map<String, dynamic>));
    } on DioException catch (e) {
      return LeaveApiResult._(
          error: _errorMessage(e, 'Không thể cập nhật trạng thái đơn nghỉ phép. Hãy thử lại.'));
    }
  }

  String _errorMessage(DioException e, String fallback) {
    final message = e.response?.statusCode == 400
        ? (e.response?.data is Map ? e.response?.data['error'] as String? : null)
        : null;
    return message ?? fallback;
  }
}
