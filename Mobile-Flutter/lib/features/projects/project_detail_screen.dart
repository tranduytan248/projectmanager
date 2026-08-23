import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import 'package:phosphor_icons/phosphor_icons.dart';

import '../../config/app_theme.dart';
import '../../core/classes/route_manager.dart';
import '../../core/theme/app_colors.dart';
import '../../core/theme/app_dimens.dart';
import '../../core/theme/app_text_styles.dart';
import '../../core/widgets/app_app_bar.dart';
import '../../core/widgets/app_button.dart';
import '../../core/widgets/app_card.dart';
import '../../core/widgets/app_detail_section.dart';
import '../../core/widgets/app_error_state.dart';
import '../../core/widgets/app_icon_button.dart';
import '../../core/widgets/app_loading.dart';
import '../../core/widgets/app_scaffold.dart';
import '../../core/widgets/app_text.dart';
import '../app_routes.dart';
import '../dashboard/dashboard_models.dart';
import 'my_projects_models.dart';
import 'project_detail_models.dart';
import 'project_detail_service.dart';

class ProjectDetailScreen extends StatefulWidget {
  const ProjectDetailScreen({super.key, required this.projectId});

  final String projectId;

  @override
  State<ProjectDetailScreen> createState() => _ProjectDetailScreenState();
}

class _ProjectDetailScreenState extends State<ProjectDetailScreen> {
  final _service = ProjectDetailService();
  late Future<ProjectDetail> _future;
  ProjectDetail? _cachedProject;

  @override
  void initState() {
    super.initState();
    _future = _service.fetch(widget.projectId);
  }

  void _reload() {
    setState(() => _future = _service.fetch(widget.projectId));
  }

  @override
  Widget build(BuildContext context) {
    return AppScaffold(
      appBar: AppAppBar(
        title: 'Chi tiết dự án',
        actions: [
          AppIconButton(
            icon: PhosphorIconsRegular.chatsCircle,
            tooltip: 'Trao đổi dự án',
            color: AppColors.primary,
            onPressed: () => Nav.toNamed(
              context,
              AppRoutes.projectDiscussion,
              arguments: {
                'projectId': widget.projectId,
                'projectName': _cachedProject?.name ?? '',
                'members': _cachedProject?.members ?? const <ProjectMember>[],
              },
            ),
          ),
        ],
      ),
      body: SafeArea(
        child: FutureBuilder<ProjectDetail>(
          future: _future,
          builder: (context, snapshot) {
            if (snapshot.connectionState != ConnectionState.done) {
              return const Center(
                child: AppLoading(),
              );
            }

            if (snapshot.hasError) {
              return AppErrorState(
                  message: 'Không tải được thông tin dự án.', onRetry: _reload);
            }

            final p = snapshot.data!;
            _cachedProject = p;

            return ListView(
              padding: const EdgeInsets.fromLTRB(20, 16, 20, 24),
              children: [
                _Header(project: p),
                const SizedBox(height: 16),
                Row(
                  children: [
                    Expanded(
                      child: AppButton(
                        label: 'Checklist',
                        icon: PhosphorIconsRegular.listChecks,
                        onPressed: () => Nav.toNamed(
                            context, AppRoutes.checklist,
                            arguments: {'projectId': p.id.toString()}),
                      ),
                    ),
                    // Chi PM hoac Quan ly To moi sua duoc du an (p.canEdit = CanEditProject ben
                    // backend) — thanh vien thuong khong thay nut nay, khop dung quyen web.
                    if (p.canEdit) ...[
                      const SizedBox(width: 10),
                      Expanded(
                        child: AppButton(
                          type: AppButtonType.outline,
                          label: 'Nhân sự',
                          icon: PhosphorIconsRegular.usersThree,
                          onPressed: () async {
                            await Nav.toNamed(
                              context,
                              AppRoutes.projectMembers,
                              arguments: {
                                'projectId': p.id.toString(),
                                'projectName': p.name,
                                'canEdit': p.canEdit,
                              },
                            );
                            _reload();
                          },
                        ),
                      ),
                    ],
                  ],
                ),
                const SizedBox(height: 16),
                _StatsStrip(
                  project: p,
                  onOpenMembers: () async {
                    await Nav.toNamed(
                      context,
                      AppRoutes.projectMembers,
                      arguments: {
                        'projectId': p.id.toString(),
                        'projectName': p.name,
                        'canEdit': p.canEdit,
                      },
                    );
                    _reload();
                  },
                  onOpenChecklist: ({String? kindFilter, String? dueFilter}) =>
                      Nav.toNamed(
                    context,
                    AppRoutes.checklist,
                    arguments: {
                      'projectId': p.id.toString(),
                      if (kindFilter != null) 'kindFilter': kindFilter,
                      if (dueFilter != null) 'dueFilter': dueFilter,
                    },
                  ),
                ),
                const SizedBox(height: 16),
                AppDetailSection(
                  title: 'Thông tin chung',
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      AppDetailInfoRow(
                          label: 'Khách hàng',
                          value: p.customer ?? '—',
                          labelWidth: 92),
                      AppDetailInfoRow(
                          label: 'Loại dự án',
                          value: p.projectType ?? '—',
                          labelWidth: 92),
                      AppDetailInfoRow(
                          label: 'Giai đoạn',
                          value: projectPhaseLabel(p.phase),
                          labelWidth: 92),
                      AppDetailInfoRow(
                          label: 'Trạng thái',
                          value: projectStateLabel(p.state),
                          labelWidth: 92),
                      AppDetailInfoRow(
                        label: 'Thời gian',
                        value: p.startDate == null
                            ? '—'
                            : '${DateFormat('dd/MM/yyyy').format(p.startDate!)}'
                                ' – ${p.endDate == null ? "nay" : DateFormat('dd/MM/yyyy').format(p.endDate!)}',
                        labelWidth: 92,
                      ),
                      AppDetailInfoRow(
                          label: 'PM',
                          value: p.pmName ?? '(chưa có PM)',
                          labelWidth: 92),
                      if (p.description.trim().isNotEmpty) ...[
                        const SizedBox(height: 8),
                        AppText(p.description,
                            variant: AppTextVariant.body,
                            fontSize: 13,
                            height: 1.5),
                      ],
                    ],
                  ),
                ),
                const SizedBox(height: 16),
                AppDetailSection(
                  title: 'Nhân sự dự án (${p.members.length})',
                  child: Column(
                    children: [
                      for (final m in p.members) _MemberRow(member: m),
                    ],
                  ),
                ),
                if (p.overdueTasks.isNotEmpty) ...[
                  const SizedBox(height: 16),
                  AppDetailSection(
                    title: 'Đầu việc quá hạn',
                    child: Column(
                      children: [
                        for (final t in p.overdueTasks)
                          _OverdueTaskRow(task: t),
                      ],
                    ),
                  ),
                ],
                const SizedBox(height: 16),
                AppDetailSection(
                  title: 'Báo cáo tuần',
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      if (p.currentReport != null)
                        _ReportStatusLine(
                            report: p.currentReport!, isCurrent: true),
                      if (p.recentReports.isNotEmpty) ...[
                        const SizedBox(height: 10),
                        const Divider(height: 1),
                        const SizedBox(height: 10),
                        for (final r in p.recentReports)
                          _ReportStatusLine(report: r),
                      ],
                    ],
                  ),
                ),
              ],
            );
          },
        ),
      ),
    );
  }
}

