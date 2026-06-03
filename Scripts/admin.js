// ═══════════════════════════════════════
// ADMIN PANEL JS - HamaStudio
// ═══════════════════════════════════════
var currentSection = 'dashboard';
var danhMucList = [];

document.addEventListener('DOMContentLoaded', function () {
    loadDanhMuc();
    switchSection('dashboard');
});

// NAV
function switchSection(id) {
    currentSection = id;
    document.querySelectorAll('.section').forEach(function (s) { s.classList.remove('active'); });
    document.querySelectorAll('.nav-item').forEach(function (n) { n.classList.remove('active'); });
    var sec = document.getElementById('sec-' + id);
    if (sec) sec.classList.add('active');
    var nav = document.querySelector('[data-sec="' + id + '"]');
    if (nav) nav.classList.add('active');
    document.getElementById('page-title').textContent = nav ? nav.querySelector('span').textContent : 'Dashboard';

    if (id === 'dashboard') loadDashboard();
    else if (id === 'booking-pending') loadDatLich('chua_xac_nhan');
    else if (id === 'booking-confirmed') loadDatLich('da_xac_nhan');
    else if (id === 'history') loadDatLich('hoan_thanh');
    else if (id === 'customers') loadKhachHang();
    else if (id === 'services') loadDichVu();
    else if (id === 'portfolio') loadPortfolio();
    else if (id === 'addons') loadDichVuBoSung();
    else if (id === 'revenue') loadThongKe(new Date().getFullYear());
}

// LOAD DANH MUC
function loadDanhMuc() {
    fetch('/Admin/GetDanhMuc').then(function (r) { return r.json(); }).then(function (d) { danhMucList = d; });
}

function danhMucOptions(selected) {
    var h = '<option value="">-- Chọn --</option>';
    danhMucList.forEach(function (dm) {
        h += '<option value="' + dm.MaDanhMuc + '"' + (dm.MaDanhMuc == selected ? ' selected' : '') + '>' + dm.TenDanhMuc + '</option>';
    });
    return h;
}

// FORMAT
function fmt(n) { return new Intl.NumberFormat('vi-VN').format(n); }

function badge(tt) {
    if (!tt) return '';
    var t = tt.trim();
    if (t === 'Chờ xác nhận') return '<span class="badge badge-wait">' + t + '</span>';
    if (t === 'Đã xác nhận') return '<span class="badge badge-confirmed">' + t + '</span>';
    if (t === 'Đã đặt cọc') return '<span class="badge badge-deposit">' + t + '</span>';
    if (t === 'Hoàn thành') return '<span class="badge badge-done">' + t + '</span>';
    if (t === 'Đã hủy') return '<span class="badge badge-cancel">' + t + '</span>';
    if (t === 'Hoạt động') return '<span class="badge badge-active">' + t + '</span>';
    if (t === 'Khóa') return '<span class="badge badge-locked">' + t + '</span>';
    return '<span class="badge">' + t + '</span>';
}

// ─── DASHBOARD ───
function loadDashboard() {
    // Dữ liệu đã được render từ server (ViewBag) trong HTML, không cần ghi đè
}

// ─── ĐẶT LỊCH ───
function loadDatLich(loai) {
    var tableId = loai === 'chua_xac_nhan' ? 'tbl-pending' : (loai === 'da_xac_nhan' ? 'tbl-confirmed' : 'tbl-history');
    fetch('/Admin/GetDatLich?loai=' + loai).then(function (r) { return r.json(); }).then(function (data) {
        var h = '';
        data.forEach(function (d) {
            h += '<tr><td>' + d.MaDatLich + '</td><td>' + d.KhachHang + '</td><td>' + (d.SoDienThoai || '--') + '</td>';
            h += '<td>' + d.DichVu + '</td><td>' + d.NgayChup + '</td><td>' + d.KhungGio + '</td>';
            h += '<td>' + (d.DiaDiem || '--') + '</td><td>' + fmt(d.TongTien) + 'đ</td><td>' + fmt(d.SoTienDaCoc) + 'đ</td>';
            h += '<td>' + badge(d.TrangThai) + '</td><td class="btn-group">';
            if (loai === 'chua_xac_nhan') {
                h += '<button class="btn btn-primary btn-sm" onclick="updateDL(' + d.MaDatLich + ',\'Đã xác nhận\')">Xác nhận</button>';
                h += '<button class="btn btn-danger btn-sm" onclick="updateDL(' + d.MaDatLich + ',\'Đã hủy\')">Hủy</button>';
            } else if (loai === 'da_xac_nhan') {
                h += '<button class="btn btn-primary btn-sm" onclick="showComplete(' + d.MaDatLich + ')">Hoàn thành</button>';
                h += '<button class="btn btn-danger btn-sm" onclick="updateDL(' + d.MaDatLich + ',\'Đã hủy\')">Hủy</button>';
            }
            h += '</td></tr>';
        });
        if (!data.length) h = '<tr><td colspan="11" class="empty-state">Không có dữ liệu</td></tr>';
        document.getElementById(tableId).innerHTML = h;
    });
}

