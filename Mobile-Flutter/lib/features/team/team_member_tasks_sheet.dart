import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

import '../../core/theme/app_colors.dart';
import '../../core/theme/app_dimens.dart';
import '../../core/theme/app_text_styles.dart';
import '../../core/widgets/app_card.dart';
import '../../core/widgets/app_error_state.dart';
import '../../core/widgets/app_icon_button.dart';
import '../../core/widgets/app_loading.dart';
import '../../core/widgets/app_text.dart';
import '../app_routes.dart';
import 'team_dashboard_models.dart';
import 'team_dashboard_service.dart';

/// Modal Bottom Sheet hiển thị danh sách công việc của 1 thành viên theo loại (Triển khai / Hỗ trợ).
class TeamMemberTasksSheet extends StatefulWidget {
  const TeamMemberTasksSheet({
    super.key,
    required this.userId,
    required this.memberName,
    required this.year,
    required this.month,
    required this.kind,
  });

  final int userId;
  final String memberName;
  final int year;
  final int month;
  final String kind;

  static Future<void> show(
    BuildContext context, {
    required int userId,
    required String memberName,
    required int year,
    required int month,
    required String kind,
  }) {
    return showModalBottomSheet<void>(
      context: context,
      isScrollControlled: true,
      useSafeArea: true,
      backgroundColor: Colors.transparent,
      builder: (context) => TeamMemberTasksSheet(
        userId: userId,
        memberName: memberName,
        year: year,
        month: month,
        kind: kind,
      ),
    );
  }

  @override
  State<TeamMemberTasksSheet> createState() => _TeamMemberTasksSheetState();
}

