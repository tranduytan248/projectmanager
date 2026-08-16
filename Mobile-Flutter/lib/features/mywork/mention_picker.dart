import 'package:flutter/material.dart';

import '../../core/theme/app_colors.dart';
import '../../core/theme/app_dimens.dart';
import '../../core/theme/app_text_styles.dart';
import '../../core/widgets/app_card.dart';
import '../../core/widgets/app_rich_editor.dart';
import '../../core/widgets/app_text.dart';
import 'task_detail_models.dart';

/// Logic goi y @nhac ten trong AppRichEditor — port dung luat cua web
/// (Views/Checklist/_Detail.cshtml, ham tokenAtCaret/renderMenu/pick) sang Dart thuan, khong phu
/// thuoc Flutter, de test doc lap duoc.

class MentionMatch {
  const MentionMatch(
      {required this.start, required this.end, required this.query});

  /// Vi tri ky tu "@" trong chuoi.
  final int start;

  /// Vi tri con tro (cuoi doan query, KHONG gom ky tu "@").
  final int end;

  final String query;
}

final RegExp _boundaryBeforeAt = RegExp(r'''[\s.,;:!?()\[\]"' ]''');

/// Tim doan "@query" ngay truoc con tro, neu co — dung luat y het web:
/// - "@" phai o dau chuoi hoac ngay sau khoang trang/dau cau.
/// - Doan query khong qua 40 ky tu, khong chua xuong dong.
MentionMatch? detectMention(String text, int caretOffset) {
  if (caretOffset < 0 || caretOffset > text.length) return null;

  var i = caretOffset - 1;
  while (i >= 0) {
    final ch = text[i];
    if (ch == '\n') return null;
    if (ch == '@') {
      final before = i == 0 ? null : text[i - 1];
      final okBefore = before == null || _boundaryBeforeAt.hasMatch(before);
      if (!okBefore) return null;

      final query = text.substring(i + 1, caretOffset);
      if (query.length > 40 || query.contains('\n')) return null;

      return MentionMatch(start: i, end: caretOffset, query: query);
    }
    i--;
  }
  return null;
}

const Map<String, String> _diacriticsMap = {
  'à': 'a',
  'á': 'a',
  'ả': 'a',
  'ã': 'a',
  'ạ': 'a',
  'ă': 'a',
  'ằ': 'a',
  'ắ': 'a',
  'ẳ': 'a',
  'ẵ': 'a',
  'ặ': 'a',
  'â': 'a',
  'ầ': 'a',
  'ấ': 'a',
  'ẩ': 'a',
  'ẫ': 'a',
  'ậ': 'a',
  'è': 'e',
  'é': 'e',
  'ẻ': 'e',
  'ẽ': 'e',
  'ẹ': 'e',
  'ê': 'e',
  'ề': 'e',
  'ế': 'e',
  'ể': 'e',
  'ễ': 'e',
  'ệ': 'e',
  'ì': 'i',
  'í': 'i',
  'ỉ': 'i',
  'ĩ': 'i',
  'ị': 'i',
  'ò': 'o',
  'ó': 'o',
  'ỏ': 'o',
  'õ': 'o',
  'ọ': 'o',
  'ô': 'o',
  'ồ': 'o',
  'ố': 'o',
  'ổ': 'o',
  'ỗ': 'o',
  'ộ': 'o',
  'ơ': 'o',
  'ờ': 'o',
  'ớ': 'o',
  'ở': 'o',
  'ỡ': 'o',
  'ợ': 'o',
  'ù': 'u',
  'ú': 'u',
  'ủ': 'u',
  'ũ': 'u',
  'ụ': 'u',
  'ư': 'u',
  'ừ': 'u',
  'ứ': 'u',
  'ử': 'u',
  'ữ': 'u',
  'ự': 'u',
  'ỳ': 'y',
  'ý': 'y',
  'ỷ': 'y',
  'ỹ': 'y',
  'ỵ': 'y',
  'đ': 'd',
};

/// Bo dau tieng Viet — Dart khong co String.normalize('NFD') nhu JS nen dung bang tra thu cong,
/// dung y nghia voi ham plain() ben web (ha chu thuong, bo dau, đ→d).
String stripDiacritics(String input) {
  final lower = input.toLowerCase();
  final buffer = StringBuffer();
  for (final rune in lower.runes) {
    final ch = String.fromCharCode(rune);
    buffer.write(_diacriticsMap[ch] ?? ch);
  }
  return buffer.toString();
}

