import 'package:flutter/material.dart';
import 'package:phosphor_icons/phosphor_icons.dart';

import '../../core/classes/route_manager.dart';
import '../../core/theme/app_colors.dart';
import '../../core/theme/app_dimens.dart';
import '../../core/theme/app_text_styles.dart';
import '../../core/widgets/app_app_bar.dart';
import '../../core/widgets/app_button.dart';
import '../../core/widgets/app_card.dart';
import '../../core/widgets/app_error_state.dart';
import '../../core/widgets/app_icon_button.dart';
import '../../core/widgets/app_loading.dart';
import '../../core/widgets/app_scaffold.dart';
import '../../core/widgets/app_text.dart';
import '../app_routes.dart';
import 'kpi_models.dart';
import 'kpi_service.dart';

class KpiDetailScreen extends StatefulWidget {
  const KpiDetailScreen({
    super.key,
    required this.userId,
    required this.userName,
    required this.year,
    required this.month,
  });

  final int userId;
  final String userName;
  final int year;
  final int month;

  @override
  State<KpiDetailScreen> createState() => _KpiDetailScreenState();
}

class _KpiDetailScreenState extends State<KpiDetailScreen>
    with SingleTickerProviderStateMixin {
  final _service = KpiService();
  late Future<KpiDetailData> _future;
  late TabController _tabController;

  bool _isRecalculating = false;

  @override
  void initState() {
    super.initState();
    _tabController = TabController(length: 4, vsync: this);
    _loadData();
  }

  @override
  void dispose() {
    _tabController.dispose();
    super.dispose();
  }

  void _loadData() {
    setState(() {
      _future = _service.fetchDetail(
        userId: widget.userId,
        year: widget.year,
        month: widget.month,
      );
    });
  }

  Future<void> _handleRecalculate() async {
    if (_isRecalculating) return;

    final confirm = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(AppDimens.radiusLg),
        ),
        title: const AppText(
          'Tính lại KPI',
          variant: AppTextVariant.title,
          fontSize: 18,
          weight: FontWeight.w700,
        ),
        content: AppText(
          'Bạn có chắc muốn tính lại KPI tháng ${widget.month}/${widget.year} cho ${widget.userName}?',
          variant: AppTextVariant.body,
          color: AppColors.textSecondary,
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(ctx).pop(false),
            child: const AppText('Hủy', color: AppColors.textSecondary),
          ),
          AppButton(
            label: 'Tính lại',
            onPressed: () => Navigator.of(ctx).pop(true),
          ),
        ],
      ),
    );

    if (confirm != true || !mounted) return;

    setState(() => _isRecalculating = true);

    try {
      final msg = await _service.recalculateUser(
        year: widget.year,
        month: widget.month,
        userId: widget.userId,
      );

      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: AppText(msg, color: AppColors.surface),
          backgroundColor: AppColors.success,
        ),
      );
      _loadData();
    } catch (e) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: AppText('Lỗi: $e', color: AppColors.surface),
          backgroundColor: AppColors.danger,
        ),
      );
    } finally {
      if (mounted) setState(() => _isRecalculating = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return AppScaffold(
      appBar: AppAppBar(
        title: 'Chi tiết KPI',
        actions: [
          AppIconButton(
            icon: PhosphorIconsRegular.arrowClockwise,
            tooltip: 'Tải lại',
            onPressed: _loadData,
          ),
        ],
      ),
      body: FutureBuilder<KpiDetailData>(
        future: _future,
        builder: (context, snapshot) {
          if (snapshot.connectionState == ConnectionState.waiting) {
            return const Center(child: AppLoading());
          }

          if (snapshot.hasError) {
            return AppErrorState(
              message: snapshot.error.toString(),
              onRetry: _loadData,
            );
          }

          if (!snapshot.hasData) {
            return const Center(child: AppLoading());
          }

          final data = snapshot.data!;
          final row = data.row;

          return RefreshIndicator(
            onRefresh: () async => _loadData(),
            color: AppColors.primary,
            child: NestedScrollView(
              headerSliverBuilder: (context, innerBoxIsScrolled) => [
                SliverToBoxAdapter(
                  child: Padding(
                    padding: const EdgeInsets.fromLTRB(
                      AppDimens.space16,
                      AppDimens.space16,
                      AppDimens.space16,
                      AppDimens.space8,
                    ),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.stretch,
                      children: [
                        _HeroScoreCard(
                          row: row,
                          isRecalculating: _isRecalculating,
                          canGenerate: data.canGenerate,
                          onRecalculate: _handleRecalculate,
                        ),
                        const SizedBox(height: AppDimens.space16),
                        _TabBarHeader(
                          controller: _tabController,
                          supportCount: data.supportTasks.length,
                          executeCount: data.executeTasks.length,
                          assignedCount: data.assignedTasks.length,
                        ),
                      ],
                    ),
                  ),
                ),
              ],
              body: TabBarView(
                controller: _tabController,
                children: [
                  _SupportTasksTab(
                    row: row,
                    tasks: data.supportTasks,
                  ),
                  _ExecuteTasksTab(
                    row: row,
                    tasks: data.executeTasks,
                  ),
                  _AssignedTasksTab(
                    row: row,
                    tasks: data.assignedTasks,
                  ),
                  _AttendanceTab(row: row),
                ],
              ),
            ),
          );
        },
      ),
    );
  }
}

