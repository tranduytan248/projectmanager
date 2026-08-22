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

class KpiScreen extends StatefulWidget {
  const KpiScreen({super.key});

  @override
  State<KpiScreen> createState() => _KpiScreenState();
}

class _KpiScreenState extends State<KpiScreen> {
  final _service = KpiService();
  late Future<KpiIndexData> _future;

  late int _year;
  late int _month;
  List<KpiUserOption> _users = [];
  int _selectedUserId = 0;
  bool _isCalculating = false;

  @override
  void initState() {
    super.initState();
    final now = DateTime.now();
    _year = now.year;
    _month = now.month;
    _loadData();
  }

  void _loadData() {
    setState(() {
      _future = _service
          .fetchIndex(
            year: _year,
            month: _month,
            userId: _selectedUserId > 0 ? _selectedUserId : null,
          )
          .then((data) {
        _users = data.users;
        return data;
      });
    });
  }

  void _changeMonth(int delta) {
    var newMonth = _month + delta;
    var newYear = _year;

    if (newMonth > 12) {
      newMonth = 1;
      newYear += 1;
    } else if (newMonth < 1) {
      newMonth = 12;
      newYear -= 1;
    }

    setState(() {
      _month = newMonth;
      _year = newYear;
    });
    _loadData();
  }

  void _goToCurrentMonth() {
    final now = DateTime.now();
    if (_year == now.year && _month == now.month) return;
    setState(() {
      _year = now.year;
      _month = now.month;
    });
    _loadData();
  }

