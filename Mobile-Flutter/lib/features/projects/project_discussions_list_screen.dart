import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import 'package:phosphor_icons/phosphor_icons.dart';

import '../../core/classes/route_manager.dart';
import '../../core/theme/app_colors.dart';
import '../../core/theme/app_dimens.dart';
import '../../core/theme/app_text_styles.dart';
import '../../core/widgets/app_app_bar.dart';
import '../../core/widgets/app_button.dart';
import '../../core/widgets/app_card.dart';
import '../../core/widgets/app_error_state.dart';
import '../../core/widgets/app_loading.dart';
import '../../core/widgets/app_scaffold.dart';
import '../../core/widgets/app_text.dart';
import '../../core/widgets/app_text_field.dart';
import '../app_routes.dart';
import 'my_projects_models.dart';
import 'my_projects_service.dart';
import 'project_discussion_service.dart';

/// Màn hình Danh sách Trao đổi Dự án (Discussions Hub)
class ProjectDiscussionsListScreen extends StatefulWidget {
  const ProjectDiscussionsListScreen({super.key, this.initialFuture});

  final Future<MyProjectsData>? initialFuture;

  @override
  State<ProjectDiscussionsListScreen> createState() =>
      _ProjectDiscussionsListScreenState();
}

class _ProjectDiscussionsListScreenState
    extends State<ProjectDiscussionsListScreen> {
  final _projectsService = MyProjectsService();
  final _discussionService = ProjectDiscussionService();
  final _searchController = TextEditingController();

  late Future<MyProjectsData> _projectsFuture;
  String _searchQuery = '';
  bool _onlyUnread = true;

  @override
  void initState() {
    super.initState();
    _projectsFuture =
        widget.initialFuture ?? _projectsService.fetch(scope: 'mine');
  }

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  void _reload() {
    setState(() {
      _projectsFuture = _projectsService.fetch(scope: 'mine', forceRefresh: true);
    });
  }

  @override
  Widget build(BuildContext context) {
    return AppScaffold(
      appBar: const AppAppBar(title: 'Trao đổi Dự án'),
      body: SafeArea(
        child: Column(
          children: [
            // Ô tìm kiếm nhanh
            Padding(
              padding: const EdgeInsets.fromLTRB(
                AppDimens.space16,
                AppDimens.space12,
                AppDimens.space16,
                AppDimens.space8,
              ),
              child: AppTextField(
                label: 'Tìm kiếm',
                controller: _searchController,
                hint: 'Tìm theo tên dự án...',
                prefixIcon: PhosphorIconsRegular.magnifyingGlass,
                onChanged: (val) =>
                    setState(() => _searchQuery = val.trim().toLowerCase()),
              ),
            ),

            // Tab chọn: Chưa xem / Tất cả dự án
            Padding(
              padding: const EdgeInsets.symmetric(
                horizontal: AppDimens.space16,
                vertical: AppDimens.space4,
              ),
              child: Row(
                children: [
                  _FilterChip(
                    label: 'Chưa xem',
                    isSelected: _onlyUnread,
                    icon: PhosphorIconsRegular.chatCircleDots,
                    onTap: () => setState(() => _onlyUnread = true),
                  ),
                  const SizedBox(width: AppDimens.space8),
                  _FilterChip(
                    label: 'Tất cả dự án',
                    isSelected: !_onlyUnread,
                    icon: PhosphorIconsRegular.folder,
                    onTap: () => setState(() => _onlyUnread = false),
                  ),
                ],
              ),
            ),
            const SizedBox(height: AppDimens.space4),

            // Danh sách các dự án
            Expanded(
              child: FutureBuilder<MyProjectsData>(
                future: _projectsFuture,
                builder: (context, snapshot) {
                  if (snapshot.connectionState == ConnectionState.waiting) {
                    return const Center(child: AppLoading());
                  }

                  if (snapshot.hasError) {
                    return AppErrorState(
                      message: 'Không tải được danh sách dự án.',
                      onRetry: _reload,
                    );
                  }

                  final allProjects = snapshot.data?.projects ?? [];
                  final myProjectIds = allProjects.map((p) => p.id).toList();

                  final filtered = allProjects.where((p) {
                    if (_searchQuery.isEmpty) return true;
                    final matchName = p.name.toLowerCase().contains(_searchQuery);
                    final matchCode = p.code?.toLowerCase().contains(_searchQuery) ?? false;
                    return matchName || matchCode;
                  }).toList();

                  return ValueListenableBuilder<int>(
                    valueListenable: ProjectDiscussionService.readStateNotifier,
                    builder: (context, _, __) {
                      return StreamBuilder<int>(
                        stream: _discussionService.streamTotalDiscussionsCount(
                          userProjectIds: myProjectIds,
                        ),
                        builder: (context, totalUnreadSnapshot) {
                          final totalUnread = totalUnreadSnapshot.data ?? 0;

                          if (_onlyUnread && totalUnread == 0 && _searchQuery.isEmpty) {
                            return Center(
                              child: SingleChildScrollView(
                                padding: const EdgeInsets.all(AppDimens.space24),
                                child: AppCard(
                                  padding: const EdgeInsets.all(AppDimens.space24),
                                  child: Column(
                                    mainAxisSize: MainAxisSize.min,
                                    children: [
                                      const Icon(
                                        PhosphorIconsRegular.checkCircle,
                                        size: 56,
                                        color: AppColors.success,
                                      ),
                                      const SizedBox(height: AppDimens.space16),
                                      const AppText(
                                        'Không có trao đổi chưa xem',
                                        variant: AppTextVariant.title,
                                        align: TextAlign.center,
                                      ),
                                      const SizedBox(height: AppDimens.space8),
                                      const AppText(
                                        'Bạn đã xem toàn bộ các tin nhắn trao đổi trong dự án.',
                                        variant: AppTextVariant.body,
                                        color: AppColors.textSecondary,
                                        align: TextAlign.center,
                                      ),
                                      const SizedBox(height: AppDimens.space16),
                                      AppButton(
                                        label: 'Xem tất cả dự án',
                                        icon: PhosphorIconsRegular.folder,
                                        onPressed: () =>
                                            setState(() => _onlyUnread = false),
                                      ),
                                    ],
                                  ),
                                ),
                              ),
                            );
                          }

                          if (filtered.isEmpty) {
                            return Center(
                              child: SingleChildScrollView(
                                padding: const EdgeInsets.all(AppDimens.space24),
                                child: AppCard(
                                  padding: const EdgeInsets.all(AppDimens.space24),
                                  child: Column(
                                    mainAxisSize: MainAxisSize.min,
                                    children: [
                                      const Icon(
                                        PhosphorIconsRegular.chatsCircle,
                                        size: 56,
                                        color: AppColors.accentBlue,
                                      ),
                                      const SizedBox(height: AppDimens.space16),
                                      AppText(
                                        _searchQuery.isEmpty
                                            ? 'Bạn chưa tham gia dự án nào'
                                            : 'Không tìm thấy dự án phù hợp',
                                        variant: AppTextVariant.title,
                                        align: TextAlign.center,
                                      ),
                                      const SizedBox(height: AppDimens.space8),
                                      AppText(
                                        _searchQuery.isEmpty
                                            ? 'Các dự án bạn tham gia sẽ xuất hiện tại đây để trao đổi thời gian thực.'
                                            : 'Hãy thử tìm kiếm với từ khóa khác.',
                                        variant: AppTextVariant.body,
                                        color: AppColors.textSecondary,
                                        align: TextAlign.center,
                                      ),
                                    ],
                                  ),
                                ),
                              ),
                            );
                          }

                          return ListView.separated(
                            padding: const EdgeInsets.symmetric(
                              horizontal: AppDimens.space16,
                              vertical: AppDimens.space8,
                            ),
                            itemCount: filtered.length,
                            separatorBuilder: (_, __) =>
                                const SizedBox(height: AppDimens.space8),
                            itemBuilder: (context, index) {
                              final project = filtered[index];
                              return _ProjectDiscussionItemCard(
                                project: project,
                                discussionService: _discussionService,
                                onlyUnread: _onlyUnread,
                              );
                            },
                          );
                        },
                      );
                    },
                  );
                },
              ),
            ),
          ],
        ),
      ),
    );
  }
}

