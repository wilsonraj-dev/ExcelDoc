namespace ExcelDoc.Server.Options
{
    public class EncryptionOptions
    {
        public const string SectionName = "Encryption";

        public string SecretKey { get; set; } = string.Empty;

        public string InitializationVector { get; set; } = string.Empty;
    }
}