function updateDL(id, tt, link) {
    var fd = new FormData();
    fd.append('maDatLich', id); fd.append('trangThai', tt);
    if (link) fd.append('linkAnh', link);
    fetch('/Admin/CapNhatTrangThaiDatLich', { method: 'POST', body: fd }).then(function (r) { return r.json(); }).then(function (d) {
        if (d.success) {
            if (currentSection === 'booking-pending') loadDatLich('chua_xac_nhan');
            else if (currentSection === 'booking-confirmed') loadDatLich('da_xac_nhan');
            else loadDatLich('hoan_thanh');
        }
    });
}

function showComplete(id) {
    document.getElementById('complete-id').value = id;
    document.getElementById('complete-link').value = '';
    showModal('modal-complete');
}

function submitComplete() {
    var id = document.getElementById('complete-id').value;
    var link = document.getElementById('complete-link').value;
    updateDL(id, 'Hoàn thành', link);
    hideModal('modal-complete');
}

// ─── KHÁCH HÀNG ───
function loadKhachHang() {
    fetch('/Admin/GetKhachHang').then(function (r) { return r.json(); }).then(function (data) {
        var h = '';
        data.forEach(function (k) {
            h += '<tr><td>' + k.MaKhachHang + '</td><td>' + k.HoTen + '</td><td>' + (k.Email || '--') + '</td>';
            h += '<td>' + (k.SoDienThoai || '--') + '</td><td>' + (k.DiaChi || '--') + '</td>';
            h += '<td>' + k.SoLichDat + '</td><td>' + badge(k.TrangThai) + '</td>';
            h += '<td class="btn-group">';
            if (k.TrangThai === 'Hoạt động') h += '<button class="btn btn-danger btn-sm" onclick="updateKH(' + k.MaKhachHang + ',\'Khóa\')">Khóa</button>';
            else h += '<button class="btn btn-primary btn-sm" onclick="updateKH(' + k.MaKhachHang + ',\'Hoạt động\')">Mở khóa</button>';
            h += '</td></tr>';
        });
        if (!data.length) h = '<tr><td colspan="8" class="empty-state">Không có dữ liệu</td></tr>';
        document.getElementById('tbl-customers').innerHTML = h;
    });
}

function updateKH(id, tt) {
    var fd = new FormData(); fd.append('maKhachHang', id); fd.append('trangThai', tt);
    fetch('/Admin/CapNhatTrangThaiKhachHang', { method: 'POST', body: fd }).then(function () { loadKhachHang(); });
}

// ─── DỊCH VỤ ───
function loadDichVu() {
    fetch('/Admin/GetDichVu').then(function (r) { return r.json(); }).then(function (data) {
        var h = '';
        data.forEach(function (d) {
            h += '<tr><td>' + d.MaDichVu + '</td><td>' + d.TenDichVu + '</td><td>' + d.TenDanhMuc + '</td>';
            h += '<td>' + fmt(d.GiaTien) + 'đ</td><td>' + d.GioiHanTho + '</td>';
            h += '<td class="btn-group">';
            h += '<button class="btn btn-sm" onclick="editDV(' + d.MaDichVu + ',\'' + esc(d.TenDichVu) + '\',' + d.GiaTien + ',' + d.GioiHanTho + ',\'' + esc(d.MoTa || '') + '\',\'' + esc(d.LinkAnhDaiDien || '') + '\',' + d.MaDanhMuc + ')">Sửa</button>';
            h += '<button class="btn btn-danger btn-sm" onclick="deleteDV(' + d.MaDichVu + ')">Xóa</button>';
            h += '</td></tr>';
        });
        if (!data.length) h = '<tr><td colspan="6" class="empty-state">Không có dữ liệu</td></tr>';
        document.getElementById('tbl-services').innerHTML = h;
    });
}