/// Nút Chip lọc tab
class _FilterChip extends StatelessWidget {
  const _FilterChip({
    required this.label,
    required this.isSelected,
    required this.icon,
    required this.onTap,
  });

  final String label;
  final bool isSelected;
  final IconData icon;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return InkWell(
      borderRadius: BorderRadius.circular(AppDimens.radiusPill),
      onTap: onTap,
      child: AnimatedContainer(
        duration: const Duration(milliseconds: 180),
        padding: const EdgeInsets.symmetric(
          horizontal: AppDimens.space12,
          vertical: AppDimens.space8,
        ),
        decoration: BoxDecoration(
          color: isSelected ? AppColors.accentBlue : AppColors.surface,
          borderRadius: BorderRadius.circular(AppDimens.radiusPill),
          border: Border.all(
            color: isSelected ? AppColors.accentBlue : AppColors.border,
            width: 1,
          ),
        ),
        child: Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(
              icon,
              size: 14,
              color: isSelected ? AppColors.textOnPrimary : AppColors.textSecondary,
            ),
            const SizedBox(width: AppDimens.space4),
            AppText(
              label,
              variant: AppTextVariant.caption,
              fontSize: 13,
              weight: isSelected ? FontWeight.w600 : FontWeight.w500,
              color: isSelected ? AppColors.textOnPrimary : AppColors.textPrimary,
            ),
          ],
        ),
      ),
    );
  }
}

/// Card một kênh trao đổi của dự án
class _ProjectDiscussionItemCard extends StatelessWidget {
  const _ProjectDiscussionItemCard({
    required this.project,
    required this.discussionService,
    this.onlyUnread = false,
  });

