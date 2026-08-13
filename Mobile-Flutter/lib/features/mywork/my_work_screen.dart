import 'package:flutter/material.dart';
import 'package:flutter_slidable/flutter_slidable.dart';
import 'package:phosphor_icons/phosphor_icons.dart';

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
    title: 'Dựng màn hình đăng nhập cho ứng dụng di động',
    state: 'Đang làm',
    dueLabel: 'Còn 2 ngày',
    icon: PhosphorIconsRegular.plusCircle,
  ),
  _DemoTask(
    id: 'CV-102',
    code: 'CV-102',
    title: 'Sửa lỗi không tải được danh sách dự án trên mạng chậm',
    state: 'Chờ duyệt',
    dueLabel: 'Còn 5 giờ',
    icon: PhosphorIconsRegular.warningCircle,
  ),
  _DemoTask(
    id: 'CV-098',
    code: 'CV-098',
    title: 'Vì sao không xem được file đính kèm trên iOS?',
    state: 'Đang làm',
    dueLabel: 'Quá hạn 1 ngày',
    icon: PhosphorIconsRegular.question,
  ),
  _DemoTask(
    id: 'CV-095',
    code: 'CV-095',
    title: 'Kết nối API chấm công với máy quét vân tay',
    state: 'Chờ duyệt',
    dueLabel: 'Còn 3 ngày',
    icon: PhosphorIconsRegular.warningCircle,
  ),
  _DemoTask(
    id: 'CV-090',
    code: 'CV-090',
    title: 'Báo giá gói triển khai thêm cho chi nhánh mới',
    state: 'Hoàn thành',
    dueLabel: 'Đã xong',
    icon: PhosphorIconsRegular.plusCircle,
  ),
];

class MyWorkScreen extends StatelessWidget {
  const MyWorkScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return AppBottomNav(
      currentIndex: 2,
      // Tieu de luon mau den (quy uoc chung toan app) — nen trang mac dinh cua AppBar thay vi
      // to xanh + chu trang nhu truoc, khop voi cach Dashboard khong dung AppBar mau.
      appBar: AppBar(title: const Text('Việc của tôi')),
      body: ListView.separated(
        itemCount: _demoTasks.length,
        separatorBuilder: (context, index) =>
            const Divider(height: 1, indent: 68),
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
            onPressed: (_) => ToastService.show(
                'Giao việc "${task.code}" — đang phát triển.'),
            backgroundColor: AppTheme.brandBlueDark,
            foregroundColor: Colors.white,
            icon: PhosphorIconsRegular.userPlus,
            label: 'Giao việc',
          ),
          SlidableAction(
            onPressed: (_) => ToastService.show(
                'Chuyển trạng thái "${task.code}" — đang phát triển.'),
            backgroundColor: AppTheme.brandBlue,
            foregroundColor: Colors.white,
            icon: PhosphorIconsRegular.arrowsClockwise,
            label: 'Xử lý',
          ),
        ],
      ),
      child: InkWell(
        onTap: () => Nav.toNamed(context, AppRoutes.taskDetail,
            arguments: {'taskId': task.id}),
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
          child: Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Container(
                width: 36,
                height: 36,
                decoration: BoxDecoration(
                  color: AppTheme.brandBlueDark,
                  borderRadius: BorderRadius.circular(8),
                ),
                child: PhosphorIcon(task.icon, color: Colors.white, size: 20),
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
                          style: const TextStyle(
                              fontSize: 12.5, color: Colors.black54),
                        ),
                        _StateBadge(state: task.state),
                        Text(
                          task.dueLabel,
                          style: const TextStyle(
                            fontSize: 12.5,
                            color: AppTheme.brandBlueDark,
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

/// Nhan trang thai kieu vien mo — mau theo dung ngu nghia (xong = xanh la, cho duyet = vang
/// canh bao), thay vi to mot mau brand cho moi trang thai nhu truoc, theo quy uoc site.css
/// (mau thuong hieu CHI danh cho thao tac, khong dung cho du lieu).
class _StateBadge extends StatelessWidget {
  const _StateBadge({required this.state});

  final String state;

  Color get _color {
    switch (state) {
      case 'Hoàn thành':
        return AppTheme.statusSuccess;
      case 'Chờ duyệt':
        return AppTheme.statusWarning;
      default:
        return AppTheme.brandBlueDark;
    }
  }

  @override
  Widget build(BuildContext context) {
    final color = _color;

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
      decoration: BoxDecoration(
        border: Border.all(color: color.withValues(alpha: 0.5)),
        borderRadius: BorderRadius.circular(4),
      ),
      child: Text(
        state.toUpperCase(),
        style: TextStyle(
          fontSize: 10.5,
          fontWeight: FontWeight.w700,
          color: color,
          letterSpacing: 0.3,
        ),
      ),
    );
  }
}
