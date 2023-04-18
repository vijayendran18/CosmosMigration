using Google.Protobuf.WellKnownTypes;
using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CRUD.Models
{
    public class TrackerData : DocumentModel
    {
        public string TimeString { get; set; }
    }

}
