using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CRUD.Models
{
    public class ToDoItem : DocumentModel
    {
        public string currencyCode { get; set; }
        public double tierMinimumUnits { get; set; }
        public double retailPrice { get; set; }
        public double unitPrice { get; set; }
        public string armRegionName { get; set; }
        public string location { get; set; }
        public DateTime effectiveStartDate { get; set; }
        public string meterId { get; set; }
        public string meterName { get; set; }
        public string productId { get; set; }
        public string skuId { get; set; }
        public object availabilityId { get; set; }
        public string productName { get; set; }
        public string skuName { get; set; }
        public string serviceName { get; set; }
        public string serviceId { get; set; }
        public string serviceFamily { get; set; }
        public string unitOfMeasure { get; set; }
        public string type { get; set; }
        public bool isPrimaryMeterRegion { get; set; }
        public string armSkuName { get; set; }
        public List<SavingsPlan> savingsPlan { get; set; }
        public DateTime? creationTime { get; set; }
        public string Country { get; set; }
        public long _ts { get; set; }
    }

    public class SavingsPlan
    {
        public double unitPrice { get; set; }
        public double retailPrice { get; set; }
        public string term { get; set; }
    }

}
