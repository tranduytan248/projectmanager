# Rules Bắt Buộc Toàn Dự Án — ProjectManager & BrewTask

Xem chi tiết đầy đủ tại `AGENTS.md` và `.agents/rules/`.

## Quy tắc cốt lõi:
1. **Tiếng Việt có dấu 100%** trong UI, thông báo, tài liệu, comment.
2. **Flutter (`Mobile-Flutter/`)**: CẤM dùng widget gốc (`CircularProgressIndicator`, `TextField`, `Text`, `Checkbox`, `DropdownButton`, `FloatingActionButton`, `Card`). LUÔN DÙNG 100% `App*` (`AppLoading`, `AppTextField`, `AppText`, `AppDropdown`, `AppCheckbox`, `AppFab`, `AppCard`, `AppButton`, `AppErrorState`).
3. **Màu sắc & Khoảng cách**: 100% dùng `AppColors` và `AppDimens` (bội số 4, touch target ≥ 48dp).
4. **Quy trình Git**: Khi user nói "đẩy code", "push code", LUÔN dùng skill `git-push-merge` (nhánh hiện tại -> main -> upload-source).
5. **Backend C# / SQL**: Tuân thủ `C_SHARP_RULES.md` và `SQL_SERVER_RULES.md`.
