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

            // Ô nằm trong hộp thoại thì thả danh sách chọn NGAY TRONG hộp. Mặc định Select2 gắn
            // danh sách vào thẳng <body>, nằm ngoài hộp — danh sách khi đó đứng yên một chỗ
            // trong lúc nội dung hộp cuộn bên dưới, trông như bị lệch khỏi ô.
            var $dialog = $select.closest('.modal');
            if ($dialog.length) options.dropdownParent = $dialog;

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

// Menu thao tác trên từng dòng bảng dựng bằng <details>. Trình duyệt không tự đóng khi bấm ra
// ngoài, nên mở vài dòng là màn hình đầy menu chồng nhau — phải tự đóng.
(function ($) {
    'use strict';

    function closeAll(except) {
        $('details.row-menu[open]').each(function () {
            if (this === except) return;
            this.removeAttribute('open');

            // Trả bảng chọn về đúng cách đặt của CSS. place() vẫn ghi đè đủ các thuộc tính ở lần
            // mở sau, nhưng để lại toạ độ cũ thì trạng thái lúc đóng phụ thuộc vào lần mở trước.
            $(this).children('.row-menu-items').css({ position: '', top: '', left: '', right: '' });
        });
    }

    /*
      Đặt bảng chọn theo toạ độ màn hình.

      Bảng nằm trong khung cuộn ngang được (.table-wrap), mà khung đã cắt tràn thì menu con
      cũng bị cắt theo. Gỡ bằng cách khi mở thì tính sẵn toạ độ rồi neo menu theo màn hình —
      menu thoát khỏi khung nên không bị cắt, còn khung vẫn cuộn ngang bình thường.

      Mặc định thả xuống dưới và canh phải theo nút; sát mép nào thì lật về phía còn chỗ.
    */
    function place($menu, summary) {
        var rect = summary.getBoundingClientRect();
        var margin = 8;

        $menu.css({ position: 'fixed', top: '0px', left: '0px', right: 'auto' });

        var width = $menu.outerWidth();
        var height = $menu.outerHeight();
        var viewW = $(window).width();
        var viewH = $(window).height();

        // Canh mép phải của menu theo mép phải của nút, rồi kéo lại nếu lọt ra ngoài màn hình.
        var left = rect.right - width;
        if (left + width > viewW - margin) left = viewW - margin - width;
        if (left < margin) left = margin;

        // Không đủ chỗ bên dưới thì lật lên trên nút.
        var top = rect.bottom + 4;
        if (top + height > viewH - margin && rect.top - height - 4 >= margin) {
            top = rect.top - height - 4;
        }
        if (top < margin) top = margin;

        $menu.css({ top: top + 'px', left: left + 'px' });
    }

    function placeOpen() {
        $('details.row-menu[open]').each(function () {
            place($(this).children('.row-menu-items'), $(this).children('summary')[0]);
        });
    }

    $(function () {
        // Mở menu này thì đóng các menu khác ngay, khỏi chờ tới lượt bấm ra ngoài.
        $(document).on('click', 'details.row-menu > summary', function () {
            var details = $(this).parent()[0];
            closeAll(details);

            // Lúc bấm thì <details> chưa kịp đổi trạng thái, nên đo ở nhịp kế tiếp.
            var summary = this;
            window.setTimeout(function () {
                if (details.hasAttribute('open')) {
                    place($(details).children('.row-menu-items'), summary);
                }
            }, 0);
        });

        // Chọn xong một mục thì đóng menu. Thiếu bước này, menu neo theo màn hình vẫn mở trong khi
        // hộp thoại của mục vừa chọn hiện lên, rồi trôi theo lúc cuộn trang.
        $(document).on('click', '.row-menu-items a, .row-menu-items button', function () {
            closeAll(null);
        });

        $(document).on('mousedown', function (e) {
            if (!$(e.target).closest('details.row-menu').length) closeAll(null);
        });

        $(document).on('keydown', function (e) {
            if (e.key === 'Escape') closeAll(null);
        });

        // Neo theo màn hình thì menu không tự đi theo khi trang/bảng cuộn — tính lại toạ độ.
        // Bắt ở nhánh capture để nhận cả cuộn bên trong .table-wrap.
        window.addEventListener('scroll', placeOpen, true);
        $(window).on('resize', placeOpen);
    });
})(jQuery);

