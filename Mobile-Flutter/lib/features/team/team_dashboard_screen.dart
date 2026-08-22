import 'package:flutter/material.dart';

import '../../core/theme/app_colors.dart';
import '../../core/theme/app_dimens.dart';
import '../../core/theme/app_text_styles.dart';
import '../../core/widgets/app_app_bar.dart';
import '../../core/widgets/app_card.dart';
import '../../core/widgets/app_error_state.dart';
import '../../core/widgets/app_icon_button.dart';
import '../../core/widgets/app_loading.dart';
import '../../core/widgets/app_scaffold.dart';
import '../../core/widgets/app_text.dart';
import '../app_routes.dart';
import 'team_dashboard_models.dart';
import 'team_dashboard_service.dart';
import 'team_member_tasks_sheet.dart';

/// Màn hình Bảng điều khiển Tổ — dành cho Quản lý Tổ và Lãnh đạo.
/// Gồm 3 Tab: Việc hôm nay, KPI tháng, Phân bổ dự án.
class TeamDashboardScreen extends StatefulWidget {
  const TeamDashboardScreen({super.key});

  @override
  State<TeamDashboardScreen> createState() => _TeamDashboardScreenState();
}

class _TeamDashboardScreenState extends State<TeamDashboardScreen>
    with SingleTickerProviderStateMixin {
  final _service = TeamDashboardService();

  late final TabController _tabController;
  late int _selectedYear;
  late int _selectedMonth;

  TeamDashboardData? _data;
  bool _isLoading = true;
  String? _errorMessage;

  @override
  void initState() {
    super.initState();
    final now = DateTime.now();
    _selectedYear = now.year;
    _selectedMonth = now.month;
    _tabController = TabController(length: 3, vsync: this);
    _loadData();
  }

  @override
  void dispose() {
    _tabController.dispose();
    super.dispose();
  }

  Future<void> _loadData() async {
    setState(() {
      _isLoading = true;
      _errorMessage = null;
    });

    try {
      final res = await _service.fetchDashboard(
        year: _selectedYear,
        month: _selectedMonth,
      );
      if (mounted) {
        setState(() {
          _data = res;
          _isLoading = false;
        });
      }
    } catch (e) {
      if (mounted) {
        setState(() {
          _errorMessage = 'Không thể tải bảng điều khiển Tổ: ${e.toString()}';
          _isLoading = false;
        });
      }
    }
  }

  void _previousMonth() {
    setState(() {
      if (_selectedMonth == 1) {
        _selectedMonth = 12;
        _selectedYear--;
      } else {
        _selectedMonth--;
      }
    });
    _loadData();
  }

  void _nextMonth() {
    setState(() {
      if (_selectedMonth == 12) {
        _selectedMonth = 1;
        _selectedYear++;
      } else {
        _selectedMonth++;
      }
    });
    _loadData();
  }

  void _resetToCurrentMonth() {
    final now = DateTime.now();
    if (_selectedYear != now.year || _selectedMonth != now.month) {
      setState(() {
        _selectedYear = now.year;
        _selectedMonth = now.month;
      });
      _loadData();
    }
  }

  void _showMonthPickerSheet() {
    showModalBottomSheet<void>(
      context: context,
      backgroundColor: Colors.transparent,
      useSafeArea: true,
      builder: (ctx) {
        int tempYear = _selectedYear;
        int tempMonth = _selectedMonth;

        return StatefulBuilder(
          builder: (context, setModalState) {
            return Container(
              decoration: const BoxDecoration(
                color: AppColors.surface,
                borderRadius: BorderRadius.vertical(
                  top: Radius.circular(AppDimens.radiusLg),
                ),
              ),
              padding: const EdgeInsets.all(AppDimens.space16),
              child: Column(
                mainAxisSize: MainAxisSize.min,
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
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
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      const AppText(
                        'Chọn tháng & năm',
                        variant: AppTextVariant.title,
                        fontSize: 18,
                        weight: FontWeight.w700,
                      ),
                      AppIconButton(
                        icon: Icons.close,
                        tooltip: 'Đóng',
                        onPressed: () => Navigator.of(ctx).pop(),
                      ),
                    ],
                  ),
                  const SizedBox(height: AppDimens.space16),

                  // Chọn Năm
                  Row(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      AppIconButton(
                        icon: Icons.chevron_left,
                        tooltip: 'Năm trước',
                        onPressed: () => setModalState(() => tempYear--),
                      ),
                      Padding(
                        padding: const EdgeInsets.symmetric(horizontal: AppDimens.space16),
                        child: AppText(
                          '$tempYear',
                          variant: AppTextVariant.title,
                          fontSize: 18,
                          weight: FontWeight.w700,
                          color: AppColors.primary,
                        ),
                      ),
                      AppIconButton(
                        icon: Icons.chevron_right,
                        tooltip: 'Năm sau',
                        onPressed: () => setModalState(() => tempYear++),
                      ),
                    ],
                  ),
                  const SizedBox(height: AppDimens.space16),

                  // Lưới 12 tháng
                  GridView.builder(
                    shrinkWrap: true,
                    physics: const NeverScrollableScrollPhysics(),
                    gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
                      crossAxisCount: 4,
                      childAspectRatio: 2.2,
                      crossAxisSpacing: AppDimens.space8,
                      mainAxisSpacing: AppDimens.space8,
                    ),
                    itemCount: 12,
                    itemBuilder: (context, i) {
                      final m = i + 1;
                      final isSelected = m == tempMonth;
                      return InkWell(
                        onTap: () {
                          Navigator.of(ctx).pop();
                          setState(() {
                            _selectedYear = tempYear;
                            _selectedMonth = m;
                          });
                          _loadData();
                        },
                        borderRadius: BorderRadius.circular(AppDimens.radiusMd),
                        child: Container(
                          decoration: BoxDecoration(
                            color: isSelected ? AppColors.primary : AppColors.primarySoft,
                            borderRadius: BorderRadius.circular(AppDimens.radiusMd),
                          ),
                          alignment: Alignment.center,
                          child: AppText(
                            'Thg $m',
                            variant: AppTextVariant.body,
                            fontSize: 13.5,
                            weight: isSelected ? FontWeight.w700 : FontWeight.w500,
                            color: isSelected ? AppColors.textOnPrimary : AppColors.primaryDark,
                          ),
                        ),
                      );
                    },
                  ),
                  const SizedBox(height: AppDimens.space16),
                ],
              ),
            );
          },
        );
      },
    );
  }

  @override
  Widget build(BuildContext context) {
    return AppScaffold(
      appBar: AppAppBar(
        title: 'Bảng điều khiển Tổ',
        actions: [
          AppIconButton(
            icon: Icons.refresh,
            tooltip: 'Làm mới',
            onPressed: _loadData,
          ),
        ],
      ),
      body: SafeArea(
        child: Column(
          children: [
            // Month selector bar
            _buildMonthPickerBar(),

            // Top Summary Cards
            if (_data != null && !_isLoading) _buildSummaryBanner(_data!),

            // Tab Bar
            Container(
              decoration: const BoxDecoration(
                color: AppColors.surface,
                border: Border(
                  bottom: BorderSide(color: AppColors.border, width: 1),
                ),
              ),
              child: TabBar(
                controller: _tabController,
                indicatorColor: AppColors.primary,
                indicatorWeight: 3,
                labelColor: AppColors.primary,
                unselectedLabelColor: AppColors.textSecondary,
                labelStyle: const TextStyle(
                  fontSize: 13.5,
                  fontWeight: FontWeight.w700,
                ),
                unselectedLabelStyle: const TextStyle(
                  fontSize: 13.5,
                  fontWeight: FontWeight.w500,
                ),
                tabs: const [
                  Tab(
                    icon: Icon(Icons.calendar_today_outlined, size: 18),
                    text: 'Hôm nay',
                  ),
                  Tab(
                    icon: Icon(Icons.emoji_events_outlined, size: 18),
                    text: 'KPI tháng',
                  ),
                  Tab(
                    icon: Icon(Icons.pie_chart_outline, size: 18),
                    text: 'Phân bổ',
                  ),
                ],
              ),
            ),

            // Tab Views Body
            Expanded(
              child: _buildTabBody(),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildMonthPickerBar() {
    final now = DateTime.now();
    final isCurrentMonth = _selectedYear == now.year && _selectedMonth == now.month;

    return Container(
      padding: const EdgeInsets.symmetric(
        horizontal: AppDimens.space16,
        vertical: AppDimens.space8,
      ),
      decoration: const BoxDecoration(
        color: AppColors.surface,
        border: Border(
          bottom: BorderSide(color: AppColors.border, width: 1),
        ),
      ),
      child: Row(
        children: [
          // Nút tháng trước
          AppIconButton(
            icon: Icons.chevron_left,
            tooltip: 'Tháng trước',
            onPressed: _previousMonth,
          ),

          // Nút chọn tháng
          Expanded(
            child: InkWell(
              onTap: _showMonthPickerSheet,
              borderRadius: BorderRadius.circular(AppDimens.radiusMd),
              child: Padding(
                padding: const EdgeInsets.symmetric(
                  vertical: AppDimens.space8,
                  horizontal: AppDimens.space8,
                ),
                child: Row(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    const Icon(
                      Icons.calendar_month_outlined,
                      size: 18,
                      color: AppColors.primary,
                    ),
                    const SizedBox(width: AppDimens.space8),
                    AppText(
                      'Tháng $_selectedMonth/$_selectedYear',
                      variant: AppTextVariant.body,
                      fontSize: 15.5,
                      weight: FontWeight.w700,
                      color: AppColors.textPrimary,
                    ),
                    const SizedBox(width: AppDimens.space4),
                    const Icon(
                      Icons.arrow_drop_down,
                      size: 18,
                      color: AppColors.textSecondary,
                    ),
                  ],
                ),
              ),
            ),
          ),

          // Nút tháng sau
          AppIconButton(
            icon: Icons.chevron_right,
            tooltip: 'Tháng sau',
            onPressed: _nextMonth,
          ),

          // Nút về tháng này nếu đang ở tháng khác
          if (!isCurrentMonth) ...[
            const SizedBox(width: AppDimens.space4),
            InkWell(
              onTap: _resetToCurrentMonth,
              borderRadius: BorderRadius.circular(AppDimens.radiusPill),
              child: Container(
                padding: const EdgeInsets.symmetric(
                  horizontal: AppDimens.space8,
                  vertical: AppDimens.space4,
                ),
                decoration: BoxDecoration(
                  color: AppColors.primarySoft,
                  borderRadius: BorderRadius.circular(AppDimens.radiusPill),
                ),
                child: const AppText(
                  'Hiện tại',
                  variant: AppTextVariant.caption,
                  fontSize: 11.5,
                  weight: FontWeight.w600,
                  color: AppColors.primaryDark,
                ),
              ),
            ),
          ],
        ],
      ),
    );
  }

  Widget _buildSummaryBanner(TeamDashboardData data) {
    return Container(
      padding: const EdgeInsets.fromLTRB(
        AppDimens.space16,
        AppDimens.space12,
        AppDimens.space16,
        AppDimens.space12,
      ),
      color: AppColors.surface,
      child: Row(
        children: [
          // Thẻ 1: Tổng thành viên
          Expanded(
            child: _buildSummaryCard(
              icon: Icons.groups_outlined,
              label: 'Thành viên',
              value: '${data.totalMembers}',
              color: AppColors.primary,
              bgColor: AppColors.primarySoft,
            ),
          ),
          const SizedBox(width: AppDimens.space8),

          // Thẻ 2: Chưa có việc hôm nay
          Expanded(
            child: _buildSummaryCard(
              icon: Icons.person_off_outlined,
              label: 'Chưa có việc',
              value: '${data.idleCount}',
              color: data.idleCount > 0 ? AppColors.warningOnSoft : AppColors.success,
              bgColor: data.idleCount > 0 ? AppColors.warningSoft : AppColors.successSoft,
            ),
          ),
          const SizedBox(width: AppDimens.space8),

          // Thẻ 3: Quá hạn hôm nay
          Expanded(
            child: _buildSummaryCard(
              icon: Icons.warning_amber_rounded,
              label: 'Việc quá hạn',
              value: '${data.overdueTodayCount}',
              color: data.overdueTodayCount > 0 ? AppColors.danger : AppColors.textSecondary,
              bgColor: data.overdueTodayCount > 0 ? AppColors.dangerSoft : AppColors.primarySoft,
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildSummaryCard({
    required IconData icon,
    required String label,
    required String value,
    required Color color,
    required Color bgColor,
  }) {
    return Container(
      padding: const EdgeInsets.symmetric(
        horizontal: AppDimens.space8,
        vertical: AppDimens.space8,
      ),
      decoration: BoxDecoration(
        color: bgColor,
        borderRadius: BorderRadius.circular(AppDimens.radiusMd),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Icon(icon, size: 14, color: color),
              const SizedBox(width: AppDimens.space4),
              Expanded(
                child: AppText(
                  label,
                  variant: AppTextVariant.caption,
                  fontSize: 11,
                  color: color,
                  weight: FontWeight.w600,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                ),
              ),
            ],
          ),
          const SizedBox(height: 2),
          AppText(
            value,
            variant: AppTextVariant.title,
            fontSize: 16,
            weight: FontWeight.w800,
            color: color,
          ),
        ],
      ),
    );
  }

  Widget _buildTabBody() {
    if (_isLoading) {
      return const Center(child: AppLoading());
    }

    if (_errorMessage != null) {
      return AppErrorState(
        message: _errorMessage!,
        onRetry: _loadData,
      );
    }

    if (_data == null || _data!.members.isEmpty) {
      return Center(
        child: Padding(
          padding: const EdgeInsets.all(AppDimens.space24),
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Icon(
                Icons.groups_outlined,
                size: 48,
                color: AppColors.textSecondary.withValues(alpha: 0.5),
              ),
              const SizedBox(height: AppDimens.space12),
              const AppText(
                'Chưa có dữ liệu thành viên',
                variant: AppTextVariant.body,
                weight: FontWeight.w700,
              ),
              const SizedBox(height: AppDimens.space4),
              const AppText(
                'Không tìm thấy thông tin nhân sự nào đang hoạt động.',
                variant: AppTextVariant.caption,
                color: AppColors.textSecondary,
                align: TextAlign.center,
              ),
            ],
          ),
        ),
      );
    }

    return RefreshIndicator(
      onRefresh: _loadData,
      color: AppColors.primary,
      child: TabBarView(
        controller: _tabController,
        children: [
          _buildTodayTab(_data!),
          _buildKpiTab(_data!),
          _buildProjectDistributionTab(_data!),
        ],
      ),
    );
  }

  // ==========================================
  // TAB 1: VIỆC HÔM NAY
  // ==========================================
  Widget _buildTodayTab(TeamDashboardData data) {
    final members = data.members;

    return ListView.separated(
      padding: const EdgeInsets.all(AppDimens.space16),
      itemCount: members.length,
      separatorBuilder: (_, __) => const SizedBox(height: AppDimens.space12),
      itemBuilder: (context, index) {
        final member = members[index];
        final isIdle = member.todayTaskCount == 0;

        return AppCard(
          padding: const EdgeInsets.all(AppDimens.space12),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              // Header thành viên
              Row(
                children: [
                  // Avatar ký tự đầu
                  CircleAvatar(
                    radius: 16,
                    backgroundColor: isIdle ? AppColors.warningSoft : AppColors.primarySoft,
                    child: AppText(
                      member.fullName.isNotEmpty ? member.fullName[0].toUpperCase() : '?',
                      variant: AppTextVariant.caption,
                      weight: FontWeight.w700,
                      color: isIdle ? AppColors.warningOnSoft : AppColors.primaryDark,
                    ),
                  ),
                  const SizedBox(width: AppDimens.space8),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        AppText(
                          member.fullName,
                          variant: AppTextVariant.body,
                          weight: FontWeight.w700,
                          fontSize: 14.5,
                        ),
                        AppText(
                          isIdle
                              ? 'Chưa có việc hôm nay'
                              : '${member.todayTaskCount} việc đang chạy',
                          variant: AppTextVariant.caption,
                          fontSize: 12,
                          color: isIdle ? AppColors.warningOnSoft : AppColors.textSecondary,
                          weight: isIdle ? FontWeight.w600 : FontWeight.normal,
                        ),
                      ],
                    ),
                  ),

                  // Badge số lượng việc / quá hạn
                  if (isIdle)
                    Container(
                      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                      decoration: BoxDecoration(
                        color: AppColors.warningSoft,
                        borderRadius: BorderRadius.circular(AppDimens.radiusPill),
                      ),
                      child: const AppText(
                        '0 việc',
                        variant: AppTextVariant.caption,
                        fontSize: 11.5,
                        weight: FontWeight.w700,
                        color: AppColors.warningOnSoft,
                      ),
                    )
                  else ...[
                    if (member.overdueTodayCount > 0) ...[
                      Container(
                        padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                        decoration: BoxDecoration(
                          color: AppColors.dangerSoft,
                          borderRadius: BorderRadius.circular(AppDimens.radiusPill),
                        ),
                        child: AppText(
                          '${member.overdueTodayCount} quá hạn',
                          variant: AppTextVariant.caption,
                          fontSize: 11.5,
                          weight: FontWeight.w700,
                          color: AppColors.danger,
                        ),
                      ),
                      const SizedBox(width: AppDimens.space4),
                    ],
                    Container(
                      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                      decoration: BoxDecoration(
                        color: AppColors.primarySoft,
                        borderRadius: BorderRadius.circular(AppDimens.radiusPill),
                      ),
                      child: AppText(
                        '${member.todayTaskCount}',
                        variant: AppTextVariant.caption,
                        fontSize: 11.5,
                        weight: FontWeight.w700,
                        color: AppColors.primaryDark,
                      ),
                    ),
                  ],
                ],
              ),

              // Nội dung công việc
              if (isIdle) ...[
                const SizedBox(height: AppDimens.space12),
                Container(
                  padding: const EdgeInsets.all(AppDimens.space8),
                  decoration: BoxDecoration(
                    color: AppColors.warningSoft,
                    borderRadius: BorderRadius.circular(AppDimens.radiusSm),
                  ),
                  child: const Row(
                    children: [
                      Icon(
                        Icons.info_outline,
                        size: 16,
                        color: AppColors.warningOnSoft,
                      ),
                      SizedBox(width: AppDimens.space8),
                      Expanded(
                        child: AppText(
                          'Không có việc nào đang chạy hôm nay — nên hỏi lại PM hoặc thành viên.',
                          variant: AppTextVariant.caption,
                          fontSize: 12,
                          color: AppColors.warningOnSoft,
                        ),
                      ),
                    ],
                  ),
                ),
              ] else ...[
                const SizedBox(height: AppDimens.space12),
                const Divider(height: 1, color: AppColors.border),
                const SizedBox(height: AppDimens.space8),
                for (final task in member.todayTasks) _buildTodayTaskItem(task),
              ],
            ],
          ),
        );
      },
    );
  }

  Widget _buildTodayTaskItem(TeamTodayTask task) {
    return InkWell(
      onTap: () {
        Navigator.of(context).pushNamed(
          AppRoutes.taskDetail,
          arguments: {'taskId': '${task.taskId}'},
        );
      },
      borderRadius: BorderRadius.circular(AppDimens.radiusSm),
      child: Padding(
        padding: const EdgeInsets.symmetric(vertical: AppDimens.space4),
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Icon trạng thái
            Padding(
              padding: const EdgeInsets.only(top: 2),
              child: Icon(
                task.isOverdue
                    ? Icons.warning_amber_rounded
                    : Icons.check_circle_outline,
                size: 16,
                color: task.isOverdue ? AppColors.danger : AppColors.primary,
              ),
            ),
            const SizedBox(width: AppDimens.space8),

            // Tên việc & Dự án
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  AppText(
                    task.title,
                    variant: AppTextVariant.body,
                    fontSize: 13.5,
                    weight: FontWeight.w600,
                    color: task.isOverdue ? AppColors.danger : AppColors.textPrimary,
                  ),
                  const SizedBox(height: 2),
                  Row(
                    children: [
                      Expanded(
                        child: AppText(
                          task.projectName.isNotEmpty ? task.projectName : 'Việc riêng',
                          variant: AppTextVariant.caption,
                          fontSize: 11.5,
                          color: AppColors.textSecondary,
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                        ),
                      ),
                      if (task.isOverdue) ...[
                        const SizedBox(width: AppDimens.space4),
                        const AppText(
                          '· Quá hạn',
                          variant: AppTextVariant.caption,
                          fontSize: 11.5,
                          weight: FontWeight.w700,
                          color: AppColors.danger,
                        ),
                      ],
                    ],
                  ),
                ],
              ),
            ),
            const SizedBox(width: AppDimens.space8),

            // Badge tiến độ
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
              decoration: BoxDecoration(
                color: task.isOverdue ? AppColors.dangerSoft : AppColors.primarySoft,
                borderRadius: BorderRadius.circular(4),
              ),
              child: AppText(
                '${task.progress}%',
                variant: AppTextVariant.caption,
                fontSize: 11,
                weight: FontWeight.w700,
                color: task.isOverdue ? AppColors.danger : AppColors.primaryDark,
              ),
            ),
          ],
        ),
      ),
    );
  }

  // ==========================================
  // TAB 2: KPI THÁNG
  // ==========================================
  Widget _buildKpiTab(TeamDashboardData data) {
    // Sắp xếp theo điểm KPI từ cao xuống thấp
    final sortedMembers = List<TeamMemberRow>.from(data.members)
      ..sort((a, b) => b.kpi.finalPoint.compareTo(a.kpi.finalPoint));

    return ListView.separated(
      padding: const EdgeInsets.all(AppDimens.space16),
      itemCount: sortedMembers.length,
      separatorBuilder: (_, __) => const SizedBox(height: AppDimens.space12),
      itemBuilder: (context, index) {
        final member = sortedMembers[index];
        final rankNo = index + 1;
        final kpi = member.kpi;

        Color rankBadgeColor = AppColors.primarySoft;
        Color rankTextColor = AppColors.primary;
        if (rankNo == 1) {
          rankBadgeColor = AppColors.rankGoldBg;
          rankTextColor = AppColors.rankGoldText;
        } else if (rankNo == 2) {
          rankBadgeColor = AppColors.rankSilverBg;
          rankTextColor = AppColors.rankSilverText;
        } else if (rankNo == 3) {
          rankBadgeColor = AppColors.rankBronzeBg;
          rankTextColor = AppColors.rankBronzeText;
        }

        return AppCard(
          padding: const EdgeInsets.all(AppDimens.space12),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              // Hàng 1: Thứ hạng + Tên + Xếp loại Badge
              Row(
                children: [
                  // Huy hiệu thứ hạng
                  Container(
                    width: 28,
                    height: 28,
                    decoration: BoxDecoration(
                      color: rankBadgeColor,
                      shape: BoxShape.circle,
                    ),
                    child: Center(
                      child: AppText(
                        '$rankNo',
                        variant: AppTextVariant.caption,
                        fontSize: 12.5,
                        weight: FontWeight.w800,
                        color: rankTextColor,
                      ),
                    ),
                  ),
                  const SizedBox(width: AppDimens.space8),
                  Expanded(
                    child: AppText(
                      member.fullName,
                      variant: AppTextVariant.body,
                      fontSize: 15,
                      weight: FontWeight.w700,
                    ),
                  ),

                  // Xếp loại A/B/C
                  Container(
                    padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 3),
                    decoration: BoxDecoration(
                      color: _getRankBgColor(kpi.rank),
                      borderRadius: BorderRadius.circular(AppDimens.radiusPill),
                    ),
                    child: AppText(
                      'Loại ${kpi.rank}',
                      variant: AppTextVariant.caption,
                      fontSize: 12,
                      weight: FontWeight.w700,
                      color: _getRankTextColor(kpi.rank),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: AppDimens.space12),
              const Divider(height: 1, color: AppColors.border),
              const SizedBox(height: AppDimens.space12),

              // Hàng 2: Điểm KPI, Giờ công, Điểm trừ
              Row(
                children: [
                  // Điểm KPI
                  Expanded(
                    flex: 4,
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        const AppText(
                          'KPI TẠM TÍNH',
                          variant: AppTextVariant.caption,
                          fontSize: 10.5,
                          color: AppColors.textSecondary,
                          weight: FontWeight.w600,
                        ),
                        const SizedBox(height: 2),
                        AppText(
                          '${kpi.finalPoint.toStringAsFixed(2)} đ',
                          variant: AppTextVariant.title,
                          fontSize: 17,
                          weight: FontWeight.w800,
                          color: AppColors.primary,
                        ),
                      ],
                    ),
                  ),

                  // Giờ công
                  Expanded(
                    flex: 4,
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        const AppText(
                          'GIỜ CÔNG',
                          variant: AppTextVariant.caption,
                          fontSize: 10.5,
                          color: AppColors.textSecondary,
                          weight: FontWeight.w600,
                        ),
                        const SizedBox(height: 2),
                        AppText(
                          '${kpi.workedHours.toStringAsFixed(1)}/${kpi.requiredHours.toStringAsFixed(1)}h',
                          variant: AppTextVariant.body,
                          fontSize: 13.5,
                          weight: FontWeight.w600,
                          color: AppColors.textPrimary,
                        ),
                      ],
                    ),
                  ),

                  // Điểm phạt
                  Expanded(
                    flex: 3,
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.end,
                      children: [
                        const AppText(
                          'ĐIỂM TRỪ',
                          variant: AppTextVariant.caption,
                          fontSize: 10.5,
                          color: AppColors.textSecondary,
                          weight: FontWeight.w600,
                        ),
                        const SizedBox(height: 2),
                        AppText(
                          member.totalPenalty > 0
                              ? '-${member.totalPenalty.toStringAsFixed(2)}'
                              : '0.00',
                          variant: AppTextVariant.body,
                          fontSize: 13.5,
                          weight: FontWeight.w700,
                          color: member.totalPenalty > 0 ? AppColors.danger : AppColors.success,
                        ),
                      ],
                    ),
                  ),
                ],
              ),
            ],
          ),
        );
      },
    );
  }

  Color _getRankBgColor(String rank) {
    switch (rank.toUpperCase()) {
      case 'A':
        return AppColors.successSoft;
      case 'B':
        return AppColors.primarySoft;
      case 'C':
        return AppColors.warningSoft;
      case 'D':
      case 'F':
        return AppColors.dangerSoft;
      default:
        return AppColors.primarySoft;
    }
  }

  Color _getRankTextColor(String rank) {
    switch (rank.toUpperCase()) {
      case 'A':
        return AppColors.successOnSoft;
      case 'B':
        return AppColors.primaryDark;
      case 'C':
        return AppColors.warningOnSoft;
      case 'D':
      case 'F':
        return AppColors.danger;
      default:
        return AppColors.primaryDark;
    }
  }

  // ==========================================
  // TAB 3: PHÂN BỔ DỰ ÁN
  // ==========================================
  Widget _buildProjectDistributionTab(TeamDashboardData data) {
    // Sắp xếp theo tổng số việc đảm nhận
    final sortedMembers = List<TeamMemberRow>.from(data.members)
      ..sort((a, b) {
        final totalA = a.implement.tasks + a.support.tasks;
        final totalB = b.implement.tasks + b.support.tasks;
        return totalB.compareTo(totalA);
      });

    return ListView.separated(
      padding: const EdgeInsets.all(AppDimens.space16),
      itemCount: sortedMembers.length,
      separatorBuilder: (_, __) => const SizedBox(height: AppDimens.space12),
      itemBuilder: (context, index) {
        final member = sortedMembers[index];

        return AppCard(
          padding: const EdgeInsets.all(AppDimens.space12),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              // Tên thành viên & Tổng số việc
              Row(
                children: [
                  CircleAvatar(
                    radius: 14,
                    backgroundColor: AppColors.primarySoft,
                    child: AppText(
                      member.fullName.isNotEmpty ? member.fullName[0].toUpperCase() : '?',
                      variant: AppTextVariant.caption,
                      weight: FontWeight.w700,
                      color: AppColors.primaryDark,
                    ),
                  ),
                  const SizedBox(width: AppDimens.space8),
                  Expanded(
                    child: AppText(
                      member.fullName,
                      variant: AppTextVariant.body,
                      fontSize: 14.5,
                      weight: FontWeight.w700,
                    ),
                  ),
                  Container(
                    padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                    decoration: BoxDecoration(
                      color: AppColors.primarySoft,
                      borderRadius: BorderRadius.circular(AppDimens.radiusPill),
                    ),
                    child: AppText(
                      'Tổng ${member.implement.tasks + member.support.tasks} việc',
                      variant: AppTextVariant.caption,
                      fontSize: 11.5,
                      weight: FontWeight.w600,
                      color: AppColors.primaryDark,
                    ),
                  ),
                ],
              ),
              const SizedBox(height: AppDimens.space12),
              const Divider(height: 1, color: AppColors.border),
              const SizedBox(height: AppDimens.space12),

              // 2 Nút/Thẻ: Triển khai vs Hỗ trợ
              Row(
                children: [
                  // Triển khai
                  Expanded(
                    child: InkWell(
                      onTap: member.implement.tasks > 0
                          ? () => TeamMemberTasksSheet.show(
                                context,
                                userId: member.userId,
                                memberName: member.fullName,
                                year: data.year,
                                month: data.month,
                                kind: 'Checklist',
                              )
                          : null,
                      borderRadius: BorderRadius.circular(AppDimens.radiusMd),
                      child: Container(
                        padding: const EdgeInsets.all(AppDimens.space8),
                        decoration: BoxDecoration(
                          color: AppColors.primarySoft,
                          borderRadius: BorderRadius.circular(AppDimens.radiusMd),
                          border: Border.all(color: AppColors.primary.withValues(alpha: 0.15)),
                        ),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Row(
                              children: [
                                const Icon(
                                  Icons.assignment_outlined,
                                  size: 15,
                                  color: AppColors.primary,
                                ),
                                const SizedBox(width: AppDimens.space4),
                                const Expanded(
                                  child: AppText(
                                    'Triển khai',
                                    variant: AppTextVariant.caption,
                                    fontSize: 12,
                                    weight: FontWeight.w700,
                                    color: AppColors.primaryDark,
                                  ),
                                ),
                                if (member.implement.tasks > 0)
                                  const Icon(
                                    Icons.open_in_new,
                                    size: 13,
                                    color: AppColors.primary,
                                  ),
                              ],
                            ),
                            const SizedBox(height: 4),
                            AppText(
                              '${member.implement.projects} dự án · ${member.implement.tasks} việc',
                              variant: AppTextVariant.caption,
                              fontSize: 11.5,
                              color: AppColors.textPrimary,
                              weight: FontWeight.w600,
                            ),
                          ],
                        ),
                      ),
                    ),
                  ),
                  const SizedBox(width: AppDimens.space8),

                  // Hỗ trợ
                  Expanded(
                    child: InkWell(
                      onTap: member.support.tasks > 0
                          ? () => TeamMemberTasksSheet.show(
                                context,
                                userId: member.userId,
                                memberName: member.fullName,
                                year: data.year,
                                month: data.month,
                                kind: 'Support',
                              )
                          : null,
                      borderRadius: BorderRadius.circular(AppDimens.radiusMd),
                      child: Container(
                        padding: const EdgeInsets.all(AppDimens.space8),
                        decoration: BoxDecoration(
                          color: AppColors.warningSoft,
                          borderRadius: BorderRadius.circular(AppDimens.radiusMd),
                          border: Border.all(color: AppColors.warning.withValues(alpha: 0.15)),
                        ),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Row(
                              children: [
                                const Icon(
                                  Icons.support_agent,
                                  size: 15,
                                  color: AppColors.warningOnSoft,
                                ),
                                const SizedBox(width: AppDimens.space4),
                                const Expanded(
                                  child: AppText(
                                    'Hỗ trợ',
                                    variant: AppTextVariant.caption,
                                    fontSize: 12,
                                    weight: FontWeight.w700,
                                    color: AppColors.warningOnSoft,
                                  ),
                                ),
                                if (member.support.tasks > 0)
                                  const Icon(
                                    Icons.open_in_new,
                                    size: 13,
                                    color: AppColors.warningOnSoft,
                                  ),
                              ],
                            ),
                            const SizedBox(height: 4),
                            AppText(
                              '${member.support.projects} dự án · ${member.support.tasks} việc',
                              variant: AppTextVariant.caption,
                              fontSize: 11.5,
                              color: AppColors.textPrimary,
                              weight: FontWeight.w600,
                            ),
                          ],
                        ),
                      ),
                    ),
                  ),
                ],
              ),
            ],
          ),
        );
      },
    );
  }
}
