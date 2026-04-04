namespace ExcelDoc.Server.Security
{
    public static class AuthRoles
    {
        public const string Administrador = "Administrador";
        public const string Usuario = "Usuario";
        public const string All = Administrador + "," + Usuario;
    }
}
