using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using GamePanel.Models;

namespace GamePanel.BackEnd
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "IQueueService" in both code and config file together.
    [ServiceContract]
    public interface IQueueService
    {
        [OperationContract]
        void Install(int serverId);

        [OperationContract]
        void ReInstall(int serverId);

        [OperationContract]
        void Delete(string abbr, int serverId);

        [OperationContract]
        void Start(int serverId);

        [OperationContract]
        void Stop(int serverId);

        [OperationContract]
        void Restart(int serverId);

        [OperationContract]
        void Update(int serverId);
    }

}
