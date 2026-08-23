import 'dart:io';

import 'package:cached_network_image/cached_network_image.dart';
import 'package:file_picker/file_picker.dart';
import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import 'package:phosphor_icons/phosphor_icons.dart';

import '../../config/app_cache.dart';
import '../../core/theme/app_colors.dart';
import '../../core/theme/app_dimens.dart';
import '../../core/theme/app_text_styles.dart';
import '../../core/utils/toast_service.dart';
import '../../core/widgets/app_app_bar.dart';
import '../../core/widgets/app_card.dart';
import '../../core/widgets/app_error_state.dart';
import '../../core/widgets/app_icon_button.dart';
import '../../core/widgets/app_loading.dart';
import '../../core/widgets/app_scaffold.dart';
import '../../core/widgets/app_text.dart';
import '../../core/widgets/app_text_field.dart';
import '../app_routes.dart';
import 'project_detail_models.dart';
import 'project_discussion_models.dart';
import 'project_discussion_service.dart';

/// Màn hình Trao đổi / Thảo luận trong Dự án (Real-time qua Firebase Realtime Database).
class ProjectDiscussionScreen extends StatefulWidget {
  const ProjectDiscussionScreen({
    super.key,
    required this.projectId,
    this.projectName = '',
    this.members = const [],
  });

  final int projectId;
  final String projectName;
  final List<ProjectMember> members;

  @override
  State<ProjectDiscussionScreen> createState() =>
      _ProjectDiscussionScreenState();
}

class _ProjectDiscussionScreenState extends State<ProjectDiscussionScreen> {
  final _service = ProjectDiscussionService();
  final _textController = TextEditingController();
  final _scrollController = ScrollController();
  final _appCache = AppCache();

  String _currentDisplayName = '';
  String _currentUsername = '';
  int _currentUserId = 0;
  bool _isSending = false;
  bool _showMentionPicker = false;
  String _mentionQuery = '';

  bool _showTaskPicker = false;
  String _taskQuery = '';
  List<ProjectTaskOption> _projectTasks = [];
  bool _isLoadingTasks = false;

  PlatformFile? _pendingAttachment;

  static const int _maxAttachmentBytes = 10 * 1024 * 1024; // 10 MB

  @override
  void initState() {
    super.initState();
    final loginInfo = _appCache.getLoginInfo();
    _currentDisplayName = loginInfo?['displayName'] as String? ?? 'Tôi';
    _currentUsername = loginInfo?['username'] as String? ?? '';
    _currentUserId = loginInfo?['userId'] is int
        ? loginInfo!['userId'] as int
        : int.tryParse(loginInfo?['userId']?.toString() ?? '') ?? 0;
    _service.markAsRead(widget.projectId);
    _loadProjectTasks();
  }

  @override
  void dispose() {
    _service.markAsRead(widget.projectId);
    _textController.dispose();
    _scrollController.dispose();
    super.dispose();
  }

  Future<void> _loadProjectTasks() async {
    if (_isLoadingTasks) return;
    _isLoadingTasks = true;
    try {
      final tasks = await _service.fetchProjectTasks(widget.projectId);
      if (mounted) {
        setState(() {
          _projectTasks = tasks;
          _isLoadingTasks = false;
        });
      }
    } catch (_) {
      if (mounted) setState(() => _isLoadingTasks = false);
    }
  }

  void _scrollToBottom() {
    if (_scrollController.hasClients) {
      _scrollController.animateTo(
        _scrollController.position.maxScrollExtent + 80,
        duration: const Duration(milliseconds: 250),
        curve: Curves.easeOut,
      );
    }
  }

  Future<void> _pickAttachment() async {
    try {
      final result = await FilePicker.platform.pickFiles(
        type: FileType.custom,
        allowedExtensions: [
          'png', 'jpg', 'jpeg', 'gif', 'webp', 'bmp',
          'mp4', 'mov', 'webm', 'm4v',
          'pdf', 'doc', 'docx', 'xls', 'xlsx', 'ppt', 'pptx', 'txt', 'zip', 'rar'
        ],
      );

      if (result != null && result.files.isNotEmpty) {
        final file = result.files.first;
        if (file.size > _maxAttachmentBytes) {
          ToastService.show(
            'File/Video đính kèm vượt quá giới hạn tối đa 10 MB.',
            type: ToastType.error,
          );
          return;
        }
        setState(() {
          _pendingAttachment = file;
        });
      }
    } catch (e) {
      ToastService.show(
        'Không thể chọn file: $e',
        type: ToastType.error,
      );
    }
  }

  void _removePendingAttachment() {
    setState(() {
      _pendingAttachment = null;
    });
  }

