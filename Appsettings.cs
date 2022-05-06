namespace DevOpsManagement
{
    using System;
    using System.Globalization;

    public class Appsettings
    {
        private static Uri _orgaUrl;
        public string APPINSIGHTS_INSTRUMENTATIONKEY { get; set; }
        public string VSTSApiVersion { get; set; }
        public string VSTSOrganization { get; set; }
        public string PAT { get; set; }
        public string ManagementProjectName { get; set; }
        public string ManagementProjectTeam { get; set; }
        public string ManagementProjectId { get; set; }
        public Uri VSTSOrganizationUrl { 
            get {
                if (String.IsNullOrEmpty(VSTSOrganization))
                {
                    throw new ApplicationException("VSTSOrganization not initialized - missing AppSettings?");
                }
                if (_orgaUrl==null )
                {
                    _orgaUrl = new Uri($"https://dev.azure.com/{VSTSOrganization}");
                }
                return _orgaUrl;
            } 
        }
        public string ProcessTemplateId { get; set; }


        /// <summary>
        /// MSAL variables for authenication
        /// </summary>
        public string DevOpsUri = "https://app.vssps.visualstudio.com/";
        public string Instance { get; set; }
        public string Tenant { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; } // Hard coded for demo purposes but must be stored in Key Vault
        public string Authority
        {
            get
            {
                return String.Format(CultureInfo.InvariantCulture, Instance, Tenant);
            }
        }
        public string AccessToken { get; set; }
    }
}