function esc(s) { return (s || '').replace(/'/g, "\\'").replace(/"/g, '&quot;'); }

function showAddDV() {
    document.getElementById('dv-mode').value = 'add';
    document.getElementById('dv-id').value = '';
    document.getElementById('dv-ten').value = '';
    document.getElementById('dv-gia').value = '';
    document.getElementById('dv-slot').value = '1';
    document.getElementById('dv-mota').value = '';
    document.getElementById('dv-anh').value = '';
    document.getElementById('dv-danhmuc').innerHTML = danhMucOptions('');
    document.querySelector('#modal-dv .modal-head h3').textContent = 'Thêm dịch vụ';
    showModal('modal-dv');
}

function editDV(id, ten, gia, slot, mota, anh, dm) {
    document.getElementById('dv-mode').value = 'edit';
    document.getElementById('dv-id').value = id;
    document.getElementById('dv-ten').value = ten;
    document.getElementById('dv-gia').value = gia;
    document.getElementById('dv-slot').value = slot;
    document.getElementById('dv-mota').value = mota;
    document.getElementById('dv-anh').value = anh;
    document.getElementById('dv-danhmuc').innerHTML = danhMucOptions(dm);
    document.querySelector('#modal-dv .modal-head h3').textContent = 'Sửa dịch vụ';
    showModal('modal-dv');
}

function submitDV() {
    var fd = new FormData();
    var mode = document.getElementById('dv-mode').value;
    fd.append('tenDichVu', document.getElementById('dv-ten').value);
    fd.append('giaTien', document.getElementById('dv-gia').value);
    fd.append('gioiHanTho', document.getElementById('dv-slot').value);
    fd.append('moTa', document.getElementById('dv-mota').value);
    fd.append('linkAnh', document.getElementById('dv-anh').value);
    fd.append('maDanhMuc', document.getElementById('dv-danhmuc').value);
    if (mode === 'edit') fd.append('maDichVu', document.getElementById('dv-id').value);
    var url = mode === 'add' ? '/Admin/ThemDichVu' : '/Admin/SuaDichVu';
    fetch(url, { method: 'POST', body: fd }).then(function (r) { return r.json(); }).then(function () { hideModal('modal-dv'); loadDichVu(); });
}

function deleteDV(id) {
    if (!confirm('Xóa dịch vụ này?')) return;
    var fd = new FormData(); fd.append('maDichVu', id);
    fetch('/Admin/XoaDichVu', { method: 'POST', body: fd }).then(function () { loadDichVu(); });
}

// ─── PORTFOLIO ───
function loadPortfolio() {
    fetch('/Admin/GetPortfolio').then(function (r) { return r.json(); }).then(function (data) {
        var h = '';
        data.forEach(function (p) {
            h += '<div class="portfolio-card"><img src="' + (p.LinkFileAnh || '') + '" alt="" onerror="this.src=\'https://placehold.co/400x300/1c1c1c/555?text=No+Image\'">';
            h += '<div class="card-info"><h5>' + (p.TieuDe || 'Không tiêu đề') + '</h5><p>' + p.TenDichVu + '</p></div>';
            h += '<div class="card-actions">';
            h += '<button class="btn btn-sm" onclick="editPF(' + p.MaAnh + ',\'' + esc(p.TieuDe || '') + '\',\'' + esc(p.LinkFileAnh) + '\',' + p.MaDichVu + ')">Sửa</button>';
            h += '<button class="btn btn-danger btn-sm" onclick="deletePF(' + p.MaAnh + ')">Xóa</button>';
            h += '</div></div>';
        });
        if (!data.length) h = '<div class="empty-state">Chưa có portfolio</div>';
        document.getElementById('portfolio-grid').innerHTML = h;
    });
}

