import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import 'package:phosphor_icons/phosphor_icons.dart';
import 'package:provider/provider.dart';

import '../../config/app_theme.dart';
import '../../core/classes/route_manager.dart';
import '../../core/theme/app_colors.dart';
import '../../core/theme/app_text_styles.dart';
import '../../core/widgets/app_button.dart';
import '../../core/widgets/app_card.dart';
import '../../core/widgets/app_icon_button.dart';
import '../../core/widgets/app_loading.dart';
import '../../core/widgets/app_text.dart';
import '../../shared/widgets/app_bottom_nav.dart';
import '../app_routes.dart';
import '../auth/auth_provider.dart';
import '../notifications/notifications_service.dart';
import 'dashboard_models.dart';
import 'dashboard_service.dart';

enum _TaskFilter { all, today, upcoming }

class DashboardScreen extends StatefulWidget {
  const DashboardScreen({super.key});

  @override
  State<DashboardScreen> createState() => _DashboardScreenState();
}

class _DashboardScreenState extends State<DashboardScreen> {
  final _service = DashboardService();
  late Future<DashboardData> _future;

  _TaskFilter _filter = _TaskFilter.all;
  bool _showTodayBanner = true;

  @override
  void initState() {
    super.initState();
    _future = _service.fetch();
  }

  void _reload() {
    setState(() {
      _future = _service.fetch();
      _showTodayBanner = true;
    });
  }

  List<TaskItem> _filteredTasks(List<TaskItem> tasks) {
    switch (_filter) {
      case _TaskFilter.today:
        return tasks.where((t) => t.isDueToday).toList();
      case _TaskFilter.upcoming:
        return tasks.where((t) => !t.isDueToday).toList();
      case _TaskFilter.all:
        return tasks;
    }
  }

  @override
  Widget build(BuildContext context) {
    final displayName = context.watch<AuthProvider>().displayName ?? 'bạn';

    return AppBottomNav(
      currentIndex: 0,
      body: SafeArea(
        child: FutureBuilder<DashboardData>(
          future: _future,
          builder: (context, snapshot) {
            if (snapshot.connectionState != ConnectionState.done) {
              return const Center(
                child: AppLoading(),
              );
            }

            if (snapshot.hasError) {
              return _ErrorState(onRetry: _reload);
            }

            final data = snapshot.data!;
            final filteredTasks = _filteredTasks(data.tasks);

            return ListView(
              padding: const EdgeInsets.fromLTRB(20, 16, 20, 24),
              children: [
                _Header(
                    displayName: displayName,
                    todayTaskCount: data.todayTaskCount),
                if (_showTodayBanner && data.todayTaskCount > 0) ...[
                  const SizedBox(height: 16),
                  _TodayBanner(
                    count: data.todayTaskCount,
                    onClose: () => setState(() => _showTodayBanner = false),
                  ),
                ],
                if (data.kpi != null) ...[
                  const SizedBox(height: 16),
                  _KpiCard(kpi: data.kpi!),
                ],
                const SizedBox(height: 16),
                _LeaveCard(leave: data.leave),
                if (data.projects.isNotEmpty) ...[
                  const SizedBox(height: 24),
                  const _SectionTitle('Dự án'),
                  const SizedBox(height: 10),
                  // Scroll ngang thay vi luoi 2 cot — xem het du an ma khong choan chieu cao
                  // man hinh, phu hop danh sach co the dai khi nhieu du an.
                  SizedBox(
                    height: 190,
                    child: ListView.separated(
                      scrollDirection: Axis.horizontal,
                      itemCount: data.projects.length,
                      separatorBuilder: (context, index) =>
                          const SizedBox(width: 12),
                      itemBuilder: (context, index) => SizedBox(
                        width: 150,
                        child: _ProjectCard(project: data.projects[index]),
                      ),
                    ),
                  ),
                ],
                const SizedBox(height: 24),
                _FilterRow(
                  current: _filter,
                  totalCount: data.tasks.length,
                  onChanged: (f) => setState(() => _filter = f),
                ),
                const SizedBox(height: 16),
                if (filteredTasks.isEmpty)
                  const _EmptyHint('Không có việc nào ở mục này.')
                else
                  for (final task in filteredTasks) _TaskRow(task: task),
                if (data.canSeeTeam && data.team != null) ...[
                  const SizedBox(height: 24),
                  const _SectionTitle('Toàn Tổ'),
                  const SizedBox(height: 10),
                  _TeamSection(
                    team: data.team!,
                    pendingLeaveApprovalCount: data.canApproveLeave
                        ? data.pendingLeaveApprovalCount
                        : null,
                  ),
                ],
              ],
            );
          },
        ),
      ),
    );
  }
}

