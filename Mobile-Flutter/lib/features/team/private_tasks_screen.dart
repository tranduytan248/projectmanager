import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

import '../../core/classes/route_manager.dart';
import '../../core/theme/app_colors.dart';
import '../../core/theme/app_dimens.dart';
import '../../core/theme/app_text_styles.dart';
import '../../core/widgets/app_app_bar.dart';
import '../../core/widgets/app_card.dart';
import '../../core/widgets/app_checkbox.dart';
import '../../core/widgets/app_dropdown.dart';
import '../../core/widgets/app_error_state.dart';
import '../../core/widgets/app_fab.dart';
import '../../core/widgets/app_loading.dart';
import '../../core/widgets/app_scaffold.dart';
import '../../core/widgets/app_text.dart';
import '../../core/widgets/app_text_field.dart';
import '../app_routes.dart';
import 'private_task_form_screen.dart';
import 'private_tasks_models.dart';
import 'private_tasks_service.dart';

class PrivateTasksScreen extends StatefulWidget {
  const PrivateTasksScreen({super.key});

  @override
  State<PrivateTasksScreen> createState() => _PrivateTasksScreenState();
}

class _PrivateTasksScreenState extends State<PrivateTasksScreen> {
  final _service = PrivateTasksService();
  final _searchController = TextEditingController();

  PrivateTasksData? _data;
  bool _isLoading = true;
  String? _errorMessage;

  int? _selectedAssigneeId;
  String? _selectedState;
  bool _showClosed = false;

  @override
  void initState() {
    super.initState();
    _loadData();
  }

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  Future<void> _loadData() async {
    setState(() {
      _isLoading = true;
      _errorMessage = null;
    });

    try {
      final data = await _service.fetch(
        query: _searchController.text.trim(),
        assigneeId: _selectedAssigneeId,
        state: _selectedState,
        showClosed: _showClosed,
      );
      if (mounted) {
        setState(() {
          _data = data;
          _isLoading = false;
        });
      }
    } catch (e) {
      if (mounted) {
        setState(() {
          _isLoading = false;
          _errorMessage = 'Không thể tải danh sách việc riêng. Hãy thử lại.';
        });
      }
    }
  }

  Future<void> _openCreateForm() async {
    if (_data == null) return;
    final created = await Navigator.of(context).push<bool>(
      MaterialPageRoute(
        builder: (_) => PrivateTaskFormScreen(members: _data!.members),
      ),
    );
    if (created == true) {
      _loadData();
    }
  }

  Future<void> _openEditForm(PrivateTaskItem item) async {
    if (_data == null) return;
    final updated = await Navigator.of(context).push<bool>(
      MaterialPageRoute(
        builder: (_) => PrivateTaskFormScreen(
          initialItem: item,
          members: _data!.members,
        ),
      ),
    );
    if (updated == true) {
      _loadData();
    }
  }

