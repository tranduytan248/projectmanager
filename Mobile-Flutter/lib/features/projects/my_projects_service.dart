import 'package:dio/dio.dart';
import 'package:file_picker/file_picker.dart';
import 'package:intl/intl.dart';

import '../../config/api_endpoint.dart';
import '../../config/app_http.dart';
import 'my_projects_models.dart';

/// Ket qua goi MyProjectsApi/Create. loi != null nghia la backend tu choi (thieu Ten/Loai du
/// an, Ngay ket thuc som hon Ngay bat dau...) — hien nguyen van cho nguoi dung, khop thong diep
/// BadRequest ben backend. Cung mau voi CreateTaskResult (checklist_service.dart).
class CreateProjectApiResult {
  const CreateProjectApiResult._({this.project, this.error});

  final CreateProjectResult? project;
  final String? error;

  bool get isSuccess => project != null;
}

class MyProjectsService {
  final _http = AppHttp();

  /// scope="team": toan bo du an cua Ca To (chi hieu luc voi tai khoan Quan ly To — server tu
  /// am tham tra ve "mine" cho tai khoan khac, xem MyProjectsApiController.Index).
  Future<MyProjectsData> fetch(
      {String? query, bool showClosed = false, String scope = 'mine'}) async {
    final response = await _http.get(
      ApiEndpoint.myProjects,
      params: {
        if (query != null && query.isNotEmpty) 'q': query,
        'showClosed': showClosed,
        'scope': scope,
      },
    );
    return MyProjectsData.fromJson(response.data as Map<String, dynamic>);
  }

  /// Danh sach "Loại dự án" hien co de do vao form Them du an — danh muc DONG, khong hard-code.
  Future<ProjectCreateFormOptions> fetchCreateForm() async {
    final response = await _http.get(ApiEndpoint.myProjectsCreateForm);
    return ProjectCreateFormOptions.fromJson(response.data as Map<String, dynamic>);
  }

  /// Dung FormData (khong dung JSON) — cung mau voi ChecklistService.create(). [files] gui qua
  /// key 'files' lap lai nhieu lan (Dio tu lap key khi value la List<MultipartFile>), ASP.NET MVC
  /// tu bind vao IEnumerable<HttpPostedFileBase> files ben MyProjectsApiController.Create — cung
  /// co che voi 'todos' o ChecklistService.create().
  Future<CreateProjectApiResult> create({
    required String name,
    String? customer,
    required String projectType,
    required String phase,
    required String state,
    DateTime? startDate,
    DateTime? endDate,
    String? description,
    String? githubLink,
    String? svnLink,
    String? ftpAccount,
    String? ftpPassword,
    String? dbType,
    String? dbServer,
    String? dbUsername,
    String? dbPassword,
    List<PlatformFile> files = const [],
  }) async {
    try {
      final response = await _http.post(
        ApiEndpoint.myProjectsCreate,
        data: FormData.fromMap({
          'name': name,
          'customer': customer ?? '',
          'projectType': projectType,
          'phase': phase,
          'state': state,
          // Cung khuon "yyyy-MM-dd" nhu input type=date ben web gui, tranh nham lan dinh dang
          // ngay/thang theo van hoa (culture) cua may chu.
          if (startDate != null) 'startDate': DateFormat('yyyy-MM-dd').format(startDate),
          if (endDate != null) 'endDate': DateFormat('yyyy-MM-dd').format(endDate),
          'description': description ?? '',
          'githubLink': githubLink ?? '',
          'svnLink': svnLink ?? '',
          'ftpAccount': ftpAccount ?? '',
          'ftpPassword': ftpPassword ?? '',
          'dbType': dbType ?? '',
          'dbServer': dbServer ?? '',
          'dbUsername': dbUsername ?? '',
          'dbPassword': dbPassword ?? '',
          if (files.isNotEmpty)
            'files': [
              for (final f in files) await _toMultipartFile(f),
            ],
        }),
      );
      return CreateProjectApiResult._(
          project: CreateProjectResult.fromJson(response.data as Map<String, dynamic>));
    } on DioException catch (e) {
      final message = e.response?.statusCode == 400
          ? (e.response?.data is Map ? e.response?.data['error'] as String? : null)
          : null;
      return CreateProjectApiResult._(error: message ?? 'Không tạo được dự án. Hãy thử lại.');
    }
  }

  /// Tren Flutter Web, PlatformFile.path luon null (file_picker chi tra ve bytes trong bo nho) —
  /// dung MultipartFile.fromBytes trong truong hop do, con tren mobile/desktop uu tien fromFile
  /// (khong doc het file vao RAM). Thieu buoc nay thi bam "Thêm dự án" kem file dinh kem tren
  /// ban Web se nem loi "Null check operator used on a null value" ngay tai `f.path!`.
  Future<MultipartFile> _toMultipartFile(PlatformFile f) {
    if (f.path != null) {
      return MultipartFile.fromFile(f.path!, filename: f.name);
    }
    return Future.value(MultipartFile.fromBytes(f.bytes!, filename: f.name));
  }
}