class _Header extends StatelessWidget {
  const _Header({required this.project});

  final ProjectDetail project;

  @override
  Widget build(BuildContext context) {
    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  Expanded(
                    child: AppText(project.name,
                        variant: AppTextVariant.title,
                        fontSize: 19,
                        weight: FontWeight.w800),
                  ),
                  if (!project.isOpen)
                    Container(
                      margin: const EdgeInsets.only(left: 8),
                      padding: const EdgeInsets.symmetric(
                          horizontal: 8, vertical: 3),
                      decoration: BoxDecoration(
                        color: AppColors.surfaceVariant,
                        borderRadius: BorderRadius.circular(6),
                      ),
                      child: const AppText('Đã đóng',
                          variant: AppTextVariant.overline,
                          fontSize: 10.5,
                          color: AppColors.textSecondary,
                          letterSpacing: 0),
                    ),
                ],
              ),
              if (project.code?.isNotEmpty == true) ...[
                const SizedBox(height: 2),
                AppText(project.code!,
                    variant: AppTextVariant.caption,
                    fontSize: 12.5,
                    color: AppColors.textSecondary),
              ],
            ],
          ),
        ),
      ],
    );
  }
}

/// [onOpenChecklist] nhan kem bo loc tuy chon — Qua han/CV Ho tro truyen dueFilter/kindFilter de
/// man Checklist mo san dung bo loc, Thanh vien/Checklist thi khong loc gi (xem qua het).
class _StatsStrip extends StatelessWidget {
  const _StatsStrip({
    required this.project,
    required this.onOpenMembers,
    required this.onOpenChecklist,
  });

  final ProjectDetail project;
  final VoidCallback onOpenMembers;
  final void Function({String? kindFilter, String? dueFilter}) onOpenChecklist;

  @override
  Widget build(BuildContext context) {
    return Row(
      children: [
        Expanded(
            child: _StatTile(
                value: '${project.activeMemberCount}',
                label: 'Thành viên',
                color: AppTheme.brandBlue,
                onTap: onOpenMembers)),
        const SizedBox(width: 10),
        Expanded(
            child: _StatTile(
                value: '${project.checklistDone}/${project.checklistTotal}',
                label: 'Checklist',
                color: AppTheme.brandBlue,
                onTap: () => onOpenChecklist())),
        const SizedBox(width: 10),
        Expanded(
            child: _StatTile(
                value: '${project.checklistOverdue}',
                label: 'Quá hạn',
                color: project.checklistOverdue > 0
                    ? AppTheme.statusDanger
                    : AppTheme.brandBlue,
                onTap: () => onOpenChecklist(dueFilter: 'quahan'))),
        const SizedBox(width: 10),
        Expanded(
            child: _StatTile(
                value: '${project.supportThisWeek}',
                label: 'CV Hỗ trợ',
                color: AppTheme.brandBlue,
                onTap: () => onOpenChecklist(kindFilter: 'HoTro'))),
      ],
    );
  }
}