// Hộp thoại thêm/sửa. Nội dung form được nạp từ máy chủ nên chỉ có một nơi định nghĩa form,
// dùng chung cho cả khi mở bằng hộp thoại lẫn khi mở thẳng bằng đường dẫn.
(function ($) {
    'use strict';

    var $backdrop, $modal, $title, $body;

    function ensureShell() {
        if ($backdrop) return;

        $backdrop = $('<div class="modal-backdrop" role="dialog" aria-modal="true">' +
            '<div class="modal">' +
            '<div class="modal-head"><span class="modal-title"></span>' +
            '<button type="button" class="modal-close" aria-label="Đóng">&times;</button></div>' +
            '<div class="modal-body"></div>' +
            '</div></div>').appendTo('body');

        $modal = $backdrop.find('.modal');
        $title = $backdrop.find('.modal-title');
        $body = $backdrop.find('.modal-body');

        // CỐ Ý KHÔNG đóng hộp thoại khi bấm ra nền.
        //
        // Hộp thoại ở đây gần như luôn là một biểu mẫu đang nhập dở — có ô mô tả nhiều dòng và
        // trình soạn thảo. Đóng bằng một cú bấm hụt ra nền là xoá sạch những gì đã gõ mà không
        // hỏi han gì, và người dùng không có cách nào lấy lại. Cái giá của việc bấm nhầm lớn hơn
        // hẳn chút tiện lợi của thao tác bấm-nền-để-đóng.
        //
        // Vẫn còn ba lối đóng rõ ràng: nút ✕ ở góc, nút Huỷ trong form, và phím Esc.
        $backdrop.on('click', '.modal-close', close);
        $(document).on('keydown', function (e) {
            if (e.key === 'Escape' && $backdrop.hasClass('open')) close();
        });
    }

    function close() {
        if (!$backdrop) return;
        $backdrop.removeClass('open');
        $body.empty();
        $('body').css('overflow', '');
    }

    function open(url, title, size) {
        ensureShell();

        $modal.removeClass('modal-sm modal-lg');
        if (size) $modal.addClass('modal-' + size);

        $title.text(title || '');
        $body.html('<div class="modal-loading">Đang tải…</div>');
        $backdrop.addClass('open');
        $('body').css('overflow', 'hidden');

        $.get(url)
            .done(function (html) {
                $body.html(html);
                if (window.AppSelect2) window.AppSelect2.init($body[0]);
                if (window.AppRichText) window.AppRichText.init($body[0]);
                if (window.AppTimeLog) window.AppTimeLog.init($body[0]);
                if (window.AppRequiredNote) window.AppRequiredNote.init($body[0]);
            })
            .fail(function () {
                $body.html('<div class="modal-loading">Không tải được nội dung.</div>');
            });
    }

    $(function () {
        $(document).on('click', '[data-modal-url]', function (e) {
            var $el = $(this);

            // Cả dòng lưới và cả thẻ Kanban đều mở được hộp thoại, nhưng bên trong chúng còn có
            // liên kết và nút riêng (menu thao tác, nút xoá, số lượt trao đổi). Bấm vào những chỗ
            // đó thì để chính chúng xử lý, đừng nuốt mất thành lệnh mở hộp thoại.
            var $inner = $(e.target).closest('a, button, input, select, textarea, summary');
            if ($inner.length && !$inner.is($el)) return;

            e.preventDefault();
            // Dòng lồng trong dòng: chỉ phần tử trong cùng được mở, không mở chồng hai hộp thoại.
            e.stopPropagation();

            open($el.data('modal-url'), $el.data('modal-title'), $el.data('modal-size'));
        });

        // Gửi form trong hộp thoại bằng AJAX. Máy chủ trả JSON khi lưu được, trả lại HTML của
        // form khi còn lỗi — nhờ vậy thông báo lỗi hiện ngay trong hộp, không mất dữ liệu đã nhập.
        $(document).on('submit', '.modal-body form', function (e) {
            var $form = $(this);

            // Form đánh dấu data-native-submit thì gửi kiểu thường (chuyển trang).
            if ($form.is('[data-native-submit]')) return;

            e.preventDefault();
            var $submit = $form.find('[type="submit"]').prop('disabled', true);

            // FormData mang được cả file đính kèm, khác serialize() chỉ lấy được ô chữ. Nhờ vậy
            // form có file vẫn báo lỗi ngay trong hộp thoại thay vì phải nhảy sang trang riêng.
            var options = { url: $form.attr('action'), type: 'POST' };
            if (window.FormData) {
                options.data = new FormData(this);
                options.processData = false;
                options.contentType = false;
            } else {
                options.data = $form.serialize();
            }

            $.ajax(options)
                .done(function (res, status, xhr) {
                    var type = xhr.getResponseHeader('Content-Type') || '';
                    if (type.indexOf('json') >= 0 && res && res.ok) {
                        // Máy chủ chỉ đường thì đi theo (ví dụ thêm dự án xong sang màn phân công),
                        // không thì nạp lại chỗ đang đứng — bộ lọc và số trang nằm sẵn trong địa chỉ.
                        if (res.redirect) window.location.href = res.redirect;
                        else window.location.reload();
                        return;
                    }
                    $body.html(res);
                    if (window.AppSelect2) window.AppSelect2.init($body[0]);
                    if (window.AppRichText) window.AppRichText.init($body[0]);
                    if (window.AppRequiredNote) window.AppRequiredNote.init($body[0]);
                })
                .fail(function (xhr) {
                    $submit.prop('disabled', false);

                    // Nói rõ hỏng ở đâu thay vì một câu chung chung. Phiên đăng nhập hết hạn là
                    // trường hợp hay gặp nhất và cũng dễ sửa nhất — nạp lại trang là xong, nên
                    // tách riêng ra; các mã còn lại kèm số để còn đối chiếu với App_Data\errors.log.
                    var msg;
                    if (xhr.status === 0) {
                        msg = 'Mất kết nối tới máy chủ. Kiểm tra mạng rồi thử lại.';
                    } else if (xhr.status === 403 || xhr.status === 401) {
                        msg = 'Phiên đăng nhập đã hết hạn hoặc bạn không có quyền. '
                            + 'Hãy tải lại trang và đăng nhập lại.';
                    } else if (xhr.status === 500) {
                        msg = 'Máy chủ gặp lỗi khi lưu (500). '
                            + 'Nếu vừa để trang mở lâu, hãy tải lại trang rồi nhập lại.';
                    } else {
                        msg = 'Không lưu được (mã ' + xhr.status + '). Hãy thử lại.';
                    }
                    alert(msg);
                });
        });
    });

    window.AppModal = { open: open, close: close };
})(jQuery);

