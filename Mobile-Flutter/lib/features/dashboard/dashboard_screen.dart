import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../config/app_theme.dart';
import '../../core/classes/route_manager.dart';
import '../../shared/widgets/app_bottom_nav.dart';
import '../app_routes.dart';
import '../auth/auth_provider.dart';

/// Mot du an hien trong luoi o dau man Home. Cau truc khop voi WorkProject ben web (Code/Name/
/// tien do...) de sau nay thay bang model that chi can doi nguon du lieu.
class _DemoProject {
  const _DemoProject({
    required this.title,
    required this.subtitle,
    required this.progress,
    required this.memberCount,
  });

  final String title;
  final String subtitle;
  final double progress; // 0..1
  final int memberCount;
}

/// Mot cong viec trong danh sach "Hom nay / Sap toi" ben duoi luoi du an.
class _DemoTask {
  const _DemoTask({
    required this.title,
    required this.projectName,
    required this.timeLabel,
    required this.highPriority,
    required this.isToday,
  });

  final String title;
  final String projectName;
  final String timeLabel;
  final bool highPriority;
  final bool isToday;
}

const _demoProjects = [
  _DemoProject(
    title: 'Ung dung di dong BrewTask',
    subtitle: 'Ke hoach cong viec, giao dien sach va hien dai',
    progress: 0.64,
    memberCount: 4,
  ),
  _DemoProject(
    title: 'Trang quan ly du an',
    subtitle: 'He thong theo doi tien do va KPI noi bo',
    progress: 0.34,
    memberCount: 6,
  ),
  _DemoProject(
    title: 'Cong thong tin nhan su',
    subtitle: 'Ho so, cham cong, nghi phep tap trung',
    progress: 0.82,
    memberCount: 3,
  ),
  _DemoProject(
    title: 'Tich hop CAS - HRM',
    subtitle: 'Dong bo tai khoan giua cac he thong noi bo',
    progress: 0.18,
    memberCount: 2,
  ),
];

const _demoTasks = [
  _DemoTask(
    title: 'Hop voi khach hang ve yeu cau moi',
    projectName: 'Ung dung di dong BrewTask',
    timeLabel: '09:00',
    highPriority: true,
    isToday: true,
  ),
  _DemoTask(
    title: 'Duyet giao dien man hinh Home',
    projectName: 'Ung dung di dong BrewTask',
    timeLabel: '14:30',
    highPriority: false,
    isToday: true,
  ),
  _DemoTask(
    title: 'Bao cao tien do tuan cho Quan ly To',
    projectName: 'Trang quan ly du an',
    timeLabel: '17:00',
    highPriority: true,
    isToday: true,
  ),
  _DemoTask(
    title: 'Kiem thu chuc nang dang nhap',
    projectName: 'Cong thong tin nhan su',
    timeLabel: 'Thu Hai',
    highPriority: false,
    isToday: false,
  ),
];

class DashboardScreen extends StatefulWidget {
  const DashboardScreen({super.key});

  @override
  State<DashboardScreen> createState() => _DashboardScreenState();
}

enum _TaskFilter { all, today, upcoming }

class _DashboardScreenState extends State<DashboardScreen> {
  _TaskFilter _filter = _TaskFilter.all;
  bool _showTodayBanner = true;

  int get _todayCount => _demoTasks.where((t) => t.isToday).length;

  List<_DemoTask> get _filteredTasks {
    switch (_filter) {
      case _TaskFilter.today:
        return _demoTasks.where((t) => t.isToday).toList();
      case _TaskFilter.upcoming:
        return _demoTasks.where((t) => !t.isToday).toList();
      case _TaskFilter.all:
        return _demoTasks;
    }
  }

