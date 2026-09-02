import 'package:flutter/material.dart';
import 'package:flutter_slidable/flutter_slidable.dart';
import 'package:intl/intl.dart';
import 'package:phosphor_icons/phosphor_icons.dart';

import '../../config/app_theme.dart';
import '../../core/classes/route_manager.dart';
import '../../core/theme/app_colors.dart';
import '../../core/theme/app_dimens.dart';
import '../../core/theme/app_text_styles.dart';
import '../../core/widgets/app_app_bar.dart';
import '../../core/widgets/app_button.dart';
import '../../core/widgets/app_error_state.dart';
import '../../core/widgets/app_icon_button.dart';
import '../../core/widgets/app_loading.dart';
import '../../core/widgets/app_text.dart';
import '../../core/widgets/app_text_field.dart';
import '../../shared/widgets/app_bottom_nav.dart';
import '../app_routes.dart';
import '../dashboard/dashboard_models.dart' show TaskItem, taskStateLabel;
import 'my_work_service.dart';

/// Màn "Công việc": hỗ trợ tìm kiếm nhanh, lọc theo trạng thái (có đếm số lượng),
/// lọc theo dự án và sắp xếp đa tiêu chí.
class MyWorkScreen extends StatefulWidget {
  const MyWorkScreen({super.key, this.scope = 'mine', this.filter});

  final String scope;
  final String? filter;

  @override
  State<MyWorkScreen> createState() => _MyWorkScreenState();
}

class _MyWorkScreenState extends State<MyWorkScreen> {
  final _service = MyWorkService();
  late Future<List<TaskItem>> _future;

  final _searchController = TextEditingController();
  String _selectedStatus = 'all'; // 'all', 'inProgress', 'overdue', 'dueToday', 'notStarted', 'completed'
  String? _selectedProject;
  String _sortOrder = 'dueDateAsc'; // 'dueDateAsc', 'dueDateDesc', 'progressDesc', 'progressAsc', 'titleAsc'

  @override
  void initState() {
    super.initState();
    // Nếu màn hình mở từ dashboard với filter cụ thể (ví dụ overdue)
    if (widget.filter == 'overdue') {
      _selectedStatus = 'overdue';
    } else if (widget.filter == 'open') {
      _selectedStatus = 'inProgress';
    }
    _loadData();
  }

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  void _loadData({bool forceRefresh = false}) {
    _future = _service.fetch(
      scope: widget.scope,
      filter: widget.filter,
      forceRefresh: forceRefresh,
    );
  }

  void _reload({bool forceRefresh = false}) {
    setState(() {
      _loadData(forceRefresh: forceRefresh);
    });
  }

  void _resetFilters() {
    setState(() {
      _searchController.clear();
      _selectedStatus = 'all';
      _selectedProject = null;
      _sortOrder = 'dueDateAsc';
    });
  }

  bool get _hasActiveFilters =>
      _searchController.text.trim().isNotEmpty ||
      _selectedStatus != 'all' ||
      _selectedProject != null ||
      _sortOrder != 'dueDateAsc';

  String get _title {
    if (widget.scope != 'team') return 'Việc của tôi';
    if (widget.filter == 'overdue') return 'Quá hạn — Toàn Tổ';
    if (widget.filter == 'open') return 'Việc chưa xong — Toàn Tổ';
    return 'Công việc — Toàn Tổ';
  }

