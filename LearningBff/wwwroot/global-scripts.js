/* Your Global Scripts */

(() => {
    const GOOGLE_STYLES = {
        'background':        '#ffffff',
        'background-color':  '#ffffff',
        'background-image':  'none',
        'color':             '#1a73e8',
        'border':            '1.5px solid #1a73e8',
        'border-color':      '#1a73e8',
        'box-shadow':        '0 1px 3px rgba(26,115,232,0.08)',
    };

    const applyGoogleStyle = (btn) => {
        // Xóa toàn bộ inline style cũ (kể cả của Bootstrap/ABP)
        btn.removeAttribute('style');

        btn.classList.remove('btn-primary', 'btn-secondary', 'btn-default', 'btn-light');
        btn.classList.add('btn-google-login');

        Object.entries(GOOGLE_STYLES).forEach(([prop, val]) =>
            btn.style.setProperty(prop, val, 'important')
        );

        if (!btn.dataset.hoverBound) {
            btn.dataset.hoverBound = 'true';
            btn.addEventListener('mouseenter', () => {
                btn.style.setProperty('background',       '#f0f7ff', 'important');
                btn.style.setProperty('background-color', '#f0f7ff', 'important');
                btn.style.setProperty('border-color',     '#1557b0', 'important');
                btn.style.setProperty('color',            '#1557b0', 'important');
            });
            btn.addEventListener('mouseleave', () => {
                btn.style.setProperty('background',       '#ffffff', 'important');
                btn.style.setProperty('background-color', '#ffffff', 'important');
                btn.style.setProperty('border-color',     '#1a73e8', 'important');
                btn.style.setProperty('color',            '#1a73e8', 'important');
            });
        }

        const trimmed = btn.textContent.trim();
        if (trimmed === 'Google') {
            btn.textContent = 'Đăng nhập bằng Google';
        }
    };

    const syncButtons = () => {
        const googleBtns = document.querySelectorAll(
            'button[value="Google"], button[name="provider"][value="Google"]'
        );
        if (!googleBtns.length) return;

        const loginBtn = document.querySelector(
            'button[name="Action"][value="Login"], #loginForm button[type="submit"], form:not([action*="ExternalLogin"]) button[type="submit"]'
        );

        googleBtns.forEach(btn => {
            applyGoogleStyle(btn);

            // Đồng bộ width với nút Login
            if (loginBtn && loginBtn.offsetWidth > 0) {
                btn.style.setProperty('width', `${loginBtn.offsetWidth}px`, 'important');
                btn.style.maxWidth = '100%';
            }
        });
    };

    // Chạy ngay khi DOM sẵn sàng
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', syncButtons);
    } else {
        syncButtons();
    }

    window.addEventListener('load', syncButtons);
    window.addEventListener('resize', syncButtons);

    // Retry phòng trường hợp ABP render muộn
    [100, 300, 600, 1200].forEach(ms => setTimeout(syncButtons, ms));

    // Các lần retry ở trên đã xử lý trường hợp ABP render muộn. Không quan sát
    // thay đổi `style`/`class` ở toàn trang vì syncButtons cũng tự đổi style nút;
    // hai việc này kết hợp sẽ tạo MutationObserver loop và làm treo trang login.
})();
