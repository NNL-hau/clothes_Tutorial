using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ClothesShop.Web.Models
{
    public class Ward
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";
    }

    public class District
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("wards")]
        public List<Ward> Wards { get; set; } = new();
    }

    public class Province
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("districts")]
        public List<District> Districts { get; set; } = new();
    }

    public static class AddressData
    {
        public static List<string> Provinces = new() { 
            "Hà Nội", "Hồ Chí Minh", "Đà Nẵng", "Hải Phòng", "Cần Thơ", 
            "An Giang", "Bà Rịa - Vũng Tàu", "Bắc Giang", "Bắc Kạn", "Bạc Liêu", 
            "Bắc Ninh", "Bến Tre", "Bình Định", "Bình Dương", "Bình Phước", 
            "Bình Thuận", "Cà Mau", "Cao Bằng", "Đắk Lắk", "Đắk Nông", 
            "Điện Biên", "Đồng Nai", "Đồng Tháp", "Gia Lai", "Hà Giang", 
            "Hà Nam", "Hà Tĩnh", "Hải Dương", "Hậu Giang", "Hòa Bình", 
            "Hưng Yên", "Khánh Hòa", "Kiên Giang", "Kon Tum", "Lai Châu", 
            "Lâm Đồng", "Lạng Sơn", "Lào Cai", "Long An", "Nam Định", 
            "Nghệ An", "Ninh Bình", "Ninh Thuận", "Phú Thọ", "Quảng Bình", 
            "Quảng Nam", "Quảng Ngãi", "Quảng Ninh", "Quảng Trị", "Sóc Trăng", 
            "Sơn La", "Tây Ninh", "Thái Bình", "Thái Nguyên", "Thanh Hóa", 
            "Thừa Thiên Huế", "Tiền Giang", "Trà Vinh", "Tuyên Quang", "Vĩnh Long", 
            "Vĩnh Phúc", "Yên Bái", "Phú Yên"
        };

        public static Dictionary<string, List<string>> Districts = new() {
            { "Hà Nội", new() { "Ba Đình", "Hoàn Kiếm", "Tây Hồ", "Long Biên", "Cầu Giấy", "Đống Đa", "Hai Bà Trưng", "Hoàng Mai", "Thanh Xuân", "Hà Đông", "Bắc Từ Liêm", "Nam Từ Liêm", "Thanh Trì", "Gia Lâm", "Đông Anh", "Sóc Sơn", "Sơn Tây", "Ba Vì", "Phúc Thọ", "Thạch Thất", "Quốc Oai", "Chương Mỹ", "Đan Phượng", "Hoài Đức", "Thanh Oai", "Mỹ Đức", "Ứng Hòa", "Thường Tín", "Phú Xuyên", "Mê Linh" } },
            { "Hồ Chí Minh", new() { "Quận 1", "Quận 3", "Quận 4", "Quận 5", "Quận 6", "Quận 7", "Quận 8", "Quận 10", "Quận 11", "Quận 12", "Bình Tân", "Bình Thạnh", "Gò Vấp", "Phú Nhuận", "Tân Bình", "Tân Phú", "Thủ Đức", "Hóc Môn", "Củ Chi", "Nhà Bè", "Bình Chánh", "Cần Giờ" } },
            { "Đà Nẵng", new() { "Hải Châu", "Thanh Khê", "Sơn Trà", "Ngũ Hành Sơn", "Liên Chiểu", "Cẩm Lệ", "Hòa Vang", "Hoàng Sa" } },
            { "Hải Phòng", new() { "Hồng Bàng", "Ngô Quyền", "Lê Chân", "Hải An", "Kiến An", "Đồ Sơn", "Dương Kinh", "Thuỷ Nguyên", "An Dương", "An Lão", "Tiên Lãng", "Vĩnh Bảo", "Kiến Thuỵ", "Cát Hải", "Bạch Long Vĩ" } },
            { "Cần Thơ", new() { "Ninh Kiều", "Ô Môn", "Bình Thuỷ", "Cái Răng", "Thốt Nốt", "Vĩnh Thạnh", "Cờ Đỏ", "Phong Điền", "Thới Lai" } }
        };

        public static Dictionary<string, List<string>> Wards = new() {
            // Hà Nội - Ba Đình
            { "Ba Đình", new() { "Cống Vị", "Điện Biên", "Đội Cấn", "Giảng Võ", "Kim Mã", "Liễu Giai", "Ngọc Hà", "Ngọc Khánh", "Nguyễn Trung Trực", "Quán Thánh", "Thành Công", "Trúc Bạch", "Vĩnh Phúc", "Phúc Xá" } },
            { "Hoàn Kiếm", new() { "Chương Dương", "Cửa Đông", "Cửa Nam", "Đồng Xuân", "Hàng Bạc", "Hàng Bài", "Hàng Bồ", "Hàng Bông", "Hàng Buồm", "Hàng Đào", "Hàng Gai", "Hàng Mã", "Hàng Trống", "Lý Thái Tổ", "Phan Chu Trinh", "Phúc Tân", "Trần Hưng Đạo", "Tràng Tiền" } },
            { "Cầu Giấy", new() { "Dịch Vọng", "Dịch Vọng Hậu", "Mai Dịch", "Nghĩa Đô", "Nghĩa Tân", "Quan Hoa", "Trung Hòa", "Yên Hòa" } },
            
            // Hồ Chí Minh - Quận 1
            { "Quận 1", new() { "Bến Nghé", "Bến Thành", "Cô Giang", "Đa Kao", "Nguyễn Cư Trinh", "Nguyễn Thái Bình", "Phạm Ngũ Lão", "Tân Định", "Đa Kao", "Tân Định" } },
            { "Quận 3", new() { "Võ Thị Sáu", "Võ Văn Tần", "Phường 1", "Phường 2", "Phường 3", "Phường 4", "Phường 5" } },
            { "Bình Thạnh", new() { "Phường 1", "Phường 2", "Phường 3", "Phường 5", "Phường 6", "Phường 7", "Phường 11", "Phường 12", "Phường 13", "Phường 14", "Phường 15", "Phường 17", "Phường 19", "Phường 21", "Phường 22", "Phường 24", "Phường 25", "Phường 26", "Phường 27", "Phường 28" } },

            // Đà Nẵng - Hải Châu
            { "Hải Châu", new() { "Bình Hiên", "Bình Thuận", "Hải Châu I", "Hải Châu II", "Hòa Cường Bắc", "Hòa Cường Nam", "Hòa Thuận Đông", "Hòa Thuận Tây", "Nam Dương", "Phước Ninh", "Thạch Thang", "Thanh Bình", "Thuận Phước" } }
        };
    }
}
