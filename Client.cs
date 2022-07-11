using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace importusers
{
    class Client
    {
        public Client(string json)
        {
            JObject jObject = JObject.Parse(json);
            JToken jUser = jObject["result"][0];

            custAccountId = (string)jUser["custAccountId"];
            kontAccountId = (string)jUser["kontAccountId"];
            custType = (int)jUser["custType"];
            worksId = (string)jUser["worksId"];
            worksName = (string)jUser["worksName"];
            eicCode = (string)jUser["eicCode"];
            serialNumber =(string)jUser["serialNumber"];
            postCode = (string)jUser["postCode"];
            city = (string)jUser["city"];
            district = (string)jUser["district"];
            street = (string)jUser["street"];
            houseNumber = (string)jUser["houseNumber"];
            corpsNumber = (string)jUser["corpsNumber"];
            flatNumber = (string)jUser["flatNumber"];
            greenTariff = (bool)jUser["greenTariff"];
        }

        public string custAccountId { get; set; }
        public string kontAccountId { get; set; }
        public int custType { get; set; }
        public string worksId { get; set; }
        public string worksName { get; set; }
        public string eicCode { get; set; }
        public string serialNumber { get; set; }
        public string postCode { get; set; }
        public string city { get; set; }
        public string district { get; set; }
        public string street { get; set; }
        public string houseNumber { get; set; }
        public string corpsNumber { get; set; }
        public string flatNumber { get; set; }
        public bool greenTariff { get; set; }
    }
}
//{ 
//    "result":[
//        { 
//            "custAccountId":"04001/250321",
//            "kontAccountId":"10400107018",
//            "custType":1,
//            "worksId":"4",
//            "worksName":"Північний РЕМ",
//            "eicCode":"62Z5936623455645",
//            "serialNumber":"01243725",
//            "postCode":"65031",
//            "city":"Одеса",
//            "district":null,
//            "street":"Проценко",
//            "houseNumber":"50/1",
//            "corpsNumber":null,
//            "flatNumber":"321",
//            "greenTariff":false
//            }
//    ]
//}