  List<TaskItem> _filterTasks(List<TaskItem> tasks) {
    final query = _searchController.text.trim().toLowerCase();

    var list = tasks.where((task) {
      // 1. Tìm kiếm theo tên, mã việc hoặc dự án
      if (query.isNotEmpty) {
        final title = task.title.toLowerCase();
        final code = task.code.toLowerCase();
        final project = (task.projectName ?? '').toLowerCase();
        if (!title.contains(query) && !code.contains(query) && !project.contains(query)) {
          return false;
        }
      }

      // 2. Lọc theo trạng thái
      switch (_selectedStatus) {
        case 'inProgress':
          if (task.state != 'DangLam') return false;
          break;
        case 'overdue':
          if (!task.isOverdue || task.state == 'HoanThanh' || task.state == 'Huy') return false;
          break;
        case 'dueToday':
          if (!task.isDueToday || task.state == 'HoanThanh' || task.state == 'Huy') return false;
          break;
        case 'notStarted':
          if (task.state != 'ChuaBatDau') return false;
          break;
        case 'completed':
          if (task.state != 'HoanThanh') return false;
          break;
        default:
          break;
      }

      // 3. Lọc theo dự án
      if (_selectedProject != null && _selectedProject!.isNotEmpty) {
        if (task.projectName != _selectedProject) return false;
      }

      return true;
    }).toList();

    // 4. Sắp xếp danh sách
    list.sort((a, b) {
      switch (_sortOrder) {
        case 'dueDateAsc':
          if (a.dueDate == null && b.dueDate == null) return 0;
          if (a.dueDate == null) return 1;
          if (b.dueDate == null) return -1;
          return a.dueDate!.compareTo(b.dueDate!);
        case 'dueDateDesc':
          if (a.dueDate == null && b.dueDate == null) return 0;
          if (a.dueDate == null) return 1;
          if (b.dueDate == null) return -1;
          return b.dueDate!.compareTo(a.dueDate!);
        case 'progressDesc':
          return b.progress.compareTo(a.progress);
        case 'progressAsc':
          return a.progress.compareTo(b.progress);
        case 'titleAsc':
          return a.title.toLowerCase().compareTo(b.title.toLowerCase());
        default:
          return 0;
      }
    });

    return list;
  }

