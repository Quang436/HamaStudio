// =============================================
// Lịch Chụp + Khung Giờ Management Functions
// =============================================

// ─── Load dropdown dịch vụ ──────────────────
function loadScheduleServices() {
    fetch('/Admin/GetDichVu').then(function (r) { return r.json() }).then(function (data) {
        var options = '<option value="">Tất cả dịch vụ</option>';
        (data || []).forEach(function (d) {
            options += '<option value="' + d.MaDichVu + '">' + d.TenDichVu + '</option>';
        });
        var el = document.getElementById('filter-schedule-service');
        if (el) el.innerHTML = options;

        var opts = '<option value="">-- Chọn dịch vụ --</option>';
        (data || []).forEach(function (d) {
            opts += '<option value="' + d.MaDichVu + '">' + d.TenDichVu + '</option>';
        });
        var el2 = document.getElementById('schedule-dv');
        if (el2) el2.innerHTML = opts;
    }).catch(function (e) { console.error('loadScheduleServices:', e) });
}

// ─── Load danh sách lịch chụp ───────────────
function loadSchedule() {
    var serviceEl = document.getElementById('filter-schedule-service');
    var monthEl = document.getElementById('filter-schedule-month');
    var serviceId = serviceEl ? serviceEl.value : '';
    var month = monthEl ? monthEl.value : '';
    var url = '/Admin/GetLichChup';
    if (serviceId) url += '?maDichVu=' + serviceId;

    fetch(url).then(function (r) { return r.json() }).then(function (data) {
        var h = '';
        (data || []).forEach(function (l) {
            // Lọc theo tháng nếu có
            var showRow = true;
            if (month) {
                var dp = l.NgayChup.split('/');              // dd/MM/yyyy
                var itemMonth = dp[1] + '/' + dp[2];        // MM/yyyy
                showRow = itemMonth === month.split('-')[1] + '/' + month.split('-')[0];
            }
            if (!showRow) return;

            var statusColor = l.TrangThai === 'Hoạt động' ? '#4CAF50'
                : l.TrangThai === 'Đã đầy' ? '#FF9800' : '#9E9E9E';

            h += '<tr id="sch-row-' + l.MaLichChup + '">';
            h += '<td>' + l.MaLichChup + '</td>';
            h += '<td>' + l.DichVu + '</td>';
            h += '<td>' + l.NgayChup + '</td>';
            h += '<td>' + l.SoLichToiDa + '</td>';
            h += '<td>' + l.SoLichConLai + '</td>';
            h += '<td><span class="badge" style="background:#5c5c5c;color:#fff">' + (l.SoKhungGio || 0) + ' khung</span></td>';
            h += '<td><span class="badge" style="background-color:' + statusColor + '">' + l.TrangThai + '</span></td>';
            h += '<td><small>' + (l.GhiChu || '--') + '</small></td>';
            h += '<td class="btn-group">';
            if (l.TrangThai === 'Hoạt động' || l.TrangThai === 'Đã đầy') {
                h += '<button class="btn btn-primary btn-sm" onclick="editSchedule(' + l.MaLichChup + ',\'' + escSch(l.DichVu) + '\',\'' + l.NgayChup + '\',' + l.SoLichToiDa + ',\'' + escSch(l.GhiChu) + '\')">Sửa</button>';
            }
            h += '<button class="btn btn-sm" style="background:#f0a500;color:#fff" onclick="toggleKhungGioPanel(' + l.MaLichChup + ')"><i class="bi bi-clock"></i> Khung giờ</button>';
            h += '<button class="btn btn-danger btn-sm" onclick="deleteSchedule(' + l.MaLichChup + ')">Xóa</button>';
            h += '</td></tr>';

            // Row mở rộng để quản lý khung giờ (ẩn mặc định)
            h += '<tr id="sch-gio-' + l.MaLichChup + '" style="display:none;background:#f9f7f4">';
            h += '<td colspan="9" style="padding:12px 20px">';
            h += '<div style="display:flex;align-items:center;gap:12px;margin-bottom:10px">';
            h += '<b style="font-size:13px;color:#555"><i class="bi bi-clock-history"></i> Khung giờ — ' + l.NgayChup + ' — ' + l.DichVu + '</b>';
            h += '<button class="btn btn-primary btn-sm" onclick="showAddKhungGio(' + l.MaLichChup + ')"><i class="bi bi-plus"></i> Thêm giờ</button>';
            h += '</div>';
            h += '<div id="kg-list-' + l.MaLichChup + '" style="display:flex;flex-wrap:wrap;gap:8px"></div>';
            h += '</td></tr>';
        });

        if (!data || !data.length) h = '<tr><td colspan="8" class="empty-state">Không có dữ liệu</td></tr>';
        var el = document.getElementById('tbl-schedule');
        if (el) el.innerHTML = h;
    }).catch(function (e) { console.error('loadSchedule:', e) });
}

