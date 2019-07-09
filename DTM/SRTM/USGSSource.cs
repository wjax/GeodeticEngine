namespace GeodeticEngine.DTM.SRTM
{
    public class USGSSource
    {
        public const string SOURCE = @"https://dds.cr.usgs.gov/srtm/version2_1/SRTM3/";

        public static string[] CONTINENTS = new string[]
        {
            "Africa",
            "Australia",
            "Eurasia",
            "Islands",
            "North_America",
            "South_America"
        };

        public static void GetMissingCell(string path, string name)
        {
            var filename = name + ".hgt.zip";
            var local = System.IO.Path.Combine(path, filename);
            
            foreach (var continent in CONTINENTS)
            {
                Tools.Network.Downloader.AddDownload(local, SOURCE + continent + "/" + filename);
            }
        }
    }
}