// ---------- Hero Score Card ----------

class _HeroScoreCard extends StatelessWidget {
  const _HeroScoreCard({
    required this.row,
    required this.isRecalculating,
    required this.canGenerate,
    required this.onRecalculate,
  });

  final KpiMemberRow row;
  final bool isRecalculating;
  final bool canGenerate;
  final VoidCallback onRecalculate;

  @override
  Widget build(BuildContext context) {
    return AppCard(
      padding: const EdgeInsets.all(AppDimens.space16),
      radius: AppDimens.radiusLg,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            crossAxisAlignment: CrossAxisAlignment.center,
            children: [
              _UserAvatar(name: row.fullName),
              const SizedBox(width: AppDimens.space12),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    AppText(
                      row.fullName,
                      variant: AppTextVariant.title,
                      fontSize: 17,
                      weight: FontWeight.w700,
                    ),
                    const SizedBox(height: 2),
                    Row(
                      children: [
                        AppText(
                          'Tháng ${row.month}/${row.year}',
                          variant: AppTextVariant.caption,
                          color: AppColors.textSecondary,
                        ),
                        const SizedBox(width: AppDimens.space8),
                        _SaveBadge(isSaved: row.isSaved),
                      ],
                    ),
                  ],
                ),
              ),
              if (canGenerate)
                AppButton(
                  label: isRecalculating ? 'Đang tính...' : 'Tính lại',
                  onPressed: isRecalculating ? null : onRecalculate,
                ),
            ],
          ),
          const SizedBox(height: AppDimens.space16),
          const Divider(height: 1, color: AppColors.border),
          const SizedBox(height: AppDimens.space16),
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceAround,
            children: [
              _ScoreMetric(
                label: 'KPI Cuối cùng',
                value: row.finalPoint.toStringAsFixed(0),
                unit: 'đ',
                color: row.rankColor,
              ),
              Container(
                width: 1,
                height: 40,
                color: AppColors.border,
              ),
              _ScoreMetric(
                label: 'Xếp loại',
                value: row.rank,
                color: row.rankColor,
                isBadge: true,
                bgColor: row.rankBgColor,
              ),
              Container(
                width: 1,
                height: 40,
                color: AppColors.border,
              ),
              _ScoreMetric(
                label: 'Chuyên cần',
                value: '${row.attendanceRate}%',
                color: row.isShortHours ? AppColors.danger : AppColors.success,
              ),
            ],
          ),
          const SizedBox(height: AppDimens.space12),
          Container(
            padding: const EdgeInsets.symmetric(
              horizontal: AppDimens.space12,
              vertical: AppDimens.space8,
            ),
            decoration: BoxDecoration(
              color: AppColors.background,
              borderRadius: BorderRadius.circular(AppDimens.radiusSm),
            ),
            child: Row(
              children: [
                const Icon(
                  Icons.info_outline,
                  size: 16,
                  color: AppColors.textSecondary,
                ),
                const SizedBox(width: AppDimens.space8),
                Expanded(
                  child: AppText(
                    'Chất lượng (${row.qualityPoint.toStringAsFixed(1)}đ) × Chuyên cần (${row.attendanceRate}%) = ${row.finalPoint.toStringAsFixed(0)}đ',
                    variant: AppTextVariant.caption,
                    fontSize: 11.5,
                    color: AppColors.textSecondary,
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _ScoreMetric extends StatelessWidget {
  const _ScoreMetric({
    required this.label,
    required this.value,
    this.unit = '',
    required this.color,
    this.isBadge = false,
    this.bgColor,
  });

  final String label;
  final String value;
  final String unit;
  final Color color;
  final bool isBadge;
  final Color? bgColor;

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        if (isBadge)
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
            decoration: BoxDecoration(
              color: bgColor ?? AppColors.primarySoft,
              borderRadius: BorderRadius.circular(AppDimens.radiusPill),
            ),
            child: AppText(
              value,
              variant: AppTextVariant.body,
              fontSize: 13,
              weight: FontWeight.w700,
              color: color,
            ),
          )
        else
          Row(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.baseline,
            textBaseline: TextBaseline.alphabetic,
            children: [
              AppText(
                value,
                variant: AppTextVariant.title,
                fontSize: 22,
                weight: FontWeight.w800,
                color: color,
              ),
              if (unit.isNotEmpty) ...[
                const SizedBox(width: 2),
                AppText(
                  unit,
                  variant: AppTextVariant.caption,
                  fontSize: 12,
                  weight: FontWeight.w600,
                  color: color,
                ),
              ],
            ],
          ),
        const SizedBox(height: 4),
        AppText(
          label,
          variant: AppTextVariant.caption,
          fontSize: 11.5,
          color: AppColors.textSecondary,
        ),
      ],
    );
  }
}

class _SaveBadge extends StatelessWidget {
  const _SaveBadge({required this.isSaved});

  final bool isSaved;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
      decoration: BoxDecoration(
        color: isSaved ? AppColors.successSoft : AppColors.warningSoft,
        borderRadius: BorderRadius.circular(AppDimens.radiusSm),
      ),
      child: AppText(
        isSaved ? 'Đã chốt' : 'Tạm tính',
        variant: AppTextVariant.caption,
        fontSize: 10.5,
        weight: FontWeight.w600,
        color: isSaved ? AppColors.successOnSoft : AppColors.warningOnSoft,
      ),
    );
  }
}

