import 'package:file_picker/file_picker.dart';
import 'package:flutter/material.dart';
import 'package:phosphor_icons/phosphor_icons.dart';

import '../../core/theme/app_colors.dart';
import '../../core/theme/app_dimens.dart';
import '../../core/theme/app_text_styles.dart';
import '../../core/utils/toast_service.dart';
import '../../core/widgets/app_app_bar.dart';
import '../../core/widgets/app_button.dart';
import '../../core/widgets/app_date_field.dart';
import '../../core/widgets/app_dropdown.dart';
import '../../core/widgets/app_icon_button.dart';
import '../../core/widgets/app_loading.dart';
import '../../core/widgets/app_rich_editor.dart';
import '../../core/widgets/app_scaffold.dart';
import '../../core/widgets/app_text.dart';
import '../../core/widgets/app_text_field.dart';
import '../mywork/task_comments_screen.dart' show kAllowedAttachmentExtensions;
import 'my_projects_models.dart';
import 'my_projects_service.dart';

/// Man "Thêm dự án" — tach thanh full-screen (cung tinh than voi AddTaskScreen), khop DAY DU
/// modal web (Views/WorkProjects/_EditForm.cshtml): Ten/Khach hang/Loai du an/Giai doan/Trang
/// thai/Ngay bat dau/Ngay ket thuc du kien/Mo ta (rich text) + khoi "Thông tin triển khai"
/// (Github/SVN/FTP/Database, deu khong bat buoc) + khoi "Tài liệu đính kèm" (chon nhieu file).
/// KHONG co truong PM — PM van gan o man "Nhan su du an" rieng, giong dung hanh vi web. Ma du an
/// (Code) khong co truong nhap, backend tu sinh. Tra ve true khi tao thanh cong de man danh sach
/// du an tu _reload().
Future<bool?> pushAddProjectScreen(
  BuildContext context, {
  required MyProjectsService service,
}) {
  return Navigator.of(context).push<bool>(
    MaterialPageRoute(
      builder: (context) => AddProjectScreen(service: service),
    ),
  );
}

class AddProjectScreen extends StatefulWidget {
  const AddProjectScreen({super.key, required this.service});

  final MyProjectsService service;

  @override
  State<AddProjectScreen> createState() => _AddProjectScreenState();
}

class _AddProjectScreenState extends State<AddProjectScreen> {
  final _formKey = GlobalKey<FormState>();
  final _nameController = TextEditingController();
  final _customerController = TextEditingController();
  final _descriptionController = AppRichEditorController();

  final _githubLinkController = TextEditingController();
  final _svnLinkController = TextEditingController();
  final _ftpAccountController = TextEditingController();
  final _ftpPasswordController = TextEditingController();
  final _dbTypeController = TextEditingController();
  final _dbServerController = TextEditingController();
  final _dbUsernameController = TextEditingController();
  final _dbPasswordController = TextEditingController();

  late Future<ProjectCreateFormOptions> _formFuture;

  // Giai doan mac dinh khi mo form la "Trien khai", kem trang thai mac dinh tuong ung.
  String _phase = 'TrienKhai';
  late String _state = projectStateDefaultFor(_phase);
  String? _projectType;
  String? _projectTypeError;
  DateTime? _startDate;
  DateTime? _endDate;
  String? _dateError;
  bool _submitting = false;
  final List<PlatformFile> _pendingFiles = [];

  @override
  void initState() {
    super.initState();
    _formFuture = widget.service.fetchCreateForm();
  }

  @override
  void dispose() {
    _nameController.dispose();
    _customerController.dispose();
    _descriptionController.dispose();
    _githubLinkController.dispose();
    _svnLinkController.dispose();
    _ftpAccountController.dispose();
    _ftpPasswordController.dispose();
    _dbTypeController.dispose();
    _dbServerController.dispose();
    _dbUsernameController.dispose();
    _dbPasswordController.dispose();
    super.dispose();
  }

  void _reloadForm() {
    setState(() {
      _formFuture = widget.service.fetchCreateForm();
    });
  }