class _ErrorState extends StatelessWidget {
  const _ErrorState({required this.onRetry});

  final VoidCallback onRetry;

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            const PhosphorIcon(PhosphorIconsRegular.warningCircle,
                color: AppTheme.statusDanger, size: 32),
            const SizedBox(height: 12),
            const AppText('Không tải được dữ liệu tổng quan.',
                variant: AppTextVariant.body, align: TextAlign.center),
            const SizedBox(height: 12),
            AppButton(
              label: 'Thử lại',
              onPressed: onRetry,
            ),
          ],
        ),
      ),
    );
  }
}

class _SectionTitle extends StatelessWidget {
  const _SectionTitle(this.text);

  final String text;

  @override
  Widget build(BuildContext context) {
    return AppText(text, variant: AppTextVariant.heading, fontSize: 17);
  }
}

class _EmptyHint extends StatelessWidget {
  const _EmptyHint(this.text);

  final String text;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 8),
      child: AppText(text,
          variant: AppTextVariant.body, color: AppColors.textSecondary),
    );
  }
}

class _Header extends StatelessWidget {
  const _Header({required this.displayName, required this.todayTaskCount});

  final String displayName;
  final int todayTaskCount;

  @override
  Widget build(BuildContext context) {
    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              AppText(
                'Chào, $displayName!',
                variant: AppTextVariant.caption,
                fontSize: 13,
                weight: FontWeight.w500,
                color: AppColors.textSecondary,
              ),
              const SizedBox(height: 4),
              AppText(
                'Bạn có $todayTaskCount công việc cần hoàn thành trong ngày',
                variant: AppTextVariant.display,
                fontSize: 20,
              ),
            ],
          ),
        ),
        const _NotificationBell(),
      ],
    );
  }
}

/// Chuong thong bao + so tin chua doc — StatefulWidget rieng de CHI goi API dem tin 1 lan (giu
/// nguyen State qua moi lan Dashboard build lai theo FutureBuilder cha), khong lam theo
/// DashboardData vi backend Dashboard hien chua tra kem so nay.
class _NotificationBell extends StatefulWidget {
  const _NotificationBell();

  @override
  State<_NotificationBell> createState() => _NotificationBellState();
}

class _NotificationBellState extends State<_NotificationBell> {
  final _service = NotificationsService();
  late final Future<int> _unreadFuture = _fetchUnread();

  Future<int> _fetchUnread() async {
    try {
      final page = await _service.fetch();
      return page.unreadCount;
    } catch (_) {
      // Loi tai dem tin khong nen chan ca man Dashboard — im lang coi nhu 0, chuong van
      // bam mo man Thong bao duoc binh thuong.
      return 0;
    }
  }

  @override
  Widget build(BuildContext context) {
    return FutureBuilder<int>(
      future: _unreadFuture,
      builder: (context, snapshot) {
        final unread = snapshot.data ?? 0;
        return Stack(
          clipBehavior: Clip.none,
          children: [
            AppIconButton(
              icon: PhosphorIconsRegular.bellSimple,
              onPressed: () => Nav.toNamed(context, AppRoutes.notifications),
              color: AppTheme.brandBlue,
              size: 22,
              background: AppTheme.brandBlue.withValues(alpha: 0.08),
            ),
            if (unread > 0)
              Positioned(
                right: 2,
                top: 2,
                child: Container(
                  padding:
                      const EdgeInsets.symmetric(horizontal: 5, vertical: 1),
                  constraints:
                      const BoxConstraints(minWidth: 18, minHeight: 18),
                  decoration: BoxDecoration(
                    color: AppTheme.statusDanger,
                    borderRadius: BorderRadius.circular(9),
                    border: Border.all(color: AppColors.surface, width: 1.5),
                  ),
                  alignment: Alignment.center,
                  child: AppText(
                    unread > 99 ? '99+' : '$unread',
                    variant: AppTextVariant.overline,
                    fontSize: 10,
                    weight: FontWeight.w800,
                    color: AppColors.textOnPrimary,
                    letterSpacing: 0,
                  ),
                ),
              ),
          ],
        );
      },
    );
  }
}