function showAddPF() {
    document.getElementById('pf-mode').value = 'add';
    document.getElementById('pf-id').value = '';
    document.getElementById('pf-tieude').value = '';
    document.getElementById('pf-link').value = '';
    document.getElementById('pf-dv').innerHTML = danhMucOptions('');
    loadDichVuDropdown('pf-dv', '');
    showModal('modal-pf');
}

function editPF(id, td, link, dv) {
    document.getElementById('pf-mode').value = 'edit';
    document.getElementById('pf-id').value = id;
    document.getElementById('pf-tieude').value = td;
    document.getElementById('pf-link').value = link;
    loadDichVuDropdown('pf-dv', dv);
    showModal('modal-pf');
}

function loadDichVuDropdown(elId, selected) {
    fetch('/Admin/GetDichVu').then(function (r) { return r.json(); }).then(function (data) {
        var h = '<option value="">-- Chọn dịch vụ --</option>';
        data.forEach(function (d) { h += '<option value="' + d.MaDichVu + '"' + (d.MaDichVu == selected ? ' selected' : '') + '>' + d.TenDichVu + '</option>'; });
        document.getElementById(elId).innerHTML = h;
    });
}

function submitPF() {
    var fd = new FormData();
    var mode = document.getElementById('pf-mode').value;
    fd.append('tieuDe', document.getElementById('pf-tieude').value);
    fd.append('linkFileAnh', document.getElementById('pf-link').value);
    fd.append('maDichVu', document.getElementById('pf-dv').value);
    if (mode === 'edit') fd.append('maAnh', document.getElementById('pf-id').value);
    var url = mode === 'add' ? '/Admin/ThemPortfolio' : '/Admin/SuaPortfolio';
    fetch(url, { method: 'POST', body: fd }).then(function () { hideModal('modal-pf'); loadPortfolio(); });
}

function deletePF(id) {
    if (!confirm('Xóa ảnh portfolio này?')) return;
    var fd = new FormData(); fd.append('maAnh', id);
    fetch('/Admin/XoaPortfolio', { method: 'POST', body: fd }).then(function () { loadPortfolio(); });
}

// ─── DỊCH VỤ BỔ SUNG ───
function loadDichVuBoSung() {
    fetch('/Admin/GetDichVuBoSung').then(function (r) { return r.json(); }).then(function (data) {
        var h = '';
        data.forEach(function (d) {
            h += '<tr><td>' + d.MaDVBS + '</td><td>' + d.TenDVBS + '</td><td>' + (d.MoTa || '--') + '</td>';
            h += '<td>' + fmt(d.GiaTien) + 'đ</td><td>' + d.TenDichVu + '</td>';
            h += '<td class="btn-group">';
            h += '<button class="btn btn-sm" onclick="editBS(' + d.MaDVBS + ',\'' + esc(d.TenDVBS) + '\',\'' + esc(d.MoTa || '') + '\',' + d.GiaTien + ',' + d.MaDichVu + ')">Sửa</button>';
            h += '<button class="btn btn-danger btn-sm" onclick="deleteBS(' + d.MaDVBS + ')">Xóa</button>';
            h += '</td></tr>';
        });
        if (!data.length) h = '<tr><td colspan="6" class="empty-state">Không có dữ liệu</td></tr>';
        document.getElementById('tbl-addons').innerHTML = h;
    });
}

function showAddBS() {
    document.getElementById('bs-mode').value = 'add';
    document.getElementById('bs-id').value = '';
    document.getElementById('bs-ten').value = '';
    document.getElementById('bs-mota').value = '';
    document.getElementById('bs-gia').value = '';
    loadDichVuDropdown('bs-dv', '');
    showModal('modal-bs');
}

function editBS(id, ten, mota, gia, dv) {
    document.getElementById('bs-mode').value = 'edit';
    document.getElementById('bs-id').value = id;
    document.getElementById('bs-ten').value = ten;
    document.getElementById('bs-mota').value = mota;
    document.getElementById('bs-gia').value = gia;
    loadDichVuDropdown('bs-dv', dv);
    showModal('modal-bs');
}

