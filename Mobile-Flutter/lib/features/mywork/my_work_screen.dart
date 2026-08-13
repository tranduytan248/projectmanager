import 'package:flutter/material.dart';
import 'package:flutter_slidable/flutter_slidable.dart';

import '../../config/app_theme.dart';
import '../../core/classes/route_manager.dart';
import '../../core/utils/toast_service.dart';
import '../../shared/widgets/app_bottom_nav.dart';
import '../app_routes.dart';

/// Mot dong cong viec hien thi tam thoi, cho toi khi noi that API GET /api/mywork. Cau truc
/// khop voi WorkTask ben web (Code/Title/State/DueDate...) de sau nay thay bang model that chi
/// can doi nguon du lieu, khong can doi lai UI.
class _DemoTask {
  const _DemoTask({
    required this.id,
    required this.code,
    required this.title,
    required this.state,
    required this.dueLabel,
    required this.icon,
  });

  final String id;
  final String code;
  final String title;
  final String state;
  final String dueLabel;
  final IconData icon;
}

const _demoTasks = [
  _DemoTask(
    id: 'CV-101',
    code: 'CV-101',
    title: 'Dung man hinh dang nhap cho ung dung di dong',
    state: 'Dang lam',
    dueLabel: 'Con 2 ngay',
    icon: Icons.add_circle_outline,
  ),
  _DemoTask(
    id: 'CV-102',
    code: 'CV-102',
    title: 'Sua loi khong tai duoc danh sach du an tren mang cham',
    state: 'Cho duyet',
    dueLabel: 'Con 5 gio',
    icon: Icons.error_outline,
  ),
  _DemoTask(
    id: 'CV-098',
    code: 'CV-098',
    title: 'Vi sao khong xem duoc file dinh kem tren iOS?',
    state: 'Dang lam',
    dueLabel: 'Qua han 1 ngay',
    icon: Icons.help_outline,
  ),
  _DemoTask(
    id: 'CV-095',
    code: 'CV-095',
    title: 'Ket noi API cham cong voi may quet van tay',
    state: 'Cho duyet',
    dueLabel: 'Con 3 ngay',
    icon: Icons.error_outline,
  ),
  _DemoTask(
    id: 'CV-090',
    code: 'CV-090',
    title: 'Bao gia goi trien khai them cho chi nhanh moi',
    state: 'Hoan thanh',
    dueLabel: 'Da xong',
    icon: Icons.add_circle_outline,
  ),
];

class MyWorkScreen extends StatelessWidget {
  const MyWorkScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return AppBottomNav(
      currentIndex: 1,
      appBar: AppBar(
        title: const Text('Viec cua toi'),
        backgroundColor: AppTheme.brandBlue,
        foregroundColor: Colors.white,
      ),
      body: ListView.separated(
        itemCount: _demoTasks.length,
        separatorBuilder: (context, index) => const Divider(height: 1, indent: 68),
        itemBuilder: (context, index) => _TaskRow(task: _demoTasks[index]),
      ),
    );
  }
}

/// Mot dong cong viec: vuot sang trai lo hai thao tac nhanh (Giao viec / Xu ly), giong danh
/// sach issue cua Jira mobile — nhung chi dung xanh thuong hieu + trang, khong dung mau khac.
class _TaskRow extends StatelessWidget {
  const _TaskRow({required this.task});

  final _DemoTask task;

  @override
  Widget build(BuildContext context) {
    return Slidable(
      key: ValueKey(task.id),
      endActionPane: ActionPane(
        motion: const DrawerMotion(),
        extentRatio: 0.5,
        children: [
          SlidableAction(
            onPressed: (_) => ToastService.show('Giao viec "${task.code}" — dang phat trien.'),
            backgroundColor: AppTheme.brandBlueLight,
            foregroundColor: Colors.white,
            icon: Icons.person_add_alt,
            label: 'Giao viec',
          ),
          SlidableAction(
            onPressed: (_) => ToastService.show('Chuyen trang thai "${task.code}" — dang phat trien.'),
            backgroundColor: AppTheme.brandBlue,
            foregroundColor: Colors.white,
            icon: Icons.sync_alt,
            label: 'Xu ly',
          ),
        ],
      ),
      child: InkWell(
        onTap: () => Nav.toNamed(context, AppRoutes.taskDetail, arguments: {'taskId': task.id}),
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
          child: Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Container(
                width: 36,
                height: 36,
                decoration: BoxDecoration(
                  color: AppTheme.brandBlueLight,
                  borderRadius: BorderRadius.circular(8),
                ),
                child: Icon(task.icon, color: Colors.white, size: 20),
              ),
              const SizedBox(width: 14),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      task.title,
                      style: const TextStyle(
                        fontSize: 15,
                        fontWeight: FontWeight.w500,
                        color: Colors.black87,
                      ),
                      maxLines: 2,
                      overflow: TextOverflow.ellipsis,
                    ),
                    const SizedBox(height: 6),
                    Wrap(
                      spacing: 8,
                      runSpacing: 4,
                      crossAxisAlignment: WrapCrossAlignment.center,
                      children: [
                        Text(
                          task.code,
                          style: const TextStyle(fontSize: 12.5, color: Colors.black54),
                        ),
                        _StateBadge(state: task.state),
                        Text(
                          task.dueLabel,
                          style: const TextStyle(
                            fontSize: 12.5,
                            color: AppTheme.brandBlueLight,
                            fontWeight: FontWeight.w600,
                          ),
                        ),
                      ],
                    ),
                  ],
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

/// Nhan trang thai kieu vien mo, chi dung xanh thuong hieu tren nen trang.
class _StateBadge extends StatelessWidget {
  const _StateBadge({required this.state});

  final String state;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
      decoration: BoxDecoration(
        border: Border.all(color: AppTheme.brandBlueLight.withValues(alpha: 0.5)),
        borderRadius: BorderRadius.circular(4),
      ),
      child: Text(
        state.toUpperCase(),
        style: const TextStyle(
          fontSize: 10.5,
          fontWeight: FontWeight.w700,
          color: AppTheme.brandBlueLight,
          letterSpacing: 0.3,
        ),
      ),
    );
  }
}