  @override
  Widget build(BuildContext context) {
    final displayName = context.watch<AuthProvider>().displayName ?? 'ban';

    return AppBottomNav(
      currentIndex: 0,
      body: SafeArea(
        child: ListView(
          padding: const EdgeInsets.fromLTRB(20, 16, 20, 24),
          children: [
            _Header(
                displayName: displayName, projectCount: _demoProjects.length),
            if (_showTodayBanner) ...[
              const SizedBox(height: 16),
              _TodayBanner(
                count: _todayCount,
                onClose: () => setState(() => _showTodayBanner = false),
              ),
            ],
            const SizedBox(height: 20),
            _FilterRow(
              current: _filter,
              totalCount: _demoTasks.length,
              onChanged: (f) => setState(() => _filter = f),
            ),
            const SizedBox(height: 16),
            GridView.builder(
              shrinkWrap: true,
              physics: const NeverScrollableScrollPhysics(),
              itemCount: _demoProjects.length,
              gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
                crossAxisCount: 2,
                mainAxisSpacing: 12,
                crossAxisSpacing: 12,
                childAspectRatio: 0.85,
              ),
              itemBuilder: (context, index) =>
                  _ProjectCard(project: _demoProjects[index]),
            ),
            const SizedBox(height: 24),
            const Text(
              'Cong viec',
              style: TextStyle(
                  fontSize: 17,
                  fontWeight: FontWeight.w700,
                  color: Colors.black87),
            ),
            const SizedBox(height: 10),
            for (final task in _filteredTasks) _TaskRow(task: task),
          ],
        ),
      ),
    );
  }
}

class _Header extends StatelessWidget {
  const _Header({required this.displayName, required this.projectCount});

  final String displayName;
  final int projectCount;

  @override
  Widget build(BuildContext context) {
    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                'Chao, $displayName!',
                style: const TextStyle(
                    fontSize: 13,
                    color: Colors.black54,
                    fontWeight: FontWeight.w500),
              ),
              const SizedBox(height: 4),
              Text(
                'Ban co $projectCount du an',
                style: const TextStyle(
                    fontSize: 24,
                    fontWeight: FontWeight.w800,
                    color: Colors.black87),
              ),
            ],
          ),
        ),
        _CircleIconButton(
          icon: Icons.notifications_outlined,
          onTap: () => Nav.toNamed(context, AppRoutes.notifications),
        ),
      ],
    );
  }
}

class _CircleIconButton extends StatelessWidget {
  const _CircleIconButton({required this.icon, required this.onTap});

  final IconData icon;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(24),
      child: Container(
        width: 44,
        height: 44,
        decoration: BoxDecoration(
          color: AppTheme.brandBlue.withValues(alpha: 0.08),
          shape: BoxShape.circle,
        ),
        child: Icon(icon, color: AppTheme.brandBlue, size: 22),
      ),
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
          const Icon(Icons.event_available_outlined,
              color: Colors.white, size: 20),
          const SizedBox(width: 10),
          Expanded(
            child: Text(
              'Ban co $count cong viec hom nay',
              style: const TextStyle(
                  color: Colors.white,
                  fontWeight: FontWeight.w600,
                  fontSize: 13.5),
            ),
          ),
          InkWell(
            onTap: onClose,
            borderRadius: BorderRadius.circular(12),
            child: const Padding(
              padding: EdgeInsets.all(4),
              child: Icon(Icons.close, color: Colors.white70, size: 18),
            ),
          ),
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
          label: 'Tat ca ($totalCount)',
          selected: current == _TaskFilter.all,
          onTap: () => onChanged(_TaskFilter.all),
        ),
        const SizedBox(width: 8),
        _FilterChip(
          label: 'Hom nay',
          selected: current == _TaskFilter.today,
          onTap: () => onChanged(_TaskFilter.today),
        ),
        const SizedBox(width: 8),
        _FilterChip(
          label: 'Sap toi',
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
        child: Text(
          label,
          style: TextStyle(
            fontSize: 12.5,
            fontWeight: FontWeight.w600,
            color: selected ? Colors.white : Colors.black54,
          ),
        ),
      ),
    );
  }
}

class _ProjectCard extends StatelessWidget {
  const _ProjectCard({required this.project});

  final _DemoProject project;