bool _matchesQuery(TaskMentionOption option, String plainQuery) {
  if (plainQuery.isEmpty) return true;

  final plainName = stripDiacritics(option.fullName);
  if (plainName.startsWith(plainQuery)) return true;

  final userName = option.userName;
  if (userName != null && stripDiacritics(userName).startsWith(plainQuery)) {
    return true;
  }

  for (final word in plainName.split(' ')) {
    if (word.startsWith(plainQuery)) return true;
  }
  return false;
}

/// Loc danh sach nguoi tham gia theo query da go sau "@" — y het thuat toan renderMenu ben web.
List<TaskMentionOption> filterMentionParticipants(
    List<TaskMentionOption> participants, String query) {
  final plainQuery = stripDiacritics(query.trim());
  return participants.where((p) => _matchesQuery(p, plainQuery)).toList();
}

/// Chen "@Ho Ten " (van ban thuan, co dau, mot khoang trang cuoi) tai dung vi tri query dang go —
/// y het cach pick() ben web dung: NotificationService.Mentions so khop nguyen van chuoi nay.
void insertMention(AppRichEditorController controller, MentionMatch match,
    TaskMentionOption option) {
  final text = controller.text;
  final before = text.substring(0, match.start);
  final after = text.substring(match.end);
  final insertion = '@${option.fullName} ';
  final newText = before + insertion + after;

  controller.value = TextEditingValue(
    text: newText,
    selection:
        TextSelection.collapsed(offset: before.length + insertion.length),
  );
}

/// Danh sach goi y hien NGAY TREN AppRichEditor khi dang go "@" (o task_comments_screen.dart, o
/// nhap nam duoi cung man hinh sat ban phim nen dat goi y phia tren de con khoang trong hien du,
/// khong bi ban phim che) — dat trong luong bo cuc binh thuong (khong dung Overlay/vi tri con tro
/// chinh xac tren man hinh) de don gian va chac chan dung, phu hop quy mo "toi gian" cua tinh
/// nang nay.
class MentionSuggestionList extends StatelessWidget {
  const MentionSuggestionList({
    super.key,
    required this.controller,
    required this.participants,
  });

  final AppRichEditorController controller;
  final List<TaskMentionOption> participants;

  @override
  Widget build(BuildContext context) {
    return AnimatedBuilder(
      animation: controller,
      builder: (context, _) {
        final match =
            detectMention(controller.text, controller.selection.start);
        if (match == null) return const SizedBox.shrink();

        final matches = filterMentionParticipants(participants, match.query);
        if (matches.isEmpty) return const SizedBox.shrink();

        return Padding(
          padding: const EdgeInsets.only(top: AppDimens.space4),
          child: AppCard(
            padding: const EdgeInsets.symmetric(vertical: 4),
            child: ConstrainedBox(
              constraints: const BoxConstraints(maxHeight: 180),
              child: ListView.builder(
                shrinkWrap: true,
                padding: EdgeInsets.zero,
                itemCount: matches.length,
                itemBuilder: (context, index) {
                  final option = matches[index];
                  return InkWell(
                    onTap: () => insertMention(controller, match, option),
                    child: ConstrainedBox(
                      constraints: const BoxConstraints(
                          minHeight: AppDimens.minTapTarget),
                      child: Padding(
                        padding: const EdgeInsets.symmetric(
                            horizontal: AppDimens.space12,
                            vertical: AppDimens.space8),
                        child: Align(
                          alignment: Alignment.centerLeft,
                          child: Row(
                            mainAxisSize: MainAxisSize.min,
                            children: [
                              AppText(option.fullName,
                                  variant: AppTextVariant.body,
                                  fontSize: 13.5,
                                  weight: FontWeight.w700),
                              if (option.userName?.isNotEmpty == true) ...[
                                const SizedBox(width: AppDimens.space8),
                                AppText('· ${option.userName}',
                                    variant: AppTextVariant.caption,
                                    fontSize: 12,
                                    color: AppColors.textSecondary),
                              ],
                            ],
                          ),
                        ),
                      ),
                    ),
                  );
                },
              ),
            ),
          ),
        );
      },
    );
  }
}