  final MyProjectRow project;
  final ProjectDiscussionService discussionService;
  final bool onlyUnread;

  @override
  Widget build(BuildContext context) {
    return ValueListenableBuilder<int>(
      valueListenable: ProjectDiscussionService.readStateNotifier,
      builder: (context, _, __) {
        return StreamBuilder<int>(
          stream: discussionService.streamMessageCount(project.id),
          builder: (context, countSnapshot) {
            final count = countSnapshot.data ?? 0;
            if (onlyUnread && count == 0) {
              return const SizedBox.shrink();
            }

            return StreamBuilder<Map<dynamic, dynamic>?>(
              stream: discussionService.streamProjectSummary(project.id),
              builder: (context, snapshot) {
                final data = snapshot.data;
                final lastMessage = data?['lastMessage'] as String?;
                final lastSender = data?['lastSenderName'] as String?;
                final lastTimestamp = data?['lastUpdatedAt'];

                String timeStr = '';
                if (lastTimestamp is int) {
                  timeStr = DateFormat('HH:mm dd/MM')
                      .format(DateTime.fromMillisecondsSinceEpoch(lastTimestamp));
                } else if (lastTimestamp is String) {
                  final parsed = DateTime.tryParse(lastTimestamp);
                  if (parsed != null) {
                    timeStr = DateFormat('HH:mm dd/MM').format(parsed);
                  }
                }

                return InkWell(
                  borderRadius: BorderRadius.circular(AppDimens.radiusMd),
                  onTap: () async {
                    await discussionService.markAsRead(project.id);
                    if (context.mounted) {
                      await Nav.toNamed(
                        context,
                        AppRoutes.projectDiscussion,
                        arguments: {
                          'projectId': project.id,
                          'projectName': project.name,
                        },
                      );
                      await discussionService.markAsRead(project.id);
                    }
                  },
                  child: AppCard(
                    padding: const EdgeInsets.all(AppDimens.space16),
                    child: Row(
                      children: [
                        // Icon dự án
                        Container(
                          width: 44,
                          height: 44,
                          decoration: BoxDecoration(
                            color: AppColors.primarySoft,
                            borderRadius: BorderRadius.circular(AppDimens.radiusMd),
                            border: Border.all(color: AppColors.border),
                          ),
                          child: const Icon(
                            PhosphorIconsRegular.chatsCircle,
                            color: AppColors.accentBlue,
                            size: 24,
                          ),
                        ),
                        const SizedBox(width: AppDimens.space12),

                        // Nội dung tin nhắn & tên dự án
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Row(
                                children: [
                                  Expanded(
                                    child: AppText(
                                      project.name,
                                      variant: AppTextVariant.title,
                                      fontSize: 15,
                                      maxLines: 1,
                                      overflow: TextOverflow.ellipsis,
                                    ),
                                  ),
                                  if (timeStr.isNotEmpty) ...[
                                    const SizedBox(width: AppDimens.space8),
                                    AppText(
                                      timeStr,
                                      variant: AppTextVariant.caption,
                                      color: AppColors.textSecondary,
                                    ),
                                  ],
                                ],
                              ),
                              const SizedBox(height: AppDimens.space4),
                              AppText(
                                lastMessage != null && lastMessage.isNotEmpty
                                    ? '${lastSender != null && lastSender.isNotEmpty ? "$lastSender: " : ""}$lastMessage'
                                    : 'Chưa có trao đổi nào. Bấm để bắt đầu cuộc trò chuyện!',
                                variant: AppTextVariant.body,
                                color: lastMessage != null && lastMessage.isNotEmpty
                                    ? AppColors.textPrimary
                                    : AppColors.textSecondary,
                                fontSize: 13,
                                maxLines: 2,
                                overflow: TextOverflow.ellipsis,
                              ),
                            ],
                          ),
                        ),

                        const SizedBox(width: AppDimens.space8),
                        Row(
                          mainAxisSize: MainAxisSize.min,
                          children: [
                            if (count > 0) ...[
                              Container(
                                padding: const EdgeInsets.symmetric(
                                    horizontal: 6, vertical: 2),
                                constraints: const BoxConstraints(
                                    minWidth: 20, minHeight: 20),
                                decoration: BoxDecoration(
                                  color: AppColors.accentBlue,
                                  borderRadius: BorderRadius.circular(10),
                                ),
                                alignment: Alignment.center,
                                child: AppText(
                                  count > 99 ? '99+' : '$count',
                                  variant: AppTextVariant.caption,
                                  fontSize: 11,
                                  weight: FontWeight.w700,
                                  color: AppColors.textOnPrimary,
                                ),
                              ),
                              const SizedBox(width: AppDimens.space8),
                            ],
                            const Icon(
                              PhosphorIconsRegular.caretRight,
                              size: 16,
                              color: AppColors.textSecondary,
                            ),
                          ],
                        ),
                      ],
                    ),
                  ),
                );
              },
            );
          },
        );
      },
    );
  }
}