class _UserAvatar extends StatelessWidget {
  const _UserAvatar({required this.name});

  final String name;

  String get _initials {
    final parts = name.trim().split(RegExp(r'\s+'));
    if (parts.isEmpty || parts.first.isEmpty) return 'U';
    if (parts.length == 1) return parts.first.substring(0, 1).toUpperCase();
    return (parts[parts.length - 2].substring(0, 1) +
            parts.last.substring(0, 1))
        .toUpperCase();
  }

  @override
  Widget build(BuildContext context) {
    return Container(
      width: 44,
      height: 44,
      alignment: Alignment.center,
      decoration: BoxDecoration(
        color: AppColors.primarySoft,
        borderRadius: BorderRadius.circular(AppDimens.radiusMd),
      ),
      child: AppText(
        _initials,
        variant: AppTextVariant.title,
        fontSize: 16,
        weight: FontWeight.w700,
        color: AppColors.primary,
      ),
    );
  }
}

// ---------- TabBar Header ----------

class _TabBarHeader extends StatelessWidget {
  const _TabBarHeader({
    required this.controller,
    required this.supportCount,
    required this.executeCount,
    required this.assignedCount,
  });

  final TabController controller;
  final int supportCount;
  final int executeCount;
  final int assignedCount;

  @override
  Widget build(BuildContext context) {
    return Container(
      decoration: BoxDecoration(
        color: AppColors.surface,
        borderRadius: BorderRadius.circular(AppDimens.radiusMd),
        border: Border.all(color: AppColors.border),
      ),
      child: TabBar(
        controller: controller,
        indicatorColor: AppColors.primary,
        indicatorWeight: 3,
        indicatorSize: TabBarIndicatorSize.tab,
        labelColor: AppColors.primary,
        unselectedLabelColor: AppColors.textSecondary,
        labelStyle: const TextStyle(
          fontSize: 12.5,
          fontWeight: FontWeight.w700,
          fontFamily: 'Inter',
        ),
        unselectedLabelStyle: const TextStyle(
          fontSize: 12.5,
          fontWeight: FontWeight.w500,
          fontFamily: 'Inter',
        ),
        tabs: [
          Tab(text: 'Hỗ trợ ($supportCount)'),
          Tab(text: 'Triển khai ($executeCount)'),
          Tab(text: 'Việc riêng ($assignedCount)'),
          const Tab(text: 'Chuyên cần'),
        ],
      ),
    );
  }
}

