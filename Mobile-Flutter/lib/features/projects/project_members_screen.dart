import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import 'package:phosphor_icons/phosphor_icons.dart';

import '../../core/theme/app_colors.dart';
import '../../core/theme/app_dimens.dart';
import '../../core/theme/app_text_styles.dart';
import '../../core/utils/dialog_service.dart';
import '../../core/utils/toast_service.dart';
import '../../core/widgets/app_app_bar.dart';
import '../../core/widgets/app_button.dart';
import '../../core/widgets/app_date_field.dart';
import '../../core/widgets/app_error_state.dart';
import '../../core/widgets/app_icon_button.dart';
import '../../core/widgets/app_loading.dart';
import '../../core/widgets/app_scaffold.dart';
import '../../core/widgets/app_text.dart';
import 'add_project_member_sheet.dart';
import 'project_detail_models.dart';
import 'project_members_service.dart';

/// Man "Nhân sự dự án" — mirror WorkProjectsController.Members ben web: danh sach nguoi tham gia
/// (moi giai doan la 1 dong, nguoi rut ra roi quay lai co nhieu dong), nut "+ Thêm nhân sự" va menu
/// "Thao tác" tren tung dong (Đặt làm PM / Kết thúc tham gia / Xoá khỏi lịch sử) — chi hien voi
/// nguoi co quyen sua du an (canEdit = CanEditProject ben backend, dung [canEdit] tu man Chi tiet
/// du an truyen sang, khong tu doan lai o day).
class ProjectMembersScreen extends StatefulWidget {
  const ProjectMembersScreen({
    super.key,
    required this.projectId,
    required this.projectName,
    required this.canEdit,
  });

  final int projectId;
  final String projectName;
  final bool canEdit;

  @override
  State<ProjectMembersScreen> createState() => _ProjectMembersScreenState();
}

class _ProjectMembersScreenState extends State<ProjectMembersScreen> {
  final _service = ProjectMembersService();
  late Future<List<ProjectMember>> _future;

  @override
  void initState() {
    super.initState();
    _future = _service.fetch(widget.projectId);
  }

  void _reload() {
    setState(() => _future = _service.fetch(widget.projectId));
  }

  Future<void> _openAddSheet() async {
    final result = await showAddProjectMemberSheet(
      context,
      service: _service,
      projectId: widget.projectId,
    );
    if (result != null) setState(() => _future = Future.value(result));
  }

  void _applyResult(List<ProjectMember> list) {
    setState(() => _future = Future.value(list));
  }