// ─── Toggle panel khung giờ ─────────────────
function toggleKhungGioPanel(maLichChup) {
    var row = document.getElementById('sch-gio-' + maLichChup);
    if (!row) return;
    if (row.style.display === 'none') {
        row.style.display = '';
        loadKhungGioList(maLichChup);
    } else {
        row.style.display = 'none';
    }
}

// ─── Load danh sách khung giờ của 1 lịch ────
function loadKhungGioList(maLichChup) {
    var container = document.getElementById('kg-list-' + maLichChup);
    if (!container) return;
    container.innerHTML = '<span style="color:#999;font-size:12px">Đang tải...</span>';

    fetch('/Admin/GetKhungGioByLich?maLichChup=' + maLichChup)
        .then(function (r) { return r.json() })
        .then(function (data) {
            if (!data || !data.length) {
                container.innerHTML = '<div style="background:#fff;padding:15px;text-align:center;border:1px dashed #ccc;color:#999;font-style:italic">Chưa có khung giờ nào. Nhấn "+ Thêm giờ" để cài đặt thời gian làm việc cho ngày này.</div>';
                return;
            }
            var h = '<table class="table" style="background:#fff;margin:0;font-size:13px;border:1px solid #eee">';
            h += '<thead style="background:#f8f9fa"><tr><th>Thời gian (Bắt đầu - Kết thúc)</th><th>Mô tả / Ghi chú</th><th>Trạng thái</th><th style="width:180px">Thao tác</th></tr></thead><tbody>';

            data.forEach(function (k) {
                var isLocked = k.TrangThai === 'Đã khóa';
                var statusHtml = isLocked
                    ? '<span style="color:#f44336"><i class="bi bi-lock-fill"></i> Tạm khóa</span>'
                    : '<span style="color:#4CAF50"><i class="bi bi-check-circle-fill"></i> Hoạt động</span>';

                h += '<tr>';
                h += '<td style="font-weight:600"><i class="bi bi-clock"></i> ' + k.GioBatDau + ' — ' + k.GioKetThuc + '</td>';
                h += '<td><small>' + (k.GhiChu || '--') + '</small></td>';
                h += '<td>' + statusHtml + '</td>';
                h += '<td class="btn-group">';

                // Nút Toggle khoá
                var lockLabel = isLocked ? 'Mở khoá' : 'Khoá giờ';
                var lockStyle = isLocked ? 'background:#4CAF50;color:#fff' : 'background:#f0a500;color:#fff';
                h += '<button class="btn btn-sm" style="padding:2px 10px;font-size:11px;' + lockStyle + '" ';
                h += 'onclick="toggleKhoaKhungGio(' + k.MaKhungGio + ',' + maLichChup + ',this)">' + lockLabel + '</button>';

                h += '<button class="btn btn-danger btn-sm" style="padding:2px 10px;font-size:11px" ';
                h += 'onclick="deleteKhungGio(' + k.MaKhungGio + ',' + maLichChup + ')">Xoá</button>';

                h += '</td></tr>';
            });
            h += '</tbody></table>';
            container.innerHTML = h;
        })
        .catch(function () {
            container.innerHTML = '<div style="color:red;padding:10px">Lỗi kết nối!</div>';
        });
}