  Future<void> _sendMessage() async {
    final text = _textController.text.trim();
    final hasAttachment = _pendingAttachment != null;
    if ((text.isEmpty && !hasAttachment) || _isSending) return;

    setState(() {
      _isSending = true;
      _showMentionPicker = false;
      _showTaskPicker = false;
    });

    _textController.clear();

    try {
      String attachmentUrl = '';
      String attachmentName = '';
      int attachmentSize = 0;
      String attachmentSizeLabel = '';
      String type = 'text';

      if (hasAttachment && _pendingAttachment?.path != null) {
        final uploadRes = await _service.uploadAttachment(
          projectId: widget.projectId,
          filePath: _pendingAttachment!.path!,
          fileName: _pendingAttachment!.name,
        );

        if (uploadRes != null) {
          attachmentUrl = uploadRes['url'] as String? ?? '';
          attachmentName = uploadRes['originalName'] as String? ?? _pendingAttachment!.name;
          attachmentSize = (uploadRes['fileSize'] as num?)?.toInt() ?? _pendingAttachment!.size;
          attachmentSizeLabel = uploadRes['fileSizeLabel'] as String? ?? '';
          type = uploadRes['fileType'] as String? ?? 'file';
        }
      }

      await _service.sendMessage(
        projectId: widget.projectId,
        content: text,
        senderId: _currentUserId,
        senderName: _currentDisplayName,
        senderUsername: _currentUsername,
        projectName: widget.projectName,
        type: type,
        attachmentUrl: attachmentUrl,
        attachmentName: attachmentName,
        attachmentSize: attachmentSize,
        attachmentSizeLabel: attachmentSizeLabel,
      );

      _removePendingAttachment();
      _scrollToBottom();
      _service.markAsRead(widget.projectId);
    } catch (e) {
      ToastService.show(
        'Lỗi gửi tin nhắn: $e',
        type: ToastType.error,
      );
    } finally {
      if (mounted) {
        setState(() => _isSending = false);
      }
    }
  }

  void _onTextChanged(String text) {
    final cursorPosition = _textController.selection.baseOffset;
    if (cursorPosition < 0) return;

    final beforeCursor = text.substring(0, cursorPosition);

    // Kiểm tra gõ @ mention
    final lastAt = beforeCursor.lastIndexOf('@');
    if (lastAt != -1 && (lastAt == 0 || beforeCursor[lastAt - 1] == ' ')) {
      final query = beforeCursor.substring(lastAt + 1);
      if (!query.contains(' ')) {
        setState(() {
          _showMentionPicker = true;
          _mentionQuery = query;
          _showTaskPicker = false;
        });
        return;
      }
    }

    // Kiểm tra gõ / gợi ý task
    final lastSlash = beforeCursor.lastIndexOf('/');
    if (lastSlash != -1 && (lastSlash == 0 || beforeCursor[lastSlash - 1] == ' ')) {
      final query = beforeCursor.substring(lastSlash + 1);
      if (!query.contains(' ')) {
        setState(() {
          _showTaskPicker = true;
          _taskQuery = query;
          _showMentionPicker = false;
        });
        return;
      }
    }

    if (_showMentionPicker || _showTaskPicker) {
      setState(() {
        _showMentionPicker = false;
        _showTaskPicker = false;
      });
    }
  }

  void _selectMention(String fullName) {
    final text = _textController.text;
    final cursorPosition = _textController.selection.baseOffset;
    final beforeCursor = text.substring(0, cursorPosition);
    final afterCursor = text.substring(cursorPosition);
    final lastAt = beforeCursor.lastIndexOf('@');

    final newBefore = '${beforeCursor.substring(0, lastAt)}@$fullName ';
    _textController.text = '$newBefore$afterCursor';
    _textController.selection = TextSelection.fromPosition(
      TextPosition(offset: newBefore.length),
    );

    setState(() => _showMentionPicker = false);
  }

  void _selectTask(ProjectTaskOption task) {
    final text = _textController.text;
    final cursorPosition = _textController.selection.baseOffset;
    final beforeCursor = text.substring(0, cursorPosition);
    final afterCursor = text.substring(cursorPosition);
    final lastSlash = beforeCursor.lastIndexOf('/');

    final token = '[task:${task.id}|${task.title}] ';
    final prefix = lastSlash != -1 ? beforeCursor.substring(0, lastSlash) : beforeCursor;
    final newBefore = '$prefix$token';
    _textController.text = '$newBefore$afterCursor';
    _textController.selection = TextSelection.fromPosition(
      TextPosition(offset: newBefore.length),
    );

    setState(() => _showTaskPicker = false);
  }

