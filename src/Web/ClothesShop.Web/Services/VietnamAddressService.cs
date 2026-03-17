using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using ClothesShop.Web.Models;

namespace ClothesShop.Web.Services
{
    public interface IVietnamAddressService
    {
        Task InitializeAsync();
        List<string> GetProvinces();
        List<string> GetDistricts(string provinceName);
        List<string> GetWards(string districtName);
    }

    public class VietnamAddressService : IVietnamAddressService
    {
        private readonly HttpClient _httpClient;
        private List<Province> _provinces = new();
        private bool _isInitialized = false;

        public VietnamAddressService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task InitializeAsync()
        {
            if (_isInitialized) return;

            try
            {
                var data = await _httpClient.GetFromJsonAsync<List<Province>>("sample-data/vietnam_provinces.json");
                if (data != null)
                {
                    _provinces = data;
                    _isInitialized = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading address data: {ex.Message}");
            }
        }

        public List<string> GetProvinces()
        {
            return _provinces.Select(p => p.Name).OrderBy(n => n).ToList();
        }

        public List<string> GetDistricts(string provinceName)
        {
            if (string.IsNullOrEmpty(provinceName)) return new List<string>();
            var province = _provinces.FirstOrDefault(p => p.Name == provinceName);
            return province?.Districts.Select(d => d.Name).OrderBy(n => n).ToList() ?? new List<string>();
        }

        public List<string> GetWards(string districtName)
        {
            if (string.IsNullOrEmpty(districtName)) return new List<string>();
            
            // Note: Since district names might not be unique across provinces in some datasets, 
            // but usually are in these JSONs for selection purposes.
            var district = _provinces.SelectMany(p => p.Districts)
                                    .FirstOrDefault(d => d.Name == districtName);
            
            return district?.Wards.Select(w => w.Name).OrderBy(n => n).ToList() ?? new List<string>();
        }
    }
}