// ─── Hiển thị form thêm khung giờ ──────────
function showAddKhungGio(maLichChup) {
    var overlay = document.getElementById('modal-khungio');
    if (!overlay) {
        // Tạo modal động
        var html = '<div class="modal-overlay" id="modal-khungio" style="display:flex">';
        html += '<div class="modal" style="max-width:450px">';
        html += '<div class="modal-head"><h3>Thêm khung giờ làm việc</h3>';
        html += '<button class="modal-close" onclick="document.getElementById(\'modal-khungio\').style.display=\'none\'">×</button></div>';
        html += '<div class="modal-body">';
        html += '<input type="hidden" id="kg-malichChup" />';

        html += '<div style="display:grid;grid-template-columns:1fr 1fr;gap:15px;margin-bottom:15px">';
        html += '<div class="form-group"><label>Giờ bắt đầu</label>';
        html += '<input type="time" id="kg-giobatdau" required /></div>';
        html += '<div class="form-group"><label>Giờ kết thúc</label>';
        html += '<input type="time" id="kg-gioketthuc" required /></div>';
        html += '</div>';

        html += '<div class="form-group"><label>Ghi chú (tuỳ chọn)</label>';
        html += '<input type="text" id="kg-ghichu" placeholder="VD: Ca sáng, Ca chiều..." /></div>';

        // Suggestions
        html += '<div class="form-group"><label>Gợi ý nhanh</label>';
        html += '<div style="display:flex;flex-wrap:wrap;gap:8px;margin-top:5px">';
        var ranges = [
            { s: '08:00', e: '10:00' }, { s: '10:00', e: '12:00' },
            { s: '13:00', e: '15:00' }, { s: '15:00', e: '17:00' },
            { s: '18:00', e: '20:00' }, { s: '20:00', e: '22:00' }
        ];
        ranges.forEach(function (r) {
            html += '<button type="button" class="btn btn-sm" style="border:1px solid #c5a880;background:#fff;font-size:11px;padding:4px 10px" ';
            html += 'onclick="document.getElementById(\'kg-giobatdau\').value=\'' + r.s + '\';document.getElementById(\'kg-gioketthuc\').value=\'' + r.e + '\'">';
            html += r.s + ' - ' + r.e + '</button>';
        });
        html += '</div></div>';

        html += '</div>';
        html += '<div class="modal-foot">';
        html += '<button class="btn" onclick="document.getElementById(\'modal-khungio\').style.display=\'none\'">Hủy</button>';
        html += '<button class="btn btn-primary" onclick="submitKhungGio()">Lưu khung giờ</button>';
        html += '</div></div></div>';
        document.body.insertAdjacentHTML('beforeend', html);
        overlay = document.getElementById('modal-khungio');
    }

    document.getElementById('kg-malichChup').value = maLichChup;
    document.getElementById('kg-giobatdau').value = '';
    document.getElementById('kg-gioketthuc').value = '';
    document.getElementById('kg-ghichu').value = '';

    overlay.style.display = 'flex';
}

// ─── Submit thêm khung giờ ──────────────────
function submitKhungGio() {
    var maLichChup = document.getElementById('kg-malichChup').value;
    var sEl = document.getElementById('kg-giobatdau');
    var eEl = document.getElementById('kg-gioketthuc');
    var gcEl = document.getElementById('kg-ghichu');

    var s = sEl ? sEl.value : '';
    var e = eEl ? eEl.value : '';
    if (!s || !e) { alert('Vui lòng nhập cả giờ bắt đầu và kết thúc!'); return; }

    var fd = new FormData();
    fd.append('maLichChup', maLichChup);
    fd.append('gioBatDau', s);
    fd.append('gioKetThuc', e);
    fd.append('ghiChu', gcEl ? gcEl.value.trim() : '');

    fetch('/Admin/ThemKhungGio', { method: 'POST', body: fd })
        .then(function (r) { return r.json() })
        .then(function (res) {
            if (res.success) {
                document.getElementById('modal-khungio').style.display = 'none';
                loadKhungGioList(parseInt(maLichChup));
            } else {
                alert('Lỗi: ' + (res.message || 'Thêm thất bại'));
            }
        })
        .catch(function () { alert('Lỗi kết nối!') });
}

// ─── Toggle khoá khung giờ ──────────────────
function toggleKhoaKhungGio(maKhungGio, maLichChup, btn) {
    var lyDo = '';
    var isCurrentlyActive = btn && btn.textContent.indexOf('Khoá') > -1;
    if (isCurrentlyActive) {
        lyDo = prompt('Lý do khoá giờ này (tuỳ chọn):', '') || '';
    }

    var fd = new FormData();
    fd.append('maKhungGio', maKhungGio);
    fd.append('lyDo', lyDo);

    fetch('/Admin/KhoaKhungGio', { method: 'POST', body: fd })
        .then(function (r) { return r.json() })
        .then(function (res) {
            if (res.success) {
                loadKhungGioList(maLichChup);
            } else {
                alert('Lỗi: ' + (res.message || 'Cập nhật thất bại'));
            }
        })
        .catch(function () { alert('Lỗi kết nối!') });
}

// ─── Xoá khung giờ ──────────────────────────
function deleteKhungGio(maKhungGio, maLichChup) {
    if (!confirm('Xoá khung giờ này?')) return;
    var fd = new FormData();
    fd.append('maKhungGio', maKhungGio);
    fetch('/Admin/XoaKhungGio', { method: 'POST', body: fd })
        .then(function (r) { return r.json() })
        .then(function (res) {
            if (res.success) loadKhungGioList(maLichChup);
            else alert('Lỗi: ' + (res.message || 'Xoá thất bại'));
        })
        .catch(function () { alert('Lỗi kết nối!') });
}