// ---------- Tab 1: Hỗ trợ ----------

class _SupportTasksTab extends StatelessWidget {
  const _SupportTasksTab({required this.row, required this.tasks});

  final KpiMemberRow row;
  final List<KpiTaskItem> tasks;

  @override
  Widget build(BuildContext context) {
    return ListView(
      padding: const EdgeInsets.all(AppDimens.space16),
      children: [
        _PillarSummaryCard(
          icon: PhosphorIconsRegular.lifebuoy,
          title: 'Điểm Hỗ trợ',
          scoreText:
              '${row.supportPoint.toStringAsFixed(1)} / ${row.supportGrossPoint.toStringAsFixed(1)} đ',
          subText:
              'Giờ làm: ${row.supportHours.toStringAsFixed(1)}h / ${row.supportCapHours.toStringAsFixed(1)}h trần',
          penaltyText: row.latePenalty > 0
              ? 'Trừ ${row.latePenalty.toStringAsFixed(1)}đ do trễ hạn'
              : null,
          color: AppColors.primary,
        ),
        const SizedBox(height: AppDimens.space12),
        if (tasks.isEmpty)
          const _EmptyTabState(message: 'Không có công việc hỗ trợ trong tháng này.')
        else
          for (final task in tasks) _KpiTaskCard(task: task),
      ],
    );
  }
}

// ---------- Tab 2: Triển khai ----------

class _ExecuteTasksTab extends StatelessWidget {
  const _ExecuteTasksTab({required this.row, required this.tasks});

  final KpiMemberRow row;
  final List<KpiTaskItem> tasks;

  @override
  Widget build(BuildContext context) {
    return ListView(
      padding: const EdgeInsets.all(AppDimens.space16),
      children: [
        _PillarSummaryCard(
          icon: PhosphorIconsRegular.kanban,
          title: 'Điểm Triển khai',
          scoreText: '${row.executePoint.toStringAsFixed(1)} đ',
          subText:
              'Giờ làm: ${row.executeHours.toStringAsFixed(1)}h / ${row.executeTargetHours.toStringAsFixed(1)}h định mức',
          color: const Color(0xFF0284C7),
        ),
        const SizedBox(height: AppDimens.space12),
        if (tasks.isEmpty)
          const _EmptyTabState(message: 'Không có công việc triển khai trong tháng này.')
        else
          for (final task in tasks) _KpiTaskCard(task: task),
      ],
    );
  }
}

// ---------- Tab 3: Việc riêng ----------

class _AssignedTasksTab extends StatelessWidget {
  const _AssignedTasksTab({required this.row, required this.tasks});

  final KpiMemberRow row;
  final List<KpiTaskItem> tasks;

  @override
  Widget build(BuildContext context) {
    return ListView(
      padding: const EdgeInsets.all(AppDimens.space16),
      children: [
        _PillarSummaryCard(
          icon: PhosphorIconsRegular.userPlus,
          title: 'Điểm Việc riêng',
          scoreText: '+${row.assignedPoint.toStringAsFixed(1)} đ',
          subText: 'Tổng ${tasks.length} đầu việc riêng được giao',
          color: AppColors.success,
        ),
        const SizedBox(height: AppDimens.space12),
        if (tasks.isEmpty)
          const _EmptyTabState(message: 'Không có việc riêng nào trong tháng này.')
        else
          for (final task in tasks) _KpiTaskCard(task: task),
      ],
    );
  }
}

// ---------- Tab 4: Chuyên cần ----------

class _AttendanceTab extends StatelessWidget {
  const _AttendanceTab({required this.row});

  final KpiMemberRow row;