  void _openProjectFilterSheet(List<String> projects) {
    showModalBottomSheet<void>(
      context: context,
      backgroundColor: AppColors.surface,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(AppDimens.radiusLg)),
      ),
      builder: (context) {
        return SafeArea(
          child: Padding(
            padding: const EdgeInsets.symmetric(
              horizontal: AppDimens.space16,
              vertical: AppDimens.space16,
            ),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    const AppText(
                      'Lọc theo Dự án',
                      variant: AppTextVariant.title,
                      fontSize: 16,
                      weight: FontWeight.w700,
                    ),
                    InkWell(
                      onTap: () => Navigator.of(context).pop(),
                      child: const Icon(Icons.close, size: 20, color: AppColors.textSecondary),
                    ),
                  ],
                ),
                const SizedBox(height: AppDimens.space12),
                ListTile(
                  contentPadding: EdgeInsets.zero,
                  leading: Icon(
                    _selectedProject == null ? Icons.radio_button_checked : Icons.radio_button_off,
                    color: _selectedProject == null ? AppColors.primary : AppColors.textSecondary,
                  ),
                  title: const AppText('Tất cả dự án', variant: AppTextVariant.body),
                  onTap: () {
                    setState(() => _selectedProject = null);
                    Navigator.of(context).pop();
                  },
                ),
                const Divider(height: 1, color: AppColors.border),
                Expanded(
                  child: ListView.builder(
                    itemCount: projects.length,
                    itemBuilder: (context, index) {
                      final p = projects[index];
                      final isSelected = _selectedProject == p;
                      return ListTile(
                        contentPadding: EdgeInsets.zero,
                        leading: Icon(
                          isSelected ? Icons.radio_button_checked : Icons.radio_button_off,
                          color: isSelected ? AppColors.primary : AppColors.textSecondary,
                        ),
                        title: AppText(p, variant: AppTextVariant.body, maxLines: 1),
                        onTap: () {
                          setState(() => _selectedProject = p);
                          Navigator.of(context).pop();
                        },
                      );
                    },
                  ),
                ),
              ],
            ),
          ),
        );
      },
    );
  }

  void _openSortSheet() {
    final sortOptions = [
      {'key': 'dueDateAsc', 'label': 'Hạn chót: Gần nhất trước'},
      {'key': 'dueDateDesc', 'label': 'Hạn chót: Xa nhất trước'},
      {'key': 'progressDesc', 'label': 'Tiến độ: Cao đến thấp'},
      {'key': 'progressAsc', 'label': 'Tiến độ: Thấp đến cao'},
      {'key': 'titleAsc', 'label': 'Tên công việc: A → Z'},
    ];

    showModalBottomSheet<void>(
      context: context,
      backgroundColor: AppColors.surface,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(AppDimens.radiusLg)),
      ),
      builder: (context) {
        return SafeArea(
          child: Padding(
            padding: const EdgeInsets.symmetric(
              horizontal: AppDimens.space16,
              vertical: AppDimens.space16,
            ),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    const AppText(
                      'Sắp xếp công việc',
                      variant: AppTextVariant.title,
                      fontSize: 16,
                      weight: FontWeight.w700,
                    ),
                    InkWell(
                      onTap: () => Navigator.of(context).pop(),
                      child: const Icon(Icons.close, size: 20, color: AppColors.textSecondary),
                    ),
                  ],
                ),
                const SizedBox(height: AppDimens.space12),
                for (final opt in sortOptions) ...[
                  ListTile(
                    contentPadding: EdgeInsets.zero,
                    leading: Icon(
                      _sortOrder == opt['key'] ? Icons.radio_button_checked : Icons.radio_button_off,
                      color: _sortOrder == opt['key'] ? AppColors.primary : AppColors.textSecondary,
                    ),
                    title: AppText(opt['label']!, variant: AppTextVariant.body),
                    onTap: () {
                      setState(() => _sortOrder = opt['key']!);
                      Navigator.of(context).pop();
                    },
                  ),
                  const Divider(height: 1, color: AppColors.border),
                ],
              ],
            ),
          ),
        );
      },
    );
  }

  String _sortLabel(String key) {
    switch (key) {
      case 'dueDateDesc':
        return 'Hạn xa';
      case 'progressDesc':
        return 'Tiến độ cao';
      case 'progressAsc':
        return 'Tiến độ thấp';
      case 'titleAsc':
        return 'Tên A-Z';
      default:
        return 'Hạn gần';
    }
  }

  @override
  Widget build(BuildContext context) {
    return AppBottomNav(
      currentIndex: 2,
      appBar: AppAppBar(
        title: _title,
        actions: [
          AppIconButton(
            icon: Icons.refresh,
            tooltip: 'Làm mới',
            onPressed: _reload,
          ),
        ],
      ),
      body: FutureBuilder<List<TaskItem>>(
        future: _future,
        initialData: _service.getCached(scope: widget.scope, filter: widget.filter),
        builder: (context, snapshot) {
          if (snapshot.data == null && snapshot.connectionState != ConnectionState.done) {
            return const Center(child: AppLoading());
          }

          if (snapshot.data == null && snapshot.hasError) {
            return AppErrorState(
              message: 'Không tải được danh sách công việc.',
              onRetry: () => _reload(forceRefresh: true),
            );
          }

          final allTasks = snapshot.data ?? [];
          final filteredTasks = _filterTasks(allTasks);

          // Lấy danh sách các dự án duy nhất
          final projects = allTasks
              .map((t) => t.projectName ?? '')
              .where((p) => p.isNotEmpty)
              .toSet()
              .toList()
            ..sort();

          final countInProgress = allTasks.where((t) => t.state == 'DangLam').length;
          final countOverdue = allTasks.where((t) => t.isOverdue && t.state != 'HoanThanh' && t.state != 'Huy').length;
          final countDueToday = allTasks.where((t) => t.isDueToday && t.state != 'HoanThanh' && t.state != 'Huy').length;
          final countNotStarted = allTasks.where((t) => t.state == 'ChuaBatDau').length;
          final countCompleted = allTasks.where((t) => t.state == 'HoanThanh').length;

          return RefreshIndicator(
            onRefresh: () async => _reload(forceRefresh: true),
            color: AppColors.primary,
            child: Column(
              children: [
                // Khối Thanh Tìm kiếm & Bộ Lọc
                Container(
                  color: AppColors.surface,
                  padding: const EdgeInsets.fromLTRB(
                    AppDimens.space16,
                    AppDimens.space12,
                    AppDimens.space16,
                    AppDimens.space8,
                  ),
                  child: Column(
                    children: [
                      // 1. Ô tìm kiếm
                      AppTextField(
                        label: 'Tìm kiếm công việc',
                        hint: 'Nhập tên việc, mã việc, dự án...',
                        controller: _searchController,
                        prefixIcon: PhosphorIconsRegular.magnifyingGlass,
                        suffixIcon: _searchController.text.isNotEmpty ? Icons.clear : null,
                        onChanged: (_) => setState(() {}),
                        onTap: () {
                          if (_searchController.text.isNotEmpty) {
                            setState(() => _searchController.clear());
                          }
                        },
                      ),
                      const SizedBox(height: AppDimens.space8),

                      // 2. Hàng Chip lọc trạng thái
                      SingleChildScrollView(
                        scrollDirection: Axis.horizontal,
                        child: Row(
                          children: [
                            _FilterPill(
                              label: 'Tất cả (${allTasks.length})',
                              isSelected: _selectedStatus == 'all',
                              onTap: () => setState(() => _selectedStatus = 'all'),
                            ),
                            const SizedBox(width: AppDimens.space8),
                            _FilterPill(
                              label: 'Đang làm ($countInProgress)',
                              isSelected: _selectedStatus == 'inProgress',
                              color: AppColors.primary,
                              onTap: () => setState(() => _selectedStatus = 'inProgress'),
                            ),
                            const SizedBox(width: AppDimens.space8),
                            _FilterPill(
                              label: 'Quá hạn ($countOverdue)',
                              isSelected: _selectedStatus == 'overdue',
                              color: AppColors.danger,
                              onTap: () => setState(() => _selectedStatus = 'overdue'),
                            ),
                            const SizedBox(width: AppDimens.space8),
                            _FilterPill(
                              label: 'Hôm nay ($countDueToday)',
                              isSelected: _selectedStatus == 'dueToday',
                              color: AppColors.warning,
                              onTap: () => setState(() => _selectedStatus = 'dueToday'),
                            ),
                            const SizedBox(width: AppDimens.space8),
                            _FilterPill(
                              label: 'Chưa làm ($countNotStarted)',
                              isSelected: _selectedStatus == 'notStarted',
                              color: AppColors.textSecondary,
                              onTap: () => setState(() => _selectedStatus = 'notStarted'),
                            ),
                            const SizedBox(width: AppDimens.space8),
                            _FilterPill(
                              label: 'Hoàn thành ($countCompleted)',
                              isSelected: _selectedStatus == 'completed',
                              color: AppColors.success,
                              onTap: () => setState(() => _selectedStatus = 'completed'),
                            ),
                          ],
                        ),
                      ),
                      const SizedBox(height: AppDimens.space8),

                      // 3. Hàng Lọc theo Dự án & Sắp xếp
                      Row(
                        children: [
                          if (projects.isNotEmpty) ...[
                            InkWell(
                              onTap: () => _openProjectFilterSheet(projects),
                              borderRadius: BorderRadius.circular(AppDimens.radiusSm),
                              child: Container(
                                padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
                                decoration: BoxDecoration(
                                  color: _selectedProject != null ? AppColors.primarySoft : AppColors.background,
                                  borderRadius: BorderRadius.circular(AppDimens.radiusSm),
                                  border: Border.all(
                                    color: _selectedProject != null ? AppColors.primary : AppColors.border,
                                  ),
                                ),
                                child: Row(
                                  mainAxisSize: MainAxisSize.min,
                                  children: [
                                    Icon(
                                      PhosphorIconsRegular.folder,
                                      size: 14,
                                      color: _selectedProject != null ? AppColors.primary : AppColors.textSecondary,
                                    ),
                                    const SizedBox(width: 6),
                                    AppText(
                                      _selectedProject != null ? 'Dự án: $_selectedProject' : 'Dự án: Tất cả',
                                      variant: AppTextVariant.caption,
                                      fontSize: 12,
                                      weight: _selectedProject != null ? FontWeight.w700 : FontWeight.w500,
                                      color: _selectedProject != null ? AppColors.primary : AppColors.textPrimary,
                                      maxLines: 1,
                                    ),
                                    const SizedBox(width: 4),
                                    const Icon(Icons.arrow_drop_down, size: 16),
                                  ],
                                ),
                              ),
                            ),
                            const SizedBox(width: AppDimens.space8),
                          ],

                          // Nút Sắp xếp
                          InkWell(
                            onTap: _openSortSheet,
                            borderRadius: BorderRadius.circular(AppDimens.radiusSm),
                            child: Container(
                              padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
                              decoration: BoxDecoration(
                                color: AppColors.background,
                                borderRadius: BorderRadius.circular(AppDimens.radiusSm),
                                border: Border.all(color: AppColors.border),
                              ),
                              child: Row(
                                mainAxisSize: MainAxisSize.min,
                                children: [
                                  const Icon(
                                    PhosphorIconsRegular.sortAscending,
                                    size: 14,
                                    color: AppColors.textSecondary,
                                  ),
                                  const SizedBox(width: 6),
                                  AppText(
                                    'Xếp: ${_sortLabel(_sortOrder)}',
                                    variant: AppTextVariant.caption,
                                    fontSize: 12,
                                    color: AppColors.textPrimary,
                                  ),
                                ],
                              ),
                            ),
                          ),

                          const Spacer(),

                          // Nút Đặt lại bộ lọc nếu có lọc
                          if (_hasActiveFilters)
                            InkWell(
                              onTap: _resetFilters,
                              child: const Padding(
                                padding: EdgeInsets.symmetric(horizontal: 4, vertical: 6),
                                child: Row(
                                  mainAxisSize: MainAxisSize.min,
                                  children: [
                                    Icon(Icons.refresh, size: 14, color: AppColors.primary),
                                    SizedBox(width: 4),
                                    AppText(
                                      'Xóa lọc',
                                      variant: AppTextVariant.caption,
                                      fontSize: 12,
                                      color: AppColors.primary,
                                      weight: FontWeight.w600,
                                    ),
                                  ],
                                ),
                              ),
                            ),
                        ],
                      ),
                    ],
                  ),
                ),

                // Thanh đếm kết quả
                Container(
                  width: double.infinity,
                  padding: const EdgeInsets.symmetric(
                    horizontal: AppDimens.space16,
                    vertical: AppDimens.space8,
                  ),
                  decoration: const BoxDecoration(
                    color: AppColors.background,
                    border: Border(bottom: BorderSide(color: AppColors.border, width: 0.5)),
                  ),
                  child: AppText(
                    'Tìm thấy ${filteredTasks.length} / ${allTasks.length} công việc',
                    variant: AppTextVariant.caption,
                    fontSize: 12,
                    color: AppColors.textSecondary,
                  ),
                ),

                // Danh sách công việc
                Expanded(
                  child: allTasks.isEmpty
                      ? Center(
                          child: AppText(
                            widget.scope == 'team'
                                ? 'Không có việc nào khớp.'
                                : 'Bạn chưa có việc nào.',
                            variant: AppTextVariant.body,
                            color: AppColors.textSecondary,
                          ),
                        )
                      : filteredTasks.isEmpty
                          ? Center(
                              child: Padding(
                                padding: const EdgeInsets.all(AppDimens.space24),
                                child: Column(
                                  mainAxisAlignment: MainAxisAlignment.center,
                                  children: [
                                    const Icon(
                                      PhosphorIconsRegular.funnel,
                                      size: 44,
                                      color: AppColors.textFaint,
                                    ),
                                    const SizedBox(height: AppDimens.space12),
                                    const AppText(
                                      'Không tìm thấy công việc phù hợp',
                                      variant: AppTextVariant.body,
                                      weight: FontWeight.w600,
                                    ),
                                    const SizedBox(height: 4),
                                    const AppText(
                                      'Thử thay đổi từ khóa tìm kiếm hoặc tiêu chí lọc.',
                                      variant: AppTextVariant.caption,
                                      color: AppColors.textSecondary,
                                      align: TextAlign.center,
                                    ),
                                    const SizedBox(height: AppDimens.space16),
                                    AppButton(
                                      label: 'Đặt lại bộ lọc',
                                      type: AppButtonType.secondary,
                                      onPressed: _resetFilters,
                                    ),
                                  ],
                                ),
                              ),
                            )
                          : ListView.separated(
                              itemCount: filteredTasks.length,
                              separatorBuilder: (context, index) =>
                                  const Divider(height: 1, indent: 68),
                              itemBuilder: (context, index) =>
                                  _TaskRow(task: filteredTasks[index]),
                            ),
                ),
              ],
            ),
          );
        },
      ),
    );
  }
}