  Future<void> _handleCalculateAll() async {
    if (_isCalculating) return;

    final confirm = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(AppDimens.radiusLg),
        ),
        title: const AppText(
          'Chốt KPI tháng',
          variant: AppTextVariant.title,
          fontSize: 18,
          weight: FontWeight.w700,
        ),
        content: AppText(
          'Chốt KPI tháng $_month/$_year cho toàn bộ nhân sự? Điểm đã lưu của tháng này sẽ được tính lại theo công thức đang áp dụng.',
          variant: AppTextVariant.body,
          color: AppColors.textSecondary,
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(ctx).pop(false),
            child: const AppText('Hủy', color: AppColors.textSecondary),
          ),
          AppButton(
            label: 'Tính & Chốt',
            onPressed: () => Navigator.of(ctx).pop(true),
          ),
        ],
      ),
    );

    if (confirm != true || !mounted) return;

    setState(() => _isCalculating = true);

    try {
      final msg = await _service.calculateAll(
        year: _year,
        month: _month,
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
      if (mounted) setState(() => _isCalculating = false);
    }
  }

  void _openMonthPicker() {
    showModalBottomSheet(
      context: context,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(AppDimens.radiusLg)),
      ),
      builder: (ctx) {
        return _MonthPickerSheet(
          initialYear: _year,
          initialMonth: _month,
          onSelected: (y, m) {
            Navigator.of(ctx).pop();
            setState(() {
              _year = y;
              _month = m;
            });
            _loadData();
          },
        );
      },
    );
  }

  void _openUserFilterSheet(List<KpiUserOption> users) {
    showModalBottomSheet(
      context: context,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(AppDimens.radiusLg)),
      ),
      builder: (ctx) {
        return SafeArea(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Padding(
                padding: const EdgeInsets.all(AppDimens.space16),
                child: Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    const AppText(
                      'Lọc theo nhân sự',
                      variant: AppTextVariant.title,
                      fontSize: 17,
                      weight: FontWeight.w700,
                    ),
                    IconButton(
                      icon: const Icon(Icons.close),
                      onPressed: () => Navigator.of(ctx).pop(),
                    ),
                  ],
                ),
              ),
              const Divider(height: 1, color: AppColors.border),
              ListTile(
                title: const AppText('— Tất cả nhân sự —'),
                trailing: _selectedUserId == 0
                    ? const Icon(Icons.check, color: AppColors.primary)
                    : null,
                onTap: () {
                  Navigator.of(ctx).pop();
                  setState(() => _selectedUserId = 0);
                  _loadData();
                },
              ),
              Expanded(
                child: ListView.builder(
                  shrinkWrap: true,
                  itemCount: users.length,
                  itemBuilder: (context, index) {
                    final u = users[index];
                    final isSelected = _selectedUserId == u.userId;
                    return ListTile(
                      title: AppText(
                        u.fullName,
                        weight: isSelected ? FontWeight.w700 : FontWeight.w400,
                        color: isSelected ? AppColors.primary : AppColors.textPrimary,
                      ),
                      trailing: isSelected
                          ? const Icon(Icons.check, color: AppColors.primary)
                          : null,
                      onTap: () {
                        Navigator.of(ctx).pop();
                        setState(() => _selectedUserId = u.userId);
                        _loadData();
                      },
                    );
                  },
                ),
              ),
            ],
          ),
        );
      },
    );
  }

  @override
  Widget build(BuildContext context) {
    final isNow = _year == DateTime.now().year && _month == DateTime.now().month;

    return AppScaffold(
      appBar: AppAppBar(
        title: 'KPI theo tháng',
        actions: [
          AppIconButton(
            icon: PhosphorIconsRegular.arrowClockwise,
            tooltip: 'Làm mới',
            onPressed: _loadData,
          ),
        ],
      ),
      body: Column(
        children: [
          // Month Selector & User Filter Bar
          _HeaderToolbar(
            year: _year,
            month: _month,
            isNow: isNow,
            selectedUserId: _selectedUserId,
            onChangeMonth: _changeMonth,
            onOpenMonthPicker: _openMonthPicker,
            onGoToCurrentMonth: _goToCurrentMonth,
            onOpenUserFilter: () => _openUserFilterSheet(_users),
          ),
          Expanded(
            child: FutureBuilder<KpiIndexData>(
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

                return RefreshIndicator(
                  onRefresh: () async => _loadData(),
                  color: AppColors.primary,
                  child: ListView(
                    padding: const EdgeInsets.fromLTRB(
                      AppDimens.space16,
                      AppDimens.space12,
                      AppDimens.space16,
                      AppDimens.space24,
                    ),
                    children: [
                      // Summary Stats Grid
                      _SummaryStatsSection(summary: data.summary),
                      const SizedBox(height: AppDimens.space12),

                      // Formula Info Note
                      _StandardInfoNote(
                        standardDays: data.standardDays,
                        scaleMax: data.scaleMax,
                        canGenerate: data.canGenerate,
                        isCalculating: _isCalculating,
                        onCalculateAll: _handleCalculateAll,
                        month: data.month,
                        year: data.year,
                      ),
                      const SizedBox(height: AppDimens.space16),

                      // Member Rows List
                      Row(
                        mainAxisAlignment: MainAxisAlignment.spaceBetween,
                        children: [
                          AppText(
                            'Bảng xếp hạng (${data.rows.length})',
                            variant: AppTextVariant.title,
                            fontSize: 16,
                            weight: FontWeight.w700,
                          ),
                          if (_selectedUserId > 0)
                            TextButton.icon(
                              onPressed: () {
                                setState(() => _selectedUserId = 0);
                                _loadData();
                              },
                              icon: const Icon(Icons.clear, size: 16),
                              label: const AppText(
                                'Xóa lọc',
                                variant: AppTextVariant.caption,
                                color: AppColors.primary,
                              ),
                            ),
                        ],
                      ),
                      const SizedBox(height: AppDimens.space8),

                      if (data.rows.isEmpty)
                        const Padding(
                          padding: EdgeInsets.all(AppDimens.space32),
                          child: Center(
                            child: Column(
                              children: [
                                Icon(
                                  Icons.inbox_outlined,
                                  size: 44,
                                  color: AppColors.textSecondary,
                                ),
                                SizedBox(height: AppDimens.space8),
                                AppText(
                                  'Không có nhân sự nào khớp bộ lọc trong tháng này.',
                                  variant: AppTextVariant.caption,
                                  color: AppColors.textSecondary,
                                ),
                              ],
                            ),
                          ),
                        )
                      else
                        for (final row in data.rows)
                          _MemberKpiCard(
                            row: row,
                            onTap: () {
                              Nav.toNamed(
                                context,
                                AppRoutes.kpiDetail,
                                arguments: {
                                  'userId': row.userId,
                                  'userName': row.fullName,
                                  'year': row.year,
                                  'month': row.month,
                                },
                              );
                            },
                          ),
                    ],
                  ),
                );
              },
            ),
          ),
        ],
      ),
    );
  }
}

