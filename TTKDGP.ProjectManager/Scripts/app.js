// Khởi tạo Select2 cho toàn bộ dropdown trong hệ thống.
// Danh sách dài (dự án 51 mục, tuần 53 mục) không thể cuộn tay để tìm, nên cần ô tìm kiếm.
(function ($) {
    'use strict';

    // Dropdown ít lựa chọn thì không cần ô tìm kiếm, hiện ra chỉ thêm rối.
    var SEARCH_THRESHOLD = 8;

    function initSelect2(context) {
        $(context || document).find('select').each(function () {
            var $select = $(this);

            // Cho phép loại trừ từng ô bằng data-no-select2 nếu về sau cần
            if ($select.is('[data-no-select2]')) return;
            if ($select.hasClass('select2-hidden-accessible')) return;

            var optionCount = $select.find('option').length;
            var $first = $select.find('option').first();
            var hasEmptyOption = $first.length > 0 && $first.val() === '';

            var options = {
                language: 'vi',
                width: 'resolve',
                minimumResultsForSearch: optionCount >= SEARCH_THRESHOLD ? 0 : Infinity
            };

            // Ô đầu tiên rỗng (— Tất cả —, — Chọn ... —) đóng vai trò gợi ý,
            // kèm nút x để bỏ chọn nhanh.
            if (hasEmptyOption) {
                options.placeholder = $first.text();
                options.allowClear = true;
            }

            $select.select2(options);
        });
    }

    $(function () {
        initSelect2(document);
    });

    // Trang khai báo nhật ký dựng lại danh sách tuần khi đổi năm; Select2 cần được
    // báo để vẽ lại phần hiển thị.
    $(document).on('optionsChanged', 'select', function () {
        $(this).trigger('change');
    });

    window.AppSelect2 = { init: initSelect2 };
})(jQuery);