  void _onPhaseChanged(String? value) {
    if (value == null) return;
    setState(() {
      _phase = value;
      // Doi lai Trang thai theo Giai doan moi — tranh gui len mot state khong thuoc phase, khop
      // hanh vi loc lai trang thai theo giai doan ben web.
      _state = projectStateDefaultFor(value);
    });
  }

  Future<void> _pickFiles() async {
    final result = await FilePicker.platform.pickFiles(
      allowMultiple: true,
      type: FileType.custom,
      allowedExtensions: kAllowedAttachmentExtensions,
    );
    if (result == null || result.files.isEmpty) return;
    setState(() => _pendingFiles.addAll(result.files));
  }

  Future<void> _submit() async {
    final nameOk = _formKey.currentState?.validate() ?? false;

    setState(() {
      _projectTypeError = _projectType == null ? 'Vui lòng chọn loại dự án' : null;
      _dateError = (_startDate != null &&
              _endDate != null &&
              _endDate!.isBefore(_startDate!))
          ? 'Ngày kết thúc dự kiến không được sớm hơn ngày bắt đầu'
          : null;
    });

    if (!nameOk || _projectTypeError != null || _dateError != null) return;

    setState(() => _submitting = true);
    final result = await widget.service.create(
      name: _nameController.text.trim(),
      customer: _customerController.text.trim(),
      projectType: _projectType!,
      phase: _phase,
      state: _state,
      startDate: _startDate,
      endDate: _endDate,
      description: _descriptionController.toHtml(),
      githubLink: _githubLinkController.text.trim(),
      svnLink: _svnLinkController.text.trim(),
      ftpAccount: _ftpAccountController.text.trim(),
      ftpPassword: _ftpPasswordController.text.trim(),
      dbType: _dbTypeController.text.trim(),
      dbServer: _dbServerController.text.trim(),
      dbUsername: _dbUsernameController.text.trim(),
      dbPassword: _dbPasswordController.text.trim(),
      files: _pendingFiles,
    );
    if (!mounted) return;
    setState(() => _submitting = false);

    if (result.isSuccess) {
      final rejected = result.project!.rejectedFiles;
      if (rejected.isNotEmpty) {
        ToastService.show(
          'Đã tạo dự án nhưng ${rejected.length} tệp không đính kèm được: '
          '${rejected.join(', ')}',
          type: ToastType.warning,
        );
      }
      Navigator.of(context).pop(true);
    } else {
      ToastService.show(result.error!, type: ToastType.error);
    }
  }