// ---------- Header Toolbar ----------

class _HeaderToolbar extends StatelessWidget {
  const _HeaderToolbar({
    required this.year,
    required this.month,
    required this.isNow,
    required this.selectedUserId,
    required this.onChangeMonth,
    required this.onOpenMonthPicker,
    required this.onGoToCurrentMonth,
    required this.onOpenUserFilter,
  });

  final int year;
  final int month;
  final bool isNow;
  final int selectedUserId;
  final ValueChanged<int> onChangeMonth;
  final VoidCallback onOpenMonthPicker;
  final VoidCallback onGoToCurrentMonth;
  final VoidCallback onOpenUserFilter;

  @override
  Widget build(BuildContext context) {
    return Container(
      color: AppColors.surface,
      padding: const EdgeInsets.symmetric(
        horizontal: AppDimens.space16,
        vertical: AppDimens.space8,
      ),
      child: Row(
        children: [
          // Month navigator
          IconButton(
            icon: const Icon(Icons.chevron_left),
            iconSize: 22,
            padding: EdgeInsets.zero,
            constraints: const BoxConstraints(minWidth: 36, minHeight: 36),
            onPressed: () => onChangeMonth(-1),
          ),
          InkWell(
            onTap: onOpenMonthPicker,
            borderRadius: BorderRadius.circular(AppDimens.radiusSm),
            child: Padding(
              padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 6),
              child: Row(
                children: [
                  AppText(
                    'Tháng $month/$year',
                    variant: AppTextVariant.body,
                    weight: FontWeight.w700,
                    fontSize: 14.5,
                  ),
                  const SizedBox(width: 4),
                  const Icon(Icons.arrow_drop_down, size: 18),
                ],
              ),
            ),
          ),
          IconButton(
            icon: const Icon(Icons.chevron_right),
            iconSize: 22,
            padding: EdgeInsets.zero,
            constraints: const BoxConstraints(minWidth: 36, minHeight: 36),
            onPressed: () => onChangeMonth(1),
          ),
          if (!isNow) ...[
            const SizedBox(width: 4),
            InkWell(
              onTap: onGoToCurrentMonth,
              borderRadius: BorderRadius.circular(AppDimens.radiusPill),
              child: Container(
                padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                decoration: BoxDecoration(
                  color: AppColors.primarySoft,
                  borderRadius: BorderRadius.circular(AppDimens.radiusPill),
                ),
                child: const AppText(
                  'Hiện tại',
                  variant: AppTextVariant.caption,
                  fontSize: 11,
                  weight: FontWeight.w700,
                  color: AppColors.primary,
                ),
              ),
            ),
          ],
          const Spacer(),
          // Filter User Button
          InkWell(
            onTap: onOpenUserFilter,
            borderRadius: BorderRadius.circular(AppDimens.radiusSm),
            child: Container(
              padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
              decoration: BoxDecoration(
                color: selectedUserId > 0 ? AppColors.primarySoft : AppColors.background,
                borderRadius: BorderRadius.circular(AppDimens.radiusSm),
                border: Border.all(
                  color: selectedUserId > 0 ? AppColors.primary : AppColors.border,
                ),
              ),
              child: Row(
                children: [
                  PhosphorIcon(
                    PhosphorIconsRegular.user,
                    size: 14,
                    color: selectedUserId > 0 ? AppColors.primary : AppColors.textSecondary,
                  ),
                  const SizedBox(width: 4),
                  AppText(
                    selectedUserId > 0 ? 'Đang lọc' : 'Nhân sự',
                    variant: AppTextVariant.caption,
                    fontSize: 12,
                    weight: FontWeight.w600,
                    color: selectedUserId > 0 ? AppColors.primary : AppColors.textSecondary,
                  ),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }
}

// ---------- Summary Stats Section ----------

class _SummaryStatsSection extends StatelessWidget {
  const _SummaryStatsSection({required this.summary});

  final KpiSummaryData summary;

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        Row(
          children: [
            Expanded(
              child: _StatCard(
                icon: PhosphorIconsRegular.chartLineUp,
                title: 'Điểm TB tổ',
                value: '${summary.averagePoint.toStringAsFixed(1)}đ',
                subText: '${summary.total} nhân sự',
                color: AppColors.primary,
              ),
            ),
            const SizedBox(width: AppDimens.space12),
            Expanded(
              child: _StatCard(
                icon: PhosphorIconsRegular.checkCircle,
                title: 'Đạt trở lên',
                value: '${summary.passCount}',
                subText: '${summary.passPercent}% tổng số',
                color: AppColors.success,
              ),
            ),
          ],
        ),
        const SizedBox(height: AppDimens.space12),
        Row(
          children: [
            Expanded(
              child: _StatCard(
                icon: PhosphorIconsRegular.warningCircle,
                title: 'Chưa đạt',
                value: '${summary.failCount}',
                subText: summary.failCount > 0 ? 'Cần cải thiện' : 'Tốt',
                color: summary.failCount > 0 ? AppColors.danger : AppColors.textSecondary,
              ),
            ),
            const SizedBox(width: AppDimens.space12),
            Expanded(
              child: _StatCard(
                icon: PhosphorIconsRegular.clockAfternoon,
                title: 'Thiếu giờ công',
                value: '${summary.shortHoursCount}',
                subText: summary.shortHoursCount > 0 ? 'Bị hạ điểm' : 'Đủ giờ',
                color: summary.shortHoursCount > 0 ? AppColors.warning : AppColors.textSecondary,
              ),
            ),
          ],
        ),
        if (summary.topName.isNotEmpty) ...[
          const SizedBox(height: AppDimens.space12),
          AppCard(
            padding: const EdgeInsets.symmetric(
              horizontal: AppDimens.space16,
              vertical: AppDimens.space12,
            ),
            radius: AppDimens.radiusMd,
            child: Row(
              children: [
                Container(
                  padding: const EdgeInsets.all(8),
                  decoration: BoxDecoration(
                    color: const Color(0xFFFEF3C7),
                    borderRadius: BorderRadius.circular(AppDimens.radiusSm),
                  ),
                  child: const Icon(
                    Icons.emoji_events,
                    size: 20,
                    color: Color(0xFFD97706),
                  ),
                ),
                const SizedBox(width: AppDimens.space12),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      const AppText(
                        'Dẫn đầu tháng',
                        variant: AppTextVariant.caption,
                        fontSize: 11.5,
                        color: AppColors.textSecondary,
                      ),
                      AppText(
                        summary.topName,
                        variant: AppTextVariant.body,
                        fontSize: 14,
                        weight: FontWeight.w700,
                      ),
                    ],
                  ),
                ),
                AppText(
                  '${summary.topPoint.toStringAsFixed(0)} đ',
                  variant: AppTextVariant.title,
                  fontSize: 18,
                  weight: FontWeight.w800,
                  color: const Color(0xFFD97706),
                ),
              ],
            ),
          ),
        ],
      ],
    );
  }
}