  @override
  Widget build(BuildContext context) {
    final title = widget.projectName.isNotEmpty
        ? 'Trao đổi: ${widget.projectName}'
        : 'Trao đổi dự án #${widget.projectId}';

    return AppScaffold(
      appBar: AppAppBar(
        title: title,
      ),
      body: SafeArea(
        child: Column(
          children: [
            // Luồng tin nhắn realtime
            Expanded(
              child: StreamBuilder<List<ProjectDiscussionMessage>>(
                stream: _service.streamMessages(widget.projectId),
                builder: (context, snapshot) {
                  if (snapshot.connectionState == ConnectionState.waiting) {
                    return const Center(child: AppLoading());
                  }

                  if (snapshot.hasError) {
                    return AppErrorState(
                      message: 'Lỗi tải luồng trao đổi: ${snapshot.error}',
                      onRetry: () => setState(() {}),
                    );
                  }

                  final messages = snapshot.data ?? [];

                  if (messages.isEmpty) {
                    return _EmptyDiscussionView(projectName: widget.projectName);
                  }

                  WidgetsBinding.instance.addPostFrameCallback((_) {
                    _scrollToBottom();
                    _service.markAsRead(widget.projectId);
                  });

                  return ListView.builder(
                    controller: _scrollController,
                    padding: const EdgeInsets.symmetric(
                      horizontal: AppDimens.space16,
                      vertical: AppDimens.space16,
                    ),
                    itemCount: messages.length,
                    itemBuilder: (context, index) {
                      final msg = messages[index];
                      final isMe = (_currentUserId > 0 && msg.senderId == _currentUserId) ||
                          (_currentUsername.isNotEmpty && msg.senderUsername.isNotEmpty &&
                              msg.senderUsername.toLowerCase() == _currentUsername.toLowerCase()) ||
                          (_currentDisplayName.isNotEmpty && msg.senderName.isNotEmpty &&
                              msg.senderName.trim().toLowerCase() == _currentDisplayName.trim().toLowerCase()) ||
                          msg.senderName == 'Tôi';
                      return _MessageBubble(
                        message: msg,
                        isMe: isMe,
                      );
                    },
                  );
                },
              ),
            ),

            // Gợi ý @mention thành viên
            if (_showMentionPicker && widget.members.isNotEmpty)
              _MentionSuggestions(
                members: widget.members,
                query: _mentionQuery,
                onSelect: _selectMention,
              ),

            // Gợi ý / danh sách công việc (Task)
            if (_showTaskPicker)
              _TaskSuggestions(
                tasks: _projectTasks,
                query: _taskQuery,
                onSelect: _selectTask,
              ),

            // Xem trước file đính kèm trước khi gửi
            if (_pendingAttachment != null)
              _AttachmentPreviewBar(
                attachment: _pendingAttachment!,
                onRemove: _removePendingAttachment,
              ),

            // Thanh soạn thảo tin nhắn
            _InputBar(
              controller: _textController,
              isSending: _isSending,
              onChanged: _onTextChanged,
              onSend: _sendMessage,
              onTapAdd: _openActionMenu,
            ),
          ],
        ),
      ),
    );
  }

  void _openActionMenu() {
    FocusScope.of(context).unfocus();
    showModalBottomSheet<void>(
      context: context,
      backgroundColor: Colors.transparent,
      builder: (context) => _DiscussionActionMenuSheet(
        onTapAttach: _pickAttachment,
        onTapTask: () {
          final text = _textController.text;
          _textController.text = '$text/';
          _textController.selection = TextSelection.fromPosition(
              TextPosition(offset: _textController.text.length));
          setState(() {
            _showTaskPicker = true;
            _taskQuery = '';
            _showMentionPicker = false;
          });
        },
        onTapMention: () {
          final text = _textController.text;
          _textController.text = '$text@';
          _textController.selection = TextSelection.fromPosition(
              TextPosition(offset: _textController.text.length));
          setState(() {
            _showMentionPicker = true;
            _mentionQuery = '';
            _showTaskPicker = false;
          });
        },
      ),
    );
  }
}

/// Thanh xem trước file đính kèm
class _AttachmentPreviewBar extends StatelessWidget {
  const _AttachmentPreviewBar({
    required this.attachment,
    required this.onRemove,
  });

  final PlatformFile attachment;
  final VoidCallback onRemove;

  String _formatSize(int bytes) {
    if (bytes >= 1024 * 1024) {
      return '${(bytes / (1024 * 1024)).toStringAsFixed(1)} MB';
    }
    if (bytes >= 1024) {
      return '${(bytes / 1024).round()} KB';
    }
    return '$bytes B';
  }