function submitBS() {
    var fd = new FormData();
    var mode = document.getElementById('bs-mode').value;
    fd.append('tenDVBS', document.getElementById('bs-ten').value);
    fd.append('moTa', document.getElementById('bs-mota').value);
    fd.append('giaTien', document.getElementById('bs-gia').value);
    fd.append('maDichVu', document.getElementById('bs-dv').value);
    if (mode === 'edit') fd.append('maDVBS', document.getElementById('bs-id').value);
    var url = mode === 'add' ? '/Admin/ThemDichVuBoSung' : '/Admin/SuaDichVuBoSung';
    fetch(url, { method: 'POST', body: fd }).then(function () { hideModal('modal-bs'); loadDichVuBoSung(); });
}

function deleteBS(id) {
    if (!confirm('Xóa dịch vụ bổ sung này?')) return;
    var fd = new FormData(); fd.append('maDVBS', id);
    fetch('/Admin/XoaDichVuBoSung', { method: 'POST', body: fd }).then(function () { loadDichVuBoSung(); });
}

// ─── THỐNG KÊ ───
function loadThongKe(nam) {
    document.getElementById('revenue-year').value = nam;
    fetch('/Admin/GetThongKe?nam=' + nam).then(function (r) { return r.json(); }).then(function (d) {
        if (!d.success) return;
        // Stats
        document.getElementById('rv-total').textContent = fmt(d.tongDoanhThu) + 'đ';
        document.getElementById('rv-booking').textContent = d.tongDatLich;
        document.getElementById('rv-cancel').textContent = d.tongHuy;
        document.getElementById('rv-star').textContent = d.tbSao.toFixed(1) + ' ★';
        // Chart
        var max = 0;
        d.doanhThuTheoThang.forEach(function (m) { if (m.doanhThu > max) max = m.doanhThu; });
        if (max === 0) max = 1;
        var ch = '';
        d.doanhThuTheoThang.forEach(function (m) {
            var h = Math.max(2, (m.doanhThu / max) * 160);
            ch += '<div class="chart-bar" style="height:' + h + 'px"><span class="bar-value">' + (m.doanhThu > 0 ? fmt(m.doanhThu / 1000000) + 'M' : '') + '</span><span class="bar-label">T' + m.thang + '</span></div>';
        });
        document.getElementById('revenue-chart').innerHTML = ch;
        // Service table
        var th = '';
        d.doanhThuTheoDV.forEach(function (s, i) {
            th += '<tr><td>' + (i + 1) + '</td><td>' + (s.tenDichVu || 'N/A') + '</td><td>' + fmt(s.tongTien) + 'đ</td></tr>';
        });
        if (!d.doanhThuTheoDV.length) th = '<tr><td colspan="3" class="empty-state">Chưa có dữ liệu</td></tr>';
        document.getElementById('tbl-revenue-dv').innerHTML = th;
    });
}

function exportReport() {
    var nam = document.getElementById('revenue-year').value;
    var content = 'BÁO CÁO DOANH THU HAMA STUDIO - NĂM ' + nam + '\n';
    content += '═══════════════════════════════════════\n';
    content += 'Ngày xuất: ' + new Date().toLocaleString('vi-VN') + '\n\n';
    content += 'Tổng doanh thu: ' + document.getElementById('rv-total').textContent + '\n';
    content += 'Tổng lịch đặt: ' + document.getElementById('rv-booking').textContent + '\n';
    content += 'Đã hủy: ' + document.getElementById('rv-cancel').textContent + '\n';
    content += 'Đánh giá TB: ' + document.getElementById('rv-star').textContent + '\n\n';
    content += 'CHI TIẾT DOANH THU THEO DỊCH VỤ\n';
    content += '───────────────────────────────────\n';
    var rows = document.querySelectorAll('#tbl-revenue-dv tr');
    rows.forEach(function (r) {
        var cells = r.querySelectorAll('td');
        if (cells.length >= 3) content += cells[0].textContent + '. ' + cells[1].textContent + ': ' + cells[2].textContent + '\n';
    });
    var blob = new Blob(['\uFEFF' + content], { type: 'text/plain;charset=utf-8' });
    var a = document.createElement('a'); a.href = URL.createObjectURL(blob);
    a.download = 'BaoCao_DoanhThu_' + nam + '.txt'; a.click();
}

// ─── MODAL ───
function showModal(id) { document.getElementById(id).classList.add('show'); }
function hideModal(id) { document.getElementById(id).classList.remove('show'); }