  @override
  Widget build(BuildContext context) {
    return InkWell(
      onTap: () => Nav.toNamed(context, AppRoutes.projects),
      borderRadius: BorderRadius.circular(16),
      child: Container(
        padding: const EdgeInsets.all(12),
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(16),
          border: Border.all(color: Colors.black.withValues(alpha: 0.06)),
          boxShadow: [
            BoxShadow(
              color: AppTheme.brandBlue.withValues(alpha: 0.06),
              blurRadius: 10,
              offset: const Offset(0, 4),
            ),
          ],
        ),
        // MainAxisSize.min + khong dung Spacer: chieu cao card do NOI DUNG quyet dinh, khong
        // ep vao mot chieu cao co dinh cua GridView — tranh tai dien loi tran khi font he thong
        // nguoi dung to hon binh thuong (Text Scale) hoac ten du an dai hai dong.
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
              child: const Icon(Icons.folder_outlined,
                  color: Colors.white, size: 16),
            ),
            const SizedBox(height: 8),
            Text(
              project.title,
              maxLines: 2,
              overflow: TextOverflow.ellipsis,
              style: const TextStyle(
                  fontSize: 13,
                  fontWeight: FontWeight.w700,
                  color: Colors.black87),
            ),
            const SizedBox(height: 3),
            Text(
              project.subtitle,
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
              style: const TextStyle(fontSize: 11, color: Colors.black45),
            ),
            const SizedBox(height: 8),
            _AvatarStack(count: project.memberCount),
            const SizedBox(height: 8),
            ClipRRect(
              borderRadius: BorderRadius.circular(6),
              child: LinearProgressIndicator(
                value: project.progress,
                minHeight: 6,
                backgroundColor: AppTheme.brandBlue.withValues(alpha: 0.1),
                valueColor:
                    const AlwaysStoppedAnimation(AppTheme.brandBlueDark),
              ),
            ),
            const SizedBox(height: 4),
            Text(
              '${(project.progress * 100).round()}%',
              style: const TextStyle(
                  fontSize: 11,
                  fontWeight: FontWeight.w700,
                  color: AppTheme.brandBlue),
            ),
          ],
        ),
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
                child: Text(
                  '+${count - 3}',
                  style: const TextStyle(
                      fontSize: 9,
                      fontWeight: FontWeight.w700,
                      color: Colors.black54),
                ),
              ),
            ),
        ],
      ),
    );
  }
}

class _TaskRow extends StatelessWidget {
  const _TaskRow({required this.task});

  final _DemoTask task;

  @override
  Widget build(BuildContext context) {
    return Container(
      margin: const EdgeInsets.only(bottom: 10),
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: Colors.black.withValues(alpha: 0.06)),
      ),
      child: Row(
        children: [
          Container(
            width: 8,
            height: 40,
            decoration: BoxDecoration(
              color: task.highPriority
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
                Text(
                  task.title,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: const TextStyle(
                      fontSize: 13.5,
                      fontWeight: FontWeight.w600,
                      color: Colors.black87),
                ),
                const SizedBox(height: 3),
                Text(
                  task.projectName,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: const TextStyle(fontSize: 11.5, color: Colors.black45),
                ),
              ],
            ),
          ),
          const SizedBox(width: 8),
          Column(
            crossAxisAlignment: CrossAxisAlignment.end,
            children: [
              Text(
                task.timeLabel,
                style: const TextStyle(
                    fontSize: 11.5,
                    fontWeight: FontWeight.w600,
                    color: AppTheme.brandBlue),
              ),
              if (task.highPriority) ...[
                const SizedBox(height: 4),
                Container(
                  padding:
                      const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
                  decoration: BoxDecoration(
                    color: AppTheme.brandBlue.withValues(alpha: 0.1),
                    borderRadius: BorderRadius.circular(4),
                  ),
                  child: const Text(
                    'Uu tien cao',
                    style: TextStyle(
                        fontSize: 9.5,
                        fontWeight: FontWeight.w700,
                        color: AppTheme.brandBlue),
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
