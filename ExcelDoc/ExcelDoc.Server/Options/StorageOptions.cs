namespace ExcelDoc.Server.Options
{
    public class StorageOptions
    {
        public const string SectionName = "Storage";

        public string UploadDirectory { get; set; } = "App_Data/Uploads";
    }
}