  @override
  Widget build(BuildContext context) {
    return ListView(
      padding: const EdgeInsets.all(AppDimens.space16),
      children: [
        AppCard(
          padding: const EdgeInsets.all(AppDimens.space16),
          radius: AppDimens.radiusMd,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const Row(
                children: [
                  Icon(
                    Icons.access_time_filled,
                    size: 20,
                    color: AppColors.primary,
                  ),
                  SizedBox(width: AppDimens.space8),
                  AppText(
                    'Thống kê Ngày & Giờ công',
                    variant: AppTextVariant.title,
                    fontSize: 16,
                    weight: FontWeight.w700,
                  ),
                ],
              ),
              const SizedBox(height: AppDimens.space16),
              _AttendanceRow(
                label: 'Ngày công chuẩn tháng',
                value: '${row.standardDays} ngày',
              ),
              const Divider(height: AppDimens.space16, color: AppColors.border),
              _AttendanceRow(
                label: 'Nghỉ phép đã duyệt',
                value: '${row.leaveDays.toStringAsFixed(1)} ngày',
              ),
              const Divider(height: AppDimens.space16, color: AppColors.border),
              _AttendanceRow(
                label: 'Tổng giờ yêu cầu',
                value: '${row.requiredHours.toStringAsFixed(1)}h',
              ),
              const Divider(height: AppDimens.space16, color: AppColors.border),
              _AttendanceRow(
                label: 'Giờ làm thực tế',
                value: '${row.workedHours.toStringAsFixed(1)}h',
              ),
              const Divider(height: AppDimens.space16, color: AppColors.border),
              _AttendanceRow(
                label: 'Tỷ lệ ngày công',
                value: '${row.attendanceRate}%',
                highlightColor:
                    row.isShortHours ? AppColors.danger : AppColors.success,
              ),
              if (row.isShortHours) ...[
                const SizedBox(height: AppDimens.space12),
                Container(
                  padding: const EdgeInsets.all(AppDimens.space12),
                  decoration: BoxDecoration(
                    color: AppColors.dangerSoft,
                    borderRadius: BorderRadius.circular(AppDimens.radiusSm),
                  ),
                  child: Row(
                    children: [
                      const Icon(
                        Icons.warning_amber_rounded,
                        color: AppColors.danger,
                        size: 18,
                      ),
                      const SizedBox(width: AppDimens.space8),
                      Expanded(
                        child: AppText(
                          'Chưa đủ giờ công (thiếu ${row.hoursShort.toStringAsFixed(1)}h). Điểm chất lượng bị nhân tỷ lệ ${row.attendanceRate}%.',
                          variant: AppTextVariant.caption,
                          fontSize: 12,
                          color: AppColors.danger,
                        ),
                      ),
                    ],
                  ),
                ),
              ],
            ],
          ),
        ),
      ],
    );
  }
}

class _AttendanceRow extends StatelessWidget {
  const _AttendanceRow({
    required this.label,
    required this.value,
    this.highlightColor,
  });

  final String label;
  final String value;
  final Color? highlightColor;

  @override
  Widget build(BuildContext context) {
    return Row(
      mainAxisAlignment: MainAxisAlignment.spaceBetween,
      children: [
        AppText(
          label,
          variant: AppTextVariant.body,
          color: AppColors.textSecondary,
        ),
        AppText(
          value,
          variant: AppTextVariant.body,
          weight: FontWeight.w700,
          color: highlightColor ?? AppColors.textPrimary,
        ),
      ],
    );
  }
}

// ---------- Reusable Task & Card Widgets ----------

class _PillarSummaryCard extends StatelessWidget {
  const _PillarSummaryCard({
    required this.icon,
    required this.title,
    required this.scoreText,
    required this.subText,
    this.penaltyText,
    required this.color,
  });

  final PhosphorIconData icon;
  final String title;
  final String scoreText;
  final String subText;
  final String? penaltyText;
  final Color color;