class _StatCard extends StatelessWidget {
  const _StatCard({
    required this.icon,
    required this.title,
    required this.value,
    required this.subText,
    required this.color,
  });

  final PhosphorIconData icon;
  final String title;
  final String value;
  final String subText;
  final Color color;

  @override
  Widget build(BuildContext context) {
    return AppCard(
      padding: const EdgeInsets.all(AppDimens.space12),
      radius: AppDimens.radiusMd,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              PhosphorIcon(icon, size: 18, color: color),
              const SizedBox(width: 6),
              Expanded(
                child: AppText(
                  title,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  variant: AppTextVariant.caption,
                  fontSize: 12,
                  color: AppColors.textSecondary,
                ),
              ),
            ],
          ),
          const SizedBox(height: 6),
          AppText(
            value,
            variant: AppTextVariant.title,
            fontSize: 20,
            weight: FontWeight.w800,
            color: color,
          ),
          const SizedBox(height: 2),
          AppText(
            subText,
            maxLines: 1,
            overflow: TextOverflow.ellipsis,
            variant: AppTextVariant.caption,
            fontSize: 11,
            color: AppColors.textSecondary,
          ),
        ],
      ),
    );
  }
}

// ---------- Standard Info Note & Actions ----------

class _StandardInfoNote extends StatelessWidget {
  const _StandardInfoNote({
    required this.standardDays,
    required this.scaleMax,
    required this.canGenerate,
    required this.isCalculating,
    required this.onCalculateAll,
    required this.month,
    required this.year,
  });

