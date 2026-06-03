USE master;
GO

IF EXISTS (SELECT name FROM sys.databases WHERE name = N'HamaStudioDB')
BEGIN
    ALTER DATABASE HamaStudioDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE HamaStudioDB;
END
GO

CREATE DATABASE HamaStudioDB;
GO

USE HamaStudioDB;
GO

-----------------------------------------------------------
-- 1. TẠO BẢNG
-----------------------------------------------------------
CREATE TABLE QuanTriVien (
    MaAdmin INT PRIMARY KEY IDENTITY(1,1),
    TenDangNhap VARCHAR(50) NOT NULL UNIQUE,
    MatKhau VARCHAR(255) NOT NULL,
    HoTen NVARCHAR(100) NOT NULL,
    Email VARCHAR(100),
    SoDienThoai VARCHAR(15),
    CapBac NVARCHAR(50), 
    NgayVaoLam DATE,
    TrangThai NVARCHAR(20) DEFAULT N'Hoạt động'
);

CREATE TABLE KhachHang (
    MaKhachHang INT PRIMARY KEY IDENTITY(1,1),
    TenDangNhap VARCHAR(50) NOT NULL UNIQUE,
    MatKhau VARCHAR(255) NOT NULL,
    HoTen NVARCHAR(100) NOT NULL,
    GioiTinh NVARCHAR(10),
    NgaySinh DATE,
    SoDienThoai VARCHAR(15) NOT NULL,
    Email VARCHAR(100),
    DiaChi NVARCHAR(255),
    TrangThai NVARCHAR(20) DEFAULT N'Hoạt động',
    NgayDangKy DATETIME DEFAULT GETDATE()
);

CREATE TABLE DanhMucDichVu (
    MaDanhMuc INT PRIMARY KEY IDENTITY(1,1),
    TenDanhMuc NVARCHAR(100) NOT NULL
);

CREATE TABLE DichVu (
    MaDichVu INT PRIMARY KEY IDENTITY(1,1),
    MaDanhMuc INT FOREIGN KEY REFERENCES DanhMucDichVu(MaDanhMuc),
    TenDichVu NVARCHAR(200) NOT NULL,
    GiaTien DECIMAL(18, 2) NOT NULL DEFAULT 0,
    GioiHanTho INT NOT NULL DEFAULT 1, -- Số lượng slot/thợ có thể nhận cùng 1 lúc cho dịch vụ này
    MoTa NVARCHAR(MAX),
    LinkAnhDaiDien VARCHAR(500)
);

CREATE TABLE DatLich (
    MaDatLich INT PRIMARY KEY IDENTITY(1,1),
    MaKhachHang INT FOREIGN KEY REFERENCES KhachHang(MaKhachHang),
    MaDichVu INT FOREIGN KEY REFERENCES DichVu(MaDichVu),
    NgayHeThongGhiNhan DATETIME DEFAULT GETDATE(),
    NgayChup DATE NOT NULL,
    KhungGio VARCHAR(20) NOT NULL, 
    DiaDiem NVARCHAR(255),
    TongTien DECIMAL(18, 2) DEFAULT 0,
    SoTienDaCoc DECIMAL(18, 2) DEFAULT 0,
    TrangThai NVARCHAR(50) DEFAULT N'Chờ xác nhận', 
    GhiChu NVARCHAR(MAX)
);

CREATE TABLE ThanhToan (
    MaThanhToan INT PRIMARY KEY IDENTITY(1,1),
    MaDatLich INT FOREIGN KEY REFERENCES DatLich(MaDatLich),
    SoTienThanhToan DECIMAL(18, 2) NOT NULL,
    PhuongThuc NVARCHAR(50), 
    NgayGiaoDich DATETIME DEFAULT GETDATE(),
    NoiDungThanhToan NVARCHAR(255)
);

CREATE TABLE DanhGia (
    MaDanhGia INT PRIMARY KEY IDENTITY(1,1),
    MaDatLich INT FOREIGN KEY REFERENCES DatLich(MaDatLich),
    MaKhachHang INT FOREIGN KEY REFERENCES KhachHang(MaKhachHang),
    SoSao INT CHECK (SoSao BETWEEN 1 AND 5),
    NoiDungBinhLuan NVARCHAR(MAX),
    NgayGui DATETIME DEFAULT GETDATE()
);

