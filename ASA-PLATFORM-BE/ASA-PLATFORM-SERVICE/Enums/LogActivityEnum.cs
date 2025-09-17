using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASA_PLATFORM_SERVICE.Enums
{
    public enum LogActivityType
    {
        [Description("Đăng nhập")]
        Login = 1,

        [Description("Đăng xuất")]
        Logout = 2,

        [Description("Tạo Khách Hàng")]
        CreateCustomer = 3,

        [Description("Cập nhật Khách Hàng")]
        UpdateCustomer = 4,

        [Description("Khóa Khách Hàng")]
        DeactivateCustomer = 5,

        [Description("Kích hoạt Khách Hàng")]
        ActivateCustomer = 6,

        [Description("Cập nhật Sản Phẩm")]
        UpdateProduct = 7,

        [Description("Khóa Sản Phẩm")]
        DeactivateProduct = 8,

        [Description("Tạo Shop")]
        CreateSubscription = 9,

        [Description("Kích hoạt Shop")]
        ActivateSubscription = 10,

        [Description("Khóa Shop")]
        DeactivateSubscription = 11,

        [Description("Tạo Sản Phẩm")]
        CreateSoftwareProduct = 12,

        [Description("Tạo Đơn hàng")]
        CreateOrder = 13,

        [Description("Thanh toán")]
        Payment = 14,

        [Description("Tạo voucher")]
        CreateVoucher = 15,

        [Description("Áp dụg Khuyến Mãi")]
        ApplyPromotion = 16,

        [Description("Cập nhật Khuyến Mãi")]
        UpdatePromotion = 17,

        [Description("Hủy Khuyến Mãi")]
        CancelPromotion = 18,

        [Description("Tạo báo cáo bán hàng")]
        GenerateSalesReport = 19
    }
}