  final int standardDays;
  final double scaleMax;
  final bool canGenerate;
  final bool isCalculating;
  final VoidCallback onCalculateAll;
  final int month;
  final int year;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(AppDimens.space12),
      decoration: BoxDecoration(
        color: AppColors.surface,
        borderRadius: BorderRadius.circular(AppDimens.radiusMd),
        border: Border.all(color: AppColors.border),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              const Icon(
                Icons.calendar_month_outlined,
                size: 16,
                color: AppColors.primary,
              ),
              const SizedBox(width: 6),
              Expanded(
                child: AppText(
                  'Chuẩn tháng: $standardDays ngày (8.0h/ngày) · Mốc chuẩn: ${scaleMax.toStringAsFixed(0)}đ',
                  variant: AppTextVariant.caption,
                  fontSize: 11.5,
                  color: AppColors.textSecondary,
                ),
              ),
            ],
          ),
          if (canGenerate) ...[
            const SizedBox(height: AppDimens.space8),
            const Divider(height: 1, color: AppColors.border),
            const SizedBox(height: AppDimens.space8),
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                const Expanded(
                  child: AppText(
                    'Chốt tính KPI cho toàn bộ nhân sự',
                    variant: AppTextVariant.caption,
                    fontSize: 12,
                    color: AppColors.textSecondary,
                  ),
                ),
                AppButton(
                  label: isCalculating ? 'Đang tính...' : 'Tính & Chốt T$month',
                  onPressed: isCalculating ? null : onCalculateAll,
                ),
              ],
            ),
          ],
        ],
      ),
    );
  }
}

// ---------- Member KPI Card ----------

class _MemberKpiCard extends StatelessWidget {
  const _MemberKpiCard({required this.row, required this.onTap});

