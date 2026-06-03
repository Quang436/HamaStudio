// Lịch Chụp (Schedule) Management Functions

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
            var showRow = true;
            if (month) {
                var dateParts = l.NgayChup.split('/');
                var itemMonth = dateParts[1] + '/' + dateParts[2];
                showRow = itemMonth === month.split('-')[1] + '/' + month.split('-')[0];
            }
            if (showRow) {
                h += '<tr><td>' + l.MaLichChup + '</td><td>' + l.DichVu + '</td><td>' + l.NgayChup + '</td>';
                h += '<td>' + l.SoLichToiDa + '</td><td>' + l.SoLichConLai + '</td>';
                h += '<td><span class="badge" style="background-color:' + (l.TrangThai === 'Hoạt động' ? '#4CAF50' : l.TrangThai === 'Đã đầy' ? '#FF9800' : '#9E9E9E') + '">' + l.TrangThai + '</span></td>';
                h += '<td><small>' + (l.GhiChu || '--') + '</small></td>';
                h += '<td class="btn-group">';
                if (l.TrangThai === 'Hoạt động' || l.TrangThai === 'Đã đầy') {
                    h += '<button class="btn btn-primary btn-sm" onclick="editSchedule(' + l.MaLichChup + ')">Sửa</button>';
                }
                h += '<button class="btn btn-danger btn-sm" onclick="deleteSchedule(' + l.MaLichChup + ')">Xóa</button>';
                h += '</td></tr>';
            }
        });
        if (!data || !data.length) h = '<tr><td colspan="8" class="empty-state">Không có dữ liệu</td></tr>';
        var el = document.getElementById('tbl-schedule');
        if (el) el.innerHTML = h;
    }).catch(function (e) { console.error('loadSchedule:', e) });
}

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

function editSchedule(id) {
    alert('Tính năng sửa lịch chụp - sẽ cập nhật thông tin lịch');
    // TODO: Load schedule details and populate form
}

function submitSchedule() {
    var modeEl = document.getElementById('schedule-mode');
    var mode = modeEl ? modeEl.value : 'add';

    var dvEl = document.getElementById('schedule-dv');
    var maDichVu = dvEl ? parseInt(dvEl.value) : 0;
    if (!maDichVu) { alert('Vui lòng chọn dịch vụ!'); return; }

    var ngayEl = document.getElementById('schedule-ngay');
    var ngayChup = ngayEl ? ngayEl.value : '';
    if (!ngayChup) { alert('Vui lòng chọn ngày chụp!'); return; }

    var solichEl = document.getElementById('schedule-solich');
    var soLich = solichEl ? parseInt(solichEl.value) : 0;
    if (!soLich || soLich < 1) { alert('Số lịch tối đa phải lớn hơn 0!'); return; }

    var ghichuEl = document.getElementById('schedule-ghichu');
    var ghichu = ghichuEl ? ghichuEl.value.trim() : '';

    var fd = new FormData();
    fd.append('maDichVu', maDichVu);
    fd.append('ngayChup', ngayChup);
    fd.append('soLichToiDa', soLich);
    fd.append('ghiChu', ghichu);

    if (mode === 'edit') {
        var idEl = document.getElementById('schedule-id');
        fd.append('maLichChup', idEl ? idEl.value : '');
    }

    var url = mode === 'add' ? '/Admin/ThemLichChup' : '/Admin/SuaLichChup';
    fetch(url, { method: 'POST', body: fd })
        .then(function (r) { return r.json() })
        .then(function (res) {
            if (res.success) {
                hideModal('modal-schedule');
                loadSchedule();
                alert('✓ Lên lịch chụp thành công!\n\nHệ thống sẽ tự động xác nhận lịch đặt phù hợp với lịch chụp này.');
            } else {
                alert('Lỗi: ' + (res.message || 'Thêm lịch thất bại'));
            }
        })
        .catch(function (e) {
            console.error('submitSchedule:', e);
            alert('Lỗi kết nối!');
        });
}

function deleteSchedule(id) {
    if (!confirm('Bạn chắc chắn muốn xóa lịch chụp này? (Lịch chụp không có đơn đặt sẽ bị xóa hoàn toàn)')) return;
    var fd = new FormData();
    fd.append('maLichChup', id);
    fetch('/Admin/XoaLichChup', { method: 'POST', body: fd })
        .then(function (r) { return r.json() })
        .then(function (res) {
            if (res.success) {
                loadSchedule();
                alert('Xóa lịch chụp thành công!');
            } else {
                alert('Lỗi: ' + (res.message || 'Xóa thất bại'));
            }
        });
}