/// Pill lọc trạng thái
class _FilterPill extends StatelessWidget {
  const _FilterPill({
    required this.label,
    required this.isSelected,
    required this.onTap,
    this.color,
  });

  final String label;
  final bool isSelected;
  final VoidCallback onTap;
  final Color? color;

  @override
  Widget build(BuildContext context) {
    final activeColor = color ?? AppColors.primary;

    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(AppDimens.radiusPill),
      child: AnimatedContainer(
        duration: const Duration(milliseconds: 200),
        padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
        decoration: BoxDecoration(
          color: isSelected ? activeColor.withValues(alpha: 0.15) : AppColors.background,
          borderRadius: BorderRadius.circular(AppDimens.radiusPill),
          border: Border.all(
            color: isSelected ? activeColor : AppColors.border,
            width: isSelected ? 1.5 : 1,
          ),
        ),
        child: AppText(
          label,
          variant: AppTextVariant.caption,
          fontSize: 12,
          weight: isSelected ? FontWeight.w700 : FontWeight.w500,
          color: isSelected ? (color ?? AppColors.primary) : AppColors.textSecondary,
        ),
      ),
    );
  }
}

/// Mot dong cong viec: vuot sang trai lo hai thao tac nhanh (Giao viec / Xu ly), giong danh
/// sach issue cua Jira mobile — nhung chi dung xanh thuong hieu + trang, khong dung mau khac.
class _TaskRow extends StatelessWidget {
  const _TaskRow({required this.task});