// ─── Hiện modal thêm lịch mới ───────────────
function showAddSchedule() {
    document.getElementById('schedule-mode').value = 'add';
    document.getElementById('schedule-id').value = '';
    document.getElementById('schedule-dv').value = '';
    document.getElementById('schedule-ngay').value = '';
    document.getElementById('schedule-solich').value = '5';
    document.getElementById('schedule-ghichu').value = '';
    document.getElementById('modal-schedule-title').textContent = 'Lên lịch chụp mới';
    showModal('modal-schedule');
}

// ─── Sửa lịch chụp ──────────────────────────
function editSchedule(id, dichVu, ngayChup, soLich, ghiChu) {
    // Convert ngayChup từ dd/MM/yyyy sang yyyy-MM-dd
    var parts = ngayChup.split('/');
    var dateInput = parts[2] + '-' + parts[1] + '-' + parts[0];

    document.getElementById('schedule-mode').value = 'edit';
    document.getElementById('schedule-id').value = id;
    document.getElementById('schedule-ngay').value = dateInput;
    document.getElementById('schedule-solich').value = soLich;
    document.getElementById('schedule-ghichu').value = (ghiChu === '--' ? '' : ghiChu) || '';
    document.getElementById('modal-schedule-title').textContent = 'Sửa lịch chụp';

    // Chọn service trong dropdown
    var dvEl = document.getElementById('schedule-dv');
    if (dvEl) {
        // chọn dv đúng tên (best effort)
        Array.from(dvEl.options).forEach(function (opt) {
            if (opt.text === dichVu) dvEl.value = opt.value;
        });
    }

    showModal('modal-schedule');
}

// ─── Submit thêm/sửa lịch chụp ──────────────
function submitSchedule() {
    var mode = document.getElementById('schedule-mode').value;

    var maDichVu = parseInt(document.getElementById('schedule-dv').value);
    if (!maDichVu) { alert('Vui lòng chọn dịch vụ!'); return; }

    var ngayChup = document.getElementById('schedule-ngay').value;
    if (!ngayChup) { alert('Vui lòng chọn ngày chụp!'); return; }

    var soLich = parseInt(document.getElementById('schedule-solich').value);
    if (!soLich || soLich < 1) { alert('Số lịch tối đa phải lớn hơn 0!'); return; }

    var ghiChu = document.getElementById('schedule-ghichu').value.trim();

    var fd = new FormData();
    fd.append('maDichVu', maDichVu);
    fd.append('ngayChup', ngayChup);
    fd.append('soLichToiDa', soLich);
    fd.append('ghiChu', ghiChu);

    if (mode === 'edit') {
        fd.append('maLichChup', document.getElementById('schedule-id').value);
    }

    var url = mode === 'add' ? '/Admin/ThemLichChup' : '/Admin/SuaLichChup';
    fetch(url, { method: 'POST', body: fd })
        .then(function (r) { return r.json() })
        .then(function (res) {
            if (res.success) {
                hideModal('modal-schedule');
                loadSchedule();
                if (mode === 'add') {
                    alert('✓ Lên lịch chụp thành công!\n\nTiếp theo: nhấn nút "⏰ Giờ" bên cạnh lịch vừa tạo để thêm khung giờ cho ngày này.');
                }
            } else {
                alert('Lỗi: ' + (res.message || 'Thất bại'));
            }
        })
        .catch(function (e) { console.error('submitSchedule:', e); alert('Lỗi kết nối!'); });
}

// ─── Xoá lịch chụp ──────────────────────────
function deleteSchedule(id) {
    if (!confirm('Bạn chắc chắn muốn xóa lịch chụp này? (Lịch chụp không có đơn đặt sẽ bị xóa)')) return;
    var fd = new FormData();
    fd.append('maLichChup', id);
    fetch('/Admin/XoaLichChup', { method: 'POST', body: fd })
        .then(function (r) { return r.json() })
        .then(function (res) {
            if (res.success) {
                loadSchedule();
            } else {
                alert('Lỗi: ' + (res.message || 'Xóa thất bại'));
            }
        });
}

// ─── Helper ─────────────────────────────────
function escSch(s) { return (s || '').replace(/'/g, "\\'").replace(/"/g, '&quot;'); }