  @override
  Widget build(BuildContext context) {
    return AppScaffold(
      appBar: AppAppBar(
        title: 'Thêm dự án',
        leading: AppIconButton(
          icon: PhosphorIconsRegular.arrowLeft,
          tooltip: 'Quay lại',
          onPressed: () => Navigator.of(context).pop(),
        ),
      ),
      body: SafeArea(
        child: FutureBuilder<ProjectCreateFormOptions>(
          future: _formFuture,
          builder: (context, snapshot) {
            if (snapshot.connectionState != ConnectionState.done) {
              return const Center(child: AppLoading());
            }

            if (snapshot.hasError) {
              return Center(
                child: Padding(
                  padding: const EdgeInsets.all(24),
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      const PhosphorIcon(PhosphorIconsRegular.warningCircle,
                          color: AppColors.danger, size: 32),
                      const SizedBox(height: AppDimens.space12),
                      const AppText(
                        'Không tải được biểu mẫu thêm dự án. Kiểm tra kết nối mạng rồi thử lại.',
                        variant: AppTextVariant.body,
                        align: TextAlign.center,
                      ),
                      const SizedBox(height: AppDimens.space12),
                      AppButton(label: 'Thử lại', onPressed: _reloadForm),
                    ],
                  ),
                ),
              );
            }

            return _buildForm(snapshot.data!);
          },
        ),
      ),
    );
  }

  Widget _buildForm(ProjectCreateFormOptions options) {
    final hasProjectTypes = options.projectTypes.isNotEmpty;

    return SingleChildScrollView(
      padding: const EdgeInsets.fromLTRB(20, 16, 20, 24),
      child: Form(
        key: _formKey,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            AppTextField(
              label: 'Tên dự án',
              required: true,
              controller: _nameController,
              validator: (v) =>
                  (v == null || v.trim().isEmpty) ? 'Vui lòng nhập tên dự án' : null,
            ),
            const SizedBox(height: AppDimens.space12),
            AppTextField(
              label: 'Khách hàng',
              hint: 'Không bắt buộc',
              controller: _customerController,
            ),
            const SizedBox(height: AppDimens.space12),
            AppDropdown<String>(
              label: 'Loại dự án',
              value: _projectType,
              items: {for (final t in options.projectTypes) t: t},
              hint: hasProjectTypes ? 'Chọn loại dự án' : 'Chưa có loại dự án nào',
              onChanged: (v) {
                setState(() {
                  _projectType = v;
                  if (v != null) _projectTypeError = null;
                });
              },
            ),
            if (_projectTypeError != null)
              Padding(
                padding: const EdgeInsets.only(top: AppDimens.space4),
                child: AppText(_projectTypeError!,
                    variant: AppTextVariant.caption,
                    fontSize: 12,
                    color: AppColors.danger),
              ),
            if (!hasProjectTypes)
              const Padding(
                padding: EdgeInsets.only(top: AppDimens.space4),
                child: AppText(
                  'Hệ thống chưa có loại dự án nào — liên hệ quản trị để thêm trước khi tạo dự án.',
                  variant: AppTextVariant.caption,
                  fontSize: 12,
                  color: AppColors.textSecondary,
                ),
              ),
            const SizedBox(height: AppDimens.space12),
            Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Expanded(
                  child: AppDropdown<String>(
                    label: 'Giai đoạn',
                    value: _phase,
                    items: {
                      'TrienKhai': projectPhaseLabel('TrienKhai'),
                      'HoTro': projectPhaseLabel('HoTro'),
                    },
                    onChanged: _onPhaseChanged,
                  ),
                ),
                const SizedBox(width: AppDimens.space12),
                Expanded(
                  child: AppDropdown<String>(
                    label: 'Trạng thái',
                    value: _state,
                    items: {
                      for (final s in projectStatesForPhase(_phase))
                        s: projectStateLabel(s),
                    },
                    onChanged: (v) {
                      if (v != null) setState(() => _state = v);
                    },
                  ),
                ),
              ],
            ),
            const SizedBox(height: AppDimens.space12),
            Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Expanded(
                  child: AppDateField(
                    label: 'Ngày bắt đầu',
                    value: _startDate,
                    onChanged: (d) => setState(() {
                      _startDate = d;
                      _dateError = null;
                    }),
                  ),
                ),
                const SizedBox(width: AppDimens.space12),
                Expanded(
                  child: AppDateField(
                    label: 'Ngày kết thúc dự kiến',
                    value: _endDate,
                    onChanged: (d) => setState(() {
                      _endDate = d;
                      _dateError = null;
                    }),
                  ),
                ),
              ],
            ),
            if (_dateError != null)
              Padding(
                padding: const EdgeInsets.only(top: AppDimens.space4),
                child: AppText(_dateError!,
                    variant: AppTextVariant.caption,
                    fontSize: 12,
                    color: AppColors.danger),
              ),
            const SizedBox(height: AppDimens.space12),
            AppRichEditor(
              controller: _descriptionController,
              label: 'Mô tả',
              hint: 'Không bắt buộc',
              minLines: 3,
              maxLines: 8,
            ),
            const SizedBox(height: AppDimens.space24),
            const AppText('Thông tin triển khai',
                variant: AppTextVariant.body, fontSize: 15, weight: FontWeight.w700),
            const SizedBox(height: AppDimens.space4),
            const AppText(
              'Không bắt buộc — bổ sung sau trong Chi tiết dự án cũng được.',
              variant: AppTextVariant.caption,
              fontSize: 12,
              color: AppColors.textSecondary,
            ),
            const SizedBox(height: AppDimens.space12),
            AppTextField(
              label: 'Link Source Github',
              hint: 'https://github.com/...',
              controller: _githubLinkController,
            ),
            const SizedBox(height: AppDimens.space12),
            AppTextField(
              label: 'Link Source SVN',
              hint: 'svn://... hoặc https://...',
              controller: _svnLinkController,
            ),
            const SizedBox(height: AppDimens.space12),
            AppTextField(
              label: 'FTP — Tài khoản',
              hint: 'host / tài khoản FTP',
              controller: _ftpAccountController,
            ),
            const SizedBox(height: AppDimens.space12),
            AppTextField(
              label: 'FTP — Mật khẩu',
              controller: _ftpPasswordController,
              obscureText: true,
            ),
            const SizedBox(height: AppDimens.space12),
            AppTextField(
              label: 'Sử dụng database',
              hint: 'SQL Server / MySQL / PostgreSQL...',
              controller: _dbTypeController,
            ),
            const SizedBox(height: AppDimens.space12),
            AppTextField(
              label: 'Database — Server',
              hint: 'địa chỉ máy chủ CSDL',
              controller: _dbServerController,
            ),
            const SizedBox(height: AppDimens.space12),
            AppTextField(
              label: 'Database — Username',
              controller: _dbUsernameController,
            ),
            const SizedBox(height: AppDimens.space12),
            AppTextField(
              label: 'Database — Password',
              controller: _dbPasswordController,
              obscureText: true,
            ),
            const SizedBox(height: AppDimens.space24),
            const AppText('Tài liệu đính kèm',
                variant: AppTextVariant.body, fontSize: 15, weight: FontWeight.w700),
            const SizedBox(height: AppDimens.space4),
            const AppText(
              'Mỗi file tối đa 10 MB; nhận tài liệu, ảnh và file nén.',
              variant: AppTextVariant.caption,
              fontSize: 12,
              color: AppColors.textSecondary,
            ),
            const SizedBox(height: AppDimens.space12),
            if (_pendingFiles.isNotEmpty) ...[
              Wrap(
                spacing: AppDimens.space8,
                runSpacing: AppDimens.space8,
                children: [
                  for (var i = 0; i < _pendingFiles.length; i++)
                    _PendingFileChip(
                      file: _pendingFiles[i],
                      onRemove: () => setState(() => _pendingFiles.removeAt(i)),
                    ),
                ],
              ),
              const SizedBox(height: AppDimens.space12),
            ],
            AppButton(
              label: 'Thêm file',
              type: AppButtonType.outline,
              icon: PhosphorIconsRegular.paperclip,
              onPressed: _pickFiles,
            ),
            const SizedBox(height: AppDimens.space24),
            AppButton(
              label: 'Thêm dự án',
              fullWidth: true,
              isLoading: _submitting,
              onPressed: _submitting ? null : _submit,
            ),
          ],
        ),
      ),
    );
  }
}