CREATE TABLE Portfolio (
    MaAnh INT PRIMARY KEY IDENTITY(1,1),
    MaDichVu INT FOREIGN KEY REFERENCES DichVu(MaDichVu),
    LinkFileAnh VARCHAR(500) NOT NULL,
    TieuDe NVARCHAR(200)
);

CREATE TABLE DacQuyen (
    MaDacQuyen INT PRIMARY KEY IDENTITY(1,1),
    MaDichVu INT FOREIGN KEY REFERENCES DichVu(MaDichVu),
    Icon VARCHAR(50) DEFAULT 'bi-check-circle',
    TieuDe NVARCHAR(100) NOT NULL,
    NoiDung NVARCHAR(200) NOT NULL
);

CREATE TABLE DichVuBoSung (
    MaDVBS INT PRIMARY KEY IDENTITY(1,1),
    MaDichVu INT FOREIGN KEY REFERENCES DichVu(MaDichVu),
    TenDVBS NVARCHAR(200) NOT NULL,
    MoTa NVARCHAR(500),
    GiaTien DECIMAL(18, 2) NOT NULL DEFAULT 0
);
GO

-----------------------------------------------------------
-- 2. RÀNG BUỘC (CONSTRAINTS)
-----------------------------------------------------------
ALTER TABLE DichVu ADD CONSTRAINT CHK_GiaTien_DV CHECK (GiaTien >= 0);
ALTER TABLE DichVu ADD CONSTRAINT CHK_GioiHanTho_DV CHECK (GioiHanTho > 0);
ALTER TABLE DatLich ADD CONSTRAINT CHK_TongTien_DL CHECK (TongTien >= 0);
ALTER TABLE ThanhToan ADD CONSTRAINT CHK_SoTien_TT CHECK (SoTienThanhToan > 0);
GO

-----------------------------------------------------------
-- 3. TRIGGERS
-----------------------------------------------------------
-- Trigger 1: Tự động tính tiền khi chèn dòng mới vào DatLich
CREATE TRIGGER trg_TuDongTinhTien
ON DatLich
AFTER INSERT
AS
BEGIN
    UPDATE DatLich
    SET TongTien = dv.GiaTien
    FROM DatLich dl
    JOIN inserted i ON dl.MaDatLich = i.MaDatLich
    JOIN DichVu dv ON i.MaDichVu = dv.MaDichVu;
END;
GO

-- Trigger 2: Ngăn đặt lịch ngày quá khứ
CREATE TRIGGER trg_KiemTraNgay
ON DatLich
FOR INSERT, UPDATE
AS
BEGIN
    IF EXISTS (SELECT 1 FROM inserted WHERE NgayChup < CAST(GETDATE() AS DATE))
    BEGIN
        RAISERROR (N'Lỗi: Không được đặt lịch cho ngày đã qua!', 16, 1);
        ROLLBACK TRANSACTION;
    END
END;
GO

-- Trigger 3: Cập nhật trạng thái 'Đã đặt cọc' khi có thanh toán
CREATE TRIGGER trg_CapNhatTrangThai
ON ThanhToan
AFTER INSERT
AS
BEGIN
    UPDATE DatLich
    SET TrangThai = N'Đã đặt cọc',
        SoTienDaCoc = SoTienDaCoc + i.SoTienThanhToan
    FROM DatLich dl
    JOIN inserted i ON dl.MaDatLich = i.MaDatLich;
END;
GO

-- Trigger 4: Chống trùng lịch chụp dựa trên Giới Hạn Thợ
CREATE TRIGGER trg_ChongTrungLich
ON DatLich
FOR INSERT, UPDATE
AS
BEGIN
    -- Đếm số lịch đặt đang active (không phải 'Đã hủy') tại cùng 1 ngày, cùng khung giờ, cùng loại dịch vụ
    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN DatLich dl ON i.NgayChup = dl.NgayChup 
                       AND i.KhungGio = dl.KhungGio 
                       AND i.MaDichVu = dl.MaDichVu
        JOIN DichVu dv ON dl.MaDichVu = dv.MaDichVu
        WHERE dl.TrangThai <> N'Đã hủy'
        GROUP BY dl.NgayChup, dl.KhungGio, dl.MaDichVu, dv.GioiHanTho
        HAVING COUNT(dl.MaDatLich) > dv.GioiHanTho
    )
    BEGIN
        RAISERROR (N'Lỗi: Đã hết slot thợ chụp cho gói dịch vụ trong khung giờ này!', 16, 1);
        ROLLBACK TRANSACTION;
    END
END;
GO

