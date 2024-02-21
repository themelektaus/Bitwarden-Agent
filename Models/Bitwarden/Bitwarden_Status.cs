using System;

namespace BitwardenAgent.Models;

public class Bitwarden_Status
{
    public object serverUrl;
    public DateTime? lastSync;
    public string userEmail;
    public string userId;
    public string status;
}
