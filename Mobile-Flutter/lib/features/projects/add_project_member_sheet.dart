import 'package:flutter/material.dart';

import '../../core/theme/app_colors.dart';
import '../../core/theme/app_dimens.dart';
import '../../core/theme/app_text_styles.dart';
import '../../core/utils/toast_service.dart';
import '../../core/widgets/app_button.dart';
import '../../core/widgets/app_checkbox.dart';
import '../../core/widgets/app_date_field.dart';
import '../../core/widgets/app_dropdown.dart';
import '../../core/widgets/app_icon_button.dart';
import '../../core/widgets/app_loading.dart';
import '../../core/widgets/app_text.dart';
import '../../core/widgets/app_text_field.dart';
import 'project_detail_models.dart';
import 'project_members_service.dart';
import 'package:phosphor_icons/phosphor_icons.dart';

/// Nam giai doan tham gia — khop AssignmentPhases ben backend (Models/Work/WorkProject.cs).
const Map<String, String> kAssignmentPhaseOptions = {
  'CaHai': 'Triển khai + Hỗ trợ',
  'TrienKhai': 'Triển khai',
  'HoTro': 'Hỗ trợ',
};

/// Bottom sheet "Thêm nhân sự vào dự án" — mirror _AddMemberForm.cshtml ben web dung thu tu
/// truong: Người dùng, Vai trò, Giai đoạn tham gia, Tham gia từ, Ghi chú, checkbox Đặt làm PM.
/// Tra ve danh sach ProjectMember moi nhat khi them thanh cong, null neu huy/dong khong doi gi.
Future<List<ProjectMember>?> showAddProjectMemberSheet(
  BuildContext context, {
  required ProjectMembersService service,
  required int projectId,
}) {
  return showModalBottomSheet<List<ProjectMember>>(
    context: context,
    isScrollControlled: true,
    shape: const RoundedRectangleBorder(
      borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
    ),
    builder: (context) =>
        _AddProjectMemberSheet(service: service, projectId: projectId),
  );
}

class _AddProjectMemberSheet extends StatefulWidget {
  const _AddProjectMemberSheet(
      {required this.service, required this.projectId});

  final ProjectMembersService service;
  final int projectId;

  @override
  State<_AddProjectMemberSheet> createState() =>
      _AddProjectMemberSheetState();
}

class _AddProjectMemberSheetState extends State<_AddProjectMemberSheet> {
  late Future<ProjectMemberFormOptions> _optionsFuture;

  int? _userId;
  String? _role;
  String _phase = 'CaHai';
  DateTime _joinedAt = DateTime.now();
  final _noteController = TextEditingController();
  bool _isPm = false;
  bool _submitting = false;
  String? _userFieldError;

  @override
  void initState() {
    super.initState();
    _optionsFuture = widget.service.fetchAddMemberForm(widget.projectId);
  }

  @override
  void dispose() {
    _noteController.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (_userId == null) {
      setState(() => _userFieldError = 'Vui lòng chọn người dùng.');
      return;
    }
    setState(() => _userFieldError = null);

    setState(() => _submitting = true);
    final result = await widget.service.addMember(
      projectId: widget.projectId,
      userId: _userId!,
      role: _role,
      phase: _phase,
      isPm: _isPm,
      joinedAt: _joinedAt,
      note: _noteController.text.trim().isEmpty
          ? null
          : _noteController.text.trim(),
    );
    if (!mounted) return;
    setState(() => _submitting = false);

    if (result.isSuccess) {
      ToastService.show('Đã thêm nhân sự vào dự án.', type: ToastType.success);
      Navigator.of(context).pop(result.data);
    } else {
      ToastService.show(result.error!, type: ToastType.error);
    }
  }