class _StatTile extends StatelessWidget {
  const _StatTile({
    required this.value,
    required this.label,
    required this.color,
    required this.onTap,
  });

  final String value;
  final String label;
  final Color color;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(12),
      child: ConstrainedBox(
        constraints: const BoxConstraints(minHeight: AppDimens.minTapTarget),
        child: AppCard(
          padding: const EdgeInsets.symmetric(vertical: 10, horizontal: 8),
          radius: 12,
          child: Column(
            children: [
              AppText(value,
                  variant: AppTextVariant.body,
                  fontSize: 16,
                  weight: FontWeight.w800,
                  color: color),
              const SizedBox(height: 2),
              AppText(label,
                  align: TextAlign.center,
                  maxLines: 2,
                  variant: AppTextVariant.overline,
                  fontSize: 10,
                  color: AppColors.textSecondary,
                  letterSpacing: 0),
            ],
          ),
        ),
      ),
    );
  }
}

class _MemberRow extends StatelessWidget {
  const _MemberRow({required this.member});

  final ProjectMember member;

  @override
  Widget build(BuildContext context) {
    final format = DateFormat('dd/MM/yyyy');

    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 7),
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
                          fontSize: 13,
                          weight: FontWeight.w600),
                    ),
                    if (member.isPm) ...[
                      const SizedBox(width: 6),
                      const _SmallTag(label: 'PM', color: AppTheme.brandBlue),
                    ] else if (member.role?.isNotEmpty == true) ...[
                      const SizedBox(width: 6),
                      _SmallTag(
                          label: member.role!, color: AppTheme.brandBlueDark),
                    ],
                  ],
                ),
                const SizedBox(height: 2),
                AppText(
                  'Từ ${format.format(member.joinedAt ?? DateTime.now())}'
                  '${member.leftAt != null ? " đến ${format.format(member.leftAt!)}" : ""}',
                  variant: AppTextVariant.overline,
                  fontSize: 11,
                  color: AppColors.textSecondary,
                  letterSpacing: 0,
                ),
              ],
            ),
          ),
          const SizedBox(width: 8),
          _SmallTag(
            label: member.isActive ? 'Đang tham gia' : 'Đã rời',
            color: member.isActive
                ? AppTheme.statusSuccess
                : AppColors.textSecondary,
          ),
        ],
      ),
    );
  }
}

class _SmallTag extends StatelessWidget {
  const _SmallTag({required this.label, required this.color});

  final String label;
  final Color color;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 7, vertical: 2),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.1),
        borderRadius: BorderRadius.circular(4),
      ),
      child: AppText(label,
          variant: AppTextVariant.overline,
          fontSize: 9.5,
          weight: FontWeight.w700,
          color: color,
          letterSpacing: 0),
    );
  }
}

class _OverdueTaskRow extends StatelessWidget {
  const _OverdueTaskRow({required this.task});

  final TaskItem task;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 6),
      child: Row(
        children: [
          const PhosphorIcon(PhosphorIconsRegular.warningCircle,
              size: 15, color: AppTheme.statusDanger),
          const SizedBox(width: 8),
          Expanded(
            child: AppText(task.title,
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
                variant: AppTextVariant.caption,
                fontSize: 12.5,
                color: AppColors.textPrimary),
          ),
          if (task.dueDate != null)
            AppText(DateFormat('dd/MM').format(task.dueDate!),
                variant: AppTextVariant.overline,
                fontSize: 11,
                weight: FontWeight.w600,
                color: AppTheme.statusDanger,
                letterSpacing: 0),
        ],
      ),
    );
  }
}

class _ReportStatusLine extends StatelessWidget {
  const _ReportStatusLine({required this.report, this.isCurrent = false});

  final WeekReportSummary report;
  final bool isCurrent;

  @override
  Widget build(BuildContext context) {
    late final String label;
    late final Color color;

    if (report.isSubmitted) {
      label = 'Đã nộp ${report.isOnTime ? "đúng hạn" : "trễ hạn"}';
      color = report.isOnTime ? AppTheme.statusSuccess : AppTheme.statusWarning;
    } else {
      label = 'Chưa nộp';
      color = AppTheme.statusDanger;
    }

    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 4),
      child: Row(
        children: [
          Expanded(
            child: AppText(
              isCurrent
                  ? 'Tuần này (${report.week}/${report.year})'
                  : 'Tuần ${report.week}/${report.year}',
              variant: AppTextVariant.caption,
              fontSize: 12.5,
              weight: isCurrent ? FontWeight.w700 : FontWeight.w500,
              color: AppColors.textPrimary,
            ),
          ),
          _SmallTag(label: label, color: color),
        ],
      ),
    );
  }
}