// Chuông thông báo trên thanh đầu trang: bấm mở panel (nạp AJAX), bấm ra ngoài thì đóng.
(function ($) {
    'use strict';

    $(function () {
        var $wrap = $('[data-notif]');
        if (!$wrap.length) return;

        var $panel = $wrap.find('[data-notif-panel]');

        $wrap.on('click', '[data-notif-toggle]', function () {
            if ($wrap.hasClass('open')) {
                $wrap.removeClass('open');
                return;
            }

            $wrap.addClass('open');
            $panel.html('<div class="notif-empty">Đang tải…</div>');

            $.get($panel.data('url'))
                .done(function (html) { $panel.html(html); })
                .fail(function () { $panel.html('<div class="notif-empty">Không tải được thông báo.</div>'); });
        });

        $(document).on('mousedown', function (e) {
            if (!$(e.target).closest('[data-notif]').length) $wrap.removeClass('open');
        });

        // "Đánh dấu đã đọc tất cả": gửi ngầm rồi cập nhật ngay giao diện, không nạp lại trang.
        $wrap.on('submit', 'form[data-notif-readall]', function (e) {
            e.preventDefault();
            var $form = $(this);

            $.post($form.attr('action'), $form.serialize())
                .done(function () {
                    $wrap.find('.notif-badge').remove();
                    $panel.find('.notif-item').removeClass('unread');
                    $panel.find('.notif-dot').remove();
                    $form.remove();
                });
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

// Trình soạn thảo WYSIWYG gọn cho textarea[data-rich] — tự viết trên contenteditable, không
// thêm thư viện ngoài (server không có internet). Nội dung HTML đồng bộ ngược về textarea để
// gửi form như thường; máy chủ LUÔN lọc lại bằng HtmlSanitizer nên đây chỉ là lớp soạn thảo.
(function ($) {
    'use strict';

    var BUTTONS = [
        { cmd: 'bold', html: '<strong>B</strong>', title: 'Đậm' },
        { cmd: 'italic', html: '<em>I</em>', title: 'Nghiêng' },
        { cmd: 'underline', html: '<u>U</u>', title: 'Gạch chân' },
        { cmd: 'insertUnorderedList', html: '&bull; Danh sách', title: 'Danh sách chấm' },
        { cmd: 'insertOrderedList', html: '1. Danh sách', title: 'Danh sách đánh số' },
        { cmd: 'link', html: '&#128279; Liên kết', title: 'Chèn liên kết' },
        { cmd: 'removeFormat', html: 'X&#818;', title: 'Xoá định dạng' }
    ];

    function insertLink() {
        var url = window.prompt('Địa chỉ liên kết (http/https):', 'https://');
        if (!url) return;
        if (!/^https?:\/\//i.test(url)) url = 'https://' + url;

        var sel = window.getSelection();
        if (sel && !sel.isCollapsed) {
            document.execCommand('createLink', false, url);
        } else {
            // Chưa bôi đen gì thì chèn thẳng địa chỉ thành liên kết.
            var a = $('<a></a>').attr('href', url).text(url);
            document.execCommand('insertHTML', false, a[0].outerHTML);
        }
    }

    function init(root) {
        $(root || document).find('textarea[data-rich]').each(function () {
            var ta = this;
            if (ta.getAttribute('data-rich-ready')) return;
            ta.setAttribute('data-rich-ready', '1');

            var $ta = $(ta).hide();
            var $wrap = $('<div class="rich-editor"></div>').insertAfter($ta);
            var $bar = $('<div class="rich-toolbar"></div>').appendTo($wrap);
            var $area = $('<div class="rich-area" contenteditable="true"></div>').appendTo($wrap);

            // Dữ liệu cũ là chữ thường: mã hoá rồi đổi xuống dòng thành <br> để không mất dòng.
            var value = ta.value || '';
            if (value && value.indexOf('<') < 0) {
                value = $('<div></div>').text(value).html().replace(/\r?\n/g, '<br />');
            }
            $area.html(value);

            BUTTONS.forEach(function (b) {
                $('<button type="button" class="rich-btn"></button>')
                    .attr('title', b.title)
                    .html(b.html)
                    // preventDefault ở mousedown để không mất vùng bôi đen trong ô soạn thảo.
                    .on('mousedown', function (e) { e.preventDefault(); })
                    .on('click', function () {
                        $area.trigger('focus');
                        if (b.cmd === 'link') { insertLink(); } else { document.execCommand(b.cmd, false, null); }
                        sync();
                    })
                    .appendTo($bar);
            });

            function sync() { ta.value = $area.html(); }

            $area.on('input blur', sync);
            if (ta.form) $(ta.form).on('submit', sync);
        });
    }

    $(function () { init(document); });
    window.AppRichText = { init: init };
})(jQuery);

// Khối "Giờ công" (logtime) — dùng chung cho hộp thoại chi tiết và trang chi tiết công việc.
// Ghi/xoá bằng AJAX rồi thay cả khối bằng HTML máy chủ trả về, nên mọi con số (trần, còn lại)
// luôn do máy chủ tính — màn hình không tự cộng trừ để khỏi lệch với luật lúc lưu.
(function ($) {
    'use strict';

    function bind($box) {
        if (!$box.length || $box.attr('data-timelog-ready')) return;
        $box.attr('data-timelog-ready', '1');

        var taskId = $box.data('task-id');
        var addUrl = $box.data('add-url');
        var deleteUrl = $box.data('delete-url');

        // Token nằm NGOÀI khối được vẽ lại nên đọc theo tài liệu cha, và đọc lại ở mỗi lượt
        // gửi thay vì nhớ sẵn — phòng khi trang có nhiều khối cùng lúc.
        function token() {
            return $box.closest('.timelog-section, .card-body, body')
                .find('input[name="__RequestVerificationToken"]').first().val();
        }

        function failed(xhr) {
            // Vi phạm mốc chặn thì máy chủ trả 400 kèm đúng lý do — hiện nguyên văn câu đó.
            window.alert(xhr && xhr.status === 400 && xhr.responseText
                ? xhr.responseText
                : 'Không ghi được giờ công. Hãy thử lại.');
        }

        // Khối tự vẽ lại sau mỗi lần ghi/xoá nên các nút bên trong phải bắt theo kiểu uỷ quyền
        // từ thẻ bọc; gắn thẳng vào nút thì lần vẽ lại thứ hai là mất trình xử lý.
        $box.on('click', '#logTimeAdd', function () {
            var $btn = $(this);
            var date = $box.find('#logDate').val();
            var hours = $box.find('#logHours').val();

            if (!date) { window.alert('Hãy chọn ngày làm.'); return; }

            // Gửi NGUYÊN VĂN chuỗi người dùng gõ, để máy chủ tự đọc theo khuôn cố định. Đổi
            // dấu ở đây rồi để model binder đọc theo culture vi-VN là hỏng: ở đó dấu chấm là
            // phân cách hàng nghìn nên "0.5" biến thành 5 giờ và lọt qua mọi mốc chặn.
            hours = $.trim((hours || '').toString());
            if (!hours || parseFloat(hours.replace(',', '.')) <= 0 ||
                isNaN(parseFloat(hours.replace(',', '.')))) {
                window.alert('Hãy nhập số giờ lớn hơn 0.');
                return;
            }

            $btn.prop('disabled', true);

            $.post(addUrl, {
                id: taskId,
                workDate: date,
                hours: hours,
                note: $box.find('#logNote').val(),
                __RequestVerificationToken: token()
            })
                .done(function (html) { $box.html(html); })
                .fail(failed)
                .always(function () { $btn.prop('disabled', false); });
        });

        $box.on('click', '[data-timelog-id]', function () {
            if (!window.confirm('Xoá lượt ghi giờ này?')) return;

            $.post(deleteUrl, {
                id: taskId,
                logId: $(this).data('timelog-id'),
                __RequestVerificationToken: token()
            })
                .done(function (html) { $box.html(html); })
                .fail(failed);
        });

        // Enter trong ô số giờ hoặc ô nội dung là ghi luôn, khỏi phải rê chuột sang nút.
        $box.on('keydown', '#logHours, #logNote', function (e) {
            if (e.key === 'Enter') {
                e.preventDefault();
                $box.find('#logTimeAdd').trigger('click');
            }
        });
    }

    function init(root) {
        $(root || document).find('[data-timelog-box]').each(function () {
            bind($(this));
        });
    }

    $(function () { init(document); });
    window.AppTimeLog = { init: init };
})(jQuery);

// Kéo thả thẻ trên bảng Kanban.
// Dùng thẳng Drag and Drop của HTML5 — dự án không thêm thư viện ngoài.
(function ($) {
    'use strict';

    var DRAG_TYPE = 'text/plain';
    var $dragging = null;

    function board() {
        return $('[data-kanban]');
    }

    function refreshCounts() {
        board().find('.kanban-col').each(function () {
            var $col = $(this);
            $col.find('[data-count]').text($col.find('[data-card]').length);
        });
    }

    function fail($card, message) {
        $card.removeClass('is-saving');
        window.alert(message || 'Không đổi được trạng thái. Hãy tải lại trang rồi thử lại.');
    }

    $(function () {
        var $board = board();
        if (!$board.length) return;

        var url = $board.data('url');
        var token = $board.find('input[name="__RequestVerificationToken"]').val();

        $board.on('dragstart', '[data-card]', function (e) {
            var $card = $(this);
            if ($card.hasClass('is-locked')) return false;

            $dragging = $card;
            $card.addClass('is-dragging');

            // Firefox không khởi động thao tác kéo nếu chưa đặt dữ liệu vào sự kiện.
            e.originalEvent.dataTransfer.setData(DRAG_TYPE, String($card.data('id')));
            e.originalEvent.dataTransfer.effectAllowed = 'move';
        });

        $board.on('dragend', '[data-card]', function () {
            $(this).removeClass('is-dragging');
            $board.find('[data-drop]').removeClass('is-over');
            $dragging = null;
        });

        // Phải chặn hành vi mặc định của dragover, nếu không trình duyệt sẽ không cho thả.
        $board.on('dragover', '[data-drop]', function (e) {
            if (!$dragging) return;
            e.preventDefault();
            e.originalEvent.dataTransfer.dropEffect = 'move';
            $(this).addClass('is-over');
        });

        $board.on('dragleave', '[data-drop]', function (e) {
            // dragleave còn bắn khi con trỏ đi qua các thẻ con bên trong; chỉ bỏ tô sáng khi
            // đã thực sự rời khỏi vùng thả, nếu không viền sẽ nhấp nháy liên tục.
            if (this.contains(e.originalEvent.relatedTarget)) return;
            $(this).removeClass('is-over');
        });

        $board.on('drop', function (e) {
            e.preventDefault();
        });

        $board.on('drop', '[data-drop]', function (e) {
            e.preventDefault();
            e.stopPropagation();

            var $drop = $(this);
            $drop.removeClass('is-over');

            var $card = $dragging;
            if (!$card) return;

            var $from = $card.parent('[data-drop]');
            var state = $drop.closest('.kanban-col').data('state');
            if ($from.is($drop)) return;

            // Chuyển thẻ ngay cho thao tác mượt, hỏng thì trả về chỗ cũ.
            var next = $card.next();
            $card.appendTo($drop).addClass('is-saving');
            refreshCounts();

            $.post(url, { id: $card.data('id'), state: state, __RequestVerificationToken: token })
                .done(function (res) {
                    if (res && res.ok) {
                        $card.removeClass('is-saving');

                        // Cập nhật màu cảnh báo hạn ngay theo trạng thái mới: kéo sang Hoàn thành
                        // thì thẻ hết đỏ/cam liền, không bắt người dùng tải lại trang mới thấy.
                        $card.removeClass('card-late card-soon');
                        if (res.overdue) $card.addClass('card-late');
                        else if (res.dueSoon) $card.addClass('card-soon');
                        else $card.find('.js-due-badge').remove();

                        return;
                    }

                    if (next.length) $card.insertBefore(next); else $card.appendTo($from);
                    refreshCounts();
                    fail($card, res && res.message);
                })
                .fail(function () {
                    if (next.length) $card.insertBefore(next); else $card.appendTo($from);
                    refreshCounts();
                    fail($card);
                });
        });
    });
})(jQuery);

// Chú thích "* Trường bắt buộc" ở đầu biểu mẫu, để người dùng biết dấu sao nghĩa là gì.
// Chèn bằng JavaScript thay vì gõ vào từng view: biểu mẫu nào có nhãn bắt buộc thì tự có
// chú thích, thêm biểu mẫu mới về sau cũng không phải nhớ thêm dòng này.
(function ($) {
    'use strict';

    function init(context) {
        $(context || document).find('form').each(function () {
            var $form = $(this);

            if ($form.find('.form-required-note').length) return;
            if (!$form.find('label.is-required').length) return;

            $('<div class="form-required-note">')
                .html('<span class="req-mark">*</span> Trường bắt buộc nhập')
                .prependTo($form);
        });
    }

    $(function () { init(document); });

    window.AppRequiredNote = { init: init };
})(jQuery);

/*
  Thanh lọc một hàng trên các màn danh sách.

  Hai việc:
    1. Đổi ô chọn là lọc luôn, khỏi phải bấm nút. Nút "Lọc" được ẩn bằng CSS khi có
       JavaScript (lớp .js trên thẻ html) và hiện lại khi không có, nên tắt JavaScript
       thì bộ lọc vẫn dùng được y như cũ.
    2. Ô đang lọc theo một giá trị thì sáng lên, để nhìn thanh là biết đang lọc theo gì
       mà không phải mở từng ô ra xem.

  Giá trị rỗng, "0" và "-1" đều là "chưa lọc" — khớp với các option đầu danh sách bên
  view; riêng "-1" là mục "(chưa giao)" nên vẫn tính là ĐANG lọc.
*/
(function ($) {
    'use strict';

    document.documentElement.className += ' js';

    function mark($pick) {
        var v = $pick.val();
        $pick.toggleClass('on', v !== '' && v !== '0');
    }

    $(function () {
        var $picks = $('.filterbar .filter-pick');
        if (!$picks.length) return;

        $picks.each(function () { mark($(this)); });

        $picks.on('change', function () {
            var $pick = $(this);
            mark($pick);
            $pick.closest('form').submit();
        });
    });
})(jQuery);