  @override
  Widget build(BuildContext context) {
    final isImage = RegExp(r'\.(png|jpg|jpeg|gif|webp|bmp)$', caseSensitive: false).hasMatch(attachment.name);
    final isVideo = RegExp(r'\.(mp4|mov|webm|m4v)$', caseSensitive: false).hasMatch(attachment.name);

    return Container(
      padding: const EdgeInsets.symmetric(
        horizontal: AppDimens.space16,
        vertical: AppDimens.space8,
      ),
      color: AppColors.surface,
      child: Container(
        padding: const EdgeInsets.all(AppDimens.space8),
        decoration: BoxDecoration(
          color: AppColors.surfaceVariant,
          borderRadius: BorderRadius.circular(AppDimens.radiusMd),
          border: Border.all(color: AppColors.border),
        ),
        child: Row(
          children: [
            ClipRRect(
              borderRadius: BorderRadius.circular(AppDimens.radiusSm),
              child: SizedBox(
                width: 44,
                height: 44,
                child: isImage && attachment.path != null
                    ? Image.file(
                        File(attachment.path!),
                        fit: BoxFit.cover,
                      )
                    : Container(
                        color: AppColors.surface,
                        alignment: Alignment.center,
                        child: Icon(
                          isVideo ? PhosphorIconsRegular.videoCamera : PhosphorIconsRegular.file,
                          color: AppColors.accentBlue,
                          size: 24,
                        ),
                      ),
              ),
            ),
            const SizedBox(width: AppDimens.space12),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  AppText(
                    attachment.name,
                    variant: AppTextVariant.body,
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                  ),
                  AppText(
                    _formatSize(attachment.size),
                    variant: AppTextVariant.caption,
                    color: AppColors.textSecondary,
                  ),
                ],
              ),
            ),
            AppIconButton(
              icon: PhosphorIconsRegular.x,
              tooltip: 'Xóa đính kèm',
              color: AppColors.danger,
              onPressed: onRemove,
            ),
          ],
        ),
      ),
    );
  }
}

/// Trạng thái rỗng khi chưa có tin nhắn nào
class _EmptyDiscussionView extends StatelessWidget {
  const _EmptyDiscussionView({required this.projectName});

  final String projectName;