  @override
  Widget build(BuildContext context) {
    return AppScaffold(
      appBar: AppAppBar(
        title: 'Nhân sự dự án',
        leading: AppIconButton(
          icon: PhosphorIconsRegular.arrowLeft,
          tooltip: 'Quay lại',
          onPressed: () => Navigator.of(context).pop(),
        ),
      ),
      body: SafeArea(
        child: FutureBuilder<List<ProjectMember>>(
          future: _future,
          builder: (context, snapshot) {
            if (snapshot.connectionState != ConnectionState.done) {
              return const Center(child: AppLoading());
            }

            if (snapshot.hasError) {
              return AppErrorState(
                  message: 'Không tải được danh sách nhân sự.',
                  onRetry: _reload);
            }

            final members = snapshot.data!;
            final activeCount = members.where((m) => m.isActive).length;

            return Column(
              children: [
                Padding(
                  padding: const EdgeInsets.fromLTRB(20, 16, 20, 8),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      AppText(widget.projectName,
                          variant: AppTextVariant.title,
                          fontSize: 17,
                          weight: FontWeight.w700),
                      const SizedBox(height: 2),
                      AppText(
                        '$activeCount người đang tham gia, ${members.length} giai đoạn tất cả',
                        variant: AppTextVariant.caption,
                        fontSize: 12.5,
                        color: AppColors.textSecondary,
                      ),
                      if (widget.canEdit) ...[
                        const SizedBox(height: AppDimens.space12),
                        AppButton(
                          label: 'Thêm nhân sự',
                          icon: PhosphorIconsRegular.userPlus,
                          onPressed: _openAddSheet,
                        ),
                      ],
                    ],
                  ),
                ),
                Expanded(
                  child: members.isEmpty
                      ? const Center(
                          child: Padding(
                            padding: EdgeInsets.all(AppDimens.space24),
                            child: AppText(
                              'Chưa có nhân sự nào trong dự án này.',
                              variant: AppTextVariant.caption,
                              color: AppColors.textSecondary,
                              align: TextAlign.center,
                            ),
                          ),
                        )
                      : ListView.separated(
                          padding: const EdgeInsets.fromLTRB(20, 8, 20, 24),
                          itemCount: members.length,
                          separatorBuilder: (_, __) =>
                              const SizedBox(height: AppDimens.space8),
                          itemBuilder: (context, index) => _MemberRow(
                            member: members[index],
                            canEdit: widget.canEdit,
                            onOpenActions: () => _openActions(members[index]),
                          ),
                        ),
                ),
              ],
            );
          },
        ),
      ),
    );
  }

  Future<void> _openActions(ProjectMember member) async {
    final action = await showModalBottomSheet<String>(
      context: context,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
      ),
      builder: (context) => _MemberActionsSheet(member: member),
    );
    if (action == null || !mounted) return;

    switch (action) {
      case 'setPm':
        await _confirmSetPm(member);
        break;
      case 'end':
        await _openEndSheet(member);
        break;
      case 'remove':
        await _confirmRemove(member);
        break;
    }
  }

  Future<void> _confirmSetPm(ProjectMember member) async {
    final ok = await DialogService.showConfirm(
      'Đặt "${member.userFullName}" làm PM dự án này?',
      title: 'Đặt làm PM',
    );
    if (!ok) return;

    final result =
        await _service.setPm(projectId: widget.projectId, assignmentId: member.id);
    if (!mounted) return;

    if (result.isSuccess) {
      _applyResult(result.data!);
      ToastService.show('Đã đặt "${member.userFullName}" làm PM.',
          type: ToastType.success);
    } else {
      ToastService.show(result.error!, type: ToastType.error);
    }
  }

  Future<void> _openEndSheet(ProjectMember member) async {
    final result = await showModalBottomSheet<bool>(
      context: context,
      isScrollControlled: true,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
      ),
      builder: (context) => _EndMemberSheet(
        service: _service,
        projectId: widget.projectId,
        member: member,
        onDone: _applyResult,
      ),
    );
    if (result == true) {
      ToastService.show('Đã kết thúc tham gia.', type: ToastType.success);
    }
  }

  Future<void> _confirmRemove(ProjectMember member) async {
    final ok = await DialogService.showConfirm(
      'Xoá hẳn giai đoạn tham gia của "${member.userFullName}" khỏi lịch sử? '
      'Hành động này không thể hoàn tác.',
      title: 'Xoá khỏi lịch sử',
    );
    if (!ok) return;

    final result = await _service.removeMember(
        projectId: widget.projectId, assignmentId: member.id);
    if (!mounted) return;

    if (result.isSuccess) {
      _applyResult(result.data!);
      ToastService.show('Đã xoá khỏi lịch sử.', type: ToastType.success);
    } else {
      ToastService.show(result.error!, type: ToastType.error);
    }
  }
}

class _MemberRow extends StatelessWidget {
  const _MemberRow(
      {required this.member, required this.canEdit, required this.onOpenActions});

  final ProjectMember member;
  final bool canEdit;
  final VoidCallback onOpenActions;

  @override
  Widget build(BuildContext context) {
    final format = DateFormat('dd/MM/yyyy');

    return Container(
      padding: const EdgeInsets.all(AppDimens.space12),
      decoration: BoxDecoration(
        color: AppColors.surface,
        borderRadius: BorderRadius.circular(AppDimens.radiusMd),
        border: Border.all(color: AppColors.border),
      ),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    Expanded(
                      child: AppText(member.userFullName,
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                          variant: AppTextVariant.body,
                          fontSize: 14,
                          weight: FontWeight.w700),
                    ),
                    if (member.isPm) ...[
                      const SizedBox(width: AppDimens.space8),
                      _Tag(
                        label: member.isActive ? 'PM' : 'PM giai đoạn này',
                        color: AppColors.primary,
                      ),
                    ],
                  ],
                ),
                if (member.role?.isNotEmpty == true) ...[
                  const SizedBox(height: 2),
                  AppText(member.role!,
                      variant: AppTextVariant.caption,
                      fontSize: 12.5,
                      color: AppColors.textSecondary),
                ],
                const SizedBox(height: AppDimens.space4),
                AppText(_phaseLabel(member.phase),
                    variant: AppTextVariant.caption,
                    fontSize: 12,
                    color: AppColors.textSecondary),
                const SizedBox(height: 2),
                AppText(
                  '${format.format(member.joinedAt ?? DateTime.now())}'
                  ' – ${member.leftAt == null ? "nay" : format.format(member.leftAt!)}',
                  variant: AppTextVariant.caption,
                  fontSize: 12,
                  color: AppColors.textSecondary,
                ),
                if (member.note?.isNotEmpty == true) ...[
                  const SizedBox(height: 2),
                  AppText(member.note!,
                      variant: AppTextVariant.overline,
                      fontSize: 11,
                      color: AppColors.textFaint,
                      letterSpacing: 0),
                ],
                const SizedBox(height: AppDimens.space8),
                _Tag(
                  label: member.isActive ? 'Đang tham gia' : 'Đã rời',
                  color: member.isActive
                      ? AppColors.success
                      : AppColors.textSecondary,
                ),
              ],
            ),
          ),
          if (canEdit) ...[
            const SizedBox(width: AppDimens.space4),
            AppIconButton(
              icon: PhosphorIconsRegular.dotsThreeVertical,
              tooltip: 'Thao tác',
              onPressed: onOpenActions,
            ),
          ],
        ],
      ),
    );
  }

  String _phaseLabel(String phase) {
    switch (phase) {
      case 'TrienKhai':
        return 'Triển khai';
      case 'HoTro':
        return 'Hỗ trợ';
      default:
        return 'Triển khai + Hỗ trợ';
    }
  }
}

