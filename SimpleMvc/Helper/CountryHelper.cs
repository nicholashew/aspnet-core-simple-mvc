using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace SimpleMvc.Helper
{
    public class CountryHelper
    {
        /// <summary>
        /// Gets the list of countries based on ISO 3166-1
        /// </summary>
        /// <returns>Returns the list of countries based on ISO 3166-1</returns>
        public static List<RegionInfo> GetCountriesByIso3166()
        {
            var countries = new List<RegionInfo>();
            foreach (CultureInfo culture in CultureInfo.GetCultures(CultureTypes.SpecificCultures))
            {
                var country = new RegionInfo(culture.LCID);
                if (countries.Where(p => p.Name == country.Name).Count() == 0)
                {
                    countries.Add(country);
                }                    
            }
            return countries.OrderBy(p => p.EnglishName).ToList();
        }

        /// <summary>
        /// Gets the list of countries by selected country codes.
        /// </summary>
        /// <param name="code">List of culture codes.</param>
        /// <returns>Returns the list of countries by selected country codes.</returns>
        public static List<RegionInfo> GetCountriesByCode(List<string> codes)
        {
            var countries = new List<RegionInfo>();
            if (codes != null && codes.Count > 0)
            {
                foreach (string code in codes)
                {
                    try
                    {
                        countries.Add(new RegionInfo(code));
                    }
                    catch
                    {
                        //  Ignores the invalid culture code.
                    }
                }
            }
            return countries.OrderBy(p => p.EnglishName).ToList();
        }
    }
}