  @override
  Widget build(BuildContext context) {
    return AppCard(
      padding: const EdgeInsets.all(AppDimens.space16),
      radius: AppDimens.radiusMd,
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Container(
            padding: const EdgeInsets.all(10),
            decoration: BoxDecoration(
              color: color.withValues(alpha: 0.12),
              borderRadius: BorderRadius.circular(AppDimens.radiusSm),
            ),
            child: PhosphorIcon(icon, size: 22, color: color),
          ),
          const SizedBox(width: AppDimens.space12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    AppText(
                      title,
                      variant: AppTextVariant.title,
                      fontSize: 15,
                      weight: FontWeight.w700,
                    ),
                    AppText(
                      scoreText,
                      variant: AppTextVariant.title,
                      fontSize: 16,
                      weight: FontWeight.w800,
                      color: color,
                    ),
                  ],
                ),
                const SizedBox(height: 4),
                AppText(
                  subText,
                  variant: AppTextVariant.caption,
                  color: AppColors.textSecondary,
                ),
                if (penaltyText != null) ...[
                  const SizedBox(height: 2),
                  AppText(
                    penaltyText!,
                    variant: AppTextVariant.caption,
                    color: AppColors.danger,
                    weight: FontWeight.w600,
                  ),
                ],
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _KpiTaskCard extends StatelessWidget {
  const _KpiTaskCard({required this.task});

  final KpiTaskItem task;

  String _formatDate(DateTime? d) {
    if (d == null) return '—';
    return '${d.day.toString().padLeft(2, '0')}/${d.month.toString().padLeft(2, '0')}/${d.year}';
  }

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: AppDimens.space8),
      child: AppCard(
        padding: const EdgeInsets.all(AppDimens.space12),
        radius: AppDimens.radiusMd,
        onTap: () {
          Nav.toNamed(
            context,
            AppRoutes.taskDetail,
            arguments: {'taskId': task.id.toString()},
          );
        },
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      AppText(
                        task.title,
                        variant: AppTextVariant.body,
                        weight: FontWeight.w600,
                        fontSize: 13.5,
                      ),
                      if (task.projectName.isNotEmpty) ...[
                        const SizedBox(height: 2),
                        AppText(
                          task.projectName,
                          variant: AppTextVariant.caption,
                          fontSize: 11.5,
                          color: AppColors.textSecondary,
                        ),
                      ],
                    ],
                  ),
                ),
                const SizedBox(width: AppDimens.space8),
                _ProgressBadge(progress: task.progress),
              ],
            ),
            const SizedBox(height: AppDimens.space8),
            Row(
              children: [
                const Icon(
                  Icons.calendar_today_outlined,
                  size: 13,
                  color: AppColors.textSecondary,
                ),
                const SizedBox(width: 4),
                AppText(
                  'Hạn: ${_formatDate(task.dueDate)}',
                  variant: AppTextVariant.caption,
                  fontSize: 11.5,
                  color: task.isOverdue ? AppColors.danger : AppColors.textSecondary,
                ),
                if (task.loggedHours > 0) ...[
                  const SizedBox(width: AppDimens.space12),
                  const Icon(
                    Icons.access_time,
                    size: 13,
                    color: AppColors.textSecondary,
                  ),
                  const SizedBox(width: 4),
                  AppText(
                    '${task.loggedHours.toStringAsFixed(1)}h',
                    variant: AppTextVariant.caption,
                    fontSize: 11.5,
                    weight: FontWeight.w600,
                    color: AppColors.primary,
                  ),
                ],
                const Spacer(),
                if (task.isOverdue)
                  Container(
                    padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
                    decoration: BoxDecoration(
                      color: AppColors.dangerSoft,
                      borderRadius: BorderRadius.circular(AppDimens.radiusSm),
                    ),
                    child: const AppText(
                      'Quá hạn',
                      variant: AppTextVariant.caption,
                      fontSize: 10.5,
                      weight: FontWeight.w600,
                      color: AppColors.danger,
                    ),
                  )
                else if (task.isLate)
                  Container(
                    padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
                    decoration: BoxDecoration(
                      color: AppColors.warningSoft,
                      borderRadius: BorderRadius.circular(AppDimens.radiusSm),
                    ),
                    child: const AppText(
                      'Trễ hạn',
                      variant: AppTextVariant.caption,
                      fontSize: 10.5,
                      weight: FontWeight.w600,
                      color: AppColors.warningOnSoft,
                    ),
                  ),
              ],
            ),
          ],
        ),
      ),
    );
  }
}

class _ProgressBadge extends StatelessWidget {
  const _ProgressBadge({required this.progress});

  final int progress;

  @override
  Widget build(BuildContext context) {
    final isDone = progress >= 100;
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
      decoration: BoxDecoration(
        color: isDone ? AppColors.successSoft : AppColors.primarySoft,
        borderRadius: BorderRadius.circular(AppDimens.radiusSm),
      ),
      child: AppText(
        '$progress%',
        variant: AppTextVariant.caption,
        fontSize: 11,
        weight: FontWeight.w700,
        color: isDone ? AppColors.successOnSoft : AppColors.primary,
      ),
    );
  }
}

class _EmptyTabState extends StatelessWidget {
  const _EmptyTabState({required this.message});

  final String message;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.all(AppDimens.space32),
      child: Center(
        child: Column(
          children: [
            const Icon(
              Icons.inbox_outlined,
              size: 44,
              color: AppColors.textSecondary,
            ),
            const SizedBox(height: AppDimens.space8),
            AppText(
              message,
              variant: AppTextVariant.caption,
              color: AppColors.textSecondary,
              align: TextAlign.center,
            ),
          ],
        ),
      ),
    );
  }
}