class _Tag extends StatelessWidget {
  const _Tag({required this.label, required this.color});

  final String label;
  final Color color;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding:
          const EdgeInsets.symmetric(horizontal: AppDimens.space8, vertical: 3),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.1),
        borderRadius: BorderRadius.circular(AppDimens.radiusSm),
      ),
      child: AppText(label,
          variant: AppTextVariant.overline,
          fontSize: 10.5,
          weight: FontWeight.w700,
          color: color,
          letterSpacing: 0),
    );
  }
}

class _MemberActionsSheet extends StatelessWidget {
  const _MemberActionsSheet({required this.member});

  final ProjectMember member;

  @override
  Widget build(BuildContext context) {
    return SafeArea(
      child: Padding(
        padding: const EdgeInsets.fromLTRB(20, 20, 20, 20),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            AppText(member.userFullName,
                variant: AppTextVariant.body,
                fontSize: 16,
                weight: FontWeight.w700),
            const SizedBox(height: AppDimens.space16),
            if (member.isActive && !member.isPm)
              _ActionTile(
                icon: PhosphorIconsRegular.crown,
                label: 'Đặt làm PM',
                onTap: () => Navigator.of(context).pop('setPm'),
              ),
            if (member.isActive)
              _ActionTile(
                icon: PhosphorIconsRegular.signOut,
                label: 'Kết thúc tham gia',
                onTap: () => Navigator.of(context).pop('end'),
              ),
            if (!member.isActive)
              _ActionTile(
                icon: PhosphorIconsRegular.trash,
                label: 'Xoá khỏi lịch sử',
                color: AppColors.danger,
                onTap: () => Navigator.of(context).pop('remove'),
              ),
          ],
        ),
      ),
    );
  }
}

class _ActionTile extends StatelessWidget {
  const _ActionTile({
    required this.icon,
    required this.label,
    required this.onTap,
    this.color,
  });

  final IconData icon;
  final String label;
  final VoidCallback onTap;
  final Color? color;

  @override
  Widget build(BuildContext context) {
    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(AppDimens.radiusSm),
      child: ConstrainedBox(
        constraints: const BoxConstraints(minHeight: AppDimens.minTapTarget),
        child: Row(
          children: [
            PhosphorIcon(icon, size: 20, color: color ?? AppColors.textPrimary),
            const SizedBox(width: AppDimens.space12),
            AppText(label,
                variant: AppTextVariant.body,
                color: color ?? AppColors.textPrimary),
          ],
        ),
      ),
    );
  }
}

class _EndMemberSheet extends StatefulWidget {
  const _EndMemberSheet({
    required this.service,
    required this.projectId,
    required this.member,
    required this.onDone,
  });

  final ProjectMembersService service;
  final int projectId;
  final ProjectMember member;
  final ValueChanged<List<ProjectMember>> onDone;

  @override
  State<_EndMemberSheet> createState() => _EndMemberSheetState();
}

class _EndMemberSheetState extends State<_EndMemberSheet> {
  late DateTime _leftAt = DateTime.now();
  bool _submitting = false;

  Future<void> _submit() async {
    setState(() => _submitting = true);
    final result = await widget.service.endMember(
      projectId: widget.projectId,
      assignmentId: widget.member.id,
      leftAt: _leftAt,
    );
    if (!mounted) return;
    setState(() => _submitting = false);

    if (result.isSuccess) {
      widget.onDone(result.data!);
      Navigator.of(context).pop(true);
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
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            AppText('Kết thúc tham gia — ${widget.member.userFullName}',
                variant: AppTextVariant.body,
                fontSize: 16,
                weight: FontWeight.w700),
            const SizedBox(height: AppDimens.space16),
            AppDateField(
              label: 'Rời dự án ngày',
              value: _leftAt,
              firstDate: widget.member.joinedAt,
              onChanged: (d) => setState(() => _leftAt = d),
            ),
            const SizedBox(height: AppDimens.space16),
            AppButton(
              label: 'Kết thúc tham gia',
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