  final KpiMemberRow row;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: AppDimens.space12),
      child: AppCard(
        onTap: onTap,
        padding: const EdgeInsets.all(AppDimens.space16),
        radius: AppDimens.radiusMd,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Top Row: Rank medal + Name + Save state + Final Score
            Row(
              crossAxisAlignment: CrossAxisAlignment.center,
              children: [
                _RankMedal(rankIndex: row.rankIndex),
                const SizedBox(width: AppDimens.space12),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Row(
                        children: [
                          Flexible(
                            child: AppText(
                              row.fullName,
                              variant: AppTextVariant.title,
                              fontSize: 15.5,
                              weight: FontWeight.w700,
                              maxLines: 1,
                              overflow: TextOverflow.ellipsis,
                            ),
                          ),
                          const SizedBox(width: 6),
                          _StatusTag(isSaved: row.isSaved),
                        ],
                      ),
                      const SizedBox(height: 2),
                      Container(
                        padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
                        decoration: BoxDecoration(
                          color: row.rankBgColor,
                          borderRadius: BorderRadius.circular(AppDimens.radiusSm),
                        ),
                        child: AppText(
                          row.rank,
                          variant: AppTextVariant.caption,
                          fontSize: 11,
                          weight: FontWeight.w700,
                          color: row.rankColor,
                        ),
                      ),
                    ],
                  ),
                ),
                const SizedBox(width: AppDimens.space8),
                Column(
                  crossAxisAlignment: CrossAxisAlignment.end,
                  children: [
                    Row(
                      mainAxisSize: MainAxisSize.min,
                      crossAxisAlignment: CrossAxisAlignment.baseline,
                      textBaseline: TextBaseline.alphabetic,
                      children: [
                        AppText(
                          row.finalPoint.toStringAsFixed(0),
                          variant: AppTextVariant.title,
                          fontSize: 22,
                          weight: FontWeight.w800,
                          color: row.rankColor,
                        ),
                        const SizedBox(width: 2),
                        AppText(
                          'đ',
                          variant: AppTextVariant.caption,
                          fontSize: 12,
                          weight: FontWeight.w600,
                          color: row.rankColor,
                        ),
                      ],
                    ),
                    const Icon(
                      Icons.chevron_right,
                      size: 16,
                      color: AppColors.textSecondary,
                    ),
                  ],
                ),
              ],
            ),
            const SizedBox(height: AppDimens.space12),
            const Divider(height: 1, color: AppColors.border),
            const SizedBox(height: AppDimens.space12),

            // Breakdown Points Chips Row
            Wrap(
              spacing: 6,
              runSpacing: 4,
              children: [
                _BreakdownPill(
                  label: 'Hỗ trợ',
                  value: '${row.supportPoint.toStringAsFixed(1)}đ',
                  sub: '(${row.supportHours.toStringAsFixed(0)}/${row.supportCapHours.toStringAsFixed(0)}h)',
                  color: AppColors.primary,
                ),
                _BreakdownPill(
                  label: 'Triển khai',
                  value: '${row.executePoint.toStringAsFixed(1)}đ',
                  color: const Color(0xFF0284C7),
                ),
                if (row.assignedPoint > 0)
                  _BreakdownPill(
                    label: 'Việc riêng',
                    value: '+${row.assignedPoint.toStringAsFixed(1)}đ',
                    color: AppColors.success,
                  ),
                if (row.latePenalty > 0)
                  _BreakdownPill(
                    label: 'Phạt trễ',
                    value: '-${row.latePenalty.toStringAsFixed(1)}đ',
                    color: AppColors.danger,
                  ),
              ],
            ),
            const SizedBox(height: AppDimens.space8),

            // Attendance & Working Hours Progress
            Row(
              children: [
                Icon(
                  Icons.timer_outlined,
                  size: 13,
                  color: row.isShortHours ? AppColors.danger : AppColors.textSecondary,
                ),
                const SizedBox(width: 4),
                Expanded(
                  child: AppText(
                    'Giờ làm: ${row.workedHours.toStringAsFixed(1)}/${row.requiredHours.toStringAsFixed(1)}h (${row.attendanceRate}%)',
                    variant: AppTextVariant.caption,
                    fontSize: 11.5,
                    color: row.isShortHours ? AppColors.danger : AppColors.textSecondary,
                    weight: row.isShortHours ? FontWeight.w600 : FontWeight.w400,
                  ),
                ),
                if (row.isShortHours)
                  AppText(
                    'Thiếu ${row.hoursShort.toStringAsFixed(1)}h',
                    variant: AppTextVariant.caption,
                    fontSize: 11,
                    weight: FontWeight.w700,
                    color: AppColors.danger,
                  ),
              ],
            ),
          ],
        ),
      ),
    );
  }
}

class _RankMedal extends StatelessWidget {
  const _RankMedal({required this.rankIndex});

  final int rankIndex;

  @override
  Widget build(BuildContext context) {
    Color bg;
    Color text;
    if (rankIndex == 1) {
      bg = const Color(0xFFFEF3C7);
      text = const Color(0xFFD97706);
    } else if (rankIndex == 2) {
      bg = const Color(0xFFF1F5F9);
      text = const Color(0xFF475569);
    } else if (rankIndex == 3) {
      bg = const Color(0xFFFFEDD5);
      text = const Color(0xFFC2410C);
    } else {
      bg = AppColors.background;
      text = AppColors.textSecondary;
    }

    return Container(
      width: 32,
      height: 32,
      alignment: Alignment.center,
      decoration: BoxDecoration(
        color: bg,
        borderRadius: BorderRadius.circular(AppDimens.radiusSm),
      ),
      child: AppText(
        '#$rankIndex',
        variant: AppTextVariant.caption,
        fontSize: 12,
        weight: FontWeight.w800,
        color: text,
      ),
    );
  }
}

class _StatusTag extends StatelessWidget {
  const _StatusTag({required this.isSaved});