-----------------------------------------------------------
-- 4. INSERT DỮ LIỆU MẪU (10 DÒNG MỖI BẢNG)
-----------------------------------------------------------

-- Insert QuanTriVien
INSERT INTO QuanTriVien (TenDangNhap, MatKhau, HoTen, Email, SoDienThoai, CapBac, NgayVaoLam) VALUES
('admin1', 'hashpass1', N'Nguyễn Văn An', 'an.nguyen@hamastudio.com', '0901112223', N'Quản lý', '2023-01-15'),
('admin2', 'hashpass2', N'Trần Thị Bích', 'bich.tran@hamastudio.com', '0901112224', N'Thợ chụp chính', '2023-02-20'),
('admin3', 'hashpass3', N'Lê Hoàng Cường', 'cuong.le@hamastudio.com', '0901112225', N'Thợ chụp chính', '2023-03-10'),
('admin4', 'hashpass4', N'Phạm Mỹ Dung', 'dung.pham@hamastudio.com', '0901112226', N'Thợ chụp phụ', '2023-04-05'),
('admin5', 'hashpass5', N'Hoàng Quốc Đạt', 'dat.hoang@hamastudio.com', '0901112227', N'Hậu kỳ (Editor)', '2023-05-12'),
('admin6', 'hashpass6', N'Vũ Ngọc Lan', 'lan.vu@hamastudio.com', '0901112228', N'Chăm sóc khách hàng', '2023-06-18'),
('admin7', 'hashpass7', N'Đinh Hữu Phong', 'phong.dinh@hamastudio.com', '0901112229', N'Thợ chụp chính', '2023-07-22'),
('admin8', 'hashpass8', N'Bùi Thanh Trúc', 'truc.bui@hamastudio.com', '0901112230', N'Makeup Artist', '2023-08-30'),
('admin9', 'hashpass9', N'Ngô Tấn Tài', 'tai.ngo@hamastudio.com', '0901112231', N'Marketing', '2023-09-15'),
('admin10', 'hashpass10', N'Đỗ Minh Yến', 'yen.do@hamastudio.com', '0901112232', N'Makeup Artist', '2023-10-01');

-- Insert KhachHang
INSERT INTO KhachHang (TenDangNhap, MatKhau, HoTen, GioiTinh, NgaySinh, SoDienThoai, Email, DiaChi) VALUES
('khach1', 'khpass1', N'Lê Minh Tuấn', N'Nam', '1995-05-12', '0912345671', 'tuan.le@gmail.com', N'Hải Châu, Đà Nẵng'),
('khach2', 'khpass2', N'Nguyễn Thu Trà', N'Nữ', '1998-08-22', '0912345672', 'tra.nguyen@gmail.com', N'Sơn Trà, Đà Nẵng'),
('khach3', 'khpass3', N'Trần Quốc Bảo', N'Nam', '1992-11-05', '0912345673', 'bao.tran@gmail.com', N'Thanh Khê, Đà Nẵng'),
('khach4', 'khpass4', N'Phạm Bích Ngọc', N'Nữ', '1997-02-14', '0912345674', 'ngoc.pham@gmail.com', N'Cẩm Lệ, Đà Nẵng'),
('khach5', 'khpass5', N'Hoàng Tấn Phát', N'Nam', '1990-12-25', '0912345675', 'phat.hoang@gmail.com', N'Ngũ Hành Sơn, Đà Nẵng'),
('khach6', 'khpass6', N'Vũ Thị Mai', N'Nữ', '2000-01-10', '0912345676', 'mai.vu@gmail.com', N'Liên Chiểu, Đà Nẵng'),
('khach7', 'khpass7', N'Đỗ Hùng Dũng', N'Nam', '1993-07-19', '0912345677', 'dung.do@gmail.com', N'Hòa Vang, Đà Nẵng'),
('khach8', 'khpass8', N'Lý Thảo Vy', N'Nữ', '1999-04-30', '0912345678', 'vy.ly@gmail.com', N'Hải Châu, Đà Nẵng'),
('khach9', 'khpass9', N'Đặng Phương Nam', N'Nam', '1996-09-02', '0912345679', 'nam.dang@gmail.com', N'Sơn Trà, Đà Nẵng'),
('khach10', 'khpass10', N'Bùi Phương Thảo', N'Nữ', '2002-10-11', '0912345680', 'thao.bui@gmail.com', N'Thanh Khê, Đà Nẵng');

