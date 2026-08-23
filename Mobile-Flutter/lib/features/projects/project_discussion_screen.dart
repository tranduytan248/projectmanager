import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import 'package:phosphor_icons/phosphor_icons.dart';

import '../../config/app_cache.dart';
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
import '../../core/widgets/app_text_field.dart';
import 'project_detail_models.dart';
import 'project_discussion_models.dart';
import 'project_discussion_service.dart';

/// Màn hình Trao đổi / Thảo luận trong Dự án (Real-time qua Firebase Cloud Firestore).
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
  }

  @override
  void dispose() {
    _service.markAsRead(widget.projectId);
    _textController.dispose();
    _scrollController.dispose();
    super.dispose();
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

  Future<void> _sendMessage() async {
    final text = _textController.text.trim();
    if (text.isEmpty || _isSending) return;

    setState(() => _isSending = true);
    _textController.clear();
    setState(() => _showMentionPicker = false);

    try {
      await _service.sendMessage(
        projectId: widget.projectId,
        content: text,
        senderId: _currentUserId,
        senderName: _currentDisplayName,
        senderUsername: _currentUsername,
        projectName: widget.projectName,
      );
      _scrollToBottom();
      _service.markAsRead(widget.projectId);
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: AppText('Lỗi gửi tin nhắn: $e'),
            backgroundColor: AppColors.danger,
          ),
        );
      }
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
    final lastAt = beforeCursor.lastIndexOf('@');

    if (lastAt != -1) {
      final query = beforeCursor.substring(lastAt + 1);
      if (!query.contains(' ')) {
        setState(() {
          _showMentionPicker = true;
          _mentionQuery = query;
        });
        return;
      }
    }

    if (_showMentionPicker) {
      setState(() => _showMentionPicker = false);
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

            // Thanh soạn thảo tin nhắn
            _InputBar(
              controller: _textController,
              isSending: _isSending,
              onChanged: _onTextChanged,
              onSend: _sendMessage,
              onTapMention: () {
                final text = _textController.text;
                _textController.text = '$text@';
                _textController.selection = TextSelection.fromPosition(
                    TextPosition(offset: _textController.text.length));
                setState(() {
                  _showMentionPicker = true;
                  _mentionQuery = '';
                });
              },
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
                  child: AppText(
                    message.content,
                    variant: AppTextVariant.body,
                    color: isMe ? AppColors.textOnPrimary : AppColors.textPrimary,
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
      return m.userFullName.toLowerCase().contains(query);
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

/// Thanh nhập nội dung tin nhắn chat ở đáy màn hình
class _InputBar extends StatelessWidget {
  const _InputBar({
    required this.controller,
    required this.isSending,
    required this.onChanged,
    required this.onSend,
    required this.onTapMention,
  });

  final TextEditingController controller;
  final bool isSending;
  final ValueChanged<String> onChanged;
  final VoidCallback onSend;
  final VoidCallback onTapMention;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(
        horizontal: AppDimens.space16,
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
            icon: PhosphorIconsRegular.at,
            tooltip: 'Nhắc tên thành viên (@)',
            color: AppColors.accentBlue,
            onPressed: onTapMention,
          ),
          const SizedBox(width: AppDimens.space8),
          Expanded(
            child: AppTextField(
              label: 'Nội dung',
              controller: controller,
              hint: 'Nhập nội dung trao đổi...',
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