class _TodayBanner extends StatelessWidget {
  const _TodayBanner({required this.count, required this.onClose});

  final int count;
  final VoidCallback onClose;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
      decoration: BoxDecoration(
        color: AppTheme.brandBlue,
        borderRadius: BorderRadius.circular(14),
      ),
      child: Row(
        children: [
          const PhosphorIcon(PhosphorIconsRegular.calendarCheck,
              color: AppColors.textOnPrimary, size: 20),
          const SizedBox(width: 10),
          Expanded(
            child: AppText(
              'Bạn có $count công việc hôm nay',
              variant: AppTextVariant.body,
              color: AppColors.textOnPrimary,
              weight: FontWeight.w600,
              fontSize: 13.5,
            ),
          ),
          AppIconButton(
            icon: PhosphorIconsRegular.x,
            onPressed: onClose,
            color: AppColors.textOnPrimary,
            size: 18,
          ),
        ],
      ),
    );
  }
}

/// Mau theo xep loai KPI — khop tinh than ScoreTone ben web (tot/dat mau xanh la, kha/dat mau
/// vang, chua dat mau do), nhung anh xa tren nhan Rank tho vi mobile khong tinh lai tone rieng.
Color _kpiRankColor(String rank) {
  switch (rank) {
    case 'Xuất sắc':
    case 'Tốt':
      return AppTheme.statusSuccess;
    case 'Khá':
    case 'Đạt':
      return AppTheme.statusWarning;
    default:
      return AppTheme.statusDanger;
  }
}

class _KpiCard extends StatelessWidget {
  const _KpiCard({required this.kpi});

  final KpiSummary kpi;

  @override
  Widget build(BuildContext context) {
    final rankColor = _kpiRankColor(kpi.rank);

    return AppCard(
      padding: const EdgeInsets.all(14),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              SizedBox(
                width: 64,
                height: 64,
                child: Stack(
                  alignment: Alignment.center,
                  children: [
                    CircularProgressIndicator(
                      value: kpi.scorePercent / 100,
                      strokeWidth: 6,
                      backgroundColor: rankColor.withValues(alpha: 0.12),
                      valueColor: AlwaysStoppedAnimation(rankColor),
                    ),
                    AppText(
                      kpi.finalPoint.toStringAsFixed(1),
                      variant: AppTextVariant.body,
                      fontSize: 15,
                      weight: FontWeight.w800,
                      color: rankColor,
                    ),
                  ],
                ),
              ),
              const SizedBox(width: 14),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    const AppText('Điểm KPI tháng này',
                        variant: AppTextVariant.body,
                        fontSize: 13,
                        weight: FontWeight.w600),
                    const SizedBox(height: 6),
                    Container(
                      padding: const EdgeInsets.symmetric(
                          horizontal: 8, vertical: 3),
                      decoration: BoxDecoration(
                        color: rankColor.withValues(alpha: 0.12),
                        borderRadius: BorderRadius.circular(4),
                      ),
                      child: AppText(
                        kpi.rank,
                        variant: AppTextVariant.caption,
                        fontSize: 11.5,
                        weight: FontWeight.w700,
                        color: rankColor,
                      ),
                    ),
                  ],
                ),
              ),
            ],
          ),
          const SizedBox(height: 14),
          Row(
            children: [
              const AppText('Giờ làm',
                  variant: AppTextVariant.caption,
                  fontSize: 12,
                  color: AppColors.textSecondary),
              const Spacer(),
              AppText(
                '${kpi.workedHours.toStringAsFixed(0)}/${kpi.requiredHours.toStringAsFixed(0)} giờ',
                variant: AppTextVariant.body,
                fontSize: 12,
                weight: FontWeight.w600,
              ),
            ],
          ),
          const SizedBox(height: 6),
          ClipRRect(
            borderRadius: BorderRadius.circular(6),
            child: LinearProgressIndicator(
              value: kpi.hoursPercent / 100,
              minHeight: 6,
              backgroundColor: AppTheme.brandBlue.withValues(alpha: 0.1),
              valueColor: AlwaysStoppedAnimation(kpi.hoursShort > 0
                  ? AppTheme.statusWarning
                  : AppTheme.statusSuccess),
            ),
          ),
          if (kpi.hoursShort > 0) ...[
            const SizedBox(height: 6),
            AppText(
              'Còn thiếu ${kpi.hoursShort.toStringAsFixed(1)} giờ so với yêu cầu tháng.',
              variant: AppTextVariant.caption,
              fontSize: 11.5,
              color: AppTheme.statusWarning,
            ),
          ],
        ],
      ),
    );
  }
}