  final bool isSaved;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 5, vertical: 1.5),
      decoration: BoxDecoration(
        color: isSaved ? AppColors.successSoft : AppColors.warningSoft,
        borderRadius: BorderRadius.circular(AppDimens.radiusSm),
      ),
      child: AppText(
        isSaved ? 'chốt' : 'tạm tính',
        variant: AppTextVariant.caption,
        fontSize: 10,
        weight: FontWeight.w600,
        color: isSaved ? AppColors.successOnSoft : AppColors.warningOnSoft,
      ),
    );
  }
}

class _BreakdownPill extends StatelessWidget {
  const _BreakdownPill({
    required this.label,
    required this.value,
    this.sub = '',
    required this.color,
  });

  final String label;
  final String value;
  final String sub;
  final Color color;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 7, vertical: 3),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.08),
        borderRadius: BorderRadius.circular(AppDimens.radiusSm),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          AppText(
            '$label: ',
            variant: AppTextVariant.caption,
            fontSize: 11,
            color: AppColors.textSecondary,
          ),
          AppText(
            value,
            variant: AppTextVariant.caption,
            fontSize: 11,
            weight: FontWeight.w700,
            color: color,
          ),
          if (sub.isNotEmpty) ...[
            const SizedBox(width: 2),
            AppText(
              sub,
              variant: AppTextVariant.caption,
              fontSize: 10,
              color: AppColors.textSecondary,
            ),
          ],
        ],
      ),
    );
  }
}

// ---------- Month Picker Sheet ----------

class _MonthPickerSheet extends StatefulWidget {
  const _MonthPickerSheet({
    required this.initialYear,
    required this.initialMonth,
    required this.onSelected,
  });

  final int initialYear;
  final int initialMonth;
  final void Function(int year, int month) onSelected;

  @override
  State<_MonthPickerSheet> createState() => _MonthPickerSheetState();
}

class _MonthPickerSheetState extends State<_MonthPickerSheet> {
  late int _selectedYear;
  late int _selectedMonth;

  @override
  void initState() {
    super.initState();
    _selectedYear = widget.initialYear;
    _selectedMonth = widget.initialMonth;
  }

  @override
  Widget build(BuildContext context) {
    return SafeArea(
      child: Padding(
        padding: const EdgeInsets.all(AppDimens.space16),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                IconButton(
                  icon: const Icon(Icons.chevron_left),
                  onPressed: () => setState(() => _selectedYear -= 1),
                ),
                AppText(
                  'Năm $_selectedYear',
                  variant: AppTextVariant.title,
                  fontSize: 16,
                  weight: FontWeight.w700,
                ),
                IconButton(
                  icon: const Icon(Icons.chevron_right),
                  onPressed: () => setState(() => _selectedYear += 1),
                ),
              ],
            ),
            const SizedBox(height: AppDimens.space12),
            GridView.builder(
              shrinkWrap: true,
              physics: const NeverScrollableScrollPhysics(),
              gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
                crossAxisCount: 4,
                childAspectRatio: 1.8,
                crossAxisSpacing: 8,
                mainAxisSpacing: 8,
              ),
              itemCount: 12,
              itemBuilder: (context, index) {
                final m = index + 1;
                final isSelected =
                    _selectedMonth == m && _selectedYear == widget.initialYear;
                return InkWell(
                  onTap: () {
                    widget.onSelected(_selectedYear, m);
                  },
                  borderRadius: BorderRadius.circular(AppDimens.radiusSm),
                  child: Container(
                    alignment: Alignment.center,
                    decoration: BoxDecoration(
                      color: isSelected ? AppColors.primary : AppColors.surface,
                      borderRadius: BorderRadius.circular(AppDimens.radiusSm),
                      border: Border.all(
                        color: isSelected ? AppColors.primary : AppColors.border,
                      ),
                    ),
                    child: AppText(
                      'Th $m',
                      variant: AppTextVariant.body,
                      fontSize: 13,
                      weight: isSelected ? FontWeight.w700 : FontWeight.w500,
                      color: isSelected ? AppColors.surface : AppColors.textPrimary,
                    ),
                  ),
                );
              },
            ),
          ],
        ),
      ),
    );
  }
}