  @override
  Widget build(BuildContext context) {
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
              const AppText(
                'Chưa có trao đổi nào',
                variant: AppTextVariant.title,
                align: TextAlign.center,
              ),
              const SizedBox(height: AppDimens.space8),
              AppText(
                'Hãy là người đầu tiên bắt đầu cuộc thảo luận trong dự án ${projectName.isNotEmpty ? '"$projectName"' : ''}!',
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
}

/// Bong bóng tin nhắn chat
class _MessageBubble extends StatelessWidget {
  const _MessageBubble({
    required this.message,
    required this.isMe,
  });

  final ProjectDiscussionMessage message;
  final bool isMe;

  void _showFullImage(BuildContext context, String imageUrl) {
    showDialog(
      context: context,
      builder: (context) => Dialog(
        backgroundColor: Colors.black,
        insetPadding: EdgeInsets.zero,
        child: Stack(
          children: [
            Center(
              child: InteractiveViewer(
                child: CachedNetworkImage(
                  imageUrl: imageUrl,
                  placeholder: (_, __) => const Center(child: AppLoading()),
                  errorWidget: (_, __, ___) => const Icon(
                    PhosphorIconsRegular.imageBroken,
                    color: Colors.white70,
                    size: 48,
                  ),
                ),
              ),
            ),
            Positioned(
              top: 40,
              right: 16,
              child: AppIconButton(
                icon: PhosphorIconsRegular.x,
                tooltip: 'Đóng',
                color: Colors.white,
                onPressed: () => Navigator.of(context).pop(),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildContent(BuildContext context) {
    final rawText = message.content;
    final hasTaskToken = rawText.contains(RegExp(r'\[task:\d+\|[^\]]+\]'));

    if (!hasTaskToken) {
      return AppText(
        rawText,
        variant: AppTextVariant.body,
        color: isMe ? AppColors.textOnPrimary : AppColors.textPrimary,
      );
    }

    final spans = <InlineSpan>[];
    final regex = RegExp(r'\[task:(\d+)\|([^\]]+)\]');
    int lastEnd = 0;

    for (final match in regex.allMatches(rawText)) {
      if (match.start > lastEnd) {
        spans.add(TextSpan(
          text: rawText.substring(lastEnd, match.start),
          style: TextStyle(
            color: isMe ? AppColors.textOnPrimary : AppColors.textPrimary,
            fontSize: 14,
          ),
        ));
      }

      final taskIdStr = match.group(1)!;
      final taskTitle = match.group(2)!;
      final taskId = int.tryParse(taskIdStr) ?? 0;

      spans.add(WidgetSpan(
        alignment: PlaceholderAlignment.middle,
        child: GestureDetector(
          onTap: () {
            if (taskId > 0) {
              Navigator.of(context).pushNamed(
                AppRoutes.taskDetail,
                arguments: {'taskId': taskId.toString()},
              );
            }
          },
          child: Container(
            margin: const EdgeInsets.symmetric(horizontal: 2, vertical: 2),
            padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
            decoration: BoxDecoration(
              color: isMe ? Colors.white.withValues(alpha: 0.25) : AppColors.accentBlue.withValues(alpha: 0.15),
              borderRadius: BorderRadius.circular(AppDimens.radiusSm),
              border: Border.all(
                color: isMe ? Colors.white.withValues(alpha: 0.5) : AppColors.accentBlue.withValues(alpha: 0.4),
              ),
            ),
            child: Row(
              mainAxisSize: MainAxisSize.min,
              children: [
                Icon(
                  PhosphorIconsRegular.clipboardText,
                  size: 14,
                  color: isMe ? Colors.white : AppColors.accentBlue,
                ),
                const SizedBox(width: 4),
                AppText(
                  '#$taskId $taskTitle',
                  variant: AppTextVariant.caption,
                  color: isMe ? Colors.white : AppColors.accentBlue,
                  weight: FontWeight.bold,
                ),
              ],
            ),
          ),
        ),
      ));

      lastEnd = match.end;
    }

    if (lastEnd < rawText.length) {
      spans.add(TextSpan(
        text: rawText.substring(lastEnd),
        style: TextStyle(
          color: isMe ? AppColors.textOnPrimary : AppColors.textPrimary,
          fontSize: 14,
        ),
      ));
    }

    return RichText(
      text: TextSpan(children: spans),
    );
  }

  @override
  Widget build(BuildContext context) {
    final timeStr = message.createdAt != null
        ? DateFormat('HH:mm dd/MM').format(message.createdAt!)
        : 'Vừa xong';

    return Padding(
      padding: const EdgeInsets.only(bottom: AppDimens.space12),
      child: Row(
        mainAxisAlignment:
            isMe ? MainAxisAlignment.end : MainAxisAlignment.start,
        crossAxisAlignment: CrossAxisAlignment.end,
        children: [
          if (!isMe) ...[
            _Avatar(name: message.senderName),
            const SizedBox(width: AppDimens.space8),
          ],
          Flexible(
            child: Column(
              crossAxisAlignment:
                  isMe ? CrossAxisAlignment.end : CrossAxisAlignment.start,
              children: [
                if (!isMe)
                  Padding(
                    padding: const EdgeInsets.only(
                      left: AppDimens.space4,
                      bottom: AppDimens.space4,
                    ),
                    child: AppText(
                      message.senderName,
                      variant: AppTextVariant.caption,
                      color: AppColors.accentBlue,
                    ),
                  ),
                Container(
                  padding: const EdgeInsets.symmetric(
                    horizontal: AppDimens.space16,
                    vertical: AppDimens.space12,
                  ),
                  decoration: BoxDecoration(
                    color: isMe ? AppColors.primary : AppColors.surface,
                    borderRadius: BorderRadius.only(
                      topLeft: const Radius.circular(AppDimens.radiusLg),
                      topRight: const Radius.circular(AppDimens.radiusLg),
                      bottomLeft: Radius.circular(isMe ? AppDimens.radiusLg : AppDimens.space4),
                      bottomRight: Radius.circular(isMe ? AppDimens.space4 : AppDimens.radiusLg),
                    ),
                    border: isMe
                        ? null
                        : Border.all(
                            color: AppColors.border,
                            width: 1,
                          ),
                  ),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      if (message.content.isNotEmpty) _buildContent(context),
                      if (message.hasAttachment) ...[
                        if (message.content.isNotEmpty)
                          const SizedBox(height: AppDimens.space8),
                        if (message.isImage)
                          GestureDetector(
                            onTap: () => _showFullImage(context, message.resolvedAttachmentUrl),
                            child: ClipRRect(
                              borderRadius: BorderRadius.circular(AppDimens.radiusMd),
                              child: ConstrainedBox(
                                constraints: const BoxConstraints(
                                  maxHeight: 200,
                                  maxWidth: 260,
                                ),
                                child: CachedNetworkImage(
                                  imageUrl: message.resolvedAttachmentUrl,
                                  fit: BoxFit.cover,
                                  placeholder: (_, __) => const SizedBox(
                                    height: 120,
                                    child: Center(child: AppLoading()),
                                  ),
                                  errorWidget: (_, __, ___) => Container(
                                    height: 80,
                                    color: Colors.black12,
                                    alignment: Alignment.center,
                                    child: const Icon(PhosphorIconsRegular.imageBroken),
                                  ),
                                ),
                              ),
                            ),
                          )
                        else if (message.isVideo)
                          Container(
                            padding: const EdgeInsets.all(AppDimens.space8),
                            decoration: BoxDecoration(
                              color: isMe ? Colors.white.withValues(alpha: 0.2) : AppColors.surfaceVariant,
                              borderRadius: BorderRadius.circular(AppDimens.radiusMd),
                            ),
                            child: Row(
                              mainAxisSize: MainAxisSize.min,
                              children: [
                                Icon(
                                  PhosphorIconsRegular.videoCamera,
                                  color: isMe ? Colors.white : AppColors.accentBlue,
                                  size: 24,
                                ),
                                const SizedBox(width: AppDimens.space8),
                                Flexible(
                                  child: Column(
                                    crossAxisAlignment: CrossAxisAlignment.start,
                                    children: [
                                      AppText(
                                        message.attachmentName.isNotEmpty ? message.attachmentName : 'Video đính kèm',
                                        variant: AppTextVariant.caption,
                                        color: isMe ? Colors.white : AppColors.textPrimary,
                                        weight: FontWeight.bold,
                                        maxLines: 1,
                                        overflow: TextOverflow.ellipsis,
                                      ),
                                      if (message.attachmentSizeLabel.isNotEmpty)
                                        AppText(
                                          message.attachmentSizeLabel,
                                          variant: AppTextVariant.caption,
                                          color: isMe ? Colors.white70 : AppColors.textSecondary,
                                        ),
                                    ],
                                  ),
                                ),
                              ],
                            ),
                          )
                        else
                          Container(
                            padding: const EdgeInsets.all(AppDimens.space8),
                            decoration: BoxDecoration(
                              color: isMe ? Colors.white.withValues(alpha: 0.2) : AppColors.surfaceVariant,
                              borderRadius: BorderRadius.circular(AppDimens.radiusMd),
                            ),
                            child: Row(
                              mainAxisSize: MainAxisSize.min,
                              children: [
                                Icon(
                                  PhosphorIconsRegular.file,
                                  color: isMe ? Colors.white : AppColors.accentBlue,
                                  size: 24,
                                ),
                                const SizedBox(width: AppDimens.space8),
                                Flexible(
                                  child: Column(
                                    crossAxisAlignment: CrossAxisAlignment.start,
                                    children: [
                                      AppText(
                                        message.attachmentName.isNotEmpty ? message.attachmentName : 'Tài liệu đính kèm',
                                        variant: AppTextVariant.caption,
                                        color: isMe ? Colors.white : AppColors.textPrimary,
                                        weight: FontWeight.bold,
                                        maxLines: 1,
                                        overflow: TextOverflow.ellipsis,
                                      ),
                                      if (message.attachmentSizeLabel.isNotEmpty)
                                        AppText(
                                          message.attachmentSizeLabel,
                                          variant: AppTextVariant.caption,
                                          color: isMe ? Colors.white70 : AppColors.textSecondary,
                                        ),
                                    ],
                                  ),
                                ),
                              ],
                            ),
                          ),
                      ],
                    ],
                  ),
                ),
                Padding(
                  padding: const EdgeInsets.only(
                    top: AppDimens.space4,
                    left: AppDimens.space4,
                    right: AppDimens.space4,
                  ),
                  child: AppText(
                    timeStr,
                    variant: AppTextVariant.caption,
                    color: AppColors.textSecondary,
                  ),
                ),
              ],
            ),
          ),
          if (isMe) const SizedBox(width: AppDimens.space8),
        ],
      ),
    );
  }
}

class _Avatar extends StatelessWidget {
  const _Avatar({required this.name});

  final String name;

  @override
  Widget build(BuildContext context) {
    final initial = name.isNotEmpty ? name.trim().characters.first.toUpperCase() : 'U';

    return Container(
      width: 32,
      height: 32,
      decoration: BoxDecoration(
        color: AppColors.accentBlue.withValues(alpha: 0.2),
        shape: BoxShape.circle,
        border: Border.all(color: AppColors.accentBlue, width: 1),
      ),
      alignment: Alignment.center,
      child: AppText(
        initial,
        variant: AppTextVariant.caption,
        color: AppColors.accentBlue,
      ),
    );
  }
}

/// Bảng gợi ý thành viên khi gõ @
class _MentionSuggestions extends StatelessWidget {
  const _MentionSuggestions({
    required this.members,
    required this.query,
    required this.onSelect,
  });

  final List<ProjectMember> members;
  final String query;
  final ValueChanged<String> onSelect;

  @override
  Widget build(BuildContext context) {
    final filtered = members.where((m) {
      if (query.isEmpty) return true;
      return m.userFullName.toLowerCase().contains(query.toLowerCase());
    }).toList();

    if (filtered.isEmpty) return const SizedBox.shrink();

    return Container(
      constraints: const BoxConstraints(maxHeight: 160),
      margin: const EdgeInsets.symmetric(horizontal: AppDimens.space16),
      decoration: BoxDecoration(
        color: AppColors.surface,
        borderRadius: BorderRadius.circular(AppDimens.radiusMd),
        border: Border.all(color: AppColors.border),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withValues(alpha: 0.3),
            blurRadius: 8,
            offset: const Offset(0, -2),
          ),
        ],
      ),
      child: ListView.separated(
        shrinkWrap: true,
        itemCount: filtered.length,
        separatorBuilder: (_, __) => const Divider(
          height: 1,
          color: AppColors.border,
        ),
        itemBuilder: (context, index) {
          final member = filtered[index];
          return ListTile(
            dense: true,
            leading: _Avatar(name: member.userFullName),
            title: AppText(
              member.userFullName,
              variant: AppTextVariant.body,
            ),
            subtitle: member.role != null && member.role!.isNotEmpty
                ? AppText(
                    member.role!,
                    variant: AppTextVariant.caption,
                    color: AppColors.textSecondary,
                  )
                : null,
            onTap: () => onSelect(member.userFullName),
          );
        },
      ),
    );
  }
}

/// Bảng gợi ý công việc khi gõ /
class _TaskSuggestions extends StatelessWidget {
  const _TaskSuggestions({
    required this.tasks,
    required this.query,
    required this.onSelect,
  });

  final List<ProjectTaskOption> tasks;
  final String query;
  final ValueChanged<ProjectTaskOption> onSelect;

  @override
  Widget build(BuildContext context) {
    final filtered = tasks.where((t) {
      if (query.isEmpty) return true;
      return t.title.toLowerCase().contains(query.toLowerCase()) ||
          t.id.toString().contains(query) ||
          (t.code != null && t.code!.toLowerCase().contains(query.toLowerCase()));
    }).toList();

    if (filtered.isEmpty) {
      return Container(
        padding: const EdgeInsets.all(AppDimens.space12),
        margin: const EdgeInsets.symmetric(horizontal: AppDimens.space16),
        decoration: BoxDecoration(
          color: AppColors.surface,
          borderRadius: BorderRadius.circular(AppDimens.radiusMd),
          border: Border.all(color: AppColors.border),
        ),
        child: const AppText(
          'Không có công việc nào phù hợp trong dự án',
          variant: AppTextVariant.caption,
          color: AppColors.textSecondary,
        ),
      );
    }

    return Container(
      constraints: const BoxConstraints(maxHeight: 180),
      margin: const EdgeInsets.symmetric(horizontal: AppDimens.space16),
      decoration: BoxDecoration(
        color: AppColors.surface,
        borderRadius: BorderRadius.circular(AppDimens.radiusMd),
        border: Border.all(color: AppColors.border),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withValues(alpha: 0.3),
            blurRadius: 8,
            offset: const Offset(0, -2),
          ),
        ],
      ),
      child: ListView.separated(
        shrinkWrap: true,
        itemCount: filtered.length,
        separatorBuilder: (_, __) => const Divider(
          height: 1,
          color: AppColors.border,
        ),
        itemBuilder: (context, index) {
          final task = filtered[index];
          return ListTile(
            dense: true,
            leading: Container(
              padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
              decoration: BoxDecoration(
                color: AppColors.accentBlue.withValues(alpha: 0.15),
                borderRadius: BorderRadius.circular(4),
              ),
              child: AppText(
                '#${task.id}',
                variant: AppTextVariant.caption,
                color: AppColors.accentBlue,
                weight: FontWeight.bold,
              ),
            ),
            title: AppText(
              task.title,
              variant: AppTextVariant.body,
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
            ),
            subtitle: AppText(
              'Trạng thái: ${task.state ?? "Mới"}${task.assigneeName != null ? " · ${task.assigneeName}" : ""}',
              variant: AppTextVariant.caption,
              color: AppColors.textSecondary,
            ),
            onTap: () => onSelect(task),
          );
        },
      ),
    );
  }
}

/// Thanh nhập nội dung tin nhắn chat ở đáy màn hình
class _InputBar extends StatelessWidget {
  const _InputBar({
    required this.controller,
    required this.isSending,
    required this.onChanged,
    required this.onSend,
    required this.onTapAdd,
  });

  final TextEditingController controller;
  final bool isSending;
  final ValueChanged<String> onChanged;
  final VoidCallback onSend;
  final VoidCallback onTapAdd;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(
        horizontal: AppDimens.space12,
        vertical: AppDimens.space8,
      ),
      decoration: const BoxDecoration(
        color: AppColors.surface,
        border: Border(
          top: BorderSide(color: AppColors.border, width: 1),
        ),
      ),
      child: Row(
        children: [
          AppIconButton(
            icon: PhosphorIconsRegular.plusCircle,
            tooltip: 'Tùy chọn đính kèm & Chèn',
            color: AppColors.accentBlue,
            size: 26,
            onPressed: onTapAdd,
          ),
          const SizedBox(width: AppDimens.space8),
          Expanded(
            child: AppTextField(
              label: 'Nội dung',
              controller: controller,
              hint: 'Nhập tin nhắn (gõ / hoặc @)...',
              onChanged: onChanged,
              textInputAction: TextInputAction.send,
              onFieldSubmitted: (_) => onSend(),
            ),
          ),
          const SizedBox(width: AppDimens.space8),
          if (isSending)
            const Padding(
              padding: EdgeInsets.all(12),
              child: SizedBox(
                width: 24,
                height: 24,
                child: AppLoading(),
              ),
            )
          else
            AppIconButton(
              icon: PhosphorIconsFill.paperPlaneRight,
              tooltip: 'Gửi tin nhắn',
              color: AppColors.primary,
              onPressed: onSend,
            ),
        ],
      ),
    );
  }
}

/// BottomSheet menu tùy chọn chèn & đính kèm
class _DiscussionActionMenuSheet extends StatelessWidget {
  const _DiscussionActionMenuSheet({
    required this.onTapAttach,
    required this.onTapTask,
    required this.onTapMention,
  });