String _sizeLabel(int bytes) {
  if (bytes < 1024) return '$bytes B';
  if (bytes < 1024 * 1024) return '${(bytes / 1024).toStringAsFixed(0)} KB';
  return '${(bytes / (1024 * 1024)).toStringAsFixed(1)} MB';
}

/// Mot file da chon cho khoi "Tài liệu đính kèm" — dang chip co nut xoa, cung bo cuc voi
/// `_PendingAttachmentChip` (task_comments_screen.dart) nhung dat trong Wrap vi o day chon duoc
/// NHIEU file cung luc.
class _PendingFileChip extends StatelessWidget {
  const _PendingFileChip({required this.file, required this.onRemove});

  final PlatformFile file;
  final VoidCallback onRemove;

  @override
  Widget build(BuildContext context) {
    return Container(
      constraints: const BoxConstraints(maxWidth: 260),
      padding: const EdgeInsets.symmetric(
          horizontal: AppDimens.space12, vertical: AppDimens.space8),
      decoration: BoxDecoration(
        color: AppColors.primarySoft,
        borderRadius: BorderRadius.circular(AppDimens.radiusSm),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          const PhosphorIcon(PhosphorIconsRegular.paperclip,
              size: 16, color: AppColors.primary),
          const SizedBox(width: AppDimens.space8),
          Flexible(
            child: AppText(
              '${file.name} (${_sizeLabel(file.size)})',
              variant: AppTextVariant.caption,
              fontSize: 12.5,
              color: AppColors.textPrimary,
              overflow: TextOverflow.ellipsis,
              maxLines: 1,
            ),
          ),
          AppIconButton(
              icon: PhosphorIconsRegular.x,
              tooltip: 'Bỏ tệp này',
              onPressed: onRemove),
        ],
      ),
    );
  }
}
