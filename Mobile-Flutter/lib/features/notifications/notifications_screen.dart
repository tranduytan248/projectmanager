import 'dart:async';

import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import 'package:phosphor_icons/phosphor_icons.dart';

import '../../config/app_theme.dart';
import '../../core/classes/route_manager.dart';
import '../../core/theme/app_colors.dart';
import '../../core/theme/app_text_styles.dart';
import '../../core/utils/toast_service.dart';
import '../../core/widgets/app_app_bar.dart';
import '../../core/widgets/app_button.dart';
import '../../core/widgets/app_card.dart';
import '../../core/widgets/app_error_state.dart';
import '../../core/widgets/app_icon_button.dart';
import '../../core/widgets/app_loading.dart';
import '../../core/widgets/app_text.dart';
import '../../shared/widgets/app_bottom_nav.dart';
import '../app_routes.dart';
import 'notification_models.dart';
import 'notifications_service.dart';

/// Man "Thong bao" — tuong duong UserNotificationsController ben web nhung la mot man rieng co
/// the cuon/tai them (khong phai khung tha xuong gioi han so dong). Bam vao mot dong: danh dau
/// da doc RIENG dong do roi dieu huong toi noi tuong ung, dung y luat UserNotificationsController.Open
/// ben web (uu tien loai thong bao, roi ProjectId, roi TaskId, cuoi cung roi ve "Cong viec").
class NotificationsScreen extends StatefulWidget {
  const NotificationsScreen({super.key});

  @override
  State<NotificationsScreen> createState() => _NotificationsScreenState();
}

class _NotificationsScreenState extends State<NotificationsScreen> {
  final _service = NotificationsService();

  List<NotificationItem> _items = [];
  int _page = 1;
  bool _hasMore = false;
  int _unreadCount = 0;

  bool _loading = true;
  bool _loadingMore = false;
  bool _hasError = false;

  @override
  void initState() {
    super.initState();
    _loadInitial();
  }

  Future<void> _loadInitial() async {
    setState(() {
      _loading = true;
      _hasError = false;
    });

    try {
      final result = await _service.fetch(page: 1);
      if (!mounted) return;
      setState(() {
        _items = result.items;
        _page = result.page;
        _hasMore = result.hasMore;
        _unreadCount = result.unreadCount;
        _loading = false;
      });
    } catch (_) {
      if (!mounted) return;
      setState(() {
        _loading = false;
        _hasError = true;
      });
    }
  }

  Future<void> _loadMore() async {
    if (_loadingMore || !_hasMore) return;

    setState(() => _loadingMore = true);
    try {
      final result = await _service.fetch(page: _page + 1);
      if (!mounted) return;
      setState(() {
        _items = [..._items, ...result.items];
        _page = result.page;
        _hasMore = result.hasMore;
        _loadingMore = false;
      });
    } catch (_) {
      if (!mounted) return;
      setState(() => _loadingMore = false);
      ToastService.show('Không tải thêm được. Hãy thử lại.');
    }
  }

  Future<void> _markAllRead() async {
    final unreadIds = _items.where((n) => !n.isRead).map((n) => n.id).toSet();

    setState(() {
      _items = _items.map((n) => n.isRead ? n : n.markedRead()).toList();
      _unreadCount = 0;
    });

    try {
      await _service.markAllRead();
    } catch (_) {
      if (!mounted) return;
      // Loi thi khoi phuc lai dung nhung dong TRUOC do chua doc, tranh sai lech voi may chu.
      setState(() {
        _items = _items
            .map((n) => unreadIds.contains(n.id)
                ? NotificationItem(
                    id: n.id,
                    type: n.type,
                    message: n.message,
                    projectId: n.projectId,
                    taskId: n.taskId,
                    isRead: false,
                    createdAt: n.createdAt,
                  )
                : n)
            .toList();
        _unreadCount = unreadIds.length;
      });
      ToastService.show('Không đánh dấu đã đọc được. Hãy thử lại.');
    }
  }

  Future<void> _openNotification(NotificationItem item) async {
    if (!item.isRead) {
      setState(() {
        _items =
            _items.map((n) => n.id == item.id ? n.markedRead() : n).toList();
        _unreadCount = _unreadCount > 0 ? _unreadCount - 1 : 0;
      });
      unawaited(_service.markRead(item.id));
    }

    if (!mounted) return;

    // Y het thu tu UserNotificationsController.Open ben web.
    if (item.type == NotificationTypes.projectAdded ||
        item.type == NotificationTypes.projectRemoved) {
      Nav.toNamed(context, AppRoutes.projects);
    } else if (item.type == NotificationTypes.leaveRequested) {
      Nav.toNamed(context, AppRoutes.teamLeaveApprovals);
    } else if (item.type == NotificationTypes.leaveResult) {
      Nav.toNamed(context, AppRoutes.leaves);
    } else if (item.projectId > 0) {
      Nav.toNamed(context, AppRoutes.checklist,
          arguments: {'projectId': item.projectId.toString()});
    } else if (item.taskId > 0) {
      Nav.toNamed(context, AppRoutes.taskDetail,
          arguments: {'taskId': item.taskId.toString()});
    } else {
      Nav.toNamed(context, AppRoutes.myWork);
    }
  }