  @override
  Widget build(BuildContext context) {
    return SafeArea(
      child: Padding(
        padding: EdgeInsets.fromLTRB(
            20, 20, 20, MediaQuery.of(context).viewInsets.bottom + 20),
        child: SingleChildScrollView(
          child: FutureBuilder<ProjectMemberFormOptions>(
            future: _optionsFuture,
            builder: (context, snapshot) {
              if (snapshot.connectionState != ConnectionState.done) {
                return const Padding(
                  padding: EdgeInsets.symmetric(vertical: 40),
                  child: Center(child: AppLoading()),
                );
              }

              if (snapshot.hasError) {
                return const Padding(
                  padding: EdgeInsets.symmetric(vertical: 24),
                  child: AppText(
                    'Không tải được danh sách người dùng/vai trò. Hãy đóng và thử lại.',
                    variant: AppTextVariant.body,
                    color: AppColors.danger,
                  ),
                );
              }

              return _buildForm(snapshot.data!);
            },
          ),
        ),
      ),
    );
  }

  Widget _buildForm(ProjectMemberFormOptions options) {
    return Column(
      mainAxisSize: MainAxisSize.min,
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Row(
          children: [
            const Expanded(
              child: AppText('Thêm nhân sự vào dự án',
                  variant: AppTextVariant.body,
                  fontSize: 16,
                  weight: FontWeight.w700),
            ),
            AppIconButton(
              icon: PhosphorIconsRegular.x,
              tooltip: 'Đóng',
              onPressed: () => Navigator.of(context).pop(),
            ),
          ],
        ),
        const SizedBox(height: AppDimens.space16),
        AppDropdown<int>(
          label: 'Người dùng',
          value: _userId,
          items: {for (final u in options.users) u.userId: u.fullName},
          onChanged: (v) {
            setState(() {
              _userId = v;
              if (v != null) _userFieldError = null;
            });
          },
        ),
        if (_userFieldError != null)
          Padding(
            padding: const EdgeInsets.only(top: AppDimens.space4),
            child: AppText(_userFieldError!,
                variant: AppTextVariant.caption,
                fontSize: 12,
                color: AppColors.danger),
          ),
        const SizedBox(height: AppDimens.space12),
        AppDropdown<String>(
          label: 'Vai trò trong dự án',
          value: _role,
          items: {for (final r in options.roles) r: r},
          hint: 'Không chọn',
          onChanged: (v) => setState(() => _role = v),
        ),
        const SizedBox(height: AppDimens.space12),
        AppDropdown<String>(
          label: 'Giai đoạn tham gia',
          value: _phase,
          items: kAssignmentPhaseOptions,
          onChanged: (v) {
            if (v != null) setState(() => _phase = v);
          },
        ),
        const SizedBox(height: AppDimens.space4),
        const AppText(
          'Người ở lại hỗ trợ sau khi triển khai xong thì chọn Hỗ trợ.',
          variant: AppTextVariant.caption,
          fontSize: 12,
          color: AppColors.textSecondary,
        ),
        const SizedBox(height: AppDimens.space12),
        AppDateField(
          label: 'Tham gia từ',
          value: _joinedAt,
          onChanged: (d) => setState(() => _joinedAt = d),
        ),
        const SizedBox(height: AppDimens.space12),
        AppTextField(
          label: 'Ghi chú',
          controller: _noteController,
          hint: 'Không bắt buộc',
        ),
        const SizedBox(height: AppDimens.space12),
        AppCheckbox(
          value: _isPm,
          label: 'Đặt làm PM dự án',
          onChanged: (v) => setState(() => _isPm = v),
        ),
        const SizedBox(height: AppDimens.space4),
        const AppText(
          'Sẽ tự cho người đang làm PM thôi vai trò đó, nhưng họ vẫn ở lại dự án.',
          variant: AppTextVariant.caption,
          fontSize: 12,
          color: AppColors.textSecondary,
        ),
        const SizedBox(height: AppDimens.space16),
        AppButton(
          label: 'Thêm nhân sự',
          fullWidth: true,
          isLoading: _submitting,
          onPressed: _submitting ? null : _submit,
        ),
      ],
    );
  }
}