  final VoidCallback onTapAttach;
  final VoidCallback onTapTask;
  final VoidCallback onTapMention;

  @override
  Widget build(BuildContext context) {
    return Container(
      decoration: const BoxDecoration(
        color: AppColors.surface,
        borderRadius: BorderRadius.vertical(
          top: Radius.circular(AppDimens.radiusLg),
        ),
      ),
      child: SafeArea(
        top: false,
        child: Padding(
          padding: const EdgeInsets.symmetric(vertical: AppDimens.space12),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              // Thanh kéo (Handle bar)
              Center(
                child: Container(
                  width: 36,
                  height: 4,
                  margin: const EdgeInsets.only(bottom: AppDimens.space12),
                  decoration: BoxDecoration(
                    color: AppColors.border,
                    borderRadius: BorderRadius.circular(2),
                  ),
                ),
              ),
              Padding(
                padding: const EdgeInsets.symmetric(horizontal: AppDimens.space16),
                child: Row(
                  children: [
                    const AppText(
                      'Tùy chọn đính kèm & Chèn',
                      variant: AppTextVariant.title,
                    ),
                    const Spacer(),
                    AppIconButton(
                      icon: PhosphorIconsRegular.x,
                      tooltip: 'Đóng',
                      size: 20,
                      onPressed: () => Navigator.of(context).pop(),
                    ),
                  ],
                ),
              ),
              const SizedBox(height: AppDimens.space8),
              const Divider(height: 1, color: AppColors.border),
              const SizedBox(height: AppDimens.space8),
              _ActionMenuItem(
                icon: PhosphorIconsRegular.paperclip,
                iconColor: AppColors.accentBlue,
                title: 'Đính kèm tệp / ảnh / video',
                subtitle: 'Chọn ảnh, video hoặc tài liệu từ thiết bị (≤ 10 MB)',
                onTap: () {
                  Navigator.of(context).pop();
                  onTapAttach();
                },
              ),
              _ActionMenuItem(
                icon: PhosphorIconsRegular.clipboardText,
                iconColor: AppColors.primary,
                title: 'Chèn liên kết công việc',
                subtitle: 'Gợi ý danh sách công việc có trong dự án (gõ /)',
                onTap: () {
                  Navigator.of(context).pop();
                  onTapTask();
                },
              ),
              _ActionMenuItem(
                icon: PhosphorIconsRegular.at,
                iconColor: AppColors.warning,
                title: 'Nhắc tên thành viên',
                subtitle: 'Tag tên thành viên trong nhóm trao đổi (gõ @)',
                onTap: () {
                  Navigator.of(context).pop();
                  onTapMention();
                },
              ),
              const SizedBox(height: AppDimens.space8),
            ],
          ),
        ),
      ),
    );
  }
}

