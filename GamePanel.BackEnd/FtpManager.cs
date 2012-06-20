using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Web.Administration;
using Microsoft.Web.Management.Server;

namespace GamePanel.BackEnd
{
    public static class FtpManager
    {
        public static void CreateFtpUser(string configurationPath, string username, string password)
        {
            ManagementUserInfo userInfo = ManagementAuthentication.CreateUser(username, password);
            ManagementAuthorization.Grant(userInfo.Name, configurationPath, false);

            using (ServerManager serverManager = new ServerManager())
            {
                Configuration config = serverManager.GetApplicationHostConfiguration();
                ConfigurationSection authorizationSection = config.GetSection("system.ftpServer/security/authorization", configurationPath);

                ConfigurationElementCollection authorizationCollection = authorizationSection.GetCollection();
                ConfigurationElement addElement = authorizationCollection.CreateElement("add");
                addElement["accessType"] = @"Allow";
                addElement["users"] = username;
                addElement["permissions"] = @"Read, Write";
                authorizationCollection.Add(addElement);
                serverManager.CommitChanges();
            }
        }
    }
}