class _LeaveCard extends StatelessWidget {
  const _LeaveCard({required this.leave});

  final LeaveSummary leave;

  @override
  Widget build(BuildContext context) {
    return AppCard(
      padding: const EdgeInsets.all(14),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const AppText('Nghỉ phép tháng này',
              variant: AppTextVariant.body,
              fontSize: 13,
              weight: FontWeight.w600),
          const SizedBox(height: 10),
          Row(
            children: [
              Expanded(
                child: _LeaveStat(
                    label: 'Đã nghỉ',
                    value: '${leave.approvedDays.toStringAsFixed(1)} ngày'),
              ),
              Expanded(
                child: _LeaveStat(
                    label: 'Chờ duyệt', value: '${leave.pendingCount} đơn'),
              ),
            ],
          ),
          if (leave.upcoming.isNotEmpty) ...[
            const SizedBox(height: 10),
            const Divider(height: 1),
            const SizedBox(height: 10),
            for (final item in leave.upcoming) _LeaveRow(item: item),
          ],
        ],
      ),
    );
  }
}

class _LeaveStat extends StatelessWidget {
  const _LeaveStat({required this.label, required this.value});

  final String label;
  final String value;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        AppText(label,
            variant: AppTextVariant.caption,
            fontSize: 11.5,
            color: AppColors.textSecondary),
        const SizedBox(height: 2),
        AppText(value,
            variant: AppTextVariant.body,
            fontSize: 14,
            weight: FontWeight.w700),
      ],
    );
  }
}

class _LeaveRow extends StatelessWidget {
  const _LeaveRow({required this.item});

  final UpcomingLeave item;

  @override
  Widget build(BuildContext context) {
    final format = DateFormat('dd/MM');
    final range = item.fromDate == null
        ? ''
        : (item.toDate == null || item.toDate!.isAtSameMomentAs(item.fromDate!)
            ? format.format(item.fromDate!)
            : '${format.format(item.fromDate!)} - ${format.format(item.toDate!)}');

    return Padding(
      padding: const EdgeInsets.only(bottom: 6),
      child: Row(
        children: [
          const PhosphorIcon(PhosphorIconsRegular.calendarCheck,
              size: 15, color: AppTheme.brandBlue),
          const SizedBox(width: 8),
          Expanded(
            child: AppText('$range · ${leaveKindLabel(item.kind)}',
                variant: AppTextVariant.caption,
                fontSize: 12.5,
                color: AppColors.textPrimary),
          ),
          AppText('${item.days.toStringAsFixed(1)} ngày',
              variant: AppTextVariant.caption,
              fontSize: 11.5,
              weight: FontWeight.w600,
              color: AppColors.textSecondary),
        ],
      ),
    );
  }
}

class _FilterRow extends StatelessWidget {
  const _FilterRow(
      {required this.current,
      required this.totalCount,
      required this.onChanged});

  final _TaskFilter current;
  final int totalCount;
  final ValueChanged<_TaskFilter> onChanged;

  @override
  Widget build(BuildContext context) {
    return Row(
      children: [
        _FilterChip(
          label: 'Tất cả ($totalCount)',
          selected: current == _TaskFilter.all,
          onTap: () => onChanged(_TaskFilter.all),
        ),
        const SizedBox(width: 8),
        _FilterChip(
          label: 'Hôm nay',
          selected: current == _TaskFilter.today,
          onTap: () => onChanged(_TaskFilter.today),
        ),
        const SizedBox(width: 8),
        _FilterChip(
          label: 'Sắp tới',
          selected: current == _TaskFilter.upcoming,
          onTap: () => onChanged(_TaskFilter.upcoming),
        ),
      ],
    );
  }
}

class _FilterChip extends StatelessWidget {
  const _FilterChip(
      {required this.label, required this.selected, required this.onTap});