-- Insert DanhMucDichVu
INSERT INTO DanhMucDichVu (TenDanhMuc) VALUES
(N'Chụp Ảnh Cưới (Pre-wedding)'),
(N'Chụp Ảnh Sự Kiện (Event)'),
(N'Chụp Ảnh Chân Dung (Portrait)'),
(N'Chụp Ảnh Kỷ Yếu'),
(N'Chụp Ảnh Gia Đình'),
(N'Chụp Ảnh Em Bé (Newborn)'),
(N'Chụp Ảnh Sản Phẩm'),
(N'Chụp Ảnh Nàng Thơ'),
(N'Chụp Ảnh Doanh Nhân'),
(N'Chụp Ảnh Cặp Đôi (Couple)');

-- Insert DichVu
INSERT INTO DichVu (MaDanhMuc, TenDichVu, GiaTien, GioiHanTho, MoTa, LinkAnhDaiDien) VALUES
(1, N'Gói Pre-wedding Cao Cấp Ngoại Cảnh', 15000000, 3, N'Bao gồm 2 thợ chụp, 1 makeup, di chuyển nội thành', 'https://images.unsplash.com/photo-1519741497674-611481863552?w=800'),
(1, N'Gói Cưới Studio Cơ Bản', 5000000, 3, N'Chụp phông trơn Hàn Quốc, 1 thợ chính', 'https://images.unsplash.com/photo-1511285560929-80b456fea0bc?w=800'),
(2, N'Chụp Sự Kiện Nửa Ngày', 3000000, 2, N'4 tiếng chụp sự kiện, giao toàn bộ file gốc', 'https://images.unsplash.com/photo-1511578314322-379afb476865?w=800'),
(3, N'Chân Dung Nghệ Thuật', 2000000, 3, N'Concept trong nhà, makeup nhẹ nhàng', 'https://images.unsplash.com/photo-1534528741775-53994a69daeb?w=800'),
(4, N'Gói Kỷ Yếu Lớp (Premium)', 8000000, 2, N'Chụp toàn thời gian, bao gồm flycam', 'https://images.unsplash.com/photo-1523050854058-8df90110c9f1?w=800'),
(5, N'Gói Gia Đình Kỷ Niệm (Studio)', 3500000, 3, N'Chụp gia đình 4-6 người, in 1 ảnh gỗ', 'https://images.unsplash.com/photo-1542038784456-1ea8e935640e?w=800'),
(6, N'Gói Newborn Baby', 4000000, 2, N'Chụp tại nhà bé, hỗ trợ trang phục và phụ kiện', 'https://images.unsplash.com/photo-1519689680058-324335c77eba?w=800'),
(7, N'Chụp Sản Phẩm Mỹ Phẩm', 1500000, 1, N'Gói chụp 10 sản phẩm, decor phông nền chuẩn', 'https://images.unsplash.com/photo-1522335789203-aabd1fc54bc9?w=800'),
(8, N'Nàng Thơ Vườn Hoa', 2500000, 3, N'Chụp ngoại cảnh, hỗ trợ 2 váy', 'https://images.unsplash.com/photo-1494790108377-be9c29b29330?w=800'),
(9, N'Profile Doanh Nhân', 1800000, 3, N'Chụp vest/phông xám đen chuyên nghiệp', 'https://images.unsplash.com/photo-1560250097-0b93528c311a?w=800');

-- Insert DatLich (Dùng DATEADD giả lập ngày tương lai)
INSERT INTO DatLich (MaKhachHang, MaDichVu, NgayChup, KhungGio, DiaDiem, GhiChu) VALUES
(1, 1, DATEADD(DAY, 5, GETDATE()), '08:00 - 17:00', N'Bà Nà Hills', N'Cần ekip mang theo hắt sáng lớn'),
(2, 8, DATEADD(DAY, 6, GETDATE()), '07:30 - 10:30', N'Bán đảo Sơn Trà', N'Khách thích tone màu phim'),
(3, 3, DATEADD(DAY, 7, GETDATE()), '18:00 - 22:00', N'Khách sạn Novotel', N'Chụp tiệc Gala Dinner'),
(4, 4, DATEADD(DAY, 10, GETDATE()), '07:00 - 17:00', N'Trường ĐH Kinh Tế', N'Lớp 40 người'),
(5, 5, DATEADD(DAY, 12, GETDATE()), '09:00 - 11:30', N'Hama Studio Cơ sở 1', N'Gia đình có em bé 2 tuổi'),
(6, 2, DATEADD(DAY, 15, GETDATE()), '13:30 - 17:30', N'Hama Studio Cơ sở 1', N'Makeup phong cách trong trẻo'),
(7, 9, DATEADD(DAY, 16, GETDATE()), '14:00 - 16:00', N'Hama Studio Cơ sở 2', N'Lấy phông nền xám đậm'),
(8, 10, DATEADD(DAY, 20, GETDATE()), '15:00 - 18:00', N'Cầu Tình Yêu', N'Chụp lúc hoàng hôn'),
(9, 7, DATEADD(DAY, 21, GETDATE()), '08:00 - 12:00', N'Hama Studio Cơ sở 2', N'Sản phẩm là nước hoa'),
(10, 6, DATEADD(DAY, 25, GETDATE()), '09:00 - 12:00', N'Nhà khách hàng', N'Nhà chung cư, ánh sáng tự nhiên tốt');