  @override
  Widget build(BuildContext context) {
    return AppBottomNav(
      // Man Thong bao khong nam trong 4 tab chinh (chi mo qua chuong thong bao tren Dashboard),
      // nen dung chi so ngoai pham vi 4 tab de khong tab nao bi to sang nham.
      currentIndex: 4,
      appBar: AppAppBar(
        title: 'Thông báo',
        actions: [
          if (_unreadCount > 0)
            AppIconButton(
              icon: PhosphorIconsRegular.checks,
              tooltip: 'Đánh dấu đã đọc tất cả',
              onPressed: _markAllRead,
            ),
        ],
      ),
      body: _buildBody(),
    );
  }

  Widget _buildBody() {
    if (_loading) {
      return const Center(child: AppLoading());
    }

    if (_hasError) {
      return AppErrorState(
          message: 'Không tải được thông báo.', onRetry: _loadInitial);
    }

    if (_items.isEmpty) {
      return const Center(
        child: Padding(
          padding: EdgeInsets.all(24),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              PhosphorIcon(PhosphorIconsRegular.bellSlash,
                  color: AppColors.textFaint, size: 32),
              SizedBox(height: 12),
              AppText(
                'Chưa có thông báo nào. Khi bạn được thêm vào dự án, được nhắc tên hay có việc '
                'sắp đến hạn, thông báo sẽ hiện ở đây.',
                variant: AppTextVariant.body,
                color: AppColors.textSecondary,
                align: TextAlign.center,
              ),
            ],
          ),
        ),
      );
    }

    return ListView.separated(
      padding: const EdgeInsets.fromLTRB(16, 12, 16, 20),
      itemCount: _items.length + 1,
      separatorBuilder: (context, index) => const SizedBox(height: 8),
      itemBuilder: (context, index) {
        if (index == _items.length) {
          return _LoadMoreFooter(
            hasMore: _hasMore,
            loading: _loadingMore,
            onPressed: _loadMore,
          );
        }

        final item = _items[index];
        return _NotificationRow(
            item: item, onTap: () => _openNotification(item));
      },
    );
  }
}

/// Chan danh sach: nut "Tai them" khi con trang sau, khong hien gi khi da het (khong lap lai
/// dong "Da het thong bao" gay roi mat cho danh sach ngan).
class _LoadMoreFooter extends StatelessWidget {
  const _LoadMoreFooter(
      {required this.hasMore, required this.loading, required this.onPressed});

  final bool hasMore;
  final bool loading;
  final VoidCallback onPressed;

  @override
  Widget build(BuildContext context) {
    if (!hasMore) return const SizedBox.shrink();

    return Padding(
      padding: const EdgeInsets.only(top: 8),
      child: Center(
        child: loading
            ? const AppLoading(size: 28)
            : AppButton(
                type: AppButtonType.outline,
                label: 'Tải thêm',
                onPressed: onPressed,
              ),
      ),
    );
  }
}

Color _iconColorOf(String type) {
  switch (type) {
    case NotificationTypes.projectAdded:
      return AppTheme.statusSuccess;
    case NotificationTypes.projectRemoved:
      return AppTheme.statusDanger;
    case NotificationTypes.mentioned:
      return AppTheme.brandBlue;
    case NotificationTypes.commentAdded:
      return AppTheme.brandBlue;
    case NotificationTypes.taskAssigned:
    case NotificationTypes.projectTaskAssigned:
      return AppTheme.statusWarning;
    default:
      return AppColors.textSecondary;
  }
}

IconData _iconOf(String type) {
  switch (type) {
    case NotificationTypes.projectAdded:
      return PhosphorIconsRegular.plusCircle;
    case NotificationTypes.projectRemoved:
      return PhosphorIconsRegular.minusCircle;
    case NotificationTypes.mentioned:
      return PhosphorIconsRegular.at;
    case NotificationTypes.commentAdded:
      return PhosphorIconsRegular.chatCircle;
    case NotificationTypes.taskAssigned:
    case NotificationTypes.projectTaskAssigned:
      return PhosphorIconsRegular.star;
    case NotificationTypes.leaveRequested:
    case NotificationTypes.leaveResult:
      return PhosphorIconsRegular.calendarCheck;
    default:
      return PhosphorIconsRegular.clock;
  }
}

class _NotificationRow extends StatelessWidget {
  const _NotificationRow({required this.item, required this.onTap});

  final NotificationItem item;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final color = _iconColorOf(item.type);

    return AppCard(
      onTap: onTap,
      padding: const EdgeInsets.all(12),
      radius: 14,
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Container(
            width: 36,
            height: 36,
            alignment: Alignment.center,
            decoration: BoxDecoration(
              color: color.withValues(alpha: 0.1),
              shape: BoxShape.circle,
            ),
            child: PhosphorIcon(_iconOf(item.type), size: 18, color: color),
          ),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                AppText(
                  item.message,
                  variant: AppTextVariant.body,
                  fontSize: 13.5,
                  weight: item.isRead ? FontWeight.w500 : FontWeight.w700,
                ),
                const SizedBox(height: 4),
                AppText(
                  item.createdAt == null
                      ? ''
                      : DateFormat('HH:mm dd/MM/yyyy').format(item.createdAt!),
                  variant: AppTextVariant.caption,
                  fontSize: 11.5,
                  color: AppColors.textSecondary,
                ),
              ],
            ),
          ),
          if (!item.isRead) ...[
            const SizedBox(width: 8),
            Container(
              width: 8,
              height: 8,
              margin: const EdgeInsets.only(top: 4),
              decoration: const BoxDecoration(
                color: AppTheme.brandBlue,
                shape: BoxShape.circle,
              ),
            ),
          ],
        ],
      ),
    );
  }
}