  final String label;
  final bool selected;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(20),
      child: Container(
        padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 8),
        decoration: BoxDecoration(
          color: selected ? AppTheme.brandBlue : Colors.white,
          borderRadius: BorderRadius.circular(20),
          border:
              Border.all(color: selected ? AppTheme.brandBlue : Colors.black12),
        ),
        child: AppText(
          label,
          variant: AppTextVariant.caption,
          fontSize: 12.5,
          weight: FontWeight.w600,
          color: selected ? Colors.white : AppColors.textSecondary,
        ),
      ),
    );
  }
}

class _ProjectCard extends StatelessWidget {
  const _ProjectCard({required this.project});

  final ProjectSummary project;

  @override
  Widget build(BuildContext context) {
    final progress = project.progressPercent / 100;

    return AppCard(
      onTap: () => Nav.toNamed(context, AppRoutes.projectDetail,
          arguments: {'projectId': project.id.toString()}),
      padding: const EdgeInsets.all(12),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Container(
            width: 30,
            height: 30,
            decoration: BoxDecoration(
              color: AppTheme.brandBlueDark,
              borderRadius: BorderRadius.circular(8),
            ),
            child: const PhosphorIcon(PhosphorIconsRegular.folder,
                color: AppColors.textOnPrimary, size: 16),
          ),
          const SizedBox(height: 8),
          AppText(
            project.name,
            maxLines: 2,
            overflow: TextOverflow.ellipsis,
            variant: AppTextVariant.body,
            fontSize: 13,
            weight: FontWeight.w700,
          ),
          const SizedBox(height: 3),
          AppText(
            project.description?.isNotEmpty == true
                ? project.description!
                : '—',
            maxLines: 1,
            overflow: TextOverflow.ellipsis,
            variant: AppTextVariant.caption,
            fontSize: 11,
            color: AppColors.textSecondary,
          ),
          const SizedBox(height: 8),
          _AvatarStack(count: project.memberCount),
          const SizedBox(height: 8),
          ClipRRect(
            borderRadius: BorderRadius.circular(6),
            child: LinearProgressIndicator(
              value: progress,
              minHeight: 6,
              backgroundColor: AppTheme.brandBlue.withValues(alpha: 0.1),
              valueColor: const AlwaysStoppedAnimation(AppTheme.brandBlueDark),
            ),
          ),
          const SizedBox(height: 4),
          AppText(
            '${project.progressPercent}%',
            variant: AppTextVariant.caption,
            fontSize: 11,
            weight: FontWeight.w700,
            color: AppTheme.brandBlue,
          ),
        ],
      ),
    );
  }
}

/// Cac tong mau xanh khac nhau de phan biet tung nguoi trong avatar stack, van nam trong he mau
/// thuong hieu (khong dung mau ngoai xanh/trang).
const _avatarTones = [
  AppTheme.brandBlue,
  AppTheme.brandBlueDark,
  AppTheme.brandBlueDarker
];

class _AvatarStack extends StatelessWidget {
  const _AvatarStack({required this.count});

  final int count;

  @override
  Widget build(BuildContext context) {
    final shown = count > 3 ? 3 : count;
    return SizedBox(
      height: 24,
      child: Stack(
        children: [
          for (var i = 0; i < shown; i++)
            Positioned(
              left: i * 16.0,
              child: Container(
                width: 24,
                height: 24,
                decoration: BoxDecoration(
                  color: _avatarTones[i % _avatarTones.length],
                  shape: BoxShape.circle,
                  border: Border.all(color: Colors.white, width: 1.5),
                ),
              ),
            ),
          if (count > 3)
            Positioned(
              left: shown * 16.0,
              child: Container(
                width: 24,
                height: 24,
                alignment: Alignment.center,
                decoration: BoxDecoration(
                  color: Colors.black12,
                  shape: BoxShape.circle,
                  border: Border.all(color: Colors.white, width: 1.5),
                ),
                child: AppText(
                  '+${count - 3}',
                  variant: AppTextVariant.overline,
                  fontSize: 9,
                  weight: FontWeight.w700,
                  color: AppColors.textSecondary,
                  letterSpacing: 0,
                ),
              ),
            ),
        ],
      ),
    );
  }
}