  final TaskItem task;

  @override
  Widget build(BuildContext context) {
    return Slidable(
      key: ValueKey(task.id),
      endActionPane: ActionPane(
        motion: const DrawerMotion(),
        extentRatio: 0.5,
        children: [
          SlidableAction(
            onPressed: (ctx) => Nav.toNamed(ctx, AppRoutes.taskDetail,
                arguments: {'taskId': task.id.toString()}),
            backgroundColor: AppColors.primaryDark,
            foregroundColor: AppColors.textOnPrimary,
            icon: PhosphorIconsRegular.userPlus,
            label: 'Giao việc',
          ),
          SlidableAction(
            onPressed: (ctx) => Nav.toNamed(ctx, AppRoutes.taskDetail,
                arguments: {'taskId': task.id.toString()}),
            backgroundColor: AppColors.primary,
            foregroundColor: AppColors.textOnPrimary,
            icon: PhosphorIconsRegular.arrowsClockwise,
            label: 'Xử lý',
          ),
        ],
      ),
      child: InkWell(
        onTap: () => Nav.toNamed(context, AppRoutes.taskDetail,
            arguments: {'taskId': task.id.toString()}),
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
          child: Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Container(
                width: 36,
                height: 36,
                decoration: BoxDecoration(
                  color: AppColors.primaryDark,
                  borderRadius: BorderRadius.circular(8),
                ),
                child: PhosphorIcon(
                  task.isOverdue
                      ? PhosphorIconsRegular.warningCircle
                      : PhosphorIconsRegular.plusCircle,
                  color: AppColors.textOnPrimary,
                  size: 20,
                ),
              ),
              const SizedBox(width: 14),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    AppText(
                      task.title,
                      variant: AppTextVariant.body,
                      fontSize: 15,
                      weight: FontWeight.w500,
                      maxLines: 2,
                      overflow: TextOverflow.ellipsis,
                    ),
                    const SizedBox(height: 6),
                    Wrap(
                      spacing: 8,
                      runSpacing: 4,
                      crossAxisAlignment: WrapCrossAlignment.center,
                      children: [
                        if (task.code.isNotEmpty)
                          AppText(
                            task.code,
                            variant: AppTextVariant.caption,
                            fontSize: 12.5,
                            color: AppColors.textSecondary,
                          ),
                        if (task.projectName?.isNotEmpty == true)
                          AppText(
                            task.projectName!,
                            variant: AppTextVariant.caption,
                            fontSize: 12.5,
                            color: AppColors.textSecondary,
                          ),
                        _StateBadge(state: task.state),
                        AppText(
                          _dueLabel(task),
                          variant: AppTextVariant.caption,
                          fontSize: 12.5,
                          color: _dueColor(task),
                          weight: FontWeight.w600,
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

  String _dueLabel(TaskItem task) {
    if (task.isOverdue) return 'Quá hạn';
    if (task.isDueToday) return 'Hôm nay';
    if (task.dueDate == null) return '—';
    return DateFormat('dd/MM').format(task.dueDate!);
  }

  // Cung 3 muc mau voi _DueInfo cua dashboard_screen.dart va _StatsStrip cua task_detail_screen.dart
  // (qua han = do, hom nay = vang canh bao, con lai = xanh thuong hieu) — truoc day thieu muc
  // "Hom nay" nen hien nham mau xanh giong ngay thuong.
  Color _dueColor(TaskItem task) {
    if (task.isOverdue) return AppTheme.statusDanger;
    if (task.isDueToday) return AppTheme.statusWarning;
    return AppTheme.brandBlueDark;
  }
}

/// Nhan trang thai kieu vien mo — mau theo dung ngu nghia (xong = xanh la, cho duyet = vang
/// canh bao), thay vi to mot mau brand cho moi trang thai, theo quy uoc site.css (mau thuong
/// hieu CHI danh cho thao tac, khong dung cho du lieu).
class _StateBadge extends StatelessWidget {
  const _StateBadge({required this.state});

  final String state;

  Color get _color {
    switch (state) {
      case 'HoanThanh':
        return AppTheme.statusSuccess;
      case 'TamDung':
        return AppTheme.statusWarning;
      default:
        return AppColors.textSecondary;
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
      child: AppText(
        taskStateLabel(state).toUpperCase(),
        variant: AppTextVariant.overline,
        fontSize: 10.5,
        weight: FontWeight.w700,
        color: color,
        letterSpacing: 0.3,
      ),
    );
  }
}