class _ActionMenuItem extends StatelessWidget {
  const _ActionMenuItem({
    required this.icon,
    required this.iconColor,
    required this.title,
    required this.subtitle,
    required this.onTap,
  });

  final IconData icon;
  final Color iconColor;
  final String title;
  final String subtitle;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return InkWell(
      onTap: onTap,
      child: Padding(
        padding: const EdgeInsets.symmetric(
          horizontal: AppDimens.space16,
          vertical: AppDimens.space12,
        ),
        child: Row(
          children: [
            Container(
              width: 40,
              height: 40,
              decoration: BoxDecoration(
                color: iconColor.withValues(alpha: 0.15),
                shape: BoxShape.circle,
              ),
              alignment: Alignment.center,
              child: Icon(icon, color: iconColor, size: 22),
            ),
            const SizedBox(width: AppDimens.space12),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  AppText(
                    title,
                    variant: AppTextVariant.body,
                    weight: FontWeight.w600,
                  ),
                  const SizedBox(height: 2),
                  AppText(
                    subtitle,
                    variant: AppTextVariant.caption,
                    color: AppColors.textSecondary,
                  ),
                ],
              ),
            ),
            const Icon(
              PhosphorIconsRegular.caretRight,
              color: AppColors.textSecondary,
              size: 16,
            ),
          ],
        ),
      ),
    );
  }
}