/// Nhan/mau hạn cho một dòng việc: quá hạn -> đỏ, hôm nay -> vàng, còn lại -> ngày/tháng mau
/// thương hiệu. Khớp ngữ nghĩa IsOverdue/IsDueToday do backend tính sẵn, không tự suy luận lại.
class _DueInfo {
  const _DueInfo(this.label, this.color);

  final String label;
  final Color color;

  factory _DueInfo.of(TaskItem task) {
    if (task.isOverdue) {
      return const _DueInfo('Quá hạn', AppTheme.statusDanger);
    }
    if (task.isDueToday) {
      return const _DueInfo('Hôm nay', AppTheme.statusWarning);
    }
    if (task.dueDate == null) {
      return const _DueInfo('—', AppColors.textSecondary);
    }
    return _DueInfo(
        DateFormat('dd/MM').format(task.dueDate!), AppTheme.brandBlue);
  }
}

class _TaskRow extends StatelessWidget {
  const _TaskRow({required this.task});

  final TaskItem task;

  @override
  Widget build(BuildContext context) {
    final highPriority = task.priority == 'Cao';
    final due = _DueInfo.of(task);

    return AppCard(
      onTap: () => Nav.toNamed(context, AppRoutes.taskDetail,
          arguments: {'taskId': task.id.toString()}),
      margin: const EdgeInsets.only(bottom: 10),
      padding: const EdgeInsets.all(12),
      radius: 14,
      child: Row(
        children: [
          Container(
            width: 8,
            height: 40,
            decoration: BoxDecoration(
              color: highPriority
                  ? AppTheme.brandBlue
                  : AppTheme.brandBlue.withValues(alpha: 0.25),
              borderRadius: BorderRadius.circular(4),
            ),
          ),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                AppText(
                  task.title,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  variant: AppTextVariant.body,
                  fontSize: 13.5,
                  weight: FontWeight.w600,
                ),
                const SizedBox(height: 3),
                AppText(
                  task.projectName?.isNotEmpty == true
                      ? task.projectName!
                      : 'Việc riêng',
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  variant: AppTextVariant.caption,
                  fontSize: 11.5,
                  color: AppColors.textSecondary,
                ),
              ],
            ),
          ),
          const SizedBox(width: 8),
          Column(
            crossAxisAlignment: CrossAxisAlignment.end,
            children: [
              AppText(
                due.label,
                variant: AppTextVariant.caption,
                fontSize: 11.5,
                weight: FontWeight.w600,
                color: due.color,
              ),
              if (highPriority) ...[
                const SizedBox(height: 4),
                Container(
                  padding:
                      const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
                  decoration: BoxDecoration(
                    color: AppTheme.brandBlue.withValues(alpha: 0.1),
                    borderRadius: BorderRadius.circular(4),
                  ),
                  child: const AppText(
                    'Ưu tiên cao',
                    variant: AppTextVariant.overline,
                    fontSize: 9.5,
                    color: AppTheme.brandBlue,
                    letterSpacing: 0,
                  ),
                ),
              ],
            ],
          ),
        ],
      ),
    );
  }
}

class _TeamSection extends StatelessWidget {
  const _TeamSection(
      {required this.team, required this.pendingLeaveApprovalCount});

  final TeamSummary team;

