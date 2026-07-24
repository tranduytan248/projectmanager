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

// Đóng/mở menu dọc trên màn hình hẹp. Trên màn hình rộng menu luôn hiện nên nút ba gạch
// bị ẩn và đoạn này không đụng tới gì.
(function ($) {
    'use strict';

    function setOpen(isOpen) {
        $('body').toggleClass('menu-open', isOpen);
        $('[data-menu-toggle]').attr('aria-expanded', isOpen ? 'true' : 'false');
    }

    $(function () {
        $('[data-menu-toggle]').on('click', function () {
            setOpen(!$('body').hasClass('menu-open'));
        });

        $('[data-menu-close]').on('click', function () {
            setOpen(false);
        });

        // Phím Esc đóng menu, cho người quen dùng bàn phím.
        $(document).on('keydown', function (e) {
            if (e.key === 'Escape') setOpen(false);
        });

        // Thu gọn / mở lại menu dọc (màn hình rộng). Lưu trạng thái để giữ nguyên khi tải lại.
        $('[data-sidebar-toggle]').on('click', function () {
            var collapsed = !$('body').hasClass('sidebar-collapsed');
            $('body').toggleClass('sidebar-collapsed', collapsed);
            try { localStorage.setItem('sidebarCollapsed', collapsed ? '1' : '0'); } catch (e) {}
        });
    });
})(jQuery);

// Nút "Sao chép" các ô có khoá/giá trị: chép nội dung ô đích vào clipboard, báo nhanh trên nút.
(function ($) {
    'use strict';

    function copyText(text) {
        // Ưu tiên Clipboard API; nếu không có (http, trình duyệt cũ) thì lùi về execCommand.
        if (navigator.clipboard && window.isSecureContext) {
            return navigator.clipboard.writeText(text);
        }
        return new Promise(function (resolve, reject) {
            var ta = document.createElement('textarea');
            ta.value = text;
            ta.style.position = 'fixed';
            ta.style.opacity = '0';
            document.body.appendChild(ta);
            ta.select();
            try { document.execCommand('copy') ? resolve() : reject(); }
            catch (e) { reject(e); }
            document.body.removeChild(ta);
        });
    }

    $(function () {
        $(document).on('click', '[data-copy-target]', function () {
            var $btn = $(this);
            var $src = $($btn.data('copy-target'));
            if (!$src.length) return;

            copyText($src.val() != null ? $src.val() : $src.text()).then(function () {
                var old = $btn.text();
                $btn.text('Đã chép');
                setTimeout(function () { $btn.text(old); }, 1500);
            });
        });
    });
})(jQuery);