-- Insert ThanhToan 
INSERT INTO ThanhToan (MaDatLich, SoTienThanhToan, PhuongThuc, NoiDungThanhToan) VALUES
(1, 5000000, N'Chuyển khoản', N'Khách 1 cọc gói pre-wedding'),
(2, 1000000, N'Chuyển khoản', N'Khách 2 cọc gói nàng thơ'),
(3, 1500000, N'Tiền mặt', N'Khách 3 cọc sự kiện'),
(4, 3000000, N'Chuyển khoản', N'Khách 4 cọc kỷ yếu'),
(5, 1000000, N'Chuyển khoản', N'Khách 5 cọc gia đình'),
(6, 2000000, N'Thẻ tín dụng', N'Khách 6 cọc cưới studio'),
(7, 500000, N'Chuyển khoản', N'Khách 7 cọc doanh nhân'),
(8, 500000, N'Chuyển khoản', N'Khách 8 cọc cặp đôi'),
(9, 1000000, N'Tiền mặt', N'Khách 9 thanh toán trước sp'),
(10, 1500000, N'Chuyển khoản', N'Khách 10 cọc chụp em bé');

-- Insert DanhGia
INSERT INTO DanhGia (MaDatLich, MaKhachHang, SoSao, NoiDungBinhLuan) VALUES
(1, 1, 5, N'Ekip nhiệt tình, ảnh rất đẹp và ưng ý!'),
(2, 2, 4, N'Màu ảnh đẹp nhưng thợ makeup hơi muộn 10p.'),
(3, 3, 5, N'Bắt khoảnh khắc sự kiện rất tốt, sếp mình rất thích.'),
(4, 4, 5, N'Chụp kỷ yếu cực vui, mấy anh thợ biết pha trò.'),
(5, 5, 5, N'Thợ chụp có tâm dỗ em bé cười rất tươi.'),
(6, 6, 5, N'Phông nền studio đẹp, ánh sáng đánh chuẩn.'),
(7, 7, 4, N'Chất lượng ảnh ok, form dáng chuẩn chỉnh.'),
(8, 8, 5, N'Ảnh hoàng hôn lãng mạn đúng ý mình mong muốn.'),
(9, 9, 5, N'Sản phẩm lên chi tiết cực kỳ rõ nét.'),
(10, 10, 5, N'Rất thích cách ekip setup phụ kiện cho bé.');

-- Insert Portfolio
INSERT INTO Portfolio (MaDichVu, LinkFileAnh, TieuDe) VALUES
(1, 'https://images.unsplash.com/photo-1583939003579-730e3918a45a?w=800', N'Pre-wedding phong cách điện ảnh'),
(1, 'https://images.unsplash.com/photo-1465495976277-4387d4b0b4c6?w=800', N'Pre-wedding nhẹ nhàng tại Bà Nà'),
(2, 'https://images.unsplash.com/photo-1475721027785-f74eccf877e2?w=800', N'Khai trương chi nhánh mới'),
(3, 'https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?w=800', N'Chân dung Beauty'),
(4, 'https://images.unsplash.com/photo-1541339907198-e08756dedf3f?w=800', N'Kỷ yếu concept Retro'),
(5, 'https://images.unsplash.com/photo-1511895426328-dc8714191300?w=800', N'Gia đình hạnh phúc concept Tết'),
(6, 'https://images.unsplash.com/photo-1536640712247-c57f8df49073?w=800', N'Thiên thần ngủ say'),
(7, 'https://images.unsplash.com/photo-1608248597279-f99d160bfcbc?w=800', N'Bộ sưu tập son mùa thu'),
(8, 'https://images.unsplash.com/photo-1518546305927-5a555bb7020d?w=800', N'Nàng thơ giữa đồng hoa'),
(9, 'https://images.unsplash.com/photo-1573496359142-b8d87734a5a2?w=800', N'Profile doanh nhân hiện đại');