  /// null = tai khoan khong co quyen duyet nghi phep, khong hien dong nay.
  final int? pendingLeaveApprovalCount;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        GridView.count(
          shrinkWrap: true,
          physics: const NeverScrollableScrollPhysics(),
          crossAxisCount: 2,
          mainAxisSpacing: 12,
          crossAxisSpacing: 12,
          childAspectRatio: 1.7,
          children: [
            _StatTile(
                label: 'Dự án đang chạy',
                value: '${team.openProjectCount}',
                color: AppTheme.brandBlue,
                onTap: () => Nav.toNamed(context, AppRoutes.projects,
                    arguments: {'scope': 'team'})),
            _StatTile(
                label: 'Dự án tôi làm PM',
                value: '${team.myPmCount}',
                color: AppTheme.brandBlue,
                onTap: () => Nav.toNamed(context, AppRoutes.projects,
                    arguments: {'roleFilter': 'pm'})),
            _StatTile(
                label: 'Việc chưa xong',
                value: '${team.openTaskCount}',
                color: AppTheme.brandBlue,
                onTap: () => Nav.toNamed(context, AppRoutes.myWork,
                    arguments: {'scope': 'team', 'filter': 'open'})),
            _StatTile(
                label: 'Quá hạn',
                value: '${team.overdueTaskCount}',
                color: team.overdueTaskCount > 0
                    ? AppTheme.statusDanger
                    : AppTheme.brandBlue,
                onTap: () => Nav.toNamed(context, AppRoutes.myWork,
                    arguments: {'scope': 'team', 'filter': 'overdue'})),
          ],
        ),
        if (pendingLeaveApprovalCount != null &&
            pendingLeaveApprovalCount! > 0) ...[
          const SizedBox(height: 10),
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
            decoration: BoxDecoration(
              color: AppTheme.statusWarningSoft,
              borderRadius: BorderRadius.circular(10),
            ),
            child: Row(
              children: [
                const PhosphorIcon(PhosphorIconsRegular.calendarCheck,
                    size: 16, color: AppTheme.statusWarning),
                const SizedBox(width: 8),
                Expanded(
                  child: AppText(
                    '$pendingLeaveApprovalCount đơn nghỉ phép đang chờ bạn duyệt',
                    variant: AppTextVariant.caption,
                    fontSize: 12.5,
                    weight: FontWeight.w600,
                    color: AppTheme.statusWarning,
                  ),
                ),
              ],
            ),
          ),
        ],
        if (team.projects.isNotEmpty) ...[
          const SizedBox(height: 16),
          const AppText('Dự án cần chú ý',
              variant: AppTextVariant.body,
              fontSize: 13,
              weight: FontWeight.w700),
          const SizedBox(height: 8),
          for (final project in team.projects)
            _ProjectAttentionRow(project: project),
        ],
      ],
    );
  }
}

class _StatTile extends StatelessWidget {
  const _StatTile(
      {required this.label,
      required this.value,
      required this.color,
      this.onTap});

  final String label;
  final String value;
  final Color color;
  final VoidCallback? onTap;

  @override
  Widget build(BuildContext context) {
    return AppCard(
      onTap: onTap,
      padding: const EdgeInsets.all(12),
      radius: 14,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          AppText(value,
              variant: AppTextVariant.title,
              fontSize: 20,
              weight: FontWeight.w800,
              color: color),
          const SizedBox(height: 2),
          AppText(label,
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
              variant: AppTextVariant.caption,
              fontSize: 11,
              color: AppColors.textSecondary),
        ],
      ),
    );
  }
}

class _ProjectAttentionRow extends StatelessWidget {
  const _ProjectAttentionRow({required this.project});

  final ProjectAttention project;

  @override
  Widget build(BuildContext context) {
    return AppCard(
      onTap: () => Nav.toNamed(context, AppRoutes.projectDetail,
          arguments: {'projectId': project.id.toString()}),
      margin: const EdgeInsets.only(bottom: 8),
      padding: const EdgeInsets.all(12),
      radius: 14,
      child: Row(
        children: [
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                AppText(project.name,
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    variant: AppTextVariant.body,
                    fontSize: 13,
                    weight: FontWeight.w700),
                const SizedBox(height: 2),
                AppText(
                  [
                    if (project.customer?.isNotEmpty == true) project.customer!,
                    if (project.pmName?.isNotEmpty == true)
                      'PM: ${project.pmName}',
                  ].join(' · '),
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  variant: AppTextVariant.caption,
                  fontSize: 11.5,
                  color: AppColors.textSecondary,
                ),
              ],
            ),
          ),
          const SizedBox(width: 8),
          Column(
            crossAxisAlignment: CrossAxisAlignment.end,
            children: [
              AppText('${project.progressPercent}%',
                  variant: AppTextVariant.caption,
                  fontSize: 12.5,
                  weight: FontWeight.w700,
                  color: AppTheme.brandBlue),
              if (project.overdueCount > 0) ...[
                const SizedBox(height: 4),
                Container(
                  padding:
                      const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
                  decoration: BoxDecoration(
                    color: AppTheme.statusDangerSoft,
                    borderRadius: BorderRadius.circular(4),
                  ),
                  child: AppText(
                    'Quá hạn ${project.overdueCount}',
                    variant: AppTextVariant.overline,
                    fontSize: 9.5,
                    color: AppTheme.statusDanger,
                    letterSpacing: 0,
                  ),
                ),
              ],
            ],
          ),
        ],
      ),
    );
  }
}