  Future<void> _deleteTask(PrivateTaskItem item) async {
    final confirm = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const AppText('Xác nhận xóa việc', variant: AppTextVariant.heading),
        content: AppText('Bạn có chắc chắn muốn xóa việc riêng "${item.title}"?'),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(ctx).pop(false),
            child: const AppText('Hủy'),
          ),
          ElevatedButton(
            style: ElevatedButton.styleFrom(backgroundColor: AppColors.danger),
            onPressed: () => Navigator.of(ctx).pop(true),
            child: const AppText('Xóa', color: AppColors.textOnPrimary),
          ),
        ],
      ),
    );

    if (confirm != true) return;

    final result = await _service.delete(item.id);
    if (result.isSuccess) {
      _loadData();
    } else {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text(result.error ?? 'Không thể xóa việc riêng.')),
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    return AppScaffold(
      appBar: const AppAppBar(
        title: 'Giao việc riêng',
      ),
      floatingActionButton: AppFab(
        label: 'Giao việc',
        icon: Icons.add,
        onPressed: _data != null ? _openCreateForm : null,
      ),
      body: RefreshIndicator(
        onRefresh: _loadData,
        child: Column(
          children: [
            // Thống kê nhanh
            if (_data != null) _buildStatsHeader(_data!),

            // Khối tìm kiếm và bộ lọc
            _buildFilterBar(),

            // Danh sách việc
            Expanded(
              child: _buildBody(),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildStatsHeader(PrivateTasksData data) {
    return Container(
      padding: const EdgeInsets.symmetric(
        horizontal: AppDimens.screenPadding,
        vertical: AppDimens.space12,
      ),
      decoration: BoxDecoration(
        color: AppColors.surface,
        border: Border(bottom: BorderSide(color: AppColors.border.withValues(alpha: 0.6))),
      ),
      child: Row(
        children: [
          _statCard('Tổng việc', '${data.totalCount}', AppColors.primary),
          const SizedBox(width: AppDimens.space8),
          _statCard('Quá hạn', '${data.overdueCount}', AppColors.danger),
          const SizedBox(width: AppDimens.space8),
          _statCard('Đang làm', '${data.inProgressCount}', AppColors.primary),
          const SizedBox(width: AppDimens.space8),
          _statCard('Hoàn thành', '${data.doneCount}', AppColors.success),
        ],
      ),
    );
  }

  Widget _statCard(String label, String count, Color color) {
    return Expanded(
      child: Container(
        padding: const EdgeInsets.symmetric(
          vertical: AppDimens.space8,
          horizontal: AppDimens.space4,
        ),
        decoration: BoxDecoration(
          color: color.withValues(alpha: 0.08),
          borderRadius: BorderRadius.circular(AppDimens.radiusSm),
          border: Border.all(color: color.withValues(alpha: 0.2)),
        ),
        child: Column(
          children: [
            AppText(
              count,
              variant: AppTextVariant.heading,
              weight: FontWeight.bold,
              color: color,
            ),
            const SizedBox(height: AppDimens.space4),
            AppText(
              label,
              variant: AppTextVariant.caption,
              color: AppColors.textSecondary,
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildFilterBar() {
    final memberItems = <int?, String>{
      null: 'Tất cả nhân sự',
      if (_data != null)
        for (final m in _data!.members) m.userId: m.fullName,
    };

    return Container(
      padding: const EdgeInsets.all(AppDimens.space12),
      color: AppColors.surface,
      child: Column(
        children: [
          // Search input
          AppTextField(
            label: 'Tìm kiếm',
            controller: _searchController,
            hint: 'Tìm theo tên việc, người thực hiện...',
            prefixIcon: Icons.search,
            suffixIcon: _searchController.text.isNotEmpty ? Icons.clear : null,
            onFieldSubmitted: (_) => _loadData(),
          ),
          const SizedBox(height: AppDimens.space12),

          // Row filters: Member dropdown & ShowClosed checkbox
          Row(
            crossAxisAlignment: CrossAxisAlignment.center,
            children: [
              Expanded(
                child: AppDropdown<int?>(
                  label: 'Nhân sự',
                  value: _selectedAssigneeId,
                  items: memberItems,
                  onChanged: (val) {
                    setState(() => _selectedAssigneeId = val);
                    _loadData();
                  },
                ),
              ),
              const SizedBox(width: AppDimens.space12),
              AppCheckbox(
                label: 'Đã đóng',
                value: _showClosed,
                onChanged: (val) {
                  setState(() => _showClosed = val);
                  _loadData();
                },
              ),
            ],
          ),
        ],
      ),
    );
  }

  Widget _buildBody() {
    if (_isLoading && _data == null) {
      return const Center(child: AppLoading());
    }

    if (_errorMessage != null && _data == null) {
      return AppErrorState(message: _errorMessage!, onRetry: _loadData);
    }

    final items = _data?.items ?? [];
    if (items.isEmpty) {
      return Center(
        child: Padding(
          padding: const EdgeInsets.all(AppDimens.space32),
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Icon(
                Icons.assignment_outlined,
                size: 56,
                color: AppColors.textSecondary.withValues(alpha: 0.5),
              ),
              const SizedBox(height: AppDimens.space12),
              const AppText(
                'Chưa có việc riêng nào.',
                variant: AppTextVariant.heading,
                weight: FontWeight.bold,
                color: AppColors.textPrimary,
              ),
              const SizedBox(height: AppDimens.space4),
              const AppText(
                'Bấm "Giao việc" để tạo việc ngoài dự án cho nhân sự trong Tổ.',
                variant: AppTextVariant.body,
                align: TextAlign.center,
                color: AppColors.textSecondary,
              ),
            ],
          ),
        ),
      );
    }

    return ListView.separated(
      padding: const EdgeInsets.fromLTRB(
        AppDimens.screenPadding,
        AppDimens.space12,
        AppDimens.screenPadding,
        80,
      ),
      itemCount: items.length,
      separatorBuilder: (_, __) => const SizedBox(height: AppDimens.space12),
      itemBuilder: (context, index) {
        return _buildTaskCard(items[index]);
      },
    );
  }

  Widget _buildTaskCard(PrivateTaskItem item) {
    final dateFormat = DateFormat('dd/MM/yyyy');

    return AppCard(
      padding: const EdgeInsets.all(AppDimens.space16),
      radius: AppDimens.radiusMd,
      onTap: () {
        Nav.toNamed(context, AppRoutes.taskDetail, arguments: {'taskId': item.id.toString()});
      },
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // Header: Người thực hiện + KPI chip + Menu (nếu CanEdit/CanDelete)
          Row(
            children: [
              CircleAvatar(
                radius: 14,
                backgroundColor: AppColors.primary.withValues(alpha: 0.15),
                child: AppText(
                  item.assigneeName.isNotEmpty ? item.assigneeName[0].toUpperCase() : '?',
                  variant: AppTextVariant.caption,
                  weight: FontWeight.bold,
                  color: AppColors.primary,
                ),
              ),
              const SizedBox(width: AppDimens.space8),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    AppText(
                      item.assigneeName.isNotEmpty ? item.assigneeName : 'Chưa giao',
                      variant: AppTextVariant.body,
                      weight: FontWeight.w600,
                    ),
                    if (item.assignedByName.isNotEmpty)
                      AppText(
                        'Giao bởi: ${item.assignedByName}',
                        variant: AppTextVariant.caption,
                        color: AppColors.textSecondary,
                      ),
                  ],
                ),
              ),
              if (item.bonusPercent > 0) ...[
                Container(
                  padding: const EdgeInsets.symmetric(
                    horizontal: AppDimens.space8,
                    vertical: AppDimens.space4,
                  ),
                  decoration: BoxDecoration(
                    color: AppColors.primary.withValues(alpha: 0.1),
                    borderRadius: BorderRadius.circular(AppDimens.radiusPill),
                  ),
                  child: AppText(
                    '+${item.bonusPercent.toStringAsFixed(1)}% KPI',
                    variant: AppTextVariant.caption,
                    weight: FontWeight.bold,
                    color: AppColors.primary,
                  ),
                ),
              ],
              if (item.canEdit || item.canDelete) ...[
                PopupMenuButton<String>(
                  icon: const Icon(Icons.more_vert, size: 18, color: AppColors.textSecondary),
                  padding: EdgeInsets.zero,
                  constraints: const BoxConstraints(),
                  onSelected: (val) {
                    if (val == 'edit') _openEditForm(item);
                    if (val == 'delete') _deleteTask(item);
                  },
                  itemBuilder: (ctx) => [
                    if (item.canEdit)
                      const PopupMenuItem(
                        value: 'edit',
                        child: Row(
                          children: [
                            Icon(Icons.edit_outlined, size: 16),
                            SizedBox(width: AppDimens.space8),
                            AppText('Sửa việc', variant: AppTextVariant.body),
                          ],
                        ),
                      ),
                    if (item.canDelete)
                      const PopupMenuItem(
                        value: 'delete',
                        child: Row(
                          children: [
                            Icon(Icons.delete_outline, size: 16, color: AppColors.danger),
                            SizedBox(width: AppDimens.space8),
                            AppText('Xóa việc', variant: AppTextVariant.body, color: AppColors.danger),
                          ],
                        ),
                      ),
                  ],
                ),
              ],
            ],
          ),
          const SizedBox(height: AppDimens.space12),

          // Title
          AppText(
            item.title,
            variant: AppTextVariant.body,
            weight: FontWeight.bold,
            color: AppColors.textPrimary,
          ),

          if (item.description.isNotEmpty) ...[
            const SizedBox(height: AppDimens.space4),
            AppText(
              item.description,
              variant: AppTextVariant.caption,
              maxLines: 2,
              overflow: TextOverflow.ellipsis,
              color: AppColors.textSecondary,
            ),
          ],
          const SizedBox(height: AppDimens.space12),

          // Progress Bar
          Row(
            children: [
              Expanded(
                child: ClipRRect(
                  borderRadius: BorderRadius.circular(AppDimens.radiusSm),
                  child: LinearProgressIndicator(
                    value: item.progress / 100.0,
                    backgroundColor: AppColors.border,
                    valueColor: AlwaysStoppedAnimation<Color>(item.stateColor),
                    minHeight: 6,
                  ),
                ),
              ),
              const SizedBox(width: AppDimens.space8),
              AppText(
                '${item.progress}%',
                variant: AppTextVariant.caption,
                weight: FontWeight.bold,
                color: item.stateColor,
              ),
            ],
          ),
          const SizedBox(height: AppDimens.space12),

          // Footer: Due Date + State Badge
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Row(
                children: [
                  Icon(
                    Icons.event,
                    size: 14,
                    color: item.isOverdue ? AppColors.danger : AppColors.textSecondary,
                  ),
                  const SizedBox(width: AppDimens.space4),
                  AppText(
                    item.dueDate != null
                        ? 'Hạn: ${dateFormat.format(item.dueDate!)}'
                        : 'Không có hạn',
                    variant: AppTextVariant.caption,
                    color: item.isOverdue ? AppColors.danger : AppColors.textSecondary,
                    weight: item.isOverdue ? FontWeight.bold : FontWeight.normal,
                  ),
                  if (item.isOverdue) ...[
                    const SizedBox(width: AppDimens.space8),
                    Container(
                      padding: const EdgeInsets.symmetric(
                        horizontal: AppDimens.space4,
                        vertical: 2,
                      ),
                      decoration: BoxDecoration(
                        color: AppColors.danger.withValues(alpha: 0.12),
                        borderRadius: BorderRadius.circular(AppDimens.radiusSm),
                      ),
                      child: const AppText(
                        'Quá hạn',
                        variant: AppTextVariant.caption,
                        color: AppColors.danger,
                        weight: FontWeight.bold,
                      ),
                    ),
                  ],
                ],
              ),
              Container(
                padding: const EdgeInsets.symmetric(
                  horizontal: AppDimens.space8,
                  vertical: AppDimens.space4,
                ),
                decoration: BoxDecoration(
                  color: item.stateColor.withValues(alpha: 0.12),
                  borderRadius: BorderRadius.circular(AppDimens.radiusSm),
                ),
                child: AppText(
                  item.stateLabel,
                  variant: AppTextVariant.caption,
                  color: item.stateColor,
                  weight: FontWeight.bold,
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }
}
