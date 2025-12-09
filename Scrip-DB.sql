-- ===== RESET DATABASE - FULL SCRIPT =====

-- Drop existing tables if they exist (in reverse order of dependencies)
DROP TABLE IF EXISTS report_detail CASCADE;
DROP TABLE IF EXISTS report CASCADE;
DROP TABLE IF EXISTS "order" CASCADE;
DROP TABLE IF EXISTS promotion_product CASCADE;
DROP TABLE IF EXISTS promotion CASCADE;
DROP TABLE IF EXISTS product_feature CASCADE;
DROP TABLE IF EXISTS feature CASCADE;
DROP TABLE IF EXISTS product CASCADE;
DROP TABLE IF EXISTS notification CASCADE;
DROP TABLE IF EXISTS log_activity CASCADE;
DROP TABLE IF EXISTS "user" CASCADE;
DROP TABLE IF EXISTS shop CASCADE;
DROP TABLE IF EXISTS password_reset_otps CASCADE;

-- Set timezone
SET TIMEZONE = 'Asia/Ho_Chi_Minh';

-- ===== CREATE TABLES =====

-- 1. Shop
CREATE TABLE shop (
    shop_id BIGSERIAL PRIMARY KEY,
    shop_name VARCHAR(150) NOT NULL,
    address TEXT,
    status SMALLINT,
    fullname VARCHAR(150),
    email VARCHAR(150),
    phonenumber VARCHAR(20),
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

-- 2. User
CREATE TABLE "user" (
    user_id BIGSERIAL PRIMARY KEY,
    username VARCHAR(50) UNIQUE NOT NULL,
    password VARCHAR(255) NOT NULL,
    full_name VARCHAR(100),
    email VARCHAR(100) UNIQUE,
    phone_number VARCHAR(20) UNIQUE,
    status SMALLINT,
    role SMALLINT,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    avatar TEXT
);

-- 3. LogActivity
CREATE TABLE log_activity (
    log_activity_id BIGSERIAL PRIMARY KEY,
    user_id BIGINT REFERENCES "user"(user_id) ON DELETE CASCADE,
    content TEXT,
    type SMALLINT,
    created_at TIMESTAMPTZ DEFAULT NOW()
);

-- 4. Notification
CREATE TABLE notification (
    notification_id BIGSERIAL PRIMARY KEY,
    target_role SMALLINT,
    title VARCHAR(150),
    content TEXT,
    type SMALLINT,
    is_read BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMPTZ DEFAULT NOW()
);

-- 5. Product
CREATE TABLE product (
    product_id BIGSERIAL PRIMARY KEY,
    product_name VARCHAR(150) NOT NULL,
    description TEXT,
    price NUMERIC(18,2),
    request_limit INT,
    account_limit INT,
    duration INTERVAL,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW(),
    discount NUMERIC(5,2),
    status SMALLINT,
    qrcode_url TEXT
);

-- 6. Feature
CREATE TABLE feature (
    feature_id BIGSERIAL PRIMARY KEY,
    feature_name VARCHAR(150) NOT NULL,
    description TEXT,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

-- 7. Product Feature (Junction Table)
CREATE TABLE product_feature (
    product_id BIGINT REFERENCES product(product_id) ON DELETE CASCADE,
    feature_id BIGINT REFERENCES feature(feature_id) ON DELETE CASCADE,
    PRIMARY KEY (product_id, feature_id)
);

-- 8. Promotion
CREATE TABLE promotion (
    promotion_id BIGSERIAL PRIMARY KEY,
    promotion_name VARCHAR(150) NOT NULL,
    description TEXT,
    start_date DATE,
    end_date DATE,
    value NUMERIC(10,2),
    type VARCHAR(50),
    status SMALLINT,
    created_at TIMESTAMPTZ DEFAULT NOW()
);

-- 9. PromotionProduct
CREATE TABLE promotion_product (
    promotion_product_id BIGSERIAL PRIMARY KEY,
    promotion_id BIGINT REFERENCES promotion(promotion_id) ON DELETE CASCADE,
    product_id BIGINT REFERENCES product(product_id) ON DELETE CASCADE
);

-- 10. Order
CREATE TABLE "order" (
    order_id BIGSERIAL PRIMARY KEY,
    shop_id BIGINT REFERENCES shop(shop_id) ON DELETE CASCADE,
    product_id BIGINT REFERENCES product(product_id) ON DELETE CASCADE,
    user_id BIGINT REFERENCES "user"(user_id) ON DELETE SET NULL,
    total_price NUMERIC(18,2),
    payment_method VARCHAR(50),
    status SMALLINT,
    discount NUMERIC(10,2),
    created_at TIMESTAMPTZ DEFAULT NOW(),
    expired_at TIMESTAMPTZ,
    note TEXT
);

-- 11. Report
CREATE TABLE report (
    report_id BIGSERIAL PRIMARY KEY,
    type VARCHAR(50),
    start_date DATE,
    end_date DATE,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    order_counter INT,
    revenue NUMERIC(18,2)
);

-- 12. ReportDetail
CREATE TABLE report_detail (
    report_detail_id BIGSERIAL PRIMARY KEY,
    report_id BIGINT REFERENCES report(report_id) ON DELETE CASCADE,
    product_id BIGINT REFERENCES product(product_id) ON DELETE CASCADE,
    quantity INT
);

-- 13. Password Reset OTPs
CREATE TABLE password_reset_otps (
    id SERIAL PRIMARY KEY,
    email VARCHAR(255) NOT NULL,
    otp VARCHAR(20) NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    expired_at TIMESTAMP NOT NULL,
    is_used BOOLEAN NOT NULL DEFAULT FALSE
);

CREATE INDEX idx_password_reset_otps_email_otp
    ON password_reset_otps (email, otp);


-- ===== INSERT SAMPLE DATA =====

-- 1. Tạo cửa hàng
INSERT INTO shop (shop_name, address, status, fullname, phonenumber) VALUES
('Tạp Hóa Minh Hạnh', '123 Đường Nguyễn Văn Cừ, Quận 5,Thành phố Hồ Chí Minh', 1, 'Nguyen Hà Mai Hân', '0912345678');

-- 2. Tạo người dùng
INSERT INTO "user"
(username, password, full_name, email, phone_number, status, role, avatar)
VALUES
('admin',      '$2a$11$thoS01O1IoPLAQlrYppKvuluVCd6M/ieaJ4fv5SfZwf5Homfb1O0u', 'System Admin', 'admin@example.com',    '0900000001', 1, 1, 'https://avatar.example.com/admin.jpg'),
('manager01',  '$2a$11$thoS01O1IoPLAQlrYppKvuluVCd6M/ieaJ4fv5SfZwf5Homfb1O0u', 'Nguyen Van Manager', 'manager01@example.com', '0900000002', 1, 2, 'https://avatar.example.com/manager1.jpg'),
('staff01',    '$2a$11$thoS01O1IoPLAQlrYppKvuluVCd6M/ieaJ4fv5SfZwf5Homfb1O0u', 'Tran Thi Staff 1', 'staff01@example.com',    '0900000003', 1, 3, 'https://avatar.example.com/staff1.jpg'),
('staff02',    '$2a$11$thoS01O1IoPLAQlrYppKvuluVCd6M/ieaJ4fv5SfZwf5Homfb1O0u', 'Le Van Staff 2', 'staff02@example.com',      '0900000004', 1, 3, 'https://avatar.example.com/staff2.jpg'),
('customer01', '$2a$11$thoS01O1IoPLAQlrYppKvuluVCd6M/ieaJ4fv5SfZwf5Homfb1O0u', 'Pham Thi Customer', 'customer01@example.com','0900000005', 1, 4, 'https://avatar.example.com/customer1.jpg');

-- 3. Tạo gói sản phẩm
INSERT INTO product (product_name, description, price, request_limit, account_limit, duration, discount, status, qrcode_url) VALUES
('Gói Basic', 'Gói cơ bản phù hợp cho cửa hàng nhỏ. Bao gồm các tính năng quản lý cơ bản và hỗ trợ kỹ thuật.', 150000.00, 50, 3, INTERVAL '30 days', 0.00, 1, 'https://qr.example.com/basic-package'),
('Gói Premium', 'Gói cao cấp dành cho cửa hàng lớn. Đầy đủ tính năng nâng cao, báo cáo chi tiết và hỗ trợ ưu tiên.', 300000.00, 100, 6, INTERVAL '30 days', 0.00, 1, 'https://qr.example.com/premium-package');

-- 4. Tạo features (8 features)
INSERT INTO feature (feature_name, description) VALUES
('Xuất báo cáo', 'Xuất báo cáo doanh thu, đơn hàng dưới dạng PDF/Excel'),
('Tư vấn AI', 'Trợ lý AI hỗ trợ trong việc quản lý cửa hàng, doanh thu và gợi ý cách bán hàng'),
('Quản lý người dùng', 'Tạo/sửa/xóa tài khoản người dùng trong hệ thống'),
('Bán hàng', 'Quản lý quy trình bán hàng, tạo đơn hàng và thanh toán'),
('Quản lý sản phẩm', 'Quản lý thông tin sản phẩm, kho hàng và giá cả'),
('Quản lý voucher', 'Tạo và quản lý các mã giảm giá, voucher khuyến mãi'),
('Quản lý promotion', 'Quản lý các chương trình khuyến mãi và ưu đãi'),
('Quản lý category', 'Phân loại và quản lý danh mục sản phẩm'),
('Quản lý khách hàng', 'Quản lý thông tin và lịch sử giao dịch của khách hàng');

-- 5. Gán tất cả 8 features cho cả 2 gói sản phẩm
INSERT INTO product_feature (product_id, feature_id) VALUES
-- Gói Basic có tất cả 8 features
(1, 1), (1, 2), (1, 3), (1, 4), (1, 5), (1, 6), (1, 7), (1, 8), (1, 9),
-- Gói Premium có tất cả 8 features
(2, 1), (2, 2), (2, 3), (2, 4), (2, 5), (2, 6), (2, 7), (2, 8), (2, 9);

-- 6. Tạo khuyến mãi
INSERT INTO promotion (promotion_name, description, start_date, end_date, value, type, status) VALUES
('Khuyến mãi khai trương', 'Giảm giá 20% cho khách hàng đăng ký lần đầu trong tháng 10', '2025-10-15', '2025-11-21', 20.00, '%', 1),
('Ưu đãi tuần lễ', 'Giảm 50,000 VNĐ cho gói Premium', '2025-10-18', '2025-11-25', 50000.00, 'VNĐ', 1),
('Khuyến mãi học sinh', 'Giảm 10% cho gói Basic dành cho các cửa hàng học đường', '2025-10-16', '2025-11-22', 10.00, '%', 1);

-- 7. Liên kết khuyến mãi với sản phẩm
INSERT INTO promotion_product (promotion_id, product_id) VALUES
(1, 1),
(1, 2),
(2, 2),
(3, 1);

-- 8. Tạo đơn hàng mẫu
-- Status: 0 = Chờ thanh toán, 1 = Đã thanh toán, 2 = Đã hủy
-- Payment method: 1 = Tiền mặt, 2 = Chuyển khoản
INSERT INTO "order" (shop_id, product_id, user_id, total_price, payment_method, status, discount, expired_at, note) VALUES
(1, 1, 2, 120000.00, 2, 1, 30000.00, '2025-11-19 23:59:59'::TIMESTAMPTZ, 'Áp dụng khuyến mãi khai trương 20%. Đơn hàng đã thanh toán qua chuyển khoản'),
(1, 2, 2, 300000.00, 2, 1, 0.00, '2025-11-20 23:59:59'::TIMESTAMPTZ, 'Gói Premium đã thanh toán thành công qua chuyển khoản'),
(1, 1, 3, 135000.00, 1, 0, 15000.00, '2025-11-21 23:59:59'::TIMESTAMPTZ, 'Khuyến mãi học sinh 10%. Đang chờ thanh toán bằng tiền mặt'),
(1, 2, 4, 250000.00, 2, 1, 50000.00, '2025-11-18 23:59:59'::TIMESTAMPTZ, 'Ưu đãi tuần lễ. Đã thanh toán qua chuyển khoản'),
(1, 1, 5, 150000.00, 1, 2, 0.00, '2025-11-17 23:59:59'::TIMESTAMPTZ, 'Khách hàng hủy do thay đổi nhu cầu');

-- 9. Tạo thông báo
INSERT INTO notification (target_role, title, content, type, is_read) VALUES
(1, 'Cập nhật hệ thống', 'Hệ thống sẽ được bảo trì vào 02:00 sáng ngày 22/10/2025', 1, FALSE),
(2, 'Khuyến mãi mới', 'Khuyến mãi tuần lễ đã được kích hoạt cho gói Premium', 2, FALSE),
(3, 'Đơn hàng mới', 'Có đơn hàng mới cần xử lý từ khách hàng manager01', 3, TRUE),
(4, 'Gia hạn gói dịch vụ', 'Gói dịch vụ của bạn sẽ hết hạn trong 7 ngày nữa', 4, FALSE),
(0, 'Thông báo chung', 'Chào mừng các bạn đến với hệ thống quản lý cửa hàng', 5, FALSE);

-- 10. Tạo log hoạt động (theo type mới)
INSERT INTO log_activity (user_id, content, type) VALUES
-- Type 1: Đăng nhập vào hệ thống quản trị
(1, 'Admin đăng nhập vào hệ thống quản trị', 1),
(2, 'Manager01 đăng nhập vào hệ thống', 1),
(3, 'Staff01 đăng nhập lần đầu', 1),

-- Type 2: Đăng xuất ra hệ thống quản trị
(1, 'Admin đăng xuất khỏi hệ thống', 2),
(3, 'Staff01 đăng xuất khỏi hệ thống', 2),

-- Type 3: Tạo tài khoản khách hàng mới
(1, 'Tạo tài khoản khách hàng customer01', 3),

-- Type 4: Cập nhật thông tin khách hàng
(1, 'Cập nhật số điện thoại cho khách hàng customer01', 4),
(2, 'Cập nhật địa chỉ email cho khách hàng customer01', 4),

-- Type 5: Vô hiệu hóa tài khoản khách hàng
(1, 'Vô hiệu hóa tài khoản customer01 do vi phạm chính sách', 5),

-- Type 6: Kích hoạt tài khoản khách hàng
(1, 'Kích hoạt lại tài khoản customer01 sau khi giải quyết vấn đề', 6),

-- Type 7: Cập nhật thông tin sản phẩm
(1, 'Cập nhật giá gói Basic từ 150,000 lên 180,000', 7),
(1, 'Cập nhật mô tả và tính năng cho gói Premium', 7),

-- Type 12: Tạo sản phẩm phần mềm mới
(1, 'Tạo gói sản phẩm Basic mới', 12),
(1, 'Tạo gói sản phẩm Premium mới', 12),

-- Type 13: Tạo đơn hàng mua license
(2, 'Tạo đơn hàng #1 mua gói Basic', 13),
(3, 'Tạo đơn hàng #3 mua gói Basic với khuyến mãi học sinh', 13),
(4, 'Tạo đơn hàng #4 mua gói Premium với ưu đãi cuối năm', 13),

-- Type 14: Thanh toán
(2, 'Thanh toán đơn hàng #1 thành công qua chuyển khoản', 14),
(2, 'Thanh toán đơn hàng #2 thành công qua chuyển khoản', 14),
(4, 'Thanh toán đơn hàng #4 thành công qua chuyển khoản', 14),

-- Type 9: Tạo subscription (khi bán)
(1, 'Tạo subscription cho đơn hàng #1 - Gói Basic', 9),
(1, 'Tạo subscription cho đơn hàng #2 - Gói Premium', 9),

-- Type 10: Kích hoạt subscription
(2, 'Kích hoạt subscription gói Basic cho shop Tạp Hóa Minh Hạnh', 10),
(2, 'Kích hoạt subscription gói Premium cho shop Tạp Hóa Minh Hạnh', 10),

-- Type 15: Tạo mã khuyến mãi (voucher)
(1, 'Tạo mã khuyến mãi "Khuyến mãi khai trương" giảm 20%', 15),
(1, 'Tạo voucher "Ưu đãi cuối năm" giảm 50,000 VNĐ', 15),
(1, 'Tạo mã "Khuyến mãi học sinh" giảm 10%', 15),

-- Type 16: Áp dụng khuyến mãi cho đơn hàng
(2, 'Áp dụng khuyến mãi khai trương cho đơn hàng #1', 16),
(3, 'Áp dụng khuyến mãi học sinh cho đơn hàng #3', 16),
(4, 'Áp dụng ưu đãi cuối năm cho đơn hàng #4', 16),

-- Type 17: Cập nhật thông tin khuyến mãi
(1, 'Cập nhật thời gian kết thúc cho khuyến mãi học sinh', 17),

-- Type 19: Tạo báo cáo bán hàng
(1, 'Tạo báo cáo doanh thu theo tuần từ 18/08 - 24/08/2024', 19),
(1, 'Tạo báo cáo doanh thu tháng 8/2024', 19);

-- 11. Tạo báo cáo doanh thu
-- Type: 1 = Tuần, 2 = Tháng
INSERT INTO report (type, start_date, end_date, order_counter, revenue) VALUES
(1, '2025-10-15', '2025-10-21', 3, 505000.00),
(1, '2025-10-08', '2025-10-14', 1, 135000.00),
(1, '2025-10-01', '2025-10-07', 0, 0.00),
(2, '2025-10-01', '2025-10-31', 4, 640000.00),
(2, '2025-09-01', '2025-09-30', 6, 1350000.00);

-- 12. Tạo chi tiết báo cáo
INSERT INTO report_detail (report_id, product_id, quantity) VALUES
-- Báo cáo tuần 14-20/10
(1, 1, 2),
(1, 2, 1),
-- Báo cáo tuần 07-13/10
(2, 1, 1),
-- Báo cáo tháng 10
(4, 1, 2),
(4, 2, 2),
-- Báo cáo tháng 9
(5, 1, 4),
(5, 2, 2);

-- ===== RESET COMPLETE =====
-- Database has been reset successfully with all sample data!