class _TeamMemberTasksSheetState extends State<TeamMemberTasksSheet> {
  final _service = TeamDashboardService();
  bool _loading = true;
  String? _error;
  TeamMemberTasksResult? _data;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _error = null;
    });

    try {
      final res = await _service.fetchMemberTasks(
        userId: widget.userId,
        year: widget.year,
        month: widget.month,
        kind: widget.kind,
      );
      if (mounted) {
        setState(() {
          _data = res;
          _loading = false;
        });
      }
    } catch (e) {
      if (mounted) {
        setState(() {
          _error = 'Không thể tải danh sách công việc: ${e.toString()}';
          _loading = false;
        });
      }
    }
  }

  String _formatDate(DateTime? dt) {
    if (dt == null) return '—';
    return DateFormat('dd/MM/yyyy').format(dt);
  }

  String _formatHours(double val) {
    if (val == val.roundToDouble()) {
      return val.toInt().toString();
    }
    return val.toStringAsFixed(1);
  }

  @override
  Widget build(BuildContext context) {
    final bottomInset = MediaQuery.of(context).viewInsets.bottom;
    final isSupport = widget.kind.toLowerCase() == 'support' || widget.kind == 'HoTro';
    final kindLabel = isSupport ? 'Công việc Hỗ trợ' : 'Công việc Triển khai';
    final kindColor = isSupport ? AppColors.warning : AppColors.primary;

    return Container(
      decoration: const BoxDecoration(
        color: AppColors.surface,
        borderRadius: BorderRadius.vertical(
          top: Radius.circular(AppDimens.radiusLg),
        ),
      ),
      constraints: BoxConstraints(
        maxHeight: MediaQuery.of(context).size.height * 0.85,
      ),
      child: SafeArea(
        top: false,
        child: Padding(
          padding: EdgeInsets.fromLTRB(
            AppDimens.space16,
            AppDimens.space16,
            AppDimens.space16,
            AppDimens.space24 + bottomInset,
          ),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              // Thanh kéo trên modal
              Center(
                child: Container(
                  width: 40,
                  height: 4,
                  margin: const EdgeInsets.only(bottom: AppDimens.space16),
                  decoration: BoxDecoration(
                    color: AppColors.borderStrong,
                    borderRadius: BorderRadius.circular(2),
                  ),
                ),
              ),

              // Header
              Row(
                children: [
                  Container(
                    width: 40,
                    height: 40,
                    decoration: BoxDecoration(
                      color: kindColor.withValues(alpha: 0.12),
                      shape: BoxShape.circle,
                    ),
                    child: Center(
                      child: Icon(
                        isSupport ? Icons.support_agent : Icons.assignment_outlined,
                        color: kindColor,
                        size: 22,
                      ),
                    ),
                  ),
                  const SizedBox(width: AppDimens.space12),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        AppText(
                          kindLabel,
                          variant: AppTextVariant.body,
                          weight: FontWeight.w700,
                          fontSize: 16,
                        ),
                        const SizedBox(height: 2),
                        AppText(
                          '${widget.memberName} · Tháng ${widget.month}/${widget.year}',
                          variant: AppTextVariant.caption,
                          color: AppColors.textSecondary,
                        ),
                      ],
                    ),
                  ),
                  AppIconButton(
                    icon: Icons.close,
                    tooltip: 'Đóng',
                    onPressed: () => Navigator.of(context).pop(),
                  ),
                ],
              ),
              const SizedBox(height: AppDimens.space12),

              // Nội dung danh sách
              Expanded(
                child: _buildBody(),
              ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildBody() {
    if (_loading) {
      return const Center(child: AppLoading());
    }

    if (_error != null) {
      return AppErrorState(
        message: _error!,
        onRetry: _load,
      );
    }

    final tasks = _data?.tasks ?? [];
    if (tasks.isEmpty) {
      return Center(
        child: Padding(
          padding: const EdgeInsets.all(AppDimens.space24),
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Icon(
                Icons.assignment_outlined,
                size: 48,
                color: AppColors.textSecondary.withValues(alpha: 0.5),
              ),
              const SizedBox(height: AppDimens.space12),
              const AppText(
                'Chưa có công việc nào',
                variant: AppTextVariant.body,
                weight: FontWeight.w700,
              ),
              const SizedBox(height: AppDimens.space4),
              const AppText(
                'Thành viên này không có đầu việc nào trong tháng được chọn.',
                variant: AppTextVariant.caption,
                color: AppColors.textSecondary,
                align: TextAlign.center,
              ),
            ],
          ),
        ),
      );
    }

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        // Thống kê nhanh
        Container(
          padding: const EdgeInsets.symmetric(
            horizontal: AppDimens.space12,
            vertical: AppDimens.space8,
          ),
          margin: const EdgeInsets.only(bottom: AppDimens.space12),
          decoration: BoxDecoration(
            color: AppColors.primarySoft,
            borderRadius: BorderRadius.circular(AppDimens.radiusMd),
          ),
          child: Row(
            children: [
              const Icon(
                Icons.folder_outlined,
                size: 16,
                color: AppColors.textSecondary,
              ),
              const SizedBox(width: AppDimens.space4),
              AppText(
                '${_data?.totalProjects ?? 0} dự án',
                variant: AppTextVariant.caption,
                weight: FontWeight.w600,
              ),
              const SizedBox(width: AppDimens.space12),
              const Icon(
                Icons.task_alt_outlined,
                size: 16,
                color: AppColors.textSecondary,
              ),
              const SizedBox(width: AppDimens.space4),
              AppText(
                '${_data?.totalTasks ?? 0} công việc',
                variant: AppTextVariant.caption,
                weight: FontWeight.w600,
              ),
            ],
          ),
        ),

        // Danh sách công việc
        Expanded(
          child: ListView.separated(
            itemCount: tasks.length,
            separatorBuilder: (_, __) => const SizedBox(height: AppDimens.space8),
            itemBuilder: (context, index) {
              final task = tasks[index];
              return _buildTaskItem(task);
            },
          ),
        ),
      ],
    );
  }

  Widget _buildTaskItem(TeamMemberTaskItem task) {
    return AppCard(
      padding: const EdgeInsets.all(AppDimens.space12),
      onTap: () {
        Navigator.of(context).pushNamed(
          AppRoutes.taskDetail,
          arguments: {'taskId': '${task.id}'},
        );
      },
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // Tiêu đề & Tag quá hạn
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Expanded(
                child: AppText(
                  task.title,
                  variant: AppTextVariant.body,
                  weight: FontWeight.w600,
                  fontSize: 14.5,
                  maxLines: 2,
                  overflow: TextOverflow.ellipsis,
                ),
              ),
              if (task.isOverdue) ...[
                const SizedBox(width: AppDimens.space8),
                Container(
                  padding: const EdgeInsets.symmetric(
                    horizontal: AppDimens.space8,
                    vertical: 2,
                  ),
                  decoration: BoxDecoration(
                    color: AppColors.dangerSoft,
                    borderRadius: BorderRadius.circular(AppDimens.radiusSm),
                  ),
                  child: const AppText(
                    'Quá hạn',
                    variant: AppTextVariant.caption,
                    fontSize: 11,
                    weight: FontWeight.w600,
                    color: AppColors.danger,
                  ),
                ),
              ],
            ],
          ),
          const SizedBox(height: AppDimens.space4),

          // Tên dự án
          Row(
            children: [
              const Icon(
                Icons.folder_open_outlined,
                size: 14,
                color: AppColors.textSecondary,
              ),
              const SizedBox(width: AppDimens.space4),
              Expanded(
                child: AppText(
                  task.projectName.isNotEmpty ? task.projectName : 'Việc riêng',
                  variant: AppTextVariant.caption,
                  color: AppColors.textSecondary,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                ),
              ),
            ],
          ),
          const SizedBox(height: AppDimens.space8),

          // Footer: Tiến độ %, Trạng thái, Giờ logtime, Hạn
          Row(
            children: [
              // Badge tiến độ
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
                decoration: BoxDecoration(
                  color: AppColors.primarySoft,
                  borderRadius: BorderRadius.circular(4),
                ),
                child: AppText(
                  '${task.progress}%',
                  variant: AppTextVariant.caption,
                  fontSize: 11.5,
                  weight: FontWeight.w700,
                  color: AppColors.primaryDark,
                ),
              ),
              const SizedBox(width: AppDimens.space8),

              // Giờ công đã log
              const Icon(
                Icons.access_time_outlined,
                size: 13,
                color: AppColors.textSecondary,
              ),
              const SizedBox(width: 2),
              AppText(
                '${_formatHours(task.loggedHours)}h',
                variant: AppTextVariant.caption,
                color: AppColors.textSecondary,
                weight: FontWeight.w600,
              ),
              const Spacer(),

              // Hạn hoàn thành
              const Icon(
                Icons.calendar_today_outlined,
                size: 13,
                color: AppColors.textSecondary,
              ),
              const SizedBox(width: 2),
              AppText(
                'Hạn: ${_formatDate(task.dueDate)}',
                variant: AppTextVariant.caption,
                color: task.isOverdue ? AppColors.danger : AppColors.textSecondary,
                weight: task.isOverdue ? FontWeight.w600 : FontWeight.normal,
              ),
            ],
          ),
        ],
      ),
    );
  }
}