-- Insert DacQuyen
INSERT INTO DacQuyen (MaDichVu, Icon, TieuDe, NoiDung) VALUES
(1, 'bi-clock-history', N'Thời gian', N'Toàn bộ ngày cưới (08 - 12 tiếng)'),
(1, 'bi-camera-reels', N'Nhân sự', N'02 Nhiếp ảnh gia & 01 Stylist'),
(1, 'bi-images', N'Sản phẩm', N'300+ ảnh đã xử lý & Album 50 trang'),
(1, 'bi-gift', N'Ưu đãi', N'Miễn phí 01 ảnh ép gỗ 60x90cm'),
(2, 'bi-clock', N'Thời gian', N'4 tiếng chụp hình'),
(2, 'bi-camera', N'Nhân sự', N'01 Nhiếp ảnh gia chính'),
(2, 'bi-images', N'Sản phẩm', N'150+ ảnh đã xử lý'),
(3, 'bi-clock-history', N'Thời gian', N'4 tiếng chụp sự kiện'),
(3, 'bi-images', N'Sản phẩm', N'Giao toàn bộ file gốc trong ngày'),
(4, 'bi-clock', N'Thời gian', N'2 tiếng tại studio'),
(4, 'bi-images', N'Sản phẩm', N'10 ảnh nghệ thuật retouch kỹ'),
(5, 'bi-camera-reels', N'Flycam', N'Hỗ trợ quay flycam toàn cảnh'),
(5, 'bi-people', N'Nhân sự', N'Ekip 3 người hỗ trợ nhiệt tình'),
(6, 'bi-house', N'Địa điểm', N'Chụp tại studio bối cảnh gia đình'),
(6, 'bi-gift', N'Sản phẩm', N'In 1 ảnh gỗ cao cấp 40x60cm'),
(7, 'bi-house-heart', N'Địa điểm', N'Chụp tại nhà bé với set up di động'),
(7, 'bi-heart', N'Chăm sóc', N'Hỗ trợ trang phục và phụ kiện cho bé'),
(8, 'bi-box-seam', N'Số lượng', N'Gói chụp 10 sản phẩm'),
(8, 'bi-palette', N'Decor', N'Decor phông nền chuẩn chỉnh'),
(9, 'bi-tree', N'Địa điểm', N'Chụp ngoại cảnh tự nhiên'),
(9, 'bi-bag-check', N'Trang phục', N'Hỗ trợ 2 váy dự tiệc/váy vintage'),
(10, 'bi-person-badge', N'Concept', N'Chụp vest/phông xám đen chuyên nghiệp'),
(10, 'bi-images', N'Sản phẩm', N'10 ảnh retouch chuyên sâu Profile');

-- Insert DichVuBoSung
INSERT INTO DichVuBoSung (MaDichVu, TenDVBS, MoTa, GiaTien) VALUES
(1, N'Makeup & Làm tóc', N'Thay đổi 3 layout theo váy', 1500000),
(1, N'Flycam quay phim', N'Toàn cảnh không gian tiệc', 3000000),
(1, N'In thêm Album Photobook', N'Album 40 trang siêu nét', 2000000),
(2, N'Makeup cô dâu', N'Layout Hàn Quốc nhẹ nhàng', 1000000),
(3, N'Quay Highlight sự kiện', N'Clip 3-5 phút tóm tắt sự kiện', 4000000),
(4, N'In ảnh Canvas 60x90', N'Tranh canvas nghệ thuật treo tường', 800000),
(5, N'Thuê trang phục concept', N'Đồng phục lớp hoặc trang phục Retro', 2000000),
(6, N'Makeup mẹ và bé', N'Makeup và làm tóc nhẹ nhàng', 800000),
(7, N'In thêm Album mini', N'Album 15x15cm để bàn', 500000),
(8, N'Chụp thêm 10 sản phẩm', N'Chi phí ưu đãi khi chụp số lượng lớn', 1000000),
(9, N'Makeup hóa trang', N'Makeup theo concept nàng thơ', 800000),
(10, N'Makeup nam/nữ doanh nhân', N'Làm tóc và trang điểm chỉnh chu', 500000);